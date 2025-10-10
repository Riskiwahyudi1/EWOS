using Microsoft.AspNetCore.Mvc;

namespace EWOS_MVC.Areas.AdminSystem.Controllers
{
    [Area("AdminSystem")]
    public class DashboardController : Controller
    {
        
        public IActionResult Index()
        {
            return View();
        }
    }
}
