using WishBound.ClientAPI.Models.Amizade;

namespace WishBound.ClientAPI.Models.Colecao
{
    /// <summary>
    /// Página de detalhes de uma personagem da coleção: os dados da
    /// personagem e, por baixo da descrição, a amizade com ela (nível,
    /// progresso, o que cada nível dá e as recompensas para equipar).
    /// </summary>
    public class DetalhesColecaoViewModel
    {
        public ItemColecao Item { get; set; } = new ItemColecao();

        /// <summary>null quando a API de amizade não respondeu (ex.: Migracao04/05 por correr).</summary>
        public AmizadePersonagem? Amizade { get; set; }

        /// <summary>
        /// Mensagens da personagem: a saudação de hoje e o conjunto com o que
        /// já está desbloqueado. null quando a API não respondeu (ex.:
        /// Migracao07 por correr).
        /// </summary>
        public MensagensPersonagem? Mensagens { get; set; }
    }
}
