using Microsoft.AspNetCore.Mvc;

namespace EWOS_MVC.Areas.Requestor.Controllers
{
    [Area("Requestor")]
    public class FabricationHistoryController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
