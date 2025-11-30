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
            var allDataItemnewReq = await _context.ItemRequests.ToListAsync();
            var allDataItemRO = await _context.RepeatOrders.ToListAsync();
            var allItemFabrication = await _context.ItemFabrications.ToListAsync();

            var currentYear = DateTime.Now.Year;
            // 1. Group Item Request per bulan
            var itemReqPerMonth = allDataItemnewReq
                .Where(r => r.CreatedAt.Year == currentYear)
                .GroupBy(r => new { r.CreatedAt.Year, r.CreatedAt.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Count = g.Count()
                })
                .ToList();

            // 2. Group Repeat Order per bulan
            var repeatOrderPerMonth = allDataItemRO
                .Where(r => r.CreatedAt.Year == currentYear)
                .GroupBy(r => new { r.CreatedAt.Year, r.CreatedAt.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    TotalQty = g.Sum(x => x.QuantityReq)
                })
                .ToList();

            // 3. Ambil semua bulan unik dari dua sumber data
            var allMonths = itemReqPerMonth
                .Select(x => new { x.Year, x.Month })
                .Union(
                    repeatOrderPerMonth.Select(x => new { x.Year, x.Month })
                )
                .OrderBy(x => x.Year)
                .ThenBy(x => x.Month)
                .ToList();

            // 4. Buat categories dan series
            var monthReq = new List<string>();
            var qtyReqByMonth = new List<int>();

            foreach (var m in allMonths)
            {
                // Nama bulan (Jan, Feb, dst)
                monthReq.Add(
                    new DateTime(m.Year, m.Month, 1).ToString("MMMM")
                );

                // Ambil jumlah Item Request di bulan ini
                int itemCount = itemReqPerMonth
                    .Where(x => x.Year == m.Year && x.Month == m.Month)
                    .Sum(x => x.Count);

                // Ambil jumlah Repeat Order di bulan ini
                int repeatQty = repeatOrderPerMonth
                    .Where(x => x.Year == m.Year && x.Month == m.Month)
                    .Sum(x => x.TotalQty);

                // Gabungkan keduanya
                qtyReqByMonth.Add(itemCount + repeatQty);
            }



            //totalRo request 
            var totalNewReq = allDataItemnewReq.Count();
            var totalRo = allDataItemRO.Sum(q =>q.QuantityReq);
            var calculatenewNRo = totalNewReq + totalRo;

            //Request selesai
            var totalDoneNewReq = allDataItemnewReq.Where(s =>s.Status == "Maspro").Count();
            var totalDoneRo = allDataItemRO.Where(s => s.Status == "Close").Sum(q => q.QuantityReq);
            var calculatFabDone = totalDoneNewReq + totalDoneRo;


            // Ambil raw saving data: per bulan, per kategori
            var savingRaw = allItemFabrication
                .Where(r => r.CreatedAt.Year == currentYear)
                .GroupBy(r => new
                {
                    r.CreatedAt.Year,
                    r.CreatedAt.Month,
                    Category = r.ItemRequest.MachineCategoryId
                })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Category = g.Key.Category,
                    TotalSaving = g.Sum(x => x.TotalSaving)
                })
                .OrderBy(x => x.Year)
                .ThenBy(x => x.Month)
                .ToList();

            // Ambil daftar kategori unik
            var categories = savingRaw
                .Select(x => x.Category)
                .Distinct()
                .ToList();

            // Ambil daftar bulan unik
            var months = savingRaw
                .Select(x => new { x.Year, x.Month })
                .Distinct()
                .OrderBy(x => x.Year)
                .ThenBy(x => x.Month)
                .ToList();

            // --- OUTPUT ---
            var monthLabels = new List<string>();
            var savingByMonth = new List<decimal>();
            var cumulative = new List<decimal>();
            var savingByCategory = new Dictionary<string, List<decimal>>();

            // Inisialisasi dictionary kategori
            foreach (var cat in categories)
            {
                savingByCategory[cat.ToString()] = new List<decimal>();
            }

            decimal runTotal = 0;

            // Loop per bulan
            foreach (var m in months)
            {
                // Nama bulan
                monthLabels.Add(new DateTime(m.Year, m.Month, 1).ToString("MMMM"));

                // Total saving bulan ini (semua kategori)
                decimal totalMonth = savingRaw
                    .Where(x => x.Year == m.Year && x.Month == m.Month)
                    .Sum(x => x.TotalSaving ?? 0);

                savingByMonth.Add(totalMonth);

                runTotal += totalMonth;
                cumulative.Add(runTotal);

                // Isi saving per kategori per bulan
                foreach (var cat in categories)
                {
                    var found = savingRaw.FirstOrDefault(
                        x => x.Year == m.Year && x.Month == m.Month && x.Category == cat
                    );

                    savingByCategory[cat.ToString()].Add(found?.TotalSaving ?? 0);
                }
            }


            // 6. Final chart data
            var chartData = new
            {

                monthReq,
                qtyReqByMonth,
                monthLabels,
                savingByMonth,
                savingByCategory,
                cumulative,
                TotalRequest = calculatenewNRo,
                TotalRequestDone = calculatFabDone,
              

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
