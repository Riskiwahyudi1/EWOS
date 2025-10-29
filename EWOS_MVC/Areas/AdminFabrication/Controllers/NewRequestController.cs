using Microsoft.AspNetCore.Mvc;

namespace EWOS_MVC.Areas.AdminFabrication.Controllers
{
    [Area("AdminFabrication")]
    public class NewRequestController : Controller
    {
        [Route("/AdminFabrication/NewRequest/{status?}")]
        public IActionResult Index()
        {
            return View();
        }
    }
}
