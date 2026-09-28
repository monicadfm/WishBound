using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Globalization;
using WishBound.WebAPI.Data;
using WishBound.WebAPI.Models;

namespace WishBound.WebAPI.Services
{
    /// <summary>Definições dos lembretes automáticos (secção "Lembretes" do appsettings.json).</summary>
    public class DefinicoesLembretes
    {
        /// <summary>Liga/desliga a verificação periódica (o botão da gestão funciona sempre).</summary>
        public bool Ativo { get; set; } = true;

        /// <summary>De quantos em quantos minutos a API verifica se há lembretes a enviar.</summary>
        public int IntervaloMinutos { get; set; } = 30;

        /// <summary>A partir desta hora (hora do servidor) enviam-se os lembretes do dia.</summary>
        public int HoraLembretes { get; set; } = 18;

        /// <summary>Só recebem lembretes as contas que entraram nos últimos N dias.</summary>
        public int DiasContaAtiva { get; set; } = 14;
    }

    /// <summary>Quantas notificações cada regra escreveu numa execução.</summary>
    public class ResultadoLembretes
    {
        public int InicioEventos { get; set; }
        public int FimEventos { get; set; }
        public int LoginDiario { get; set; }
        public int RecompensasEvento { get; set; }

        public int Total => InicioEventos + FimEventos + LoginDiario + RecompensasEvento;

        public override string ToString() =>
            Total + " notificações (início de eventos/banners " + InicioEventos + ", eventos a terminar " + FimEventos +
            ", check-in diário " + LoginDiario + ", recompensas de eventos " + RecompensasEvento + ")";
    }

    /// <summary>
    /// LEMBRETES AUTOMÁTICOS — escrevem linhas em Notificacoes sem ninguém
    /// ter de as enviar (o separador Notificações da app e o contador do
    /// Início mostram-nas; a app também as mostra como notificação do
    /// telemóvel). Quatro regras, todas com os tipos que o CHECK da tabela
    /// já aceitava:
    ///
    ///   1. 'Evento' / 'Banner' — um banner começou (nos últimos 3 dias):
    ///      "Novo evento: X" a todas as contas ativas. É o mesmo título que o
    ///      "Avisar" da gestão usa, por isso nunca sai duas vezes.
    ///   2. 'Evento' — faltam menos de 24 horas para um evento acabar:
    ///      "Últimas horas: X" às contas que entraram recentemente.
    ///   3. 'LoginDiario' — depois da HoraLembretes, a quem ainda não
    ///      recebeu o check-in diário de hoje.
    ///   4. 'Recompensa' — depois da HoraLembretes, a quem ainda não recebeu
    ///      o dia de hoje de um evento a decorrer (e ainda tem dias por receber).
    ///
    /// Cada regra é UM INSERT … SELECT com NOT EXISTS — nunca duplica: o
    /// início e o fim de um evento saem uma vez por conta; os lembretes
    /// diários uma vez por conta e por dia (UTC, como a recompensa diária).
    /// Corre sozinho de IntervaloMinutos em IntervaloMinutos
    /// (ServicoLembretesPeriodico) e também a pedido da gestão
    /// (Gestão → Notificações → "Correr lembretes agora").
    /// </summary>
    public class ServicoLembretes
    {
        private const int IdBannerPermanente = 1;
        private static readonly CultureInfo Pt = CultureInfo.GetCultureInfo("pt-PT");

        private readonly WishBoundContext _contexto;
        private readonly DefinicoesLembretes _definicoes;

        public ServicoLembretes(WishBoundContext contexto, IOptions<DefinicoesLembretes> definicoes)
        {
            _contexto = contexto;
            _definicoes = definicoes.Value;
        }

