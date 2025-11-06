using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;


namespace EWOS_MVC.Areas.Requestor.Controllers
{
    [Authorize(Roles = "Requestor,AdminFabrication,AdminSystem,Supervisor")]
    [Area("Requestor")]

    public class DashboardController : BaseController
    {
        public IActionResult Index()
        {
            var chartData = new
            {
                categories = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Agus", "Sep", "Okt", "Nov", "Des" },
                series = new[] { 30, 40, 35, 50, 49, 52, 60, 56, 42, 10, 11, 32 }
            };
            return View(chartData);
        }
        //testing role user
        [Authorize]
        public IActionResult WhoAmI()
        {
            return Json(new
            {
                User = User.Identity.Name,
                Roles = User.Claims
                    .Where(c => c.Type == ClaimTypes.Role)
                    .Select(c => c.Value)
                    .ToList()
            });
        }
    }
}
