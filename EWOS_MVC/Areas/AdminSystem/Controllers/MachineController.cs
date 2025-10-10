using Microsoft.AspNetCore.Mvc;

namespace EWOS_MVC.Areas.AdminSystem.Controllers
{
    [Area("AdminSystem")]
    public class MachineController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
