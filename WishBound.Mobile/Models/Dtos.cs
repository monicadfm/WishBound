namespace WishBound.Mobile.Models
{
    // ============================================================
    //  "Formas" dos pedidos e respostas trocados com a WebAPI.
    //  São cópias dos DTOs da API (ContaDtos.cs e MensagensDtos.cs):
    //  os nomes das propriedades têm de coincidir com o JSON.
    // ============================================================

    /// <summary>POST api/conta/login</summary>
    public class LoginPedido
    {
        public string Identificador { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    /// <summary>Resposta do login - o que fica guardado como sessão no telemóvel.</summary>
    public class UtilizadorSessao
    {
        public int Id { get; set; }
        public string NomeUtilizador { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsAdmin { get; set; }
        public string? FotoPerfilUrl { get; set; }
    }

    /// <summary>Uma personagem da coleção que pode ser a companheira.</summary>
    public class CandidataCompanheira
    {
        public int PersonagemId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? ImagemUrl { get; set; }
        public string RaridadeNome { get; set; } = string.Empty;
        public string? RaridadeCor { get; set; }
        public string NivelAmizadeNome { get; set; } = string.Empty;
        public int NivelOrdem { get; set; }

        // ----- Apenas para o ecrã (não vêm da API) -----

        /// <summary>Primeira letra do nome, mostrada dentro do círculo enquanto não há imagens.</summary>
        public string Inicial => string.IsNullOrEmpty(Nome) ? "?" : Nome.Substring(0, 1).ToUpperInvariant();

        /// <summary>Cor da raridade (vem em hexadecimal da tabela Raridades).</summary>
        public Color Cor => Color.TryParse(RaridadeCor ?? string.Empty, out var cor) ? cor : Color.FromArgb("#8b5cf6");

        public Brush CorPincel => new SolidColorBrush(Cor);

        /// <summary>Sprite embutido ("luna.png"); null = sem imagem, mostra-se a inicial.</summary>
        public string? Sprite => Configuracao.NomeSprite(ImagemUrl);

        public string Resumo => RaridadeNome + " · " + NivelAmizadeNome;
    }

    /// <summary>GET api/mensagens/companheira</summary>
    public class CompanheiraResposta
    {
        public CandidataCompanheira? Escolhida { get; set; }
        public string? Saudacao { get; set; }
        public List<CandidataCompanheira> Candidatas { get; set; } = new List<CandidataCompanheira>();
    }

    /// <summary>POST api/mensagens/companheira (PersonagemId null = sem companheira)</summary>
    public class EscolherCompanheiraPedido
    {
        public int UtilizadorId { get; set; }
        public int? PersonagemId { get; set; }
    }

    /// <summary>Resultado de uma chamada à API: ou traz dados, ou traz a mensagem de erro.</summary>
    public class ResultadoApi<T>
    {
        public bool Sucesso { get; set; }
        public T? Dados { get; set; }
        public string Erro { get; set; } = string.Empty;

        public static ResultadoApi<T> Ok(T? dados) => new ResultadoApi<T> { Sucesso = true, Dados = dados };
        public static ResultadoApi<T> Falha(string erro) => new ResultadoApi<T> { Sucesso = false, Erro = erro };
    }
}

namespace WishBound.Mobile.Models
{
    // ============================================================
    //  COLEÇÃO (api/colecao) e AMIZADE (api/amizade) — só o que a app usa.
    // ============================================================

    /// <summary>Uma personagem da coleção (ItemColecaoResposta da API).</summary>
    public class ItemColecao
    {
        public int PersonagemId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? Descricao { get; set; }
        public string? ImagemUrl { get; set; }

        public int RaridadeId { get; set; }
        public string RaridadeNome { get; set; } = string.Empty;
        public string? RaridadeCor { get; set; }
        public int RaridadeOrdem { get; set; }

        public int Quantidade { get; set; }
        public bool IsFavorito { get; set; }
        public DateTime DataObtencao { get; set; }

        public int PontosAmizade { get; set; }
        public int NivelAmizadeId { get; set; }
        public string NivelAmizadeNome { get; set; } = string.Empty;
        public int NivelOrdem { get; set; }
        public int NivelMaximoOrdem { get; set; }
        public string NivelMaximoNome { get; set; } = string.Empty;
        public int PontosNivelAtual { get; set; }
        public int? PontosProximoNivel { get; set; }

        // ----- Apenas para o ecrã -----

        public string Inicial => string.IsNullOrEmpty(Nome) ? "?" : Nome.Substring(0, 1).ToUpperInvariant();

        public Color Cor => Color.TryParse(RaridadeCor ?? string.Empty, out var cor) ? cor : Color.FromArgb("#8b5cf6");

        public Brush CorPincel => new SolidColorBrush(Cor);

        /// <summary>"x3" — só aparece quando há cópias repetidas.</summary>
        public string Copias => "x" + Quantidade;

        public bool TemRepetidas => Quantidade > 1;

        public string Estrela => IsFavorito ? "★" : "☆";

        public Color CorEstrela => IsFavorito ? Color.FromArgb("#f3c04f") : Color.FromArgb("#a79fc4");

        public bool NivelMaximo => !PontosProximoNivel.HasValue || NivelOrdem >= NivelMaximoOrdem;

        /// <summary>Progresso dentro do nível atual (0..1), para a barra.</summary>
        public double Progresso
        {
            get
            {
                if (NivelMaximo || !PontosProximoNivel.HasValue)
                {
                    return 1;
                }

                var largura = PontosProximoNivel.Value - PontosNivelAtual;
                if (largura <= 0)
                {
                    return 1;
                }

                var feito = PontosAmizade - PontosNivelAtual;
                return Math.Clamp(feito / (double)largura, 0, 1);
            }
        }

        public string TextoNivel => "Nv. " + NivelOrdem + " · " + NivelAmizadeNome;

        public string TextoPontos => NivelMaximo
            ? PontosAmizade + " pontos · nível máximo"
            : PontosAmizade + " / " + PontosProximoNivel + " pontos";

        public string TextoObtencao => "Obtida a " + DataObtencao.ToLocalTime().ToString("dd/MM/yyyy");

        /// <summary>Sprite embutido ("luna.png"); null = sem imagem, mostra-se a inicial.</summary>
        public string? Sprite => Configuracao.NomeSprite(ImagemUrl);
    }

    /// <summary>GET api/colecao</summary>
    public class ColecaoResposta
    {
        public List<ItemColecao> Itens { get; set; } = new List<ItemColecao>();
        public int Ocupado { get; set; }
        public int Capacidade { get; set; }
        public int PersonagensDistintas { get; set; }
        public int PersonagensExistentes { get; set; }
        public int InteracoesRestantes { get; set; }
        public int InteracoesPorDia { get; set; }
    }

    /// <summary>POST api/colecao/favorito</summary>
    public class FavoritoPedido
    {
        public int UtilizadorId { get; set; }
        public int PersonagemId { get; set; }
        public bool Favorito { get; set; }
    }

    /// <summary>POST api/amizade/interagir</summary>
    public class InteragirPedido
    {
        public int UtilizadorId { get; set; }
        public int PersonagemId { get; set; }
    }

    public class InteracaoResposta
    {
        public string Mensagem { get; set; } = string.Empty;
        public string Reacao { get; set; } = string.Empty;
        public int PersonagemId { get; set; }
        public string PersonagemNome { get; set; } = string.Empty;
        public int PontosGanhos { get; set; }
        public int PontosAmizade { get; set; }
        public string NivelAnterior { get; set; } = string.Empty;
        public string NivelAtual { get; set; } = string.Empty;
        public bool SubiuDeNivel { get; set; }
        public bool NivelMaximo { get; set; }
        public int InteracoesRestantes { get; set; }
    }

    // ----- Mensagens de uma personagem (GET api/mensagens/personagem) -----

    public class MensagemPersonagem
    {
        public int Id { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public int NivelOrdem { get; set; }
        public string NivelNome { get; set; } = string.Empty;
        public string? Conteudo { get; set; }
        public bool Desbloqueada { get; set; }
        public bool Alcancavel { get; set; }

        // Apenas para o ecrã
        public string Texto => Desbloqueada
            ? (Conteudo ?? string.Empty)
            : "🔒 Desbloqueia no nível " + NivelOrdem + " (" + NivelNome + ")";

        public string Etiqueta => Tipo + " · nível " + NivelOrdem;

        public Color CorTexto => Desbloqueada ? Color.FromArgb("#efecfa") : Color.FromArgb("#a79fc4");
    }

    public class MensagensPersonagemResposta
    {
        public int PersonagemId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public int NivelOrdem { get; set; }
        public int NivelMaximoOrdem { get; set; }
        public string? Saudacao { get; set; }
        public bool EhCompanheira { get; set; }
        public int NivelMensagensDiarias { get; set; }
        public int Total { get; set; }
        public int Desbloqueadas { get; set; }
        public List<MensagemPersonagem> Mensagens { get; set; } = new List<MensagemPersonagem>();
    }
}
