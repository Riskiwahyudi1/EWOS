using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EWOS_MVC.Areas.AdminSystem.Controllers
{

    [Authorize(Roles = "AdminSystem")]
    [Area("AdminSystem")]
    public class DashboardController : BaseController
    {
        
        public IActionResult Index()
        {
            return View();
        }
    }
}
