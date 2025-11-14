using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EWOS_MVC.Areas.Requestor.Controllers
{
    [Authorize(Roles = "Requestor,AdminFabrication,AdminSystem,Supervisor")]
    [Area("Requestor")]
    public class FabricationHistoryController : BaseController
    {
        private readonly AppDbContext _context;
        public FabricationHistoryController(AppDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Index()
        {
            var fabricationList = await _context.ItemFabrications
                    .Include(ir => ir.ItemRequest)
                    .ToListAsync();
            return View(fabricationList);
        }
    }
}
