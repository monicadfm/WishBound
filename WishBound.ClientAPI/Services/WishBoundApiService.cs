using System.Net;
using System.Net.Http.Json;
using WishBound.ClientAPI.Models;
using WishBound.ClientAPI.Models.Amizade;
using WishBound.ClientAPI.Models.Colecao;
using WishBound.ClientAPI.Models.Conta;
using WishBound.ClientAPI.Models.Economia;
using WishBound.ClientAPI.Models.Gestao;

namespace WishBound.ClientAPI.Services
{
    /// <summary>
    /// Serviço responsável por toda a comunicação com a WishBound.WebAPI.
    /// Os controllers usam este serviço em vez de falarem diretamente com o HttpClient.
    /// Os erros de rede são tratados (try-catch) nos controllers.
    /// </summary>
    public class WishBoundApiService
    {
        private readonly HttpClient _http;

        public WishBoundApiService(HttpClient http)
        {
            _http = http;
        }

        // ---------- Personagens ----------

        /// <summary>SELECT - obtém todas as personagens.</summary>
        public async Task<List<Personagem>> ObterPersonagensAsync()
        {
            return await _http.GetFromJsonAsync<List<Personagem>>("api/personagens")
                   ?? new List<Personagem>();
        }

        /// <summary>SELECT - obtém uma personagem pelo Id (null se não existir).</summary>
        public async Task<Personagem?> ObterPersonagemAsync(int id)
        {
            var resposta = await _http.GetAsync("api/personagens/" + id);

            if (resposta.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            resposta.EnsureSuccessStatusCode();
            return await resposta.Content.ReadFromJsonAsync<Personagem>();
        }

        /// <summary>INSERT - cria uma nova personagem.</summary>
        public async Task<(bool Sucesso, string? Erro)> CriarPersonagemAsync(Personagem personagem)
        {
            var resposta = await _http.PostAsJsonAsync("api/personagens", personagem);

            if (resposta.IsSuccessStatusCode)
            {
                return (true, null);
            }

            string erro = await resposta.Content.ReadAsStringAsync();
            return (false, erro);
        }

        /// <summary>UPDATE - atualiza uma personagem existente.</summary>
        public async Task<(bool Sucesso, string? Erro)> AtualizarPersonagemAsync(Personagem personagem)
        {
            var resposta = await _http.PutAsJsonAsync("api/personagens/" + personagem.Id, personagem);

            if (resposta.IsSuccessStatusCode)
            {
                return (true, null);
            }

            string erro = await resposta.Content.ReadAsStringAsync();
            return (false, erro);
        }

        /// <summary>DELETE - apaga uma personagem.</summary>
        public async Task<(bool Sucesso, string? Erro)> ApagarPersonagemAsync(int id)
        {
            var resposta = await _http.DeleteAsync("api/personagens/" + id);

            if (resposta.IsSuccessStatusCode)
            {
                return (true, null);
            }

            string erro = await resposta.Content.ReadAsStringAsync();
            return (false, erro);
        }

        // ---------- Raridades ----------

        /// <summary>SELECT - obtém todas as raridades (para dropdowns e probabilidades).</summary>
        public async Task<List<Raridade>> ObterRaridadesAsync()
        {
            return await _http.GetFromJsonAsync<List<Raridade>>("api/raridades")
                   ?? new List<Raridade>();
        }

        // ---------- Invocações (gacha) ----------

        /// <summary>
        /// SELECT - banners a decorrer (o permanente e os de evento). Com o
        /// utilizadorId de um administrador a API devolve também os eventos
        /// já terminados (o admin mantém o acesso às exclusivas).
        /// </summary>
        public async Task<List<Banner>> ObterBannersAsync(int utilizadorId = 0)
        {
            return await _http.GetFromJsonAsync<List<Banner>>("api/banners?utilizadorId=" + utilizadorId)
                   ?? new List<Banner>();
        }

        /// <summary>SELECT - detalhe de um banner (pool, rate-up, exclusivas) e a participação do utilizador nele.</summary>
        public async Task<BannerDetalhe?> ObterBannerAsync(int bannerId, int utilizadorId)
        {
            var resposta = await _http.GetAsync("api/banners/" + bannerId + "?utilizadorId=" + utilizadorId);

            if (!resposta.IsSuccessStatusCode)
            {
                return null;
            }

            return await resposta.Content.ReadFromJsonAsync<BannerDetalhe>();
        }

        /// <summary>SELECT - contadores de garantia (pity) do utilizador num banner.</summary>
        public async Task<EstadoPity?> ObterEstadoPityAsync(int utilizadorId, int bannerId)
        {
            var resposta = await _http.GetAsync(
                "api/invocacoes/estado?utilizadorId=" + utilizadorId + "&bannerId=" + bannerId);

            if (!resposta.IsSuccessStatusCode)
            {
                return null;
            }

            return await resposta.Content.ReadFromJsonAsync<EstadoPity>();
        }

        /// <summary>
        /// Realiza 1 ou 10 invocações em nome do utilizador autenticado, no
        /// banner escolhido. Devolve o resultado ou a mensagem de erro da API
        /// — por exemplo quando não há espaço na coleção.
        /// </summary>
        public async Task<(ResultadoInvocacao? Resultado, string? Erro)> InvocarAsync(
            int utilizadorId, int bannerId, int quantidade)
        {
            var resposta = await _http.PostAsJsonAsync("api/invocacoes", new
            {
                UtilizadorId = utilizadorId,
                BannerId = bannerId,
                Quantidade = quantidade
            });

            if (!resposta.IsSuccessStatusCode)
            {
                return (null, await resposta.Content.ReadAsStringAsync());
            }

            return (await resposta.Content.ReadFromJsonAsync<ResultadoInvocacao>(), null);
        }

        /// <summary>SELECT - obtém o histórico de invocações DO utilizador.</summary>
        public async Task<List<Invocacao>> ObterHistoricoAsync(int utilizadorId)
        {
            return await _http.GetFromJsonAsync<List<Invocacao>>("api/invocacoes?utilizadorId=" + utilizadorId)
                   ?? new List<Invocacao>();
        }

        // ---------- Coleção pessoal ----------

        /// <summary>SELECT - coleção do utilizador, já ordenada pela API.</summary>
        public async Task<ColecaoViewModel> ObterColecaoAsync(int utilizadorId, string ordenar, bool apenasFavoritos)
        {
            string url = "api/colecao?utilizadorId=" + utilizadorId +
                         "&ordenar=" + Uri.EscapeDataString(ordenar) +
                         "&favoritos=" + (apenasFavoritos ? "true" : "false");

            return await _http.GetFromJsonAsync<ColecaoViewModel>(url) ?? new ColecaoViewModel();
        }

        /// <summary>SELECT - uma personagem da coleção (null se não a tiver).</summary>
        public async Task<ItemColecao?> ObterItemColecaoAsync(int utilizadorId, int personagemId)
        {
            var resposta = await _http.GetAsync(
                "api/colecao/item?utilizadorId=" + utilizadorId + "&personagemId=" + personagemId);

            // Só o 404 significa "não tem esta personagem"; qualquer outro erro
            // (API em baixo, chave errada) deve rebentar para o controller
            // mostrar a mensagem certa.
            if (resposta.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            resposta.EnsureSuccessStatusCode();
            return await resposta.Content.ReadFromJsonAsync<ItemColecao>();
        }

        /// <summary>UPDATE - marca ou desmarca uma personagem como favorita.</summary>
        public async Task<(bool Sucesso, string Mensagem)> MarcarFavoritoAsync(int utilizadorId, int personagemId, bool favorito)
        {
            var resposta = await _http.PostAsJsonAsync("api/colecao/favorito", new
            {
                UtilizadorId = utilizadorId,
                PersonagemId = personagemId,
                Favorito = favorito
            });

            return (resposta.IsSuccessStatusCode, await resposta.Content.ReadAsStringAsync());
        }

        /// <summary>UPDATE - liberta cópias repetidas (mantém sempre uma).</summary>
        public async Task<(bool Sucesso, string Mensagem)> LibertarDuplicadosAsync(int utilizadorId, int personagemId, int quantidade)
        {
            var resposta = await _http.PostAsJsonAsync("api/colecao/libertar", new
            {
                UtilizadorId = utilizadorId,
                PersonagemId = personagemId,
                Quantidade = quantidade
            });

            return (resposta.IsSuccessStatusCode, await resposta.Content.ReadAsStringAsync());
        }

        /// <summary>UPDATE - liberta de uma vez as repetidas de todas as personagens.</summary>
        public async Task<(bool Sucesso, string Mensagem)> LibertarTodosOsDuplicadosAsync(int utilizadorId)
        {
            var resposta = await _http.PostAsJsonAsync("api/colecao/libertar-tudo", new
            {
                UtilizadorId = utilizadorId
            });

            return (resposta.IsSuccessStatusCode, await resposta.Content.ReadAsStringAsync());
        }

        /// <summary>UPDATE - compra mais lugares para a coleção, pagos em Moedas.</summary>
        public async Task<(bool Sucesso, string Mensagem)> ExpandirInventarioAsync(int utilizadorId)
        {
            var resposta = await _http.PostAsJsonAsync("api/colecao/expandir", new
            {
                UtilizadorId = utilizadorId
            });

            return (resposta.IsSuccessStatusCode, await resposta.Content.ReadAsStringAsync());
        }

        // ---------- Economia (carteira, recompensas, transações) ----------

        /// <summary>SELECT - saldos, recompensa diária e eventos a decorrer do utilizador.</summary>
        public async Task<EconomiaViewModel> ObterEconomiaAsync(int utilizadorId)
        {
            return await _http.GetFromJsonAsync<EconomiaViewModel>("api/economia?utilizadorId=" + utilizadorId)
                   ?? new EconomiaViewModel();
        }

        /// <summary>SELECT - últimos movimentos de moeda do utilizador.</summary>
        public async Task<List<Transacao>> ObterTransacoesAsync(int utilizadorId, int limite = 100)
        {
            return await _http.GetFromJsonAsync<List<Transacao>>(
                       "api/economia/transacoes?utilizadorId=" + utilizadorId + "&limite=" + limite)
                   ?? new List<Transacao>();
        }

        /// <summary>UPDATE - recebe a recompensa de login diário (uma por dia).</summary>
        public async Task<(RecompensaRecebida? Resultado, string? Erro)> ReceberLoginDiarioAsync(int utilizadorId)
        {
            var resposta = await _http.PostAsJsonAsync("api/economia/login-diario", new
            {
                UtilizadorId = utilizadorId
            });

            if (!resposta.IsSuccessStatusCode)
            {
                return (null, await resposta.Content.ReadAsStringAsync());
            }

            return (await resposta.Content.ReadFromJsonAsync<RecompensaRecebida>(), null);
        }

        /// <summary>UPDATE - resgata a recompensa do dia num evento a decorrer.</summary>
        public async Task<(RecompensaRecebida? Resultado, string? Erro)> ResgatarEventoAsync(int utilizadorId, int bannerId)
        {
            var resposta = await _http.PostAsJsonAsync("api/economia/evento/resgatar", new
            {
                UtilizadorId = utilizadorId,
                BannerId = bannerId
            });

            if (!resposta.IsSuccessStatusCode)
            {
                return (null, await resposta.Content.ReadAsStringAsync());
            }

            return (await resposta.Content.ReadFromJsonAsync<RecompensaRecebida>(), null);
        }

        // ---------- Amizade (interações, níveis, emblemas, títulos, molduras) ----------

        /// <summary>SELECT - estado completo do sistema de amizade do utilizador.</summary>
        public async Task<AmizadeViewModel> ObterAmizadeAsync(int utilizadorId)
        {
            return await _http.GetFromJsonAsync<AmizadeViewModel>("api/amizade?utilizadorId=" + utilizadorId)
                   ?? new AmizadeViewModel();
        }

        /// <summary>SELECT - amizade com UMA personagem (níveis, recompensas, estado). null se não a tiver.</summary>
        public async Task<AmizadePersonagem?> ObterAmizadePersonagemAsync(int utilizadorId, int personagemId)
        {
            var resposta = await _http.GetAsync(
                "api/amizade/personagem?utilizadorId=" + utilizadorId + "&personagemId=" + personagemId);

            if (!resposta.IsSuccessStatusCode)
            {
                return null;
            }

            return await resposta.Content.ReadFromJsonAsync<AmizadePersonagem>();
        }

        /// <summary>UPDATE - põe ou tira um emblema do perfil (máximo 3 equipados).</summary>
        public async Task<(bool Sucesso, string Mensagem)> EquiparEmblemaAsync(int utilizadorId, int emblemaId, bool equipar)
        {
            var resposta = await _http.PostAsJsonAsync("api/amizade/emblema", new
            {
                UtilizadorId = utilizadorId,
                EmblemaId = emblemaId,
                Equipar = equipar
            });

            return (resposta.IsSuccessStatusCode, await resposta.Content.ReadAsStringAsync());
        }

        /// <summary>UPDATE - gasta uma das 3 interações do dia numa personagem da coleção.</summary>
        public async Task<(ResultadoInteracao? Resultado, string? Erro)> InteragirAsync(int utilizadorId, int personagemId)
        {
            var resposta = await _http.PostAsJsonAsync("api/amizade/interagir", new
            {
                UtilizadorId = utilizadorId,
                PersonagemId = personagemId
            });

            if (!resposta.IsSuccessStatusCode)
            {
                return (null, await resposta.Content.ReadAsStringAsync());
            }

            return (await resposta.Content.ReadFromJsonAsync<ResultadoInteracao>(), null);
        }

        /// <summary>UPDATE - escolhe o título do perfil (null = sem título).</summary>
        public async Task<(bool Sucesso, string Mensagem)> EquiparTituloAsync(int utilizadorId, int? tituloId)
        {
            var resposta = await _http.PostAsJsonAsync("api/amizade/titulo", new
            {
                UtilizadorId = utilizadorId,
                TituloId = tituloId
            });

            return (resposta.IsSuccessStatusCode, await resposta.Content.ReadAsStringAsync());
        }

        /// <summary>UPDATE - escolhe a moldura do perfil (null = sem moldura).</summary>
        public async Task<(bool Sucesso, string Mensagem)> EquiparMolduraAsync(int utilizadorId, int? molduraId)
        {
            var resposta = await _http.PostAsJsonAsync("api/amizade/moldura", new
            {
                UtilizadorId = utilizadorId,
                MolduraId = molduraId
            });

            return (resposta.IsSuccessStatusCode, await resposta.Content.ReadAsStringAsync());
        }

        // ---------- Mensagens de personagem (api/mensagens) ----------

        /// <summary>SELECT - o conjunto de mensagens de uma personagem da coleção (com a saudação de hoje). null se falhar.</summary>
        public async Task<MensagensPersonagem?> ObterMensagensPersonagemAsync(int utilizadorId, int personagemId)
        {
            var resposta = await _http.GetAsync(
                "api/mensagens/personagem?utilizadorId=" + utilizadorId + "&personagemId=" + personagemId);

            if (!resposta.IsSuccessStatusCode)
            {
                return null;
            }

            return await resposta.Content.ReadFromJsonAsync<MensagensPersonagem>();
        }

        /// <summary>SELECT - a companheira da página inicial, a saudação dela e as personagens que podem ser escolhidas.</summary>
        public async Task<CompanheiraViewModel> ObterCompanheiraAsync(int utilizadorId)
        {
            return await _http.GetFromJsonAsync<CompanheiraViewModel>("api/mensagens/companheira?utilizadorId=" + utilizadorId)
                   ?? new CompanheiraViewModel();
        }

        /// <summary>UPDATE - escolhe a companheira da página inicial (null = nenhuma).</summary>
        public async Task<(bool Sucesso, string Mensagem)> EscolherCompanheiraAsync(int utilizadorId, int? personagemId)
        {
            var resposta = await _http.PostAsJsonAsync("api/mensagens/companheira", new
            {
                UtilizadorId = utilizadorId,
                PersonagemId = personagemId
            });

            return (resposta.IsSuccessStatusCode, await resposta.Content.ReadAsStringAsync());
        }

        // ---------- Gestão das mensagens de personagem (api/admin/mensagens) ----------

        /// <summary>SELECT - resumo por personagem + mensagens (de todas ou só de uma personagem).</summary>
        public async Task<GestaoMensagensViewModel> AdminObterMensagensAsync(int adminId, int? personagemId)
        {
            string url = "api/admin/mensagens?adminId=" + adminId;
            if (personagemId.HasValue && personagemId.Value > 0)
            {
                url += "&personagemId=" + personagemId.Value;
            }

            var modelo = await _http.GetFromJsonAsync<GestaoMensagensViewModel>(url) ?? new GestaoMensagensViewModel();
            modelo.PersonagemId = personagemId;
            return modelo;
        }

        /// <summary>SELECT - uma mensagem (para editar). null se não existir.</summary>
        public async Task<MensagemAdmin?> AdminObterMensagemAsync(int adminId, int id)
        {
            var resposta = await _http.GetAsync("api/admin/mensagens/" + id + "?adminId=" + adminId);

            if (!resposta.IsSuccessStatusCode)
            {
                return null;
            }

            return await resposta.Content.ReadFromJsonAsync<MensagemAdmin>();
        }

        /// <summary>INSERT - nova mensagem de uma personagem.</summary>
        public Task<(ResultadoAcaoAdmin? Resultado, string? Erro)> AdminCriarMensagemAsync(int adminId, MensagemFormViewModel form)
        {
            return EnviarAcaoAdminAsync("api/admin/mensagens", CorpoMensagem(adminId, form));
        }

        /// <summary>UPDATE - edita o tipo, o nível e o texto de uma mensagem.</summary>
        public async Task<(ResultadoAcaoAdmin? Resultado, string? Erro)> AdminEditarMensagemAsync(int adminId, MensagemFormViewModel form)
        {
            var resposta = await _http.PutAsJsonAsync("api/admin/mensagens/" + form.Id, CorpoMensagem(adminId, form));

            if (!resposta.IsSuccessStatusCode)
            {
                return (null, await resposta.Content.ReadAsStringAsync());
            }

            return (await resposta.Content.ReadFromJsonAsync<ResultadoAcaoAdmin>(), null);
        }

        /// <summary>DELETE - apaga uma mensagem.</summary>
        public async Task<(ResultadoAcaoAdmin? Resultado, string? Erro)> AdminApagarMensagemAsync(int adminId, int id, string? motivo)
        {
            string url = "api/admin/mensagens/" + id + "?adminId=" + adminId;
            if (!string.IsNullOrWhiteSpace(motivo))
            {
                url += "&motivo=" + Uri.EscapeDataString(motivo.Trim());
            }

            var resposta = await _http.DeleteAsync(url);

            if (!resposta.IsSuccessStatusCode)
            {
                return (null, await resposta.Content.ReadAsStringAsync());
            }

            return (await resposta.Content.ReadFromJsonAsync<ResultadoAcaoAdmin>(), null);
        }

        private static object CorpoMensagem(int adminId, MensagemFormViewModel form) => new
        {
            AdminId = adminId,
            form.PersonagemId,
            form.Tipo,
            form.NivelOrdem,
            form.Conteudo,
            form.Motivo
        };

        // ---------- Administração de contas (api/admin) ----------
        // Todos os pedidos levam o Id do administrador com sessão iniciada;
        // a API confirma que é mesmo uma conta de administrador ativa.

        /// <summary>SELECT - lista paginada de contas, com pesquisa e filtro.</summary>
        public async Task<ListaContasViewModel> AdminObterContasAsync(int adminId, string? pesquisa, string? filtro, int pagina, int tamanho = 20)
        {
            string url = "api/admin/utilizadores?adminId=" + adminId +
                         "&pagina=" + pagina + "&tamanho=" + tamanho +
                         "&filtro=" + Uri.EscapeDataString(filtro ?? "todos");

            if (!string.IsNullOrWhiteSpace(pesquisa))
            {
                url += "&pesquisa=" + Uri.EscapeDataString(pesquisa.Trim());
            }

            return await _http.GetFromJsonAsync<ListaContasViewModel>(url) ?? new ListaContasViewModel();
        }

        /// <summary>SELECT - detalhe completo de uma conta (null se não existir).</summary>
        public async Task<ContaDetalheViewModel?> AdminObterContaAsync(int adminId, int utilizadorId)
        {
            var resposta = await _http.GetAsync("api/admin/utilizadores/" + utilizadorId + "?adminId=" + adminId);

            if (resposta.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            resposta.EnsureSuccessStatusCode();
            return await resposta.Content.ReadFromJsonAsync<ContaDetalheViewModel>();
        }

        /// <summary>SELECT - registo de ações (opcionalmente filtrado pela conta alvo ou pelo autor).</summary>
        public async Task<List<AcaoAdmin>> AdminObterAcoesAsync(int adminId, int? utilizadorId = null, int? autorId = null, int limite = 100)
        {
            string url = "api/admin/acoes?adminId=" + adminId + "&limite=" + limite;
            if (utilizadorId.HasValue) url += "&utilizadorId=" + utilizadorId.Value;
            if (autorId.HasValue) url += "&autorId=" + autorId.Value;

            return await _http.GetFromJsonAsync<List<AcaoAdmin>>(url) ?? new List<AcaoAdmin>();
        }

        /// <summary>UPDATE - ativar/desativar, promover/despromover, validar email (só os campos não nulos).</summary>
        public Task<(ResultadoAcaoAdmin? Resultado, string? Erro)> AdminAlterarEstadoAsync(
            int adminId, int utilizadorId, bool? isAtivo, bool? isAdmin, bool? emailValidado, string? motivo)
        {
            return EnviarAcaoAdminAsync("api/admin/utilizadores/" + utilizadorId + "/estado", new
            {
                AdminId = adminId,
                IsAtivo = isAtivo,
                IsAdmin = isAdmin,
                EmailValidado = emailValidado,
                Motivo = motivo
            });
        }

        /// <summary>UPDATE - define uma nova password para a conta.</summary>
        public Task<(ResultadoAcaoAdmin? Resultado, string? Erro)> AdminReporPasswordAsync(
            int adminId, int utilizadorId, string novaPassword, string? motivo)
        {
            return EnviarAcaoAdminAsync("api/admin/utilizadores/" + utilizadorId + "/repor-password", new
            {
                AdminId = adminId,
                NovaPassword = novaPassword,
                Motivo = motivo
            });
        }

        /// <summary>UPDATE - dá (positivo) ou tira (negativo) moeda.</summary>
        public Task<(ResultadoAcaoAdmin? Resultado, string? Erro)> AdminAjustarMoedaAsync(
            int adminId, int utilizadorId, int tipoMoedaId, decimal quantidade, string? motivo)
        {
            return EnviarAcaoAdminAsync("api/admin/utilizadores/" + utilizadorId + "/moeda", new
            {
                AdminId = adminId,
                TipoMoedaId = tipoMoedaId,
                Quantidade = quantidade,
                Motivo = motivo
            });
        }

        /// <summary>UPDATE - dá (positivo) ou tira (negativo) cópias de uma personagem.</summary>
        public Task<(ResultadoAcaoAdmin? Resultado, string? Erro)> AdminAjustarPersonagemAsync(
            int adminId, int utilizadorId, int personagemId, int quantidade, string? motivo)
        {
            return EnviarAcaoAdminAsync("api/admin/utilizadores/" + utilizadorId + "/personagem", new
            {
                AdminId = adminId,
                PersonagemId = personagemId,
                Quantidade = quantidade,
                Motivo = motivo
            });
        }

        /// <summary>UPDATE - define a capacidade extra do inventário.</summary>
        public Task<(ResultadoAcaoAdmin? Resultado, string? Erro)> AdminDefinirInventarioAsync(
            int adminId, int utilizadorId, int capacidadeExtra, string? motivo)
        {
            return EnviarAcaoAdminAsync("api/admin/utilizadores/" + utilizadorId + "/inventario", new
            {
                AdminId = adminId,
                CapacidadeExtra = capacidadeExtra,
                Motivo = motivo
            });
        }

        /// <summary>UPDATE - define os pontos de amizade com uma personagem.</summary>
        public Task<(ResultadoAcaoAdmin? Resultado, string? Erro)> AdminDefinirAmizadeAsync(
            int adminId, int utilizadorId, int personagemId, int pontos, string? motivo)
        {
            return EnviarAcaoAdminAsync("api/admin/utilizadores/" + utilizadorId + "/amizade", new
            {
                AdminId = adminId,
                PersonagemId = personagemId,
                Pontos = pontos,
                Motivo = motivo
            });
        }

        /// <summary>UPDATE - concede ou revoga um título/emblema/moldura.</summary>
        public Task<(ResultadoAcaoAdmin? Resultado, string? Erro)> AdminRecompensaAsync(
            int adminId, int utilizadorId, string tipo, int id, bool conceder, string? motivo)
        {
            return EnviarAcaoAdminAsync("api/admin/utilizadores/" + utilizadorId + "/recompensa", new
            {
                AdminId = adminId,
                Tipo = tipo,
                Id = id,
                Conceder = conceder,
                Motivo = motivo
            });
        }

        /// <summary>POST comum das ações de administração: resultado ou texto do erro.</summary>
        private async Task<(ResultadoAcaoAdmin? Resultado, string? Erro)> EnviarAcaoAdminAsync(string url, object corpo)
        {
            var resposta = await _http.PostAsJsonAsync(url, corpo);

            if (!resposta.IsSuccessStatusCode)
            {
                return (null, await resposta.Content.ReadAsStringAsync());
            }

            return (await resposta.Content.ReadFromJsonAsync<ResultadoAcaoAdmin>(), null);
        }

        // ---------- Conta e autenticação ----------

        /// <summary>Regista um novo utilizador. Devolve a resposta com o token de validação (modo dev).</summary>
        public async Task<(TokenResposta? Resposta, string? Erro)> RegistarAsync(RegistoViewModel registo)
        {
            var resposta = await _http.PostAsJsonAsync("api/conta/registar", new
            {
                registo.NomeUtilizador,
                registo.Email,
                registo.Password
            });

            if (!resposta.IsSuccessStatusCode)
            {
                return (null, await resposta.Content.ReadAsStringAsync());
            }

            return (await resposta.Content.ReadFromJsonAsync<TokenResposta>(), null);
        }

        /// <summary>Verifica as credenciais. Devolve o utilizador se o login for válido.</summary>
        public async Task<(UtilizadorSessao? Utilizador, string? Erro)> LoginAsync(string identificador, string password)
        {
            var resposta = await _http.PostAsJsonAsync("api/conta/login", new
            {
                Identificador = identificador,
                Password = password
            });

            if (!resposta.IsSuccessStatusCode)
            {
                return (null, await resposta.Content.ReadAsStringAsync());
            }

            return (await resposta.Content.ReadFromJsonAsync<UtilizadorSessao>(), null);
        }

        /// <summary>
        /// Login com Google: a API liga/cria a conta associada a esta conta
        /// Google e devolve o utilizador (chamado APÓS a Google confirmar a
        /// identidade no fluxo OAuth).
        /// </summary>
        public async Task<(UtilizadorSessao? Utilizador, string? Erro)> LoginGoogleAsync(
            string googleId, string email, string? nome, string? fotoUrl)
        {
            var resposta = await _http.PostAsJsonAsync("api/conta/login-google", new
            {
                GoogleId = googleId,
                Email = email,
                Nome = nome,
                FotoUrl = fotoUrl
            });

            if (!resposta.IsSuccessStatusCode)
            {
                return (null, await resposta.Content.ReadAsStringAsync());
            }

            return (await resposta.Content.ReadFromJsonAsync<UtilizadorSessao>(), null);
        }

        /// <summary>Valida o email de uma conta com o token do link.</summary>
        public async Task<(bool Sucesso, string Mensagem)> ValidarEmailAsync(string token)
        {
            var resposta = await _http.PostAsJsonAsync("api/conta/validar-email", new { Token = token });
            return (resposta.IsSuccessStatusCode, await resposta.Content.ReadAsStringAsync());
        }

        /// <summary>Pede um novo link de validação de email.</summary>
        public async Task<(TokenResposta? Resposta, string? Erro)> ReenviarValidacaoAsync(string email)
        {
            var resposta = await _http.PostAsJsonAsync("api/conta/reenviar-validacao", new { Email = email });

            if (!resposta.IsSuccessStatusCode)
            {
                return (null, await resposta.Content.ReadAsStringAsync());
            }

            return (await resposta.Content.ReadFromJsonAsync<TokenResposta>(), null);
        }

        /// <summary>Pede um link de recuperação de password.</summary>
        public async Task<(TokenResposta? Resposta, string? Erro)> RecuperarPasswordAsync(string email)
        {
            var resposta = await _http.PostAsJsonAsync("api/conta/recuperar-password", new { Email = email });

            if (!resposta.IsSuccessStatusCode)
            {
                return (null, await resposta.Content.ReadAsStringAsync());
            }

            return (await resposta.Content.ReadFromJsonAsync<TokenResposta>(), null);
        }

        /// <summary>Repõe a password usando o token do link de recuperação.</summary>
        public async Task<(bool Sucesso, string Mensagem)> ReporPasswordAsync(string token, string novaPassword)
        {
            var resposta = await _http.PostAsJsonAsync("api/conta/repor-password", new
            {
                Token = token,
                NovaPassword = novaPassword
            });

            return (resposta.IsSuccessStatusCode, await resposta.Content.ReadAsStringAsync());
        }

        /// <summary>
        /// Altera (ou define, nas contas Google sem password local) a password
        /// de um utilizador autenticado. passwordAtual pode ir a null quando a
        /// conta ainda não tem password — a API confirma esse caso.
        /// </summary>
        public async Task<(bool Sucesso, string Mensagem)> AlterarPasswordAsync(int utilizadorId, string? passwordAtual, string novaPassword)
        {
            var resposta = await _http.PostAsJsonAsync("api/conta/alterar-password", new
            {
                UtilizadorId = utilizadorId,
                PasswordAtual = passwordAtual,
                NovaPassword = novaPassword
            });

            return (resposta.IsSuccessStatusCode, await resposta.Content.ReadAsStringAsync());
        }

        /// <summary>SELECT - obtém os dados públicos de um utilizador.</summary>
        public async Task<UtilizadorSessao?> ObterUtilizadorAsync(int id)
        {
            var resposta = await _http.GetAsync("api/conta/" + id);

            if (!resposta.IsSuccessStatusCode)
            {
                return null;
            }

            return await resposta.Content.ReadFromJsonAsync<UtilizadorSessao>();
        }

        /// <summary>UPDATE - atualiza o perfil e devolve os dados atualizados.</summary>
        public async Task<(UtilizadorSessao? Utilizador, string? Erro)> AtualizarPerfilAsync(int utilizadorId, PerfilViewModel perfil)
        {
            var resposta = await _http.PutAsJsonAsync("api/conta/perfil", new
            {
                UtilizadorId = utilizadorId,
                perfil.NomeUtilizador,
                perfil.FotoPerfilUrl
            });

            if (!resposta.IsSuccessStatusCode)
            {
                return (null, await resposta.Content.ReadAsStringAsync());
            }

            return (await resposta.Content.ReadFromJsonAsync<UtilizadorSessao>(), null);
        }
    }
}