        /// <summary>
        /// Corre as quatro regras. ignorarHora = true envia já os lembretes
        /// diários mesmo antes da HoraLembretes (botão da gestão / demonstração).
        /// </summary>
        public async Task<ResultadoLembretes> ExecutarAsync(bool ignorarHora = false, CancellationToken cancelamento = default)
        {
            var resultado = new ResultadoLembretes();
            var agora = DateTime.UtcNow;
            var hoje = agora.Date;
            var ativaDesde = agora.AddDays(-Math.Max(1, _definicoes.DiasContaAtiva));
            bool horaDosLembretes = ignorarHora || DateTime.Now.Hour >= _definicoes.HoraLembretes;

            var banners = await _contexto.Banners.AsNoTracking()
                .Where(b => b.IsAtivo && b.Id != IdBannerPermanente && b.DataInicio <= agora && b.DataFim >= agora)
                .ToListAsync(cancelamento);

            // ----- 1) Banner/evento que começou há pouco -----
            foreach (var banner in banners.Where(b => b.DataInicio >= agora.AddDays(-3)))
            {
                bool evento = banner.TipoBanner == Banner.TipoEvento;
                string tipo = evento ? "Evento" : "Banner";
                string titulo = Truncar((evento ? "Novo evento: " : "Novo banner: ") + banner.Nome, 100);
                string mensagem = Truncar("Já está a decorrer, até " + Local(banner.DataFim) + ". Vai à Invocação!", 255);

                resultado.InicioEventos += await _contexto.Database.ExecuteSqlAsync(
                    $@"INSERT INTO Notificacoes (UtilizadorId, Tipo, Titulo, Mensagem, IsLida, DataCriacao)
                       SELECT u.UtilizadorId, {tipo}, {titulo}, {mensagem}, 0, {agora}
                       FROM Utilizadores u
                       WHERE u.IsAtivo = 1 AND u.NomeUtilizador <> 'Sistema'
                         AND NOT EXISTS (SELECT 1 FROM Notificacoes n
                                         WHERE n.UtilizadorId = u.UtilizadorId AND n.Tipo = {tipo} AND n.Titulo = {titulo})",
                    cancelamento);
            }

            // ----- 2) Evento a terminar (menos de 24 horas) -----
            foreach (var banner in banners.Where(b => b.TipoBanner == Banner.TipoEvento && b.DataFim <= agora.AddHours(24)))
            {
                string titulo = Truncar("Últimas horas: " + banner.Nome, 100);
                string mensagem = Truncar("O evento termina " + Local(banner.DataFim) +
                                          ". Últimas invocações com as personagens em destaque!", 255);

                resultado.FimEventos += await _contexto.Database.ExecuteSqlAsync(
                    $@"INSERT INTO Notificacoes (UtilizadorId, Tipo, Titulo, Mensagem, IsLida, DataCriacao)
                       SELECT u.UtilizadorId, 'Evento', {titulo}, {mensagem}, 0, {agora}
                       FROM Utilizadores u
                       WHERE u.IsAtivo = 1 AND u.NomeUtilizador <> 'Sistema' AND u.UltimoLogin >= {ativaDesde}
                         AND NOT EXISTS (SELECT 1 FROM Notificacoes n
                                         WHERE n.UtilizadorId = u.UtilizadorId AND n.Tipo = 'Evento' AND n.Titulo = {titulo})",
                    cancelamento);
            }

            if (!horaDosLembretes)
            {
                return resultado;
            }

            // ----- 3) Check-in diário por receber -----
            var hojeData = DateOnly.FromDateTime(agora);

            resultado.LoginDiario = await _contexto.Database.ExecuteSqlAsync(
                $@"INSERT INTO Notificacoes (UtilizadorId, Tipo, Titulo, Mensagem, IsLida, DataCriacao)
                   SELECT u.UtilizadorId, 'LoginDiario', N'Recompensa diária por receber',
                          N'O teu check-in de hoje ainda está à espera — abre a Recompensa na app ou a Carteira no site.', 0, {agora}
                   FROM Utilizadores u
                   WHERE u.IsAtivo = 1 AND u.NomeUtilizador <> 'Sistema' AND u.UltimoLogin >= {ativaDesde}
                     AND (u.UltimoLoginDiario IS NULL OR u.UltimoLoginDiario < {hojeData})
                     AND NOT EXISTS (SELECT 1 FROM Notificacoes n
                                     WHERE n.UtilizadorId = u.UtilizadorId AND n.Tipo = 'LoginDiario' AND n.DataCriacao >= {hoje})",
                cancelamento);

            // ----- 4) Dia de evento por receber -----
            var diasPorEvento = await _contexto.RecompensasEvento.AsNoTracking()
                .GroupBy(r => r.BannerId)
                .Select(g => new { BannerId = g.Key, Total = g.Count() })
                .ToDictionaryAsync(x => x.BannerId, x => x.Total, cancelamento);

            foreach (var banner in banners.Where(b => b.TipoBanner == Banner.TipoEvento && diasPorEvento.ContainsKey(b.Id)))
            {
                int totalDias = diasPorEvento[banner.Id];
                string titulo = Truncar("Recompensa do evento: " + banner.Nome, 100);
                string mensagem = Truncar("Tens o dia de hoje do evento \"" + banner.Nome + "\" por receber.", 255);

                resultado.RecompensasEvento += await _contexto.Database.ExecuteSqlAsync(
                    $@"INSERT INTO Notificacoes (UtilizadorId, Tipo, Titulo, Mensagem, IsLida, DataCriacao)
                       SELECT u.UtilizadorId, 'Recompensa', {titulo}, {mensagem}, 0, {agora}
                       FROM Utilizadores u
                       WHERE u.IsAtivo = 1 AND u.NomeUtilizador <> 'Sistema' AND u.UltimoLogin >= {ativaDesde}
                         AND NOT EXISTS (SELECT 1 FROM ParticipacaoEventos p
                                         WHERE p.UtilizadorId = u.UtilizadorId AND p.BannerId = {banner.Id}
                                           AND (CAST(p.DataParticipacao AS date) >= {hojeData} OR p.Progresso >= {totalDias}))
                         AND NOT EXISTS (SELECT 1 FROM Notificacoes n
                                         WHERE n.UtilizadorId = u.UtilizadorId AND n.Tipo = 'Recompensa'
                                           AND n.Titulo = {titulo} AND n.DataCriacao >= {hoje})",
                    cancelamento);
            }

            return resultado;
        }

