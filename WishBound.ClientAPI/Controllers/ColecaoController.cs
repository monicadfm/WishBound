using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WishBound.ClientAPI.Models.Amizade;
using WishBound.ClientAPI.Models.Colecao;
using WishBound.ClientAPI.Services;

namespace WishBound.ClientAPI.Controllers
{
    /// <summary>
    /// Coleção pessoal do utilizador autenticado: lista com ordenação e
    /// filtro de favoritos, detalhe de cada personagem, marcação de favoritos,
    /// libertação de cópias repetidas (que dá Moedas) e compra de mais
    /// lugares para a coleção.
    ///
    /// SISTEMA DE AMIZADE: as 3 interações diárias (botão "Interagir" em
    /// cada cartão e nos detalhes), o nível de amizade com cada personagem
    /// e, na página de detalhes, os níveis com o que cada um dá e as
    /// recompensas dessa personagem (título, emblema, moldura) para equipar
    /// no perfil.
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

                // Amizade com esta personagem (bloco por baixo da descrição).
                // Se falhar (ex.: Migracao04/05 por correr), a página abre na
                // mesma sem esse bloco.
                AmizadePersonagem? amizade = null;
                try
                {
                    amizade = await _api.ObterAmizadePersonagemAsync(ObterUtilizadorId(), id);
                }
                catch (Exception)
                {
                    // sem amizade
                }

                // Mensagens da personagem (saudação + conjunto desbloqueado).
                // Também opcional: sem Migracao07 a página abre sem elas.
                MensagensPersonagem? mensagens = null;
                try
                {
                    mensagens = await _api.ObterMensagensPersonagemAsync(ObterUtilizadorId(), id);
                }
                catch (Exception)
                {
                    // sem mensagens
                }

                return View(new DetalhesColecaoViewModel { Item = item, Amizade = amizade, Mensagens = mensagens });
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

        // POST: /Colecao/LibertarTudo  (todas as repetidas de uma vez)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LibertarTudo(string? ordenar = null, bool favoritos = false)
        {
            try
            {
                var (sucesso, mensagem) = await _api.LibertarTodosOsDuplicadosAsync(ObterUtilizadorId());

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

            return RedirectToAction(nameof(Index), new { ordenar, favoritos });
        }

        // POST: /Colecao/Expandir  (comprar mais lugares)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Expandir(string? ordenar = null, bool favoritos = false)
        {
            try
            {
                var (sucesso, mensagem) = await _api.ExpandirInventarioAsync(ObterUtilizadorId());

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

            return RedirectToAction(nameof(Index), new { ordenar, favoritos });
        }

        // ------------------------------------------------------------
        // Sistema de amizade
        // ------------------------------------------------------------

        // POST: /Colecao/Interagir  (gasta 1 das 3 interações do dia)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Interagir(int personagemId, string? ordenar = null, bool favoritos = false, string? voltar = null)
        {
            try
            {
                var (resultado, erro) = await _api.InteragirAsync(ObterUtilizadorId(), personagemId);

                if (resultado != null)
                {
                    TempData["Sucesso"] = resultado.Mensagem;
                    // A "fala" da personagem aparece numa caixa própria (ver _Layout)
                    TempData["Reacao"] = resultado.Reacao;
                }
                else
                {
                    TempData["Erro"] = string.IsNullOrWhiteSpace(erro)
                        ? "Não foi possível interagir com a personagem."
                        : erro;
                }
            }
            catch (Exception)
            {
                TempData["Erro"] = "Não foi possível contactar a API. Verifique se a WishBound.WebAPI está em execução.";
            }

            return VoltarPara(voltar, personagemId, ordenar, favoritos);
        }

        // POST: /Colecao/EquiparTitulo  (tituloId vazio = remover do perfil)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EquiparTitulo(int? tituloId, string? ordenar = null, bool favoritos = false, string? voltar = null, int personagemId = 0)
        {
            try
            {
                var (sucesso, mensagem) = await _api.EquiparTituloAsync(ObterUtilizadorId(), tituloId);
                TempData[sucesso ? "Sucesso" : "Erro"] = mensagem;
            }
            catch (Exception)
            {
                TempData["Erro"] = "Não foi possível contactar a API. Verifique se a WishBound.WebAPI está em execução.";
            }

            if (voltar == "perfil")
            {
                return RedirectToAction("Perfil", "Conta");
            }

            return personagemId > 0
                ? RedirectToAction(nameof(Detalhes), new { id = personagemId })
                : RedirectToAction(nameof(Index), new { ordenar, favoritos });
        }

        // POST: /Colecao/EquiparMoldura  (molduraId vazio = remover do perfil)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EquiparMoldura(int? molduraId, string? ordenar = null, bool favoritos = false, string? voltar = null, int personagemId = 0)
        {
            try
            {
                var (sucesso, mensagem) = await _api.EquiparMolduraAsync(ObterUtilizadorId(), molduraId);
                TempData[sucesso ? "Sucesso" : "Erro"] = mensagem;
            }
            catch (Exception)
            {
                TempData["Erro"] = "Não foi possível contactar a API. Verifique se a WishBound.WebAPI está em execução.";
            }

            if (voltar == "perfil")
            {
                return RedirectToAction("Perfil", "Conta");
            }

            return personagemId > 0
                ? RedirectToAction(nameof(Detalhes), new { id = personagemId })
                : RedirectToAction(nameof(Index), new { ordenar, favoritos });
        }

        // POST: /Colecao/EquiparEmblema  (põe/tira um emblema do perfil, até 3)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EquiparEmblema(int emblemaId, bool equipar, int personagemId = 0, string? voltar = null)
        {
            try
            {
                var (sucesso, mensagem) = await _api.EquiparEmblemaAsync(ObterUtilizadorId(), emblemaId, equipar);
                TempData[sucesso ? "Sucesso" : "Erro"] = mensagem;
            }
            catch (Exception)
            {
                TempData["Erro"] = "Não foi possível contactar a API. Verifique se a WishBound.WebAPI está em execução.";
            }

            if (voltar == "perfil")
            {
                return RedirectToAction("Perfil", "Conta");
            }

            return personagemId > 0
                ? RedirectToAction(nameof(Detalhes), new { id = personagemId })
                : RedirectToAction(nameof(Index));
        }

        // POST: /Colecao/Companheira  (escolhe a personagem que recebe o
        // utilizador na página inicial; personagemId vazio = nenhuma)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Companheira(int? personagemId, string? voltar = null, int voltarId = 0)
        {
            try
            {
                var (sucesso, mensagem) = await _api.EscolherCompanheiraAsync(ObterUtilizadorId(), personagemId);
                TempData[sucesso ? "Sucesso" : "Erro"] = mensagem;
            }
            catch (Exception)
            {
                TempData["Erro"] = "Não foi possível contactar a API. Verifique se a WishBound.WebAPI está em execução.";
            }

            if (voltar == "inicio")
            {
                return RedirectToAction("Index", "Home");
            }

            int destino = personagemId ?? voltarId;
            return destino > 0
                ? RedirectToAction(nameof(Detalhes), new { id = destino })
                : RedirectToAction(nameof(Index));
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
