using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WishBound.ClientAPI.Controllers
{
    /// <summary>
    /// O sistema de amizade vive na página da Coleção (ColecaoController:
    /// Interagir, EquiparTitulo, EquiparMoldura + prateleiras na view).
    /// Este controller só redireciona /Amizade para lá, para as ligações
    /// antigas não darem 404. Pode ser apagado juntamente com Views/Amizade.
    /// </summary>
    [Authorize]
    public class AmizadeController : Controller
    {
        // GET: /Amizade  ->  /Colecao#amizade
        public IActionResult Index()
        {
            return Redirect(Url.Action("Index", "Colecao") + "#amizade");
        }
    }
}
