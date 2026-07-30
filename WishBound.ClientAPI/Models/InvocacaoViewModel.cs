namespace WishBound.ClientAPI.Models
{
    /// <summary>
    /// ViewModel da página de Invocação: junta as probabilidades (raridades)
    /// e, depois de invocar, a personagem obtida.
    /// </summary>
    public class InvocacaoViewModel
    {
        public List<Raridade> Raridades { get; set; } = new List<Raridade>();
        public Personagem? Resultado { get; set; }
    }
}
