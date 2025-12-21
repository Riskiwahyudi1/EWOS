using Microsoft.AspNetCore.Mvc;

namespace EWOS_MVC.Controllers
{
    public class MachineListController : Controller
    {
        private readonly AppDbContext _context;

        public MachineListController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("/MachineList")]
        public async Task<IActionResult> Index(int MachineCategoryId)
        {
            return ViewComponent("MachineList", new { MachineCategoryId });
        }
    }
}