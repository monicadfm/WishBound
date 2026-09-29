using WishBound.Mobile.Models;

namespace WishBound.Mobile.Services
{
    /// <summary>
    /// NOTIFICAÇÕES NO TELEMÓVEL (lembretes locais).
    ///
    /// O Termo de Abertura pede notificações push, lembretes de login diário,
    /// avisos de eventos, de recompensas e das mensagens das personagens.
    /// Em vez de um serviço de push (Firebase), a app usa as notificações
    /// LOCAIS do Android — funcionam no emulador sem contas nem chaves:
    ///
    ///   1. Lembrete do check-in diário — alarme diário às 20h. Quando a app
    ///      sabe que a recompensa de hoje já foi recebida, o próximo passa
    ///      para amanhã.
    ///   2. Evento a terminar — um aviso 24 horas antes do fim de cada evento
    ///      a decorrer (agendado quando o separador Recompensa carrega).
    ///   3. Notificações do servidor — as que a API escreve (mensagens das
    ///      personagens, lembretes automáticos, avisos da equipa) aparecem
    ///      como notificação do telemóvel quando a app abre, volta ao
    ///      primeiro plano e, com a app aberta, de minuto a minuto.
    ///
    /// Limitação (para o relatório): com a app fechada só saem os alarmes já
    /// agendados (1 e 2); as notificações novas do servidor aparecem quando
    /// a app volta a abrir. Um push "a sério" precisaria do Firebase Cloud
    /// Messaging (conta Google + google-services.json).
    ///
    /// O trabalho específico do Android está em
    /// Platforms/Android/NotificacoesAndroid.cs.
    /// </summary>
    public class ServicoLembretesTelemovel
    {
        private const int IdLembreteDiario = 100;
        private const int BaseIdEvento = 1000;
        private const int BaseIdServidor = 5000;
        private const int HoraLembreteDiario = 20;
        private const int MaximoMostradasDeUmaVez = 3;

        private readonly ServicoApi _api;
        private readonly ServicoSessao _sessao;
        private bool _permissaoPedida;
        private bool _aVerificar;

        public ServicoLembretesTelemovel(ServicoApi api, ServicoSessao sessao)
        {
            _api = api;
            _sessao = sessao;
        }

        /// <summary>Android 13+ pede autorização para mostrar notificações (uma vez por arranque).</summary>
        public async Task PedirPermissaoAsync()
        {
            if (_permissaoPedida)
            {
                return;
            }

            _permissaoPedida = true;

            try
            {
                var estado = await Permissions.CheckStatusAsync<Permissions.PostNotifications>();
                if (estado != PermissionStatus.Granted)
                {
                    await Permissions.RequestAsync<Permissions.PostNotifications>();
                }
            }
            catch (Exception)
            {
                // Sem permissão a app funciona na mesma, só não mostra notificações
            }
        }

        /// <summary>
        /// Agenda o lembrete diário das 20h: hoje, se a recompensa ainda não
        /// foi recebida e ainda não são 20h; senão, a partir de amanhã.
        /// </summary>
        public void AtualizarLembreteDiario(bool recebidaHoje)
        {
            var agora = DateTime.Now;
            var hoje = agora.Date.AddHours(HoraLembreteDiario);
            var primeiro = !recebidaHoje && agora < hoje ? hoje : hoje.AddDays(1);

            Agendar(IdLembreteDiario,
                "Recompensa diária por receber",
                "O teu check-in de hoje ainda está à espera. Abre o WishBound e recebe-o no separador Recompensa.",
                primeiro, diario: true);
        }

        /// <summary>Um aviso 24 horas antes do fim de cada evento a decorrer.</summary>
        public void AgendarFimDosEventos(IEnumerable<EventoResposta> eventos)
        {
            var agora = DateTime.Now;

            foreach (var evento in eventos)
            {
                var fim = DateTime.SpecifyKind(evento.DataFim, DateTimeKind.Utc).ToLocalTime();
                var aviso = fim.AddHours(-24);

                if (aviso > agora)
                {
                    Agendar(BaseIdEvento + evento.BannerId,
                        "Últimas horas: " + evento.Nome,
                        "O evento termina a " + fim.ToString("dd/MM 'às' HH:mm") + ". Aproveita as últimas invocações!",
                        aviso, diario: false);
                }
            }
        }

