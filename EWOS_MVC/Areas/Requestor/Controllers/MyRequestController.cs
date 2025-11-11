using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EWOS_MVC.Areas.Requestor.Controllers
{
    [Authorize(Roles = "Requestor,AdminFabrication,AdminSystem,Supervisor")]
    [Area("Requestor")]
    public class MyRequestController : BaseController
    {
        private readonly AppDbContext _context;
        public MyRequestController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [Route("/Requestor/MyRequest/{status}")]
        public async Task<IActionResult> Index(string status)
        {
            int userId = ViewBag.Id != null ? Convert.ToInt32(ViewBag.Id) : 0;

            var baseQuery = _context.ItemRequests
                .Include(mc => mc.MachineCategories)
                .Include(u => u.Users)
                .Include(rs => rs.RequestStatus)
                    .ThenInclude(u => u.Users)
                .Where(u => u.UserId == userId)
                .AsQueryable();

            var statusSummary = await baseQuery
                .GroupBy(r => r.Status ?? "Unknown")
                .ToDictionaryAsync(g => g.Key, g => g.Count());

            ViewBag.StatusSummary = statusSummary;

            if (!string.IsNullOrEmpty(status))
            {
                baseQuery = baseQuery.Where(r => r.Status.ToLower() == status.ToLower());
            }

            var myRequest = await baseQuery.ToListAsync();

            return View(myRequest);
        }

    }
}