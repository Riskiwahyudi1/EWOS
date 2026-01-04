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
            int tahunSekarang = DateTime.Now.Year;
            // 1. Hitung jumlah ItemRequest per bulan
            var allDataItemnewReq = await _context.ItemRequests.Where(x => x.CreatedAt.Year == tahunSekarang).ToListAsync();
            var allDataItemRO = await _context.RepeatOrders.Where(x => x.CreatedAt.Year == tahunSekarang).ToListAsync();
            var allItemFabrication = await _context.ItemFabrications.Where(x => x.CreatedAt.Year == tahunSekarang).ToListAsync();
            var allWeeks = await _context.WeeksSetting.ToListAsync();
            var totalMachine = await _context.Machines.Where(mc => mc.MachineCategoryId != 3).CountAsync();


            var currentYear = DateTime.Now.Year;

            // -------------------------------------------------------
            // POTENTIAL SAVING
            // -------------------------------------------------------
            // New Req
            var totalPotentialSavingCost = allDataItemnewReq
                .Where(r => r.CreatedAt.Year == currentYear && r.IsCalculateSaving == true)
                .Sum(r => r.ExternalFabCost ?? 0);
            //Ro
            var totalPotentialSavingCostRo = allDataItemRO
               .Where(r => r.CreatedAt.Year == currentYear && r.ItemRequests.IsCalculateSaving == true)
               .Sum(r => r.QuantityReq * r.ItemRequests.ExternalFabCost);

            ////sudah di fabrikasi
            //var totalFabrikasiDone = allItemFabrication
            //    .Where(r => r.CreatedAt.Year == currentYear)
            //    .Sum(r => r.TotalSaving ?? 0);

            //jumlahkan
            var potentialSaving = totalPotentialSavingCost + totalPotentialSavingCostRo;



            // -------------------------------------------------------
            // REQUEST PER BULAN
            // -------------------------------------------------------
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
            var cumulativeReqQty = new List<int>();
            int runningTotal = 0;

            foreach (var m in allMonths)
            {
                // Nama bulan (Jan, Feb, dst)
                monthReq.Add(new DateTime(m.Year, m.Month, 1).ToString("MMMM"));

                // Ambil jumlah Item Request di bulan ini
                int itemCount = itemReqPerMonth
                    .Where(x => x.Year == m.Year && x.Month == m.Month)
                    .Sum(x => x.Count);

                // Ambil jumlah Repeat Order di bulan ini
                int repeatQty = repeatOrderPerMonth
                    .Where(x => x.Year == m.Year && x.Month == m.Month)
                    .Sum(x => x.TotalQty);

                // Gabungkan keduanya
                int total = itemCount + repeatQty;
                qtyReqByMonth.Add(total);

                // Hitung cumulative
                runningTotal += total;
                cumulativeReqQty.Add(runningTotal);
            }


            // -------------------------------------------------------
            // TOTAL REQUEST DONE
            // -------------------------------------------------------
            //totalRo request 
            var totalNewReq = allDataItemnewReq.Count();
            var totalRejectReq = allDataItemnewReq.Where(s => s.Status == "Reject").Count();
            var totalRo = allDataItemRO.Sum(q => q.QuantityReq);
            var calculatenewNRo = totalNewReq + totalRo;

            //Request selesai
            var totalDoneNewReq = allDataItemnewReq.Where(s => s.Status == "Maspro").Count();
            var totalDoneRo = allDataItemRO.Where(s => s.Status == "Close" || s.Status == "Done").Sum(q => q.QuantityReq);
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
            // -------------------------------------------------------
            // MACHINE UTILIZATION
            // -------------------------------------------------------
            var machineUtilization = allItemFabrication
                .Where(x => x.CreatedAt.Year == currentYear)
                .GroupBy(x => x.WeeksSettingId)
                .Select(g => new
                {
                    Week = g.First().WeeksSetting.Week,

                    // PLAN (jam rencana fabrikasi)
                    PlanJam = (decimal)(g.First().WeeksSetting.WorkingDays * 24 * totalMachine),

                    // ACTUAL (jam fabrikasi)
                    ActualJam = g.Sum(x => x.FabricationTime),

                    // UTILIZATION %
                    Percent = (g.First().WeeksSetting.WorkingDays * 24 * totalMachine) == 0
                        ? 0
                        : Math.Round(
                            (g.Sum(x => x.FabricationTime) /
                            (decimal)(g.First().WeeksSetting.WorkingDays * 24 * totalMachine)) * 100, 2)
                })
                .OrderBy(x => x.Week)
                .ToList();


            var chartData = new
            {
                potentialSaving,
                machineUtilization,
                monthReq,
                qtyReqByMonth,
                cumulativeReqQty,
                monthLabels,
                savingByMonth,
                savingByCategory,
                cumulative,
                TotalRequest = calculatenewNRo,
                TotalReqReject = totalRejectReq,
                TotalRequestDone = calculatFabDone,


            };
            return View(chartData);
        }

        [HttpGet]
        public async Task<IActionResult> Filter(int year, int categoryId)
        {
            var currentYear = await _yearsHelper.GetYearByIdAsync(year);
            var yearName = currentYear?.Year;

            // 1. Filter data sesuai year + category
            var allDataItemnewReq = await _context.ItemRequests
                .Where(r => r.CreatedAt.Year == yearName &&
                            (categoryId == 0 || r.MachineCategoryId == categoryId))
                .ToListAsync();

            var allDataItemRO = await _context.RepeatOrders
                .Where(r => r.CreatedAt.Year == yearName &&
                            (categoryId == 0 || r.ItemRequests.MachineCategoryId == categoryId))
                .ToListAsync();

            var allItemFabrication = await _context.ItemFabrications
                .Where(r => r.CreatedAt.Year == yearName &&
                            (categoryId == 0 || r.ItemRequest.MachineCategoryId == categoryId))
                .Include(r => r.WeeksSetting)
                .ToListAsync();

            var totalMachine = await _context.Machines
                .Where(mc => mc.MachineCategoryId != 3 && mc.MachineCategoryId == categoryId)
                .CountAsync();

            // -------------------------------------------------------
            // POTENTIAL SAVING
            // -------------------------------------------------------
            var totalPotentialSavingCost = allDataItemnewReq.Sum(r => r.ExternalFabCost ?? 0);

            var totalPotentialSavingCostRo = allDataItemRO
                .Where(r => r.ItemRequests.IsCalculateSaving == true)
                .Sum(r => r.QuantityReq * r.ItemRequests.ExternalFabCost);

            var totalFabrikasiDone = allItemFabrication.Sum(r => r.TotalSaving ?? 0);

            var potentialSaving = totalPotentialSavingCost +
                                  totalPotentialSavingCostRo +
                                  totalFabrikasiDone;


            // -------------------------------------------------------
            // REQUEST PER BULAN
            // -------------------------------------------------------
            var itemReqPerMonth = allDataItemnewReq
                .GroupBy(r => new { r.CreatedAt.Year, r.CreatedAt.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
                .ToList();

            var repeatOrderPerMonth = allDataItemRO
                .GroupBy(r => new { r.CreatedAt.Year, r.CreatedAt.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, TotalQty = g.Sum(x => x.QuantityReq) })
                .ToList();

            var allMonths = itemReqPerMonth
                .Select(x => new { x.Year, x.Month })
                .Union(repeatOrderPerMonth.Select(x => new { x.Year, x.Month }))
                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .ToList();

            var monthReq = new List<string>();
            var qtyReqByMonth = new List<int>();
            var cumulativeReqQty = new List<int>();
            int runningTotal = 0;

            foreach (var m in allMonths)
            {
                monthReq.Add(new DateTime(m.Year, m.Month, 1).ToString("MMMM"));

                int itemCount = itemReqPerMonth
                    .Where(x => x.Year == m.Year && x.Month == m.Month)
                    .Sum(x => x.Count);

                int repeatQty = repeatOrderPerMonth
                    .Where(x => x.Year == m.Year && x.Month == m.Month)
                    .Sum(x => x.TotalQty);

                // Gabungkan keduanya
                int total = itemCount + repeatQty;
                qtyReqByMonth.Add(total);

                // Hitung cumulative
                runningTotal += total;
                cumulativeReqQty.Add(runningTotal);
            }

            // -------------------------------------------------------
            // TOTAL REQUEST DONE
            // -------------------------------------------------------
            var totalNewReq = allDataItemnewReq.Count();
            var totalRejectReq = allDataItemnewReq.Where(s => s.Status == "Reject").Count();
            var totalRo = allDataItemRO.Sum(q => q.QuantityReq);
            var calculatenewNRo = totalNewReq + totalRo;

            var totalDoneNewReq = allDataItemnewReq.Where(s => s.Status == "Maspro").Count();
            var totalDoneRo = allDataItemRO.Where(s => s.Status == "Close" || s.Status == "Done").Sum(q => q.QuantityReq);
            var calculatFabDone = totalDoneNewReq + totalDoneRo;


            // -------------------------------------------------------
            // SAVING COST/MONTH
            // -------------------------------------------------------
            var savingRaw = allItemFabrication
                .GroupBy(r => new { r.CreatedAt.Year, r.CreatedAt.Month, Category = r.ItemRequest.MachineCategoryId })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    Category = g.Key.Category,
                    TotalSaving = g.Sum(x => x.TotalSaving)
                })
                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .ToList();

            var categories = savingRaw.Select(x => x.Category).Distinct().ToList();

            var months = savingRaw
                .Select(x => new { x.Year, x.Month })
                .Distinct()
                .OrderBy(x => x.Year)
                .ThenBy(x => x.Month)
                .ToList();

            var monthLabels = new List<string>();
            var savingByMonth = new List<decimal>();
            var cumulative = new List<decimal>();
            var savingByCategory = new Dictionary<string, List<decimal>>();

            foreach (var cat in categories)
                savingByCategory[cat.ToString()] = new List<decimal>();

            decimal runTotal = 0;

            foreach (var m in months)
            {
                monthLabels.Add(new DateTime(m.Year, m.Month, 1).ToString("MMMM"));

                decimal totalMonth = savingRaw
                    .Where(x => x.Year == m.Year && x.Month == m.Month)
                    .Sum(x => x.TotalSaving ?? 0);

                savingByMonth.Add(totalMonth);

                runTotal += totalMonth;
                cumulative.Add(runTotal);

                foreach (var cat in categories)
                {
                    var found = savingRaw.FirstOrDefault(
                        x => x.Year == m.Year && x.Month == m.Month && x.Category == cat);

                    savingByCategory[cat.ToString()].Add(found?.TotalSaving ?? 0);
                }
            }

            // -------------------------------------------------------
            // MACHINE UTILIZATION
            // -------------------------------------------------------
            var machineUtilization = allItemFabrication
                .GroupBy(x => x.WeeksSettingId)
                .Select(g => new
                {
                    Week = g.First().WeeksSetting.Week,

                    // PLAN (jam rencana fabrikasi)
                    PlanJam = (decimal)(g.First().WeeksSetting.WorkingDays * 24 * totalMachine),

                    // ACTUAL (jam fabrikasi)
                    ActualJam = g.Sum(x => x.FabricationTime),

                    // UTILIZATION %
                    Percent = (g.First().WeeksSetting.WorkingDays * 24 * totalMachine) == 0
                        ? 0
                        : Math.Round(
                            (g.Sum(x => x.FabricationTime) /
                            (decimal)(g.First().WeeksSetting.WorkingDays * 24 * totalMachine)) * 100, 2)
                })
                .OrderBy(x => x.Week)
                .ToList();



            // -------------------------------------------------------
            // RETURN JSON
            // -------------------------------------------------------
            return Json(new
            {
                potentialSaving,
                machineUtilization,
                monthReq,
                cumulativeReqQty,
                qtyReqByMonth,
                monthLabels,
                savingByMonth,
                savingByCategory,
                cumulative,
                TotalRequest = calculatenewNRo,
                TotalRequestDone = calculatFabDone,
                TotalReqReject = totalRejectReq,
            });
        }


        //testing role user
        //[Authorize]
        //public IActionResult WhoAmI()
        //{
        //    return Json(new
        //    {
        //        User = User.Identity.Name,
        //        Roles = User.Claims
        //            .Where(c => c.Type == ClaimTypes.Role)
        //            .Select(c => c.Value)
        //            .ToList()
        //    });
        //}
    }
}