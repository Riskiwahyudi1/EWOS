using EWOS_MVC.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Threading.Tasks;

public class HomeController : BaseController
{
    private readonly ILogger<HomeController> _logger;
    private readonly AppDbContext _context;
    public HomeController(ILogger<HomeController> logger, AppDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var getTotalNewReq = await _context.ItemRequests.CountAsync();
        var getTotalRo = await _context.RepeatOrders.SumAsync(qty => qty.QuantityReq);
        var getTotalFabDone = await _context.ItemFabrications.SumAsync(qty => qty.Quantity);

        ViewBag.TotalReqNew = getTotalNewReq + getTotalRo;
        ViewBag.TotalRO = getTotalRo;
        ViewBag.TotalFab = getTotalFabDone;

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
