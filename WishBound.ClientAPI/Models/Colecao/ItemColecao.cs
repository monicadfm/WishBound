namespace WishBound.ClientAPI.Models.Colecao
{
    /// <summary>
    /// Uma personagem da coleção do utilizador, tal como a WebAPI a devolve
    /// (espelho do DTO ItemColecaoResposta).
    /// </summary>
    public class ItemColecao
    {
        public int PersonagemId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? Descricao { get; set; }
        public string? ImagemUrl { get; set; }

        public int RaridadeId { get; set; }
        public string RaridadeNome { get; set; } = "Desconhecida";
        public string? RaridadeCor { get; set; }
        public int RaridadeOrdem { get; set; }

        public int Quantidade { get; set; }
        public bool IsFavorito { get; set; }
        public DateTime DataObtencao { get; set; }

        /// <summary>Cor da raridade com um valor de recurso quando não há nenhuma.</summary>
        public string Cor => string.IsNullOrEmpty(RaridadeCor) ? "#9aa5b1" : RaridadeCor;

        /// <summary>Imagem da personagem ou a silhueta "desconhecido".</summary>
        public string Imagem => string.IsNullOrEmpty(ImagemUrl) ? "/img/personagens/desconhecido.svg" : ImagemUrl;

        /// <summary>Cópias a mais da mesma personagem.</summary>
        public int Repetidas => Quantidade - 1;
    }
}
