using Microsoft.AspNetCore.Mvc;

namespace EWOS_MVC.Controllers
{
    public class WeeksSettingController : Controller
    {
        private readonly AppDbContext _context;

        public WeeksSettingController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("/WeeksSetting")]
        public async Task<IActionResult> Index(int YearSettingId, int MonthSelect)
        {
            return ViewComponent("WeeksSetting", new { YearSettingId, MonthSelect });
        }

    }
}
