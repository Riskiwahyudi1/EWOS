using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EWOS_MVC.Areas.Requestor.Controllers
{
    [Authorize(Roles = "Requestor,AdminFabrication,AdminSystem,Supervisor")]
    [Area("Requestor")]
    public class FabricationHistoryController : BaseController
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
