using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Linq;

namespace EWOS_MVC.ViewComponents
{
    public class YearsSettingViewComponent : ViewComponent
    {
        private readonly AppDbContext _context;

        public YearsSettingViewComponent(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync(int? SelectedId = null)
        {
            var query = _context.YearsSetting.AsQueryable();


            var Years = await query
                                .OrderBy(r => r.Year)
                                .ToListAsync();

            ViewData["SelectedId"] = SelectedId;
            return View(Years);
        }
    }
}
