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
    }
}
