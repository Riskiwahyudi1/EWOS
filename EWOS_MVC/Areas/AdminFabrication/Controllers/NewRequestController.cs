using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EWOS_MVC.Areas.AdminFabrication.Controllers
{
    [Authorize(Roles = "AdminSystem,AdminFabrication")]
    [Area("AdminSystem")]
    public class NewRequestController : BaseController
    {
        [Route("/AdminFabrication/NewRequest/{status?}")]
        public IActionResult Index()
        {
            return View();
        }
    }
}
