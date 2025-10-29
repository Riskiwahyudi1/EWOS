using Microsoft.AspNetCore.Mvc;

namespace EWOS_MVC.Areas.AdminFabrication.Controllers
{
    [Area("AdminFabrication")]
    public class RepeatOrderController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
