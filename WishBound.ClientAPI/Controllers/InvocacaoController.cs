using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WishBound.ClientAPI.Models;
using WishBound.ClientAPI.Services;

namespace WishBound.ClientAPI.Controllers
{
    /// <summary>
    /// Página de invocação (gacha) e histórico de invocações.
    /// Requer sessão iniciada: cada invocação é registada em nome do
    /// utilizador autenticado, entra na coleção dele e o histórico mostrado
    /// é apenas o seu.
    ///
    /// O banner deixou de estar fixo: a página mostra os que estão a decorrer
    /// e as garantias (pity) são contadas por banner.
    /// </summary>
    [Authorize]
    public class InvocacaoController : Controller
    {
        private readonly WishBoundApiService _api;

        public InvocacaoController(WishBoundApiService api)
        {
            _api = api;
        }

        // Página 3: Invocação
        public async Task<IActionResult> Index(int bannerId = 0)
        {
            var modelo = new InvocacaoViewModel { BannerId = bannerId };

            try
            {
                await PreencherAsync(modelo);
            }
            catch (Exception)
            {
                TempData["Erro"] = "Não foi possível contactar a API. Verifique se a WishBound.WebAPI está em execução.";
            }

            return View(modelo);
        }

        // Botão "Invocar" (POST para evitar invocações acidentais por refresh/link)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Invocar(int bannerId = 0, int quantidade = 1)
        {
            // Só existem invocações simples ou de 10
            quantidade = quantidade == 10 ? 10 : 1;

            var modelo = new InvocacaoViewModel { BannerId = bannerId };

            try
            {
                var (resultado, erro) = await _api.InvocarAsync(ObterUtilizadorId(), bannerId, quantidade);
                modelo.Resultado = resultado;

                if (resultado == null)
                {
                    // A API explica o motivo (ex.: sem espaço na coleção); só
                    // usamos a mensagem genérica quando não vem nenhuma.
                    TempData["Erro"] = string.IsNullOrWhiteSpace(erro)
                        ? "A invocação falhou. Confirme que existem personagens na base de dados."
                        : erro;
                }
                else
                {
                    modelo.BannerId = resultado.BannerId;
                }

                await PreencherAsync(modelo);
            }
            catch (Exception)
            {
                TempData["Erro"] = "Não foi possível realizar a invocação. Verifique se a WishBound.WebAPI está em execução.";
            }

            return View("Index", modelo);
        }

        // Página 4: Histórico (apenas as invocações do utilizador autenticado)
        public async Task<IActionResult> Historico()
        {
            try
            {
                var historico = await _api.ObterHistoricoAsync(ObterUtilizadorId());
                return View(historico);
            }
            catch (Exception)
            {
                TempData["Erro"] = "Não foi possível obter o histórico. Verifique se a WishBound.WebAPI está em execução.";
                return View(new List<Invocacao>());
            }
        }

        /// <summary>
        /// Carrega o que a página precisa: banners a decorrer, probabilidades
        /// e o estado das garantias em cada banner.
        /// </summary>
        private async Task PreencherAsync(InvocacaoViewModel modelo)
        {
            modelo.Banners = await _api.ObterBannersAsync(ObterUtilizadorId());
            modelo.Raridades = await _api.ObterRaridadesAsync();

            // Sem escolha (ou com uma escolha que já não existe), fica o primeiro
            if (modelo.Banners.Count > 0 && !modelo.Banners.Any(b => b.Id == modelo.BannerId))
            {
                modelo.BannerId = modelo.Banners[0].Id;
            }

            // Garantias em todos os banners: a página tem um painel por banner
            // e troca entre eles no browser, sem voltar ao servidor.
            foreach (var banner in modelo.Banners)
            {
                var estado = await _api.ObterEstadoPityAsync(ObterUtilizadorId(), banner.Id);
                if (estado != null)
                {
                    modelo.Estados[banner.Id] = estado;
                }

                // Pool completa do banner: alimenta a janela de probabilidades
                // (o "i" no canto do painel) com todas as personagens e o %
                // de cada uma.
                var detalhe = await _api.ObterBannerAsync(banner.Id, ObterUtilizadorId());
                if (detalhe != null)
                {
                    modelo.Detalhes[banner.Id] = detalhe;
                }
            }
        }

        /// <summary>Id do utilizador autenticado, guardado nos claims da sessão.</summary>
        private int ObterUtilizadorId()
        {
            return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
        }
    }
}
