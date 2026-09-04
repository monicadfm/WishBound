using WishBound.ClientAPI.Models.Amizade;

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

        // ----- Amizade com esta personagem -----
        public int PontosAmizade { get; set; }
        public int NivelAmizadeId { get; set; }
        public string NivelAmizadeNome { get; set; } = "Desconhecido";
        public int NivelOrdem { get; set; } = 1;
        public int NivelMaximoOrdem { get; set; } = 3;
        public string NivelMaximoNome { get; set; } = string.Empty;
        public int PontosNivelAtual { get; set; }
        public int? PontosProximoNivel { get; set; }
        public DateOnly? UltimaInteracao { get; set; }

        public bool NivelMaximo => !PontosProximoNivel.HasValue;
        public int PercentagemAmizade => ProgressoAmizade.Percentagem(PontosAmizade, PontosNivelAtual, PontosProximoNivel);
        public string ProgressoAmizadeTexto => ProgressoAmizade.Texto(PontosAmizade, PontosProximoNivel, NivelMaximoNome);

        /// <summary>"Nível 2 de 5" — para o cartão.</summary>
        public string NivelTexto => "Nível " + NivelOrdem + " de " + NivelMaximoOrdem;

        /// <summary>Cor da raridade com um valor de recurso quando não há nenhuma.</summary>
        public string Cor => string.IsNullOrEmpty(RaridadeCor) ? "#9aa5b1" : RaridadeCor;

        /// <summary>Imagem da personagem ou a silhueta "desconhecido".</summary>
        public string Imagem => string.IsNullOrEmpty(ImagemUrl) ? "/img/personagens/desconhecido.svg" : ImagemUrl;

        /// <summary>Cópias a mais da mesma personagem.</summary>
        public int Repetidas => Quantidade - 1;
    }
}
