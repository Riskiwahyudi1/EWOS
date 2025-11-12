using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Linq;

namespace InhouseFabricationSystem.ViewComponents
{
    public class RawMaterialsViewComponent : ViewComponent
    {
        private readonly AppDbContext _context;

        public RawMaterialsViewComponent(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync(int? SelectedId = null, int? machineCategoryId = null)
        {
            var query = _context.RawMaterials.AsQueryable();

            // Filter sesuai machineCategoryId 
            if (machineCategoryId.HasValue)
            {
                query = query.Where(r => r.MachineCategoryId == machineCategoryId.Value);
            }

            var rawMaterials = await query
                                .OrderBy(r => r.Name)
                                .ToListAsync();

            ViewData["SelectedId"] = SelectedId;
            return View(rawMaterials);
        }
    }
}