using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;
using WishBound.ClientAPI.Models;
using WishBound.ClientAPI.Models.Amizade;
using WishBound.ClientAPI.Models.Gestao;
using WishBound.ClientAPI.Services;

namespace WishBound.ClientAPI.Controllers
{
    /// <summary>
    /// Área de gestão (administração).
    ///
    /// PERSONAGENS — o CRUD completo através da WebAPI:
    ///   Index  -> SELECT
    ///   Criar  -> INSERT
    ///   Editar -> UPDATE
    ///   Apagar -> DELETE
    ///
    /// CONTAS (api/admin) — ver e gerir as contas dos utilizadores:
    ///   Utilizadores -> lista com pesquisa, filtro e paginação
    ///   Utilizador   -> detalhe de uma conta + formulários das ações
    ///   AlterarEstado / ReporPassword / AjustarMoeda / AjustarPersonagem /
    ///   DefinirInventario / DefinirAmizade / AjustarRecompensa -> ações
    ///   (POST) que voltam ao detalhe da conta com a mensagem da API
    ///   Acoes        -> registo de tudo o que os administradores fizeram
    ///
    /// MENSAGENS DE PERSONAGEM (api/admin/mensagens) — os conjuntos de
    /// saudações, reações e mensagens diárias de cada personagem:
    ///   Mensagens       -> resumo por personagem + lista (SELECT)
    ///   CriarMensagem   -> INSERT
    ///   EditarMensagem  -> UPDATE (GET mostra o formulário, POST grava)
    ///   ApagarMensagem  -> DELETE
    /// Todas as ações enviam o Id do administrador com sessão iniciada
    /// (claim NameIdentifier) e um motivo opcional que fica no registo.
    ///
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
    

        // ============================================================
        //  CONTAS DOS UTILIZADORES
        // ============================================================

        /// <summary>Id do administrador com sessão iniciada (claim NameIdentifier).</summary>
        private int ObterAdminId()
        {
            return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
        }

        // GET: /Gestao/Utilizadores?pesquisa=ana&filtro=ativos&pagina=2
        public async Task<IActionResult> Utilizadores(string? pesquisa, string filtro = "todos", int pagina = 1)
        {
            try
            {
                var modelo = await _api.AdminObterContasAsync(ObterAdminId(), pesquisa, filtro, Math.Max(1, pagina));
                modelo.Pesquisa = pesquisa;
                modelo.Filtro = filtro;
                return View(modelo);
            }
            catch (Exception)
            {
                TempData["Erro"] = "Não foi possível obter as contas. Verifique se a WishBound.WebAPI está em execução.";
                return View(new ListaContasViewModel { Pesquisa = pesquisa, Filtro = filtro });
            }
        }

        // GET: /Gestao/Utilizador/5
        public async Task<IActionResult> Utilizador(int id)
        {
            try
            {
                var modelo = await _api.AdminObterContaAsync(ObterAdminId(), id);

                if (modelo == null)
                {
                    TempData["Erro"] = "A conta que tentou abrir não existe.";
                    return RedirectToAction(nameof(Utilizadores));
                }

                modelo.EhAPropriaConta = modelo.Conta.Id == ObterAdminId();
                return View(modelo);
            }
            catch (Exception)
            {
                TempData["Erro"] = "Não foi possível obter a conta. Verifique se a WishBound.WebAPI está em execução.";
                return RedirectToAction(nameof(Utilizadores));
            }
        }

        // POST: /Gestao/AlterarEstado/5  (acao: ativar | desativar | promover | despromover | validar-email)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AlterarEstado(int id, string acao, string? motivo)
        {
            bool? isAtivo = null, isAdmin = null, emailValidado = null;

            switch (acao)
            {
                case "ativar": isAtivo = true; break;
                case "desativar": isAtivo = false; break;
                case "promover": isAdmin = true; break;
                case "despromover": isAdmin = false; break;
                case "validar-email": emailValidado = true; break;
                default:
                    TempData["Erro"] = "Ação desconhecida.";
                    return RedirectToAction(nameof(Utilizador), new { id });
            }

            return await ExecutarAcaoAsync(id, () => _api.AdminAlterarEstadoAsync(ObterAdminId(), id, isAtivo, isAdmin, emailValidado, motivo));
        }

