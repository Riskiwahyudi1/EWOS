using Microsoft.AspNetCore.Mvc;

namespace EWOS_MVC.Areas.Requestor.Controllers
{
    [Area("Requestor")]

    public class MyRequestController : Controller
    {
        [Route("/Requestor/MyRequest/{status?}")]
        public IActionResult Index()
        {
            return View();
        }
    }
}
