using Microsoft.AspNetCore.Mvc;

namespace EWOS_MVC.Areas.Requestor.Controllers
{
    [Area("Requestor")]
    public class ListProductController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
