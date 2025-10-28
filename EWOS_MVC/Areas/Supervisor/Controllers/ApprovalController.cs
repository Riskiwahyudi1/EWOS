using Microsoft.AspNetCore.Mvc;

namespace EWOS_MVC.Areas.Supervisor.Controllers
{
    [Area("Supervisor")]
    public class ApprovalController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