        /// <summary>
        /// Vai buscar o estado da recompensa diária e os eventos a decorrer
        /// (api/economia) e agenda os lembretes 1 e 2. Chamado ao abrir a
        /// página inicial, para os lembretes existirem mesmo que a pessoa
        /// nunca abra o separador Recompensa.
        /// </summary>
        public async Task AtualizarAgendamentosAsync()
        {
            var utilizador = _sessao.Atual;
            if (utilizador == null)
            {
                return;
            }

            try
            {
                var resultado = await _api.ObterEconomiaAsync(utilizador.Id);
                if (resultado.Sucesso && resultado.Dados != null)
                {
                    AtualizarLembreteDiario(resultado.Dados.RecompensaDiaria.RecebidaHoje);
                    AgendarFimDosEventos(resultado.Dados.Eventos);
                }
            }
            catch (Exception)
            {
                // Sem rede: fica o que já estava agendado
            }
        }

        /// <summary>
        /// Mostra no telemóvel as notificações por ler que chegaram desde a
        /// última verificação (guarda o maior Id já mostrado, por conta). Na
        /// primeira vez só mostra as das últimas 24 horas, para não despejar
        /// o histórico todo de uma vez.
        /// </summary>
        public async Task MostrarNovasNotificacoesAsync()
        {
            var utilizador = _sessao.Atual;
            if (utilizador == null || _aVerificar)
            {
                return;
            }

            _aVerificar = true;

            try
            {
                var resultado = await _api.ObterNotificacoesAsync(utilizador.Id, apenasNaoLidas: true);
                if (!resultado.Sucesso || resultado.Dados == null)
                {
                    return;
                }

                string chave = "wb_ultima_notificacao_" + utilizador.Id;
                int ultima = Preferences.Get(chave, -1);
                var itens = resultado.Dados.Itens;

                var novas = ultima < 0
                    ? itens.Where(n => n.DataCriacao >= DateTime.UtcNow.AddHours(-24)).ToList()
                    : itens.Where(n => n.Id > ultima).ToList();

                novas = novas.OrderBy(n => n.Id).ToList();

                foreach (var n in novas.TakeLast(MaximoMostradasDeUmaVez))
                {
                    Mostrar(BaseIdServidor + n.Id % 100000, n.Titulo, n.Mensagem);
                }

                if (novas.Count > MaximoMostradasDeUmaVez)
                {
                    Mostrar(BaseIdServidor - 1, "WishBound",
                        "Tens " + novas.Count + " notificações novas — vê-as no separador Notificações.");
                }

                int maior = itens.Count == 0 ? Math.Max(ultima, 0) : Math.Max(ultima, itens.Max(n => n.Id));
                Preferences.Set(chave, maior);
            }
            catch (Exception)
            {
                // Sem rede / API desligada: tenta-se na próxima verificação
            }
            finally
            {
                _aVerificar = false;
            }
        }

        /// <summary>Ao terminar a sessão: nada de lembretes para uma conta que saiu.</summary>
        public void CancelarTudo()
        {
            Cancelar(IdLembreteDiario);
            for (int bannerId = 1; bannerId <= 200; bannerId++)
            {
                Cancelar(BaseIdEvento + bannerId);
            }
        }

        // ------------------------------------------------------------
        //  Ponte para o código do Android
        // ------------------------------------------------------------

        private static void Mostrar(int id, string titulo, string texto)
        {
#if ANDROID
            Plataforma.NotificacoesAndroid.Mostrar(id, titulo, texto);
#endif
        }

        private static void Agendar(int id, string titulo, string texto, DateTime quandoLocal, bool diario)
        {
#if ANDROID
            Plataforma.NotificacoesAndroid.Agendar(id, titulo, texto, quandoLocal, diario);
#endif
        }

        private static void Cancelar(int id)
        {
#if ANDROID
            Plataforma.NotificacoesAndroid.Cancelar(id);
#endif
        }
    }
}
