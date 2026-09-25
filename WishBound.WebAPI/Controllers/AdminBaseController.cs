using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WishBound.WebAPI.Data;
using WishBound.WebAPI.Models;

namespace WishBound.WebAPI.Controllers
{
    /// <summary>
    /// Base comum dos controllers de administração acrescentados na fase de
    /// GESTÃO & ESTATÍSTICAS (raridades, banners/eventos, moedas,
    /// notificações, painel, estatísticas e exportações).
    ///
    /// Dá a cada um as mesmas três peças que o AdminController já usava:
    ///   - ObterAdminAsync: confirma que o Id enviado é de uma conta ativa
    ///     com IsAdmin (a fronteira de confiança é a mesma do resto da API —
    ///     chave partilhada + Id enviado pelo site);
    ///   - RegistarAcao: acrescenta uma linha a LogsAdministrador (é gravada
    ///     no mesmo SaveChanges / transação da ação);
    ///   - Truncar / ComMotivo: textos dentro dos limites das colunas.
    /// </summary>
    public abstract class AdminBaseController : ControllerBase
    {
        protected readonly WishBoundContext _contexto;

        protected AdminBaseController(WishBoundContext contexto)
        {
            _contexto = contexto;
        }

        /// <summary>Conta de administrador ativa com este Id, ou null.</summary>
        protected async Task<Utilizador?> ObterAdminAsync(int adminId)
        {
            if (adminId <= 0)
            {
                return null;
            }

            return await _contexto.Utilizadores.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == adminId && u.IsAdmin && u.IsAtivo);
        }

        /// <summary>Resposta 403 comum.</summary>
        protected ObjectResult ApenasAdmins(string oQue) =>
            StatusCode(403, "Apenas administradores podem " + oQue + ".");

        /// <summary>Acrescenta uma linha ao registo de ações (fica pendente até ao SaveChanges).</summary>
        protected void RegistarAcao(int adminId, int? alvoId, string acao, string? tabela, int? registoId, string? detalhes)
        {
            _contexto.LogsAdministrador.Add(new LogAdministrador
            {
                AdminId = adminId,
                UtilizadorAlvoId = alvoId,
                Acao = Truncar(acao, 100),
                TabelaAlvo = tabela == null ? null : Truncar(tabela, 50),
                RegistoAlvoId = registoId,
                Detalhes = detalhes == null ? null : Truncar(detalhes, 500),
                DataCriacao = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Últimas linhas do registo de ações, com os nomes das contas.
        /// categoria filtra pelo prefixo da ação ("Banner", "Notificacao", ...).
        /// </summary>
        protected async Task<List<AdminAcaoResposta>> ObterAcoesRecentesAsync(int limite, string? categoria = null)
        {
            IQueryable<LogAdministrador> consulta = _contexto.LogsAdministrador.AsNoTracking();

            if (!string.IsNullOrEmpty(categoria))
            {
                string prefixo = categoria + ":";
                consulta = consulta.Where(l => l.Acao.StartsWith(prefixo));
            }

            var logs = await consulta
                .OrderByDescending(l => l.DataCriacao).ThenByDescending(l => l.Id)
                .Take(limite)
                .ToListAsync();

            var ids = logs.Select(l => l.AdminId)
                .Concat(logs.Where(l => l.UtilizadorAlvoId.HasValue).Select(l => l.UtilizadorAlvoId!.Value))
                .Distinct()
                .ToList();

            var nomes = await _contexto.Utilizadores.AsNoTracking()
                .Where(u => ids.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.NomeUtilizador);

            return logs.Select(l => new AdminAcaoResposta
            {
                Id = l.Id,
                AdminId = l.AdminId,
                AdminNome = nomes.TryGetValue(l.AdminId, out var a) ? a : "#" + l.AdminId,
                UtilizadorAlvoId = l.UtilizadorAlvoId,
                UtilizadorAlvoNome = l.UtilizadorAlvoId.HasValue
                    ? (nomes.TryGetValue(l.UtilizadorAlvoId.Value, out var n) ? n : "#" + l.UtilizadorAlvoId)
                    : null,
                Acao = l.Acao,
                TabelaAlvo = l.TabelaAlvo,
                RegistoAlvoId = l.RegistoAlvoId,
                Detalhes = l.Detalhes,
                Data = l.DataCriacao
            }).ToList();
        }

        /// <summary>
        /// Escreve a mesma notificação para todas as contas ativas (menos a
        /// conta técnica "Sistema") com um único INSERT ... SELECT. Devolve
        /// quantas linhas foram criadas. Os tipos aceites são os do CHECK da
        /// tabela Notificacoes.
        /// </summary>
        protected async Task<int> NotificarTodosAsync(string tipo, string titulo, string mensagem)
        {
            titulo = Truncar(titulo.Trim(), 100);
            mensagem = Truncar(mensagem.Trim(), 255);
            var agora = DateTime.UtcNow;

            return await _contexto.Database.ExecuteSqlAsync(
                $@"INSERT INTO Notificacoes (UtilizadorId, Tipo, Titulo, Mensagem, IsLida, DataCriacao)
                   SELECT UtilizadorId, {tipo}, {titulo}, {mensagem}, 0, {agora}
                   FROM Utilizadores
                   WHERE IsAtivo = 1 AND NomeUtilizador <> 'Sistema'");
        }

        /// <summary>Invocações por dia nos últimos N dias (UTC), com os dias sem invocações a 0.</summary>
        protected async Task<List<AdminSerieDia>> InvocacoesPorDiaAsync(int dias)
        {
            var desde = DateTime.UtcNow.Date.AddDays(-(dias - 1));

            var porDia = await _contexto.Invocacoes.AsNoTracking()
                .Where(i => i.Data >= desde)
                .GroupBy(i => i.Data.Date)
                .Select(g => new { Dia = g.Key, Total = g.Count() })
                .ToListAsync();

            return PreencherDias(desde, dias, porDia.ToDictionary(x => x.Dia, x => x.Total));
        }

        /// <summary>Série diária completa (dias sem registos a 0), do mais antigo para hoje.</summary>
        protected static List<AdminSerieDia> PreencherDias(DateTime desde, int dias, Dictionary<DateTime, int> valores) =>
            Enumerable.Range(0, dias)
                .Select(i => desde.AddDays(i))
                .Select(d => new AdminSerieDia { Data = d, Total = valores.GetValueOrDefault(d, 0) })
                .ToList();

        /// <summary>Estado de um banner para as listas: A decorrer / Agendado / Terminado / Inativo.</summary>
        protected static string EstadoBanner(Banner banner, DateTime agora)
        {
            if (!banner.IsAtivo) return "Inativo";
            if (banner.DataInicio > agora) return "Agendado";
            if (banner.DataFim < agora) return "Terminado";
            return "A decorrer";
        }

        protected static string ComMotivo(string detalhes, string? motivo) =>
            string.IsNullOrWhiteSpace(motivo) ? detalhes : detalhes + " Motivo: " + motivo.Trim();

        protected static string Truncar(string texto, int max) =>
            texto.Length <= max ? texto : texto.Substring(0, max - 1) + "…";
    }
}
