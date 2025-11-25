using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EWOS_MVC.Areas.AdminSystem.Controllers
{
    [Authorize(Roles = "AdminSystem")]
    [Area("AdminSystem")]
    public class WeekFabricationController : BaseController
    {
        private readonly AppDbContext _context;
        private const int PageSize = 10;
        public WeekFabricationController(AppDbContext context)
        {
            _context = context;
        }
        public IActionResult Index(int page = 1, string search = "")
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

            // Kirim data ke ViewBag
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

        

    }
}
