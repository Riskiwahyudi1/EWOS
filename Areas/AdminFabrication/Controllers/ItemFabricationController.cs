using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EWOS_MVC.Areas.AdminFabrication.Controllers
{
    [Authorize(Roles = "AdminSystem,AdminFabrication")]
    [Area("AdminSystem")]
    public class ItemFabricationController : BaseController
    {

        [Route("/AdminFabrication/ItemFabrication/{status?}")]
        public IActionResult Index()
        {
            return View();
        }
        
    }
}
