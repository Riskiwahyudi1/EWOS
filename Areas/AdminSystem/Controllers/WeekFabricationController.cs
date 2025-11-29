using EWOS_MVC.Controllers;
using EWOS_MVC.Models;
using EWOS_MVC.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static System.Runtime.InteropServices.JavaScript.JSType;

// defauld YearsSettingId di bagian generate week masih belum ada, bisa generate jika filternya di klik
namespace EWOS_MVC.Areas.AdminSystem.Controllers
{
    [Authorize(Roles = "AdminSystem")]
    [Area("AdminSystem")]
    public class WeekFabricationController : BaseController
    {
        private readonly AppDbContext _context;
        private readonly YearsHelper _yearHelper;

        private const int PageSize = 10;
        public WeekFabricationController(AppDbContext context, YearsHelper yearHelper)
        {
            _context = context;
            _yearHelper = yearHelper;
        }
        public async Task<IActionResult> Index(int page = 1, string search = "")
        {

            // Ambil data awal
            var query = _context.WeeksSetting.AsQueryable();

            // Filter kalau ada kata pencarian
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(w =>
                    w.Week.ToString().Contains(search) ||
                    w.WorkingDays.ToString().Contains(search) ||
                    w.StartDate.ToString().Contains(search) ||
                    w.EndDate.ToString().Contains(search));
            }


            // Hitung total data
            int totalItems = query.Count();

            // Ambil data per halaman
            var weeks = query
                .OrderBy(w => w.Week)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            // Hitung total halaman
            var totalPages = (int)Math.Ceiling(totalItems / (double)PageSize);

            //ambil id tahun sekarang
            var getYear = await  _yearHelper.GetCurrentYearAsync();


            // Kirim data ke ViewBag
            ViewBag.YearId = getYear.Id;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.Search = search;

            return View(weeks);
        }

        [HttpGet]
        public IActionResult Search(string keyword, int? YearSettingId)
        {
            try
            {
                var query = _context.WeeksSetting.AsQueryable();

                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    query = query.Where(w =>
                        w.Week.ToString().Contains(keyword) ||
                        w.WorkingDays.ToString().Contains(keyword)
                    );
                }

                if (YearSettingId.HasValue)
                {
                    query = query.Where(r => r.YearSettingId == YearSettingId.Value);
                }

                var result = query
                    .OrderBy(w => w.Week)
                    .Select(w => new
                    {
                        id = w.Id,
                        week = w.Week,
                        dayCount = w.WorkingDays,
                        startDate = w.StartDate.ToString("yyyy-MM-dd HH:mm"),
                        endDate = w.EndDate.ToString("yyyy-MM-dd HH:mm")
                    })
                    .Take(10)
                    .ToList();

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new { error = true, message = ex.Message });
            }
        }
        //Edit data
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(WeeksSettingModel w)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);

                TempData["Error"] = "Terjadi kesalahan: " + string.Join(", ", errors);
                return View("Index");
            }

            var getWeek = await _context.WeeksSetting.FindAsync(w.Id);
            if (getWeek == null)
            {
                TempData["Error"] = "data week tidak ditemukan";
                return View("Index");
            }

            getWeek.WorkingDays = w.WorkingDays;
            getWeek.Month = w.Month;
            getWeek.StartDate = w.StartDate;
            getWeek.EndDate = w.EndDate;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Data berhasil diubah.";
            return RedirectToAction("Index");
        }

        //Generate Week
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateWeek(int YearSettingId)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);

                TempData["Error"] = "Terjadi kesalahan: " + string.Join(", ", errors);
                return View("Index");
            }

            var getYear = await _context.YearsSetting.FirstOrDefaultAsync(y => y.Id == YearSettingId);

            if(getYear == null)
            {
                TempData["Error"] = "Data tahun tidak ditemukan";
                return View("Index");
            }

            //generate minggu jika belum ada
          
                var weekStart = getYear.StartDate;
                int weekNumber = 1;
                var akhirTahun = new DateTime(getYear.Year, 12, 31, 23, 59, 59);

                while (weekStart <= akhirTahun)
                {
                    var nextWeekDate = weekStart.AddDays(7);
                    DateTime weekEnd = new DateTime(
                        nextWeekDate.Year, nextWeekDate.Month, nextWeekDate.Day,
                        6, 59, 59
                    );
                    if (weekEnd > akhirTahun)
                        weekEnd = akhirTahun;

                    _context.WeeksSetting.Add(new WeeksSettingModel
                    {
                        YearSettingId = YearSettingId,
                        Week = weekNumber,
                        Month = weekStart.Month,
                        WorkingDays = 5.5m,
                        StartDate = weekStart,
                        EndDate = weekEnd
                    });

                    weekNumber++;
                    weekStart = weekEnd.AddSeconds(1);
                }

                await _context.SaveChangesAsync();
            return RedirectToAction("Index");


        }
    }
}
