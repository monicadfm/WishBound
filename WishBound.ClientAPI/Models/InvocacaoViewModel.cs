namespace WishBound.ClientAPI.Models
{
    /// <summary>
    /// ViewModel da página de Invocação: junta as probabilidades (raridades)
    /// e, depois de invocar, o resultado (personagem obtida, se é nova ou
    /// repetida e o espaço ocupado na coleção).
    /// </summary>
    public class InvocacaoViewModel
    {
        public List<Raridade> Raridades { get; set; } = new List<Raridade>();
        public ResultadoInvocacao? Resultado { get; set; }
    }
}
