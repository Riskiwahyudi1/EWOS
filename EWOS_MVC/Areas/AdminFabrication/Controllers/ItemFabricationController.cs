using Microsoft.AspNetCore.Mvc;

namespace EWOS_MVC.Areas.AdminFabrication.Controllers
{
    [Area("AdminFabrication")]
    public class ItemFabricationController : Controller
    {

        [Route("/AdminFabrication/ItemFabrication/{status?}")]
        public IActionResult Index()
        {
            return View();
        }
    }
}
