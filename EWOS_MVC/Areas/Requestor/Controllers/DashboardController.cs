using Microsoft.AspNetCore.Mvc;

namespace EWOS_MVC.Areas.Requestor.Controllers
{
    [Area("Requestor")]
    public class DashboardController : Controller
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
    }
}
