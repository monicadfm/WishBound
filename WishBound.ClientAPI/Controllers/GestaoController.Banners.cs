using Microsoft.AspNetCore.Mvc;
using WishBound.ClientAPI.Models.Gestao;

namespace WishBound.ClientAPI.Controllers
{
    /// <summary>
    /// Área de gestão — BANNERS E EVENTOS: criar/editar/apagar banners,
    /// escolher as personagens e o rate-up (quota por raridade) e gerir os
    /// dias de recompensa dos eventos.
    ///
    /// Parte da classe GestaoController ([Authorize(Roles = "Admin")] está na
    /// outra parte, GestaoController.cs). Todos os POST têm antiforgery.
    /// </summary>
    public partial class GestaoController
    {
        // ============================================================
        //  BANNERS E EVENTOS
        // ============================================================

        // GET: /Gestao/Banners
        public async Task<IActionResult> Banners()
        {
            try
            {
                return View(new BannersViewModel { Itens = await _api.AdminObterBannersAsync(ObterAdminId()) });
            }
            catch (Exception)
            {
                TempData["Erro"] = "Não foi possível obter os banners. " + MensagemApiEmBaixo;
                return View(new BannersViewModel());
            }
        }

        // POST: /Gestao/CriarBanner
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CriarBanner(BannerFormViewModel form)
        {
            if (form.InicioUtc == null || form.FimUtc == null)
            {
                TempData["Erro"] = "Indique as datas de início e de fim.";
                return RedirectToAction(nameof(Banners));
            }

            try
            {
                var (resultado, erro) = await _api.AdminGuardarBannerAsync(ObterAdminId(), null, form);

                if (resultado == null)
                {
                    TempData["Erro"] = "A API recusou a operação: " + erro;
                    return RedirectToAction(nameof(Banners));
                }

                TempData["Sucesso"] = resultado.Mensagem;

                // Vai direto para o novo banner (pool e recompensas)
                return resultado.NovoValor.HasValue
                    ? RedirectToAction(nameof(EditarBanner), new { id = (int)resultado.NovoValor.Value })
                    : RedirectToAction(nameof(Banners));
            }
            catch (Exception)
            {
                TempData["Erro"] = "Não foi possível criar o banner. " + MensagemApiEmBaixo;
                return RedirectToAction(nameof(Banners));
            }
        }

        // GET: /Gestao/EditarBanner/5
        public async Task<IActionResult> EditarBanner(int id)
        {
            try
            {
                var modelo = await _api.AdminObterBannerAsync(ObterAdminId(), id);

                if (modelo == null)
                {
                    TempData["Erro"] = "O banner que tentou abrir não existe.";
                    return RedirectToAction(nameof(Banners));
                }

                return View(modelo);
            }
            catch (Exception)
            {
                TempData["Erro"] = "Não foi possível obter o banner. " + MensagemApiEmBaixo;
                return RedirectToAction(nameof(Banners));
            }
        }

        // POST: /Gestao/EditarBanner/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarBanner(int id, BannerFormViewModel form)
        {
            if (form.InicioUtc == null || form.FimUtc == null)
            {
                TempData["Erro"] = "Indique as datas de início e de fim.";
                return RedirectToAction(nameof(EditarBanner), new { id });
            }

            return await ExecutarGestaoAsync(
                () => _api.AdminGuardarBannerAsync(ObterAdminId(), id, form),
                RedirectToAction(nameof(EditarBanner), new { id }));
        }

        // POST: /Gestao/ApagarBanner/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApagarBanner(int id, string? motivo)
        {
            try
            {
                var (resultado, erro) = await _api.AdminApagarBannerAsync(ObterAdminId(), id, motivo);

                if (resultado == null)
                {
                    TempData["Erro"] = "A API recusou a operação: " + erro;
                    return RedirectToAction(nameof(EditarBanner), new { id });
                }

                TempData["Sucesso"] = resultado.Mensagem;
                return RedirectToAction(nameof(Banners));
            }
            catch (Exception)
            {
                TempData["Erro"] = "Não foi possível apagar o banner. " + MensagemApiEmBaixo;
                return RedirectToAction(nameof(EditarBanner), new { id });
            }
        }

        // POST: /Gestao/GuardarPool/5
        // personagens = ids marcados "no banner"; rateUp = ids marcados "em destaque";
        // quotas[raridadeId] = percentagem do rate-up nessa raridade (1..99)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuardarPool(int id, int[]? personagens, int[]? rateUp, Dictionary<int, string>? quotas, string? motivo)
        {
            var incluidas = (personagens ?? Array.Empty<int>()).ToHashSet();

            // Uma personagem em destaque tem de estar no banner
            var destacadas = (rateUp ?? Array.Empty<int>()).Where(incluidas.Contains).ToList();

            var fracoes = new Dictionary<int, decimal>();
            foreach (var (raridadeId, texto) in quotas ?? new Dictionary<int, string>())
            {
                var valor = LerDecimal(texto);
                if (valor.HasValue)
                {
                    fracoes[raridadeId] = Math.Round(valor.Value / 100m, 4);
                }
            }

            return await ExecutarGestaoAsync(
                () => _api.AdminGuardarPoolAsync(ObterAdminId(), id, incluidas, destacadas, fracoes, motivo),
                RedirectToAction(nameof(EditarBanner), "Gestao", new { id }, "pool"));
        }

        // POST: /Gestao/GuardarDiaEvento/5   (recompensaId vazio = acrescentar dia)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuardarDiaEvento(int id, int? recompensaId, int tipoMoedaId, string quantidade, string? descricao, string? motivo)
        {
            decimal? valor = LerDecimal(quantidade);
            if (valor == null || valor <= 0)
            {
                TempData["Erro"] = "Indique uma quantidade maior do que zero.";
                return RedirectToAction(nameof(EditarBanner), "Gestao", new { id }, "recompensas");
            }

            return await ExecutarGestaoAsync(
                () => _api.AdminGuardarDiaEventoAsync(ObterAdminId(), id, recompensaId, tipoMoedaId, valor.Value, descricao, motivo),
                RedirectToAction(nameof(EditarBanner), "Gestao", new { id }, "recompensas"));
        }

        // POST: /Gestao/ApagarDiaEvento/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApagarDiaEvento(int id, int recompensaId, string? motivo)
        {
            return await ExecutarGestaoAsync(
                () => _api.AdminApagarDiaEventoAsync(ObterAdminId(), id, recompensaId, motivo),
                RedirectToAction(nameof(EditarBanner), "Gestao", new { id }, "recompensas"));
        }
    }
}
