using EWOS_MVC.Services;
using Microsoft.AspNetCore.Mvc;

namespace EWOS_MVC.Controllers
{
    public class MonthSettingController : Controller
    {
        private readonly AppDbContext _context;
        private readonly YearsHelper _Yearcontext;

        public MonthSettingController(AppDbContext context, YearsHelper yearsHelper)
        {
            _context = context;
            _Yearcontext = yearsHelper;
        }

        [HttpGet("/MonthSetting")]
        public async Task<IActionResult> Index(int YearSettingId)
        {
            var getYear = await _Yearcontext.GetYearByIdAsync(YearSettingId);

            return ViewComponent(
                "MonthSetting",
                new { SelectedYear = getYear?.Year }
            );
        }


    }
}