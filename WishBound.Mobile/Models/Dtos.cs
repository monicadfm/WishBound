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
