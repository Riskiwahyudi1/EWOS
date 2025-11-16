using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace EWOS_MVC.ViewComponents
{
    public class WeeksSettingViewComponent : ViewComponent
    {
        private readonly AppDbContext _context;

        public WeeksSettingViewComponent(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync(int YearSettingId, int? SelectedId = null, int? MonthSelect = null)
        {
            int currentYear = YearSettingId;
            int currentMonth = MonthSelect ?? DateTime.Now.Month;

            var weeks = await _context.WeeksSetting
                .Include(w => w.YearsSetting)
                .Where(w => w.YearsSetting.Id == currentYear && w.Month == currentMonth)
                .OrderBy(w => w.Week)
                .ToListAsync();

            ViewData["SelectedId"] = SelectedId;
            return View(weeks);
        }

    }
}
