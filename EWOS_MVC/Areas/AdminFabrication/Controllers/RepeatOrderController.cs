using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EWOS_MVC.Areas.AdminFabrication.Controllers
{
    [Authorize(Roles = "AdminSystem,AdminFabrication")]
    [Area("AdminSystem")]
    public class RepeatOrderController : BaseController
    {
     
        public IActionResult Index()
        {
            return View();
        }
    }
}
