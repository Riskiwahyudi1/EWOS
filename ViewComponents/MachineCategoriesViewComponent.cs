using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;


namespace EWOS_MVC.ViewComponents
{
    public class MachineCategoriesViewComponent : ViewComponent
    {
        private readonly AppDbContext _context;

        public MachineCategoriesViewComponent(AppDbContext context)
        {
            _context = context;
        }
        public async Task<IViewComponentResult> InvokeAsync(int? SelectedId = null)
        {
            var MachineCategories = await _context.MachineCategories
                .OrderBy(c => c.CategoryName)
                .ToListAsync();

            ViewData["SelectedId"] = SelectedId;
            return View(MachineCategories);
        }
    }
}
