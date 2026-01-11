using EWOS_MVC.Services;
using Microsoft.EntityFrameworkCore;

public class DashboardService
{
    private readonly AppDbContext _context;
    private readonly YearsHelper _yearHelper;
    private readonly CalculateSavingHelper _savingHelper;

    public DashboardService(
        AppDbContext context,
        YearsHelper yearHelper,
        CalculateSavingHelper savingHelper)
    {
        _context = context;
        _yearHelper = yearHelper;
        _savingHelper = savingHelper;
    }

    // ======================================================
    // DATA DARI SERVICE (khusus year 2025)
    // ======================================================
    public async Task<object> GetDashboardFromService(int year, int? categoryId)
    {
        return new
        {
            potentialSaving = 125000000m,

            machineUtilization = new[]
    {
        // CNC (existing – contoh tetap)
        new { week = 1, planJam = 168m, actualJam = 120m, percent = 71.43m },

        // 3D Printing
        new { week = 2, planJam = 168m, actualJam = 195m, percent = 116.1m },
        new { week = 3, planJam = 168m, actualJam = 260m, percent = 154.8m },
        new { week = 4, planJam = 168m, actualJam = 178m, percent = 105.9m },
        new { week = 5, planJam = 168m, actualJam = 310m, percent = 184.5m }
    },

            monthReq = new[] { "January", "February", "March" },

            qtyReqByMonth = new[]
    {
        132,   // CNC + 3D
        113,
        38
    },

            cumulativeReqQty = new[]
    {
        132,
        245,
        283
    },

            monthLabels = new[] { "January", "February", "March" },

            savingByMonth = new[]
    {
        5024500m,
        12018200m,
        6800m
    },

            savingByCategory = new Dictionary<string, decimal[]>
    {
        { "1", new[] { 5000000m, 12000000m, 0m } },   // CNC
        { "2", new[] { 24500m, 18200m, 6800m } }      // 3D Printing
    },

            cumulative = new[]
    {
        5024500m,
        17042700m,
        17049500m
    },

            totalRequest = 283,
            totalRequestDone = 210,
            totalReqReject = 43
        };
    }
    // ======================================================
    // DATA DARI DATABASE
    // ======================================================
    public async Task<object> GetDashboardFromDatabase(int year, int? categoryId)
    {
        var currentYear = await _yearHelper.GetYearByIdAsync(year);
        var yearName = currentYear?.Year;

        // -------------------------------------------------------
        // FILTER DATA
        // -------------------------------------------------------
        var allDataItemnewReq = await _context.ItemRequests
            .Include(x => x.RawMaterials)
            .Include(x => x.MachineCategories)
            .Where(r => r.CreatedAt.Year == yearName &&
                        (categoryId == 0 || r.MachineCategoryId == categoryId))
            .ToListAsync();

        var allDataItemRO = await _context.RepeatOrders
            .Include(x => x.ItemRequests)
                .ThenInclude(x => x.RawMaterials)
            .Include(x => x.ItemRequests)
                .ThenInclude(x => x.MachineCategories)
            .Where(r => r.CreatedAt.Year == yearName &&
                        (categoryId == 0 || r.ItemRequests.MachineCategoryId == categoryId))
            .ToListAsync();

        var allItemFabrication = await _context.ItemFabrications
            .Include(x => x.ItemRequest)
            .Include(x => x.RepeatOrder)
            .Include(x => x.WeeksSetting)
            .Where(r => r.CreatedAt.Year == yearName &&
                        (categoryId == 0 || r.ItemRequest.MachineCategoryId == categoryId))
            .ToListAsync();

        // -------------------------------------------------------
        // TOTAL MACHINE
        // -------------------------------------------------------
        int totalMachine = categoryId == 0
            ? await _context.Machines.Where(m => m.MachineCategoryId != 3).CountAsync()
            : await _context.Machines.Where(m => m.MachineCategoryId != 3 &&
                                                 m.MachineCategoryId == categoryId).CountAsync();

        // -------------------------------------------------------
        // POTENTIAL SAVING
        // -------------------------------------------------------
        var validNewReq = allDataItemnewReq
            .Where(r => r.Status != "Reject" &&
                        r.IsCalculateSaving &&
                        r.MachineCategoryId != 3)
            .ToList();

        var validRo = allDataItemRO
            .Where(r => r.ItemRequests != null &&
                        r.ItemRequests.IsCalculateSaving &&
                        r.ItemRequests.MachineCategoryId != 3)
            .ToList();

        var (savingNew, _) = await _savingHelper.CalculateAll(validNewReq, currentYear);
        var (savingRo, _) = await _savingHelper.CalculateAllRo(validRo, currentYear);

        var potentialSaving = savingNew + savingRo;

        // -------------------------------------------------------
        // REQUEST PER BULAN
        // -------------------------------------------------------
        var itemReqPerMonth = allDataItemnewReq
            .GroupBy(r => new { r.CreatedAt.Year, r.CreatedAt.Month })
            .ToDictionary(g => g.Key, g => g.Count());

        var roReqPerMonth = allDataItemRO
            .GroupBy(r => new { r.CreatedAt.Year, r.CreatedAt.Month })
            .ToDictionary(g => g.Key, g => g.Sum(x => x.QuantityReq));

        var allMonths = itemReqPerMonth.Keys
            .Union(roReqPerMonth.Keys)
            .OrderBy(x => x.Year).ThenBy(x => x.Month)
            .ToList();

        var monthReq = new List<string>();
        var qtyReqByMonth = new List<int>();
        var cumulativeReqQty = new List<int>();

        int runReq = 0;

        foreach (var m in allMonths)
        {
            monthReq.Add(new DateTime(m.Year, m.Month, 1).ToString("MMMM"));

            int total =
                (itemReqPerMonth.ContainsKey(m) ? itemReqPerMonth[m] : 0) +
                (roReqPerMonth.ContainsKey(m) ? roReqPerMonth[m] : 0);

            qtyReqByMonth.Add(total);
            runReq += total;
            cumulativeReqQty.Add(runReq);
        }

        // -------------------------------------------------------
        // TOTAL REQUEST
        // -------------------------------------------------------
        var totalRequest = allDataItemnewReq.Count + allDataItemRO.Sum(x => x.QuantityReq);
        var totalRequestDone =
            allDataItemnewReq.Count(x => x.Status == "Maspro" || x.Status == "Fail") +
            allDataItemRO.Where(x => x.Status == "Close" || x.Status == "Done")
                         .Sum(x => x.QuantityReq);

        var totalReqReject = allDataItemnewReq.Count(x => x.Status == "Reject");

        // -------------------------------------------------------
        // SAVING PER BULAN & CATEGORY
        // -------------------------------------------------------
        var savingRaw = allItemFabrication
            .Where(r => r.Status == "FabricationDone")
            .GroupBy(r => new
            {
                r.CreatedAt.Year,
                r.CreatedAt.Month,
                Category = r.ItemRequest?.MachineCategoryId ?? 0
            })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                g.Key.Category,
                TotalSaving = g.Sum(x => x.TotalSaving ?? 0)
            })
            .OrderBy(x => x.Year).ThenBy(x => x.Month)
            .ToList();

        var monthLabels = new List<string>();
        var savingByMonth = new List<decimal>();
        var cumulative = new List<decimal>();
        var savingByCategory = new Dictionary<string, List<decimal>>();

        var categories = savingRaw.Select(x => x.Category).Distinct();

        foreach (var c in categories)
            savingByCategory[c.ToString()] = new List<decimal>();

        decimal runSaving = 0;

        foreach (var m in savingRaw.Select(x => new { x.Year, x.Month }).Distinct())
        {
            monthLabels.Add(new DateTime(m.Year, m.Month, 1).ToString("MMMM"));

            var totalMonth = savingRaw
                .Where(x => x.Year == m.Year && x.Month == m.Month)
                .Sum(x => x.TotalSaving);

            savingByMonth.Add(totalMonth);

            runSaving += totalMonth;
            cumulative.Add(runSaving);

            foreach (var c in categories)
            {
                var found = savingRaw.FirstOrDefault(
                    x => x.Year == m.Year && x.Month == m.Month && x.Category == c);

                savingByCategory[c.ToString()].Add(found?.TotalSaving ?? 0);
            }
        }

        // -------------------------------------------------------
        // MACHINE UTILIZATION (MENIT → JAM)
        // -------------------------------------------------------
        var machineUtilization = allItemFabrication
            .Where(r => r.Status == "FabricationDone")
            .GroupBy(r => r.WeeksSettingId)
            .Select(g =>
            {
                var planJam = g.First().WeeksSetting.WorkingDays * 24 * totalMachine;
                var actualJam = g.Sum(x => x.FabricationTime) / 60m;

                return new
                {
                    week = g.First().WeeksSetting.Week,
                    planJam,
                    actualJam,
                    percent = planJam == 0 ? 0 : Math.Round((actualJam / planJam) * 100, 2)
                };
            })
            .OrderBy(x => x.week)
            .ToList();

        // -------------------------------------------------------
        // RETURN FINAL JSON
        // -------------------------------------------------------
        return new
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
            totalRequest,
            totalRequestDone,
            totalReqReject
        };
    }
}
