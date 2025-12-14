using EWOS_MVC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EWOS_MVC.Areas.AdminSystem.Controllers
{
    [Authorize(Roles = "AdminSystem")]
    [Area("AdminSystem")]
    public class RawMaterialController : BaseController
    {
        private readonly AppDbContext _context;
        public RawMaterialController(AppDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Index()
        {
            var rawmaterials = await _context.RawMaterials
                                .Include(m => m.MachineCategories)
                                .ToListAsync();
            return View(rawmaterials);
        }

        // load data modal
        [HttpGet]
        public async Task<IActionResult> LoadData(long id, string type)
        {
            var data = await _context.RawMaterials
                .FirstOrDefaultAsync(i => i.Id == id);

            if (data == null) return NotFound();

            return type switch
            {
                "Edit" => PartialView("~/Views/modals/AdminSystem/EditRawModal.cshtml", data),
                _ => BadRequest("Unknown modal type")
            };

            ;
        }

        //Searching
        [HttpGet]
        public IActionResult Search(string keyword)
        {
            var hasil = _context.RawMaterials
                .Include(m => m.MachineCategories)
                .Where(s =>
                    string.IsNullOrEmpty(keyword) ||
                    s.Name.Contains(keyword) ||
                    s.SAPID.ToString().Contains(keyword) ||
                    s.MachineCategories.CategoryName.Contains(keyword)
                )
                .Select(s => new {
                    s.Id,
                    s.SAPID,
                    s.Name,
                    s.MachineCategories.CategoryName,
                    s.Price,
                    CreatedAt = s.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                    UpdatedAt = s.UpdatedAt.ToString("dd/MM/yyyy HH:mm")
                })
                .ToList();

            return Json(hasil);
        }

        //Tambah data
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RawMaterialModel rawmaterial)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);

                TempData["Error"] = "Terjadi kesalahan: " + string.Join(", ", errors);
                return View("Index");
            }

            bool exists = await _context.RawMaterials
            .AnyAsync(r => r.SAPID == rawmaterial.SAPID);

            if (exists)
            {
                TempData["ErrorMessage"] = $"SAP ID: {rawmaterial.SAPID} sudah digunakan.";
                return RedirectToAction("Index"); ;
            }

            rawmaterial.CreatedAt = DateTime.Now;
            rawmaterial.UpdatedAt = DateTime.Now;

            _context.RawMaterials.Add(rawmaterial);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Rawmaterial has been created.";
            return RedirectToAction("Index");
        }

      
        //Edit Data
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(RawMaterialModel rawMaterials)
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

            // Cek apakah SAP ID sudah digunakan 
            bool exists = await _context.RawMaterials
                .AnyAsync(r => r.SAPID == rawMaterials.SAPID && r.Id != rawMaterials.Id);

            if (exists)
            {
                TempData["Error"] = $"SAP ID: {rawMaterials.SAPID} sudah digunakan.";
                return RedirectToAction("Index");
            }

            var existingDataRaw = await _context.RawMaterials.FindAsync(rawMaterials.Id);
            if (existingDataRaw == null)
            {
                return NotFound();
            }

            // Update data
            existingDataRaw.SAPID = rawMaterials.SAPID;
            existingDataRaw.Name = rawMaterials.Name;
            existingDataRaw.MachineCategoryId = rawMaterials.MachineCategoryId;
            existingDataRaw.Price = rawMaterials.Price;
            existingDataRaw.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Data berhasil diubah.";
            return RedirectToAction("Index");
        }
    }
}