        // POST: /Gestao/ReporPassword/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReporPassword(int id, ReporPasswordAdminViewModel modelo)
        {
            if (!ModelState.IsValid)
            {
                TempData["Erro"] = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return RedirectToAction(nameof(Utilizador), new { id });
            }

            return await ExecutarAcaoAsync(id, () => _api.AdminReporPasswordAsync(ObterAdminId(), id, modelo.NovaPassword, modelo.Motivo));
        }

        // POST: /Gestao/AjustarMoeda/5  (operacao: dar | tirar)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AjustarMoeda(int id, int tipoMoedaId, decimal quantidade, string operacao, string? motivo)
        {
            if (quantidade <= 0)
            {
                TempData["Erro"] = "Indique uma quantidade maior do que zero.";
                return RedirectToAction(nameof(Utilizador), new { id });
            }

            decimal assinada = operacao == "tirar" ? -quantidade : quantidade;
            return await ExecutarAcaoAsync(id, () => _api.AdminAjustarMoedaAsync(ObterAdminId(), id, tipoMoedaId, assinada, motivo));
        }

        // POST: /Gestao/AjustarPersonagem/5  (operacao: dar | tirar)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AjustarPersonagem(int id, int personagemId, int quantidade, string operacao, string? motivo)
        {
            if (quantidade <= 0)
            {
                TempData["Erro"] = "Indique uma quantidade maior do que zero.";
                return RedirectToAction(nameof(Utilizador), new { id });
            }

            int assinada = operacao == "tirar" ? -quantidade : quantidade;
            return await ExecutarAcaoAsync(id, () => _api.AdminAjustarPersonagemAsync(ObterAdminId(), id, personagemId, assinada, motivo));
        }

        // POST: /Gestao/DefinirInventario/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DefinirInventario(int id, int capacidadeExtra, string? motivo)
        {
            if (capacidadeExtra < 0)
            {
                TempData["Erro"] = "A capacidade extra não pode ser negativa.";
                return RedirectToAction(nameof(Utilizador), new { id });
            }

            return await ExecutarAcaoAsync(id, () => _api.AdminDefinirInventarioAsync(ObterAdminId(), id, capacidadeExtra, motivo));
        }

        // POST: /Gestao/DefinirAmizade/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DefinirAmizade(int id, int personagemId, int pontos, string? motivo)
        {
            if (pontos < 0)
            {
                TempData["Erro"] = "Os pontos não podem ser negativos.";
                return RedirectToAction(nameof(Utilizador), new { id });
            }

            return await ExecutarAcaoAsync(id, () => _api.AdminDefinirAmizadeAsync(ObterAdminId(), id, personagemId, pontos, motivo));
        }

        // POST: /Gestao/AjustarRecompensa/5  (tipo: Titulo | Emblema | Moldura; conceder: true/false)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AjustarRecompensa(int id, string tipo, int recompensaId, bool conceder, string? motivo)
        {
            return await ExecutarAcaoAsync(id, () => _api.AdminRecompensaAsync(ObterAdminId(), id, tipo, recompensaId, conceder, motivo));
        }

        // GET: /Gestao/Acoes?utilizadorId=5&autorId=1
        public async Task<IActionResult> Acoes(int? utilizadorId, int? autorId)
        {
            try
            {
                ViewBag.UtilizadorId = utilizadorId;
                ViewBag.AutorId = autorId;
                var acoes = await _api.AdminObterAcoesAsync(ObterAdminId(), utilizadorId, autorId, 200);
                return View(acoes);
            }
            catch (Exception)
            {
                TempData["Erro"] = "Não foi possível obter o registo de ações. Verifique se a WishBound.WebAPI está em execução.";
                return View(new List<AcaoAdmin>());
            }
        }

        // ============================================================
        //  MENSAGENS DE PERSONAGEM
        // ============================================================

        // GET: /Gestao/Mensagens?personagemId=3   (SELECT)
        public async Task<IActionResult> Mensagens(int? personagemId)
        {
            try
            {
                var modelo = await _api.AdminObterMensagensAsync(ObterAdminId(), personagemId);

                // Formulário de criação pré-preenchido com a personagem filtrada
                if (modelo.PersonagemAtual != null)
                {
                    modelo.Form.PersonagemId = modelo.PersonagemAtual.PersonagemId;
                    modelo.Form.PersonagemNome = modelo.PersonagemAtual.Nome;
                }

                return View(modelo);
            }
            catch (Exception)
            {
                TempData["Erro"] = "Não foi possível obter as mensagens. Verifique se a WishBound.WebAPI está em execução.";
                return View(new GestaoMensagensViewModel { PersonagemId = personagemId });
            }
        }

