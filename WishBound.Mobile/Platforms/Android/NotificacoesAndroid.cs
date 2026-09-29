using Android.App;
using Android.Content;

// NOTA: o namespace NÃO pode terminar em ".Android" — senão "Android.App"
// passava a apontar para WishBound.Mobile.Platforms.Android.App.
namespace WishBound.Mobile.Plataforma
{
    /// <summary>
    /// Notificações locais do Android, sem bibliotecas externas:
    ///   - Mostrar: mostra já uma notificação (canal "Lembretes WishBound");
    ///   - Agendar: um alarme (AlarmManager) que à hora marcada acorda o
    ///     LembreteReceiver, que mostra a notificação. Os alarmes são
    ///     "inexatos" (o Android pode atrasá-los uns minutos para poupar
    ///     bateria) e por isso não precisam da permissão de alarmes exatos;
    ///   - Cancelar: desliga o alarme e tira a notificação.
    /// Tocar numa notificação abre a aplicação.
    /// Usado pelo ServicoLembretesTelemovel (código comum).
    /// </summary>
    public static class NotificacoesAndroid
    {
        private const string IdCanal = "wishbound_lembretes";

        internal const string ExtraId = "wb_id";
        internal const string ExtraTitulo = "wb_titulo";
        internal const string ExtraTexto = "wb_texto";

        private static Context Contexto => global::Android.App.Application.Context;

        public static void Mostrar(int id, string titulo, string texto)
        {
            var gestor = (NotificationManager?)Contexto.GetSystemService(Context.NotificationService);
            if (gestor == null)
            {
                return;
            }

            GarantirCanal(gestor);

            Notification.Builder construtor = OperatingSystem.IsAndroidVersionAtLeast(26)
                ? new Notification.Builder(Contexto, IdCanal)
                : new Notification.Builder(Contexto);

            construtor
                .SetContentTitle(titulo)
                .SetContentText(texto)
                .SetStyle(new Notification.BigTextStyle().BigText(texto))
                .SetSmallIcon(Contexto.ApplicationInfo?.Icon ?? global::Android.Resource.Drawable.IcDialogInfo)
                .SetAutoCancel(true);

            // Tocar na notificação abre a aplicação
            var abrir = Contexto.PackageManager?.GetLaunchIntentForPackage(Contexto.PackageName ?? string.Empty);
            if (abrir != null)
            {
                abrir.SetFlags(ActivityFlags.NewTask | ActivityFlags.ClearTop);
                construtor.SetContentIntent(PendingIntent.GetActivity(Contexto, id, abrir, Flags()));
            }

            gestor.Notify(id, construtor.Build());
        }

        public static void Agendar(int id, string titulo, string texto, DateTime quandoLocal, bool diario)
        {
            var alarmes = (AlarmManager?)Contexto.GetSystemService(Context.AlarmService);
            if (alarmes == null)
            {
                return;
            }

            var intencao = PendingIntentDoAlarme(id, titulo, texto);
            long quando = new DateTimeOffset(DateTime.SpecifyKind(quandoLocal, DateTimeKind.Local)).ToUnixTimeMilliseconds();

            // Substitui um alarme anterior com o mesmo Id
            alarmes.Cancel(intencao);

            if (diario)
            {
                alarmes.SetInexactRepeating(AlarmType.RtcWakeup, quando, AlarmManager.IntervalDay, intencao);
            }
            else if (OperatingSystem.IsAndroidVersionAtLeast(23))
            {
                alarmes.SetAndAllowWhileIdle(AlarmType.RtcWakeup, quando, intencao);
            }
            else
            {
                alarmes.Set(AlarmType.RtcWakeup, quando, intencao);
            }
        }

        public static void Cancelar(int id)
        {
            var alarmes = (AlarmManager?)Contexto.GetSystemService(Context.AlarmService);
            alarmes?.Cancel(PendingIntentDoAlarme(id, string.Empty, string.Empty));

            var gestor = (NotificationManager?)Contexto.GetSystemService(Context.NotificationService);
            gestor?.Cancel(id);
        }

        // ------------------------------------------------------------

        private static PendingIntent PendingIntentDoAlarme(int id, string titulo, string texto)
        {
            var intencao = new Intent(Contexto, typeof(LembreteReceiver));
            intencao.PutExtra(ExtraId, id);
            intencao.PutExtra(ExtraTitulo, titulo);
            intencao.PutExtra(ExtraTexto, texto);

            return PendingIntent.GetBroadcast(Contexto, id, intencao, Flags())!;
        }

        /// <summary>Android 12+ obriga a dizer se o PendingIntent é imutável.</summary>
        private static PendingIntentFlags Flags() =>
            OperatingSystem.IsAndroidVersionAtLeast(23)
                ? PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable
                : PendingIntentFlags.UpdateCurrent;

        /// <summary>Android 8+ só mostra notificações de um canal registado.</summary>
        private static void GarantirCanal(NotificationManager gestor)
        {
            if (!OperatingSystem.IsAndroidVersionAtLeast(26) || gestor.GetNotificationChannel(IdCanal) != null)
            {
                return;
            }

            var canal = new NotificationChannel(IdCanal, "Lembretes WishBound", NotificationImportance.Default)
            {
                Description = "Check-in diário, eventos, recompensas e mensagens das personagens"
            };

            gestor.CreateNotificationChannel(canal);
        }
    }

    /// <summary>
    /// Recebe os alarmes agendados e mostra a notificação correspondente.
    /// O atributo [BroadcastReceiver] faz o registo no AndroidManifest
    /// durante a compilação.
    /// </summary>
    [BroadcastReceiver(Enabled = true, Exported = false)]
    public class LembreteReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context? context, Intent? intent)
        {
            if (intent == null)
            {
                return;
            }

            NotificacoesAndroid.Mostrar(
                intent.GetIntExtra(NotificacoesAndroid.ExtraId, 1),
                intent.GetStringExtra(NotificacoesAndroid.ExtraTitulo) ?? "WishBound",
                intent.GetStringExtra(NotificacoesAndroid.ExtraTexto) ?? string.Empty);
        }
    }
}
