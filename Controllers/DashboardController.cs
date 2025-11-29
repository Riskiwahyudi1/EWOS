using EWOS_MVC.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;


namespace EWOS_MVC.Controllers
{
    [Authorize(Roles = "Requestor,AdminFabrication,AdminSystem,Supervisor")]

    public class DashboardController : BaseController
    {
        private readonly AppDbContext _context;
        private readonly YearsHelper _yearsHelper;
        public DashboardController(AppDbContext context, YearsHelper yearsHelper)
        {
            _context = context;
            _yearsHelper = yearsHelper;
        }
        public async Task<IActionResult> Index()
        {
            // 1. Hitung jumlah ItemRequest per bulan
            var itemReqPerMonth = await _context.ItemRequests
                .GroupBy(r => r.CreatedAt.Month)
                .Select(g => new
                {
                    Month = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();

            // 2. Hitung total QuantityReq dari RepeatOrder per bulan
            var repeatOrderPerMonth = await _context.RepeatOrders
                .GroupBy(r => r.CreatedAt.Month)
                .Select(g => new
                {
                    Month = g.Key,
                    TotalQty = g.Sum(x => x.QuantityReq)
                })
                .ToListAsync();

            // 3. Siapkan array 12 bulan
            int[] series = new int[12];

            // 4. Masukkan ItemRequest ke array
            foreach (var row in itemReqPerMonth)
            {
                series[row.Month - 1] += row.Count;
            }

            // 5. Masukkan RepeatOrder ke array
            foreach (var row in repeatOrderPerMonth)
            {
                series[row.Month - 1] += row.TotalQty;
            }

            var totalReq = _context.ItemRequests.Count();
            // 6. Final chart data
            var chartData = new
            {
                categories = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Agus", "Sep", "Okt", "Nov", "Des" },
                series = series,
                tes = totalReq
            };
            return View(chartData);
        }

        [HttpGet]
        public async Task<IActionResult> Filter(int year, int categoryId)
        {
            var Getyears = await _yearsHelper.GetYearByIdAsync(year);

            var yearName = Getyears.Year;

            // 1. Query ItemRequest per bulan
            var itemReqPerMonth = await _context.ItemRequests
                .Where(x => x.CreatedAt.Year == yearName && x.MachineCategoryId == categoryId)
                .GroupBy(x => x.CreatedAt.Month)
                .Select(g => new { Month = g.Key, Count = g.Count() })
                .ToListAsync();

            // 2. Query RepeatOrder per bulan
            var repeatOrderPerMonth = await _context.RepeatOrders
                .Where(x => x.CreatedAt.Year == yearName && x.ItemRequests.MachineCategoryId == categoryId)
                .GroupBy(x => x.CreatedAt.Month)
                .Select(g => new { Month = g.Key, Qty = g.Sum(x => x.QuantityReq) })
                .ToListAsync();


            // 3. Gabungkan
            int[] series = new int[12];

            foreach (var r in itemReqPerMonth)
                series[r.Month - 1] += r.Count;

            foreach (var r in repeatOrderPerMonth)
                series[r.Month - 1] += r.Qty;

            var categories = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Agus", "Sep", "Okt", "Nov", "Des" };

            return Json(new { categories, series });
        }

        //testing role user
        [Authorize]
        public IActionResult WhoAmI()
        {
            return Json(new
            {
                User = User.Identity.Name,
                Roles = User.Claims
                    .Where(c => c.Type == ClaimTypes.Role)
                    .Select(c => c.Value)
                    .ToList()
            });
        }
    }
}
