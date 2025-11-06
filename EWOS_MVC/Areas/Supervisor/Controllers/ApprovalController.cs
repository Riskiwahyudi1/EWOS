using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EWOS_MVC.Areas.Supervisor.Controllers
{
    [Authorize(Roles = "AdminSystem,Supervisor")]
    [Area("Supervisor")]
    public class ApprovalController : BaseController
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
