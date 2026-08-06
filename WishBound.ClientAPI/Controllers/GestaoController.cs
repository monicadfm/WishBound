using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using WishBound.ClientAPI.Models;
using WishBound.ClientAPI.Services;

namespace WishBound.ClientAPI.Controllers
{
    /// <summary>
    /// Área de gestão (administração) das personagens.
    /// Aqui demonstra-se o CRUD completo através da WebAPI:
    ///   Index  -> SELECT
    ///   Criar  -> INSERT
    ///   Editar -> UPDATE
    ///   Apagar -> DELETE
    /// Usa o segundo layout (_LayoutGestao, com barra lateral).
    ///
    /// [Authorize(Roles = "Admin")]: TODA a área de gestão exige sessão
    /// iniciada com uma conta de administrador. Quem não tiver sessão é
    /// enviado para o login; quem tiver sessão mas não for admin vê a
    /// página "Acesso negado".
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class GestaoController : Controller
    {
        private readonly WishBoundApiService _api;

        public GestaoController(WishBoundApiService api)
        {
            _api = api;
        }

        // Preenche a dropdown de raridades usada nos formulários
        private async Task CarregarRaridadesAsync(int? selecionada = null)
        {
            var raridades = await _api.ObterRaridadesAsync();
            ViewBag.Raridades = new SelectList(raridades, "Id", "Nome", selecionada);
        }

        // GET: /Gestao  (SELECT)
        public async Task<IActionResult> Index()
        {
            try
            {
                var personagens = await _api.ObterPersonagensAsync();

                // Agrupa por raridade e, dentro de cada raridade, ordena por Id
                personagens = personagens
                    .OrderBy(p => p.RaridadeId)
                    .ThenBy(p => p.Id)
                    .ToList();

                return View(personagens);
            }
            catch (Exception)
            {
                TempData["Erro"] = "Não foi possível obter as personagens. Verifique se a WishBound.WebAPI está em execução.";
                return View(new List<Personagem>());
            }
        }

        // GET: /Gestao/Criar
        public async Task<IActionResult> Criar()
        {
            try
            {
                await CarregarRaridadesAsync();
                return View(new Personagem());
            }
            catch (Exception)
            {
                TempData["Erro"] = "Não foi possível contactar a API. Verifique se a WishBound.WebAPI está em execução.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: /Gestao/Criar  (INSERT)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Criar(Personagem personagem)
        {
            try
            {
                // Validação de dados de entrada (server-side)
                if (!ModelState.IsValid)
                {
                    await CarregarRaridadesAsync(personagem.RaridadeId);
                    return View(personagem);
                }

                var resultado = await _api.CriarPersonagemAsync(personagem);

                if (!resultado.Sucesso)
                {
                    ModelState.AddModelError(string.Empty, "A API recusou a operação: " + resultado.Erro);
                    await CarregarRaridadesAsync(personagem.RaridadeId);
                    return View(personagem);
                }

                TempData["Sucesso"] = "Personagem \"" + personagem.Nome + "\" criada com sucesso!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {
                TempData["Erro"] = "Não foi possível criar a personagem. Verifique se a WishBound.WebAPI está em execução.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: /Gestao/Editar/5
        public async Task<IActionResult> Editar(int id)
        {
            try
            {
                var personagem = await _api.ObterPersonagemAsync(id);

                if (personagem == null)
                {
                    TempData["Erro"] = "A personagem que tentou editar não existe.";
                    return RedirectToAction(nameof(Index));
                }

                await CarregarRaridadesAsync(personagem.RaridadeId);
                return View(personagem);
            }
            catch (Exception)
            {
                TempData["Erro"] = "Não foi possível obter a personagem. Verifique se a WishBound.WebAPI está em execução.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: /Gestao/Editar/5  (UPDATE)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, Personagem personagem)
        {
            try
            {
                if (id != personagem.Id)
                {
                    TempData["Erro"] = "Pedido inválido: os identificadores não coincidem.";
                    return RedirectToAction(nameof(Index));
                }

                if (!ModelState.IsValid)
                {
                    await CarregarRaridadesAsync(personagem.RaridadeId);
                    return View(personagem);
                }

                var resultado = await _api.AtualizarPersonagemAsync(personagem);

                if (!resultado.Sucesso)
                {
                    ModelState.AddModelError(string.Empty, "A API recusou a operação: " + resultado.Erro);
                    await CarregarRaridadesAsync(personagem.RaridadeId);
                    return View(personagem);
                }

                TempData["Sucesso"] = "Personagem \"" + personagem.Nome + "\" atualizada com sucesso!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {
                TempData["Erro"] = "Não foi possível atualizar a personagem. Verifique se a WishBound.WebAPI está em execução.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: /Gestao/Apagar/5  (página de confirmação)
        public async Task<IActionResult> Apagar(int id)
        {
            try
            {
                var personagem = await _api.ObterPersonagemAsync(id);

                if (personagem == null)
                {
                    TempData["Erro"] = "A personagem que tentou apagar não existe.";
                    return RedirectToAction(nameof(Index));
                }

                return View(personagem);
            }
            catch (Exception)
            {
                TempData["Erro"] = "Não foi possível obter a personagem. Verifique se a WishBound.WebAPI está em execução.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: /Gestao/Apagar/5  (DELETE)
        [HttpPost, ActionName("Apagar")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApagarConfirmado(int id)
        {
            try
            {
                var resultado = await _api.ApagarPersonagemAsync(id);

                if (!resultado.Sucesso)
                {
                    TempData["Erro"] = "A API recusou a operação: " + resultado.Erro;
                }
                else
                {
                    TempData["Sucesso"] = "Personagem apagada com sucesso.";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {
                TempData["Erro"] = "Não foi possível apagar a personagem. Verifique se a WishBound.WebAPI está em execução.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
