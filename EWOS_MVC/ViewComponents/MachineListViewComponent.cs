using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace EWOS_MVC.ViewComponents
{
    public class MachineListViewComponent : ViewComponent
    {
        private readonly AppDbContext _context;

        public MachineListViewComponent(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync(int? SelectedId = null, int? MachineCategoryId = null)
        {
            var mcCategoryId = SelectedId ?? MachineCategoryId;

            var MachineList = await _context.Machines
                .Where(m => m.MachineCategoryId == mcCategoryId)
                .ToListAsync();

            ViewData["SelectedId"] = SelectedId;
            return View(MachineList);
        }
    }
}
