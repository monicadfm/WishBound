using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WishBound.ClientAPI.Models.Colecao;
using WishBound.ClientAPI.Services;

namespace WishBound.ClientAPI.Controllers
{
    /// <summary>
    /// Coleção pessoal do utilizador autenticado: lista com ordenação e
    /// filtro de favoritos, detalhe de cada personagem, marcação de favoritos
    /// e libertação de cópias repetidas.
    /// Requer sessão iniciada — a coleção é sempre a do próprio.
    /// </summary>
    [Authorize]
    public class ColecaoController : Controller
    {
        private readonly WishBoundApiService _api;

        public ColecaoController(WishBoundApiService api)
        {
            _api = api;
        }

        // GET: /Colecao?ordenar=raridade&favoritos=false
        public async Task<IActionResult> Index(string ordenar = "raridade", bool favoritos = false)
        {
            // Só aceitamos as ordenações previstas (validação de dados de entrada)
            if (ordenar != "nome" && ordenar != "data")
            {
                ordenar = "raridade";
            }

            try
            {
                var modelo = await _api.ObterColecaoAsync(ObterUtilizadorId(), ordenar, favoritos);
                modelo.Ordenar = ordenar;
                modelo.ApenasFavoritos = favoritos;

                return View(modelo);
            }
            catch (Exception)
            {
                TempData["Erro"] = "Não foi possível obter a coleção. Verifique se a WishBound.WebAPI está em execução.";
                return View(new ColecaoViewModel { Ordenar = ordenar, ApenasFavoritos = favoritos });
            }
        }

        // GET: /Colecao/Detalhes/3
        public async Task<IActionResult> Detalhes(int id)
        {
            try
            {
                var item = await _api.ObterItemColecaoAsync(ObterUtilizadorId(), id);

                if (item == null)
                {
                    TempData["Erro"] = "Essa personagem ainda não faz parte da sua coleção.";
                    return RedirectToAction(nameof(Index));
                }

                return View(item);
            }
            catch (Exception)
            {
                TempData["Erro"] = "Não foi possível contactar a API. Verifique se a WishBound.WebAPI está em execução.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: /Colecao/Favorito  (marcar/desmarcar)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Favorito(int personagemId, bool favorito, string? ordenar = null, bool favoritos = false, string? voltar = null)
        {
            try
            {
                var (sucesso, mensagem) = await _api.MarcarFavoritoAsync(ObterUtilizadorId(), personagemId, favorito);

                if (sucesso)
                {
                    TempData["Sucesso"] = mensagem;
                }
                else
                {
                    TempData["Erro"] = mensagem;
                }
            }
            catch (Exception)
            {
                TempData["Erro"] = "Não foi possível contactar a API. Verifique se a WishBound.WebAPI está em execução.";
            }

            return VoltarPara(voltar, personagemId, ordenar, favoritos);
        }

        // POST: /Colecao/Libertar  (gestão de duplicados)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Libertar(int personagemId, int quantidade = 1, string? ordenar = null, bool favoritos = false, string? voltar = null)
        {
            try
            {
                var (sucesso, mensagem) = await _api.LibertarDuplicadosAsync(ObterUtilizadorId(), personagemId, quantidade);

                if (sucesso)
                {
                    TempData["Sucesso"] = mensagem;
                }
                else
                {
                    TempData["Erro"] = mensagem;
                }
            }
            catch (Exception)
            {
                TempData["Erro"] = "Não foi possível contactar a API. Verifique se a WishBound.WebAPI está em execução.";
            }

            return VoltarPara(voltar, personagemId, ordenar, favoritos);
        }

        /// <summary>
        /// Depois de marcar favorita ou libertar repetidas, volta à página de
        /// onde o botão foi carregado: o detalhe da personagem ou a lista
        /// (mantendo a ordenação e o filtro escolhidos).
        /// </summary>
        private IActionResult VoltarPara(string? voltar, int personagemId, string? ordenar, bool favoritos)
        {
            if (voltar == "detalhes")
            {
                return RedirectToAction(nameof(Detalhes), new { id = personagemId });
            }

            return RedirectToAction(nameof(Index), new { ordenar, favoritos });
        }

        /// <summary>Id do utilizador autenticado, guardado nos claims da sessão.</summary>
        private int ObterUtilizadorId()
        {
            return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
        }
    }
}
