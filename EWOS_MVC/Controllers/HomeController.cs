using EWOS_MVC.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

public class HomeController : BaseController
{
    private readonly ILogger<HomeController> _logger;
    public readonly AppDbContext _context;

    public HomeController(ILogger<HomeController> logger, AppDbContext context)
    {
        _logger = logger;
        _context = context;
    }


    public async Task<IActionResult> Index()
    {
        var summary = await _context.ItemRequests
    .Select(_ => new
    {
        TotalNewReq = _context.ItemRequests.Count(),
        TotalRoReq = _context.RepeatOrders.Count(),
        TotalRoQty = _context.RepeatOrders.Sum(x => (int?)x.QuantityReq) ?? 0,
        TotalFabDone = _context.ItemFabrications.Sum(x => (int?)x.Quantity) ?? 0,
        TotalSaving = _context.ItemFabrications.Sum(x => (decimal?)x.TotalSaving) ?? 0
    })
    .FirstOrDefaultAsync();

        // ---- NULL SAFE MAPPING KE VIEWBAG ----
        int totalNewReq = summary?.TotalNewReq ?? 0;
        int totalRoReq = summary?.TotalRoReq ?? 0;
        int totalRoQty = summary?.TotalRoQty ?? 0;
        int totalFabDone = summary?.TotalFabDone ?? 0;
        decimal totalSaving = summary?.TotalSaving ?? 0m;

        ViewBag.TotalReq = totalNewReq + totalRoReq;
        ViewBag.TotalQty = totalRoQty + totalNewReq;
        ViewBag.TotalQtyFab = totalFabDone;
        ViewBag.TotalSavingFab = totalSaving;



        return View();

    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
