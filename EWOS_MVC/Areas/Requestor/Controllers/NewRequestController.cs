using Microsoft.AspNetCore.Mvc;

namespace EWOS_MVC.Areas.Requestor.Controllers
{
    [Area("Requestor")]
    public class NewRequestController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