        private static string Local(DateTime utc) =>
            DateTime.SpecifyKind(utc, DateTimeKind.Utc).ToLocalTime().ToString("dd/MM 'às' HH:mm", Pt);

        private static string Truncar(string texto, int max) =>
            texto.Length <= max ? texto : texto.Substring(0, max - 1) + "…";
    }

    /// <summary>
    /// Corre o ServicoLembretes de IntervaloMinutos em IntervaloMinutos
    /// enquanto a WebAPI estiver ligada (serviço de fundo do ASP.NET Core).
    /// A primeira verificação é 1 minuto depois do arranque. Um erro (ex.:
    /// a base de dados em baixo) só fica escrito na consola — a próxima
    /// volta tenta outra vez.
    /// </summary>
    public class ServicoLembretesPeriodico : BackgroundService
    {
        private readonly IServiceScopeFactory _fabricaScopes;
        private readonly DefinicoesLembretes _definicoes;
        private readonly ILogger<ServicoLembretesPeriodico> _registo;

        public ServicoLembretesPeriodico(IServiceScopeFactory fabricaScopes, IOptions<DefinicoesLembretes> definicoes,
                                         ILogger<ServicoLembretesPeriodico> registo)
        {
            _fabricaScopes = fabricaScopes;
            _definicoes = definicoes.Value;
            _registo = registo;
        }

        protected override async Task ExecuteAsync(CancellationToken paragem)
        {
            if (!_definicoes.Ativo)
            {
                return;
            }

            try
            {
                await Task.Delay(TimeSpan.FromMinutes(1), paragem);

                using var relogio = new PeriodicTimer(TimeSpan.FromMinutes(Math.Max(1, _definicoes.IntervaloMinutos)));

                do
                {
                    try
                    {
                        using var scope = _fabricaScopes.CreateScope();
                        var lembretes = scope.ServiceProvider.GetRequiredService<ServicoLembretes>();
                        var resultado = await lembretes.ExecutarAsync(ignorarHora: false, paragem);

                        if (resultado.Total > 0)
                        {
                            _registo.LogInformation("[WishBound] Lembretes: {Resultado}", resultado);
                        }
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        _registo.LogWarning("[WishBound] Lembretes não enviados: {Erro}", ex.Message);
                    }
                }
                while (await relogio.WaitForNextTickAsync(paragem));
            }
            catch (OperationCanceledException)
            {
                // A API está a desligar
            }
        }
    }
}
