using EWOS_MVC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EWOS_MVC.Areas.AdminSystem.Controllers
{
    [Authorize(Roles = "AdminSystem")]
    [Area("AdminSystem")]
    public class YearsSettingController : BaseController
    {

        private readonly AppDbContext _context;
        public YearsSettingController(AppDbContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            var years = _context.YearsSetting.ToList();
            return View(years);
        }

        //Seaching async

        [HttpGet]
        public IActionResult Search(string keyword)
        {
            var hasil = _context.YearsSetting
                .Where(y =>
                    string.IsNullOrEmpty(keyword) ||
                    y.Year.ToString().Contains(keyword)
                )
                .Select(y => new
                {
                    y.Id,
                    y.ElectricalCost,
                    y.Year,
                    StartDate = y.StartDate.ToString("dd/MM/yyyy HH:mm"),
                    CreatedAt = y.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                    UpdatedAt = y.UpdatedAt.ToString("dd/MM/yyyy HH:mm")
                })
                .ToList();

            return Json(hasil);
        }

        // load data modal
        [HttpGet]
        public async Task<IActionResult> LoadData(long id, string type)
        {
            var data = await _context.YearsSetting
                .FirstOrDefaultAsync(i => i.Id == id);

            if (data == null) return NotFound();

            return type switch
            {
                "Edit" => PartialView("~/Views/modals/AdminSystem/EditYearModal.cshtml", data),
                _ => BadRequest("Unknown modal type")
            };

            ;
        }

        //tambah data
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(YearsSettingModel y)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);

                TempData["Error"] = "Terjadi kesalahan: " + string.Join(", ", errors);
                return RedirectToAction("Index");
            }
            bool exists = await _context.YearsSetting
            .AnyAsync(obj => obj.Year == y.Year);

            if (exists)
            {
                TempData["Alert"] = $"Data tahun {y.Year} sudah ada.";
                return RedirectToAction("Index"); ;
            }
            y.StartDate = y.StartDate.Date.AddHours(7);
            y.CreatedAt = DateTime.Now;
            y.UpdatedAt = DateTime.Now;

            _context.YearsSetting.Add(y);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Year berhasil ditambahkan.";
            return RedirectToAction("Index");
        }

        //Edit Data
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int? Id, DateTime? StartDate, Decimal ElectricalCost)
        {
            // Validasi model
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage);

                TempData["Error"] = "Terjadi kesalahan: " + string.Join(", ", errors);
                return RedirectToAction("Index");
            }


            var existingDate = await _context.YearsSetting.FindAsync(Id);
            if (existingDate == null)
            {
                return NotFound();
            }

            // Update data
            existingDate.StartDate = StartDate.Value.Date.AddHours(7);
            existingDate.ElectricalCost = ElectricalCost;
            existingDate.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Data berhasil diubah.";
            return RedirectToAction("Index");
        }
    }
}