        // POST: /Gestao/CriarMensagem   (INSERT)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CriarMensagem(MensagemFormViewModel form)
        {
            // Validação de entrada no site (a API repete-a)
            if (string.IsNullOrWhiteSpace(form.Conteudo) || form.Conteudo.Trim().Length < 2)
            {
                TempData["Erro"] = "A mensagem não pode estar vazia.";
                return RedirectToAction(nameof(Mensagens), new { personagemId = form.PersonagemId });
            }

            return await ExecutarAcaoMensagemAsync(form.PersonagemId,
                () => _api.AdminCriarMensagemAsync(ObterAdminId(), form));
        }

        // GET: /Gestao/EditarMensagem/12   (formulário)
        public async Task<IActionResult> EditarMensagem(int id)
        {
            try
            {
                var mensagem = await _api.AdminObterMensagemAsync(ObterAdminId(), id);

                if (mensagem == null)
                {
                    TempData["Erro"] = "Mensagem não encontrada.";
                    return RedirectToAction(nameof(Mensagens));
                }

                var modelo = await _api.AdminObterMensagensAsync(ObterAdminId(), mensagem.PersonagemId);
                modelo.Form = new MensagemFormViewModel
                {
                    Id = mensagem.Id,
                    PersonagemId = mensagem.PersonagemId,
                    PersonagemNome = mensagem.PersonagemNome,
                    Tipo = mensagem.Tipo,
                    NivelOrdem = mensagem.NivelOrdem,
                    Conteudo = mensagem.Conteudo
                };

                return View(modelo);
            }
            catch (Exception)
            {
                TempData["Erro"] = "Não foi possível obter a mensagem. Verifique se a WishBound.WebAPI está em execução.";
                return RedirectToAction(nameof(Mensagens));
            }
        }

        // POST: /Gestao/EditarMensagem/12   (UPDATE)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarMensagem(int id, MensagemFormViewModel form)
        {
            form.Id = id;

            if (string.IsNullOrWhiteSpace(form.Conteudo) || form.Conteudo.Trim().Length < 2)
            {
                TempData["Erro"] = "A mensagem não pode estar vazia.";
                return RedirectToAction(nameof(EditarMensagem), new { id });
            }

            return await ExecutarAcaoMensagemAsync(form.PersonagemId,
                () => _api.AdminEditarMensagemAsync(ObterAdminId(), form));
        }

        // POST: /Gestao/ApagarMensagem/12   (DELETE)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApagarMensagem(int id, int personagemId, string? motivo)
        {
            return await ExecutarAcaoMensagemAsync(personagemId,
                () => _api.AdminApagarMensagemAsync(ObterAdminId(), id, motivo));
        }

        /// <summary>Corre uma ação sobre as mensagens e volta à lista da personagem com a mensagem da API.</summary>
        private async Task<IActionResult> ExecutarAcaoMensagemAsync(int personagemId, Func<Task<(ResultadoAcaoAdmin? Resultado, string? Erro)>> acao)
        {
            try
            {
                var (resultado, erro) = await acao();

                if (resultado == null)
                {
                    TempData["Erro"] = "A API recusou a operação: " + erro;
                }
                else
                {
                    TempData["Sucesso"] = resultado.Mensagem;
                }
            }
            catch (Exception)
            {
                TempData["Erro"] = "Não foi possível concluir a operação. Verifique se a WishBound.WebAPI está em execução.";
            }

            return RedirectToAction(nameof(Mensagens), new { personagemId });
        }

        /// <summary>
        /// Corre uma ação de administração e volta ao detalhe da conta com a
        /// mensagem da API (sucesso a verde, recusa a vermelho).
        /// </summary>
        private async Task<IActionResult> ExecutarAcaoAsync(int id, Func<Task<(ResultadoAcaoAdmin? Resultado, string? Erro)>> acao)
        {
            try
            {
                var (resultado, erro) = await acao();

                if (resultado == null)
                {
                    TempData["Erro"] = "A API recusou a operação: " + erro;
                }
                else
                {
                    TempData["Sucesso"] = resultado.Mensagem;
                }
            }
            catch (Exception)
            {
                TempData["Erro"] = "Não foi possível concluir a operação. Verifique se a WishBound.WebAPI está em execução.";
            }

            return RedirectToAction(nameof(Utilizador), new { id });
        }
    }
}
