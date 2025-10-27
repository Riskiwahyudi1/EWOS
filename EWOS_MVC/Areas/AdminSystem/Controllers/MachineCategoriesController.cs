using EWOS_MVC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace EWOS_MVC.Areas.AdminSystem.Controllers
{
    [Area("AdminSystem")]

    public class MachineCategoriesController : Controller
    {
        private readonly AppDbContext _context;
        public MachineCategoriesController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var category = _context.MachineCategories.ToList();
            return View(category);

        }

        //Seaching 
        [HttpGet]
        public IActionResult Search(string keyword)
        {
            var hasil = _context.MachineCategories

                .Where(m =>
                    string.IsNullOrEmpty(keyword) ||
                    m.CategoryName.Contains(keyword)
                )
                .Select(m => new {
                    m.Id,
                    m.CategoryName,
                    CreatedAt = m.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                    UpdatedAt = m.UpdatedAt.ToString("dd/MM/yyyy HH:mm")
                })
                .ToList();

            return Json(hasil);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MachineCategoriesModel category)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Terjadi kesalahan.";
                return View("Index");
            }

            category.CreatedAt = DateTime.Now;
            category.UpdatedAt = DateTime.Now;

            _context.MachineCategories.Add(category);
            await _context.SaveChangesAsync();

            TempData["Success"] = "User berhasil ditambahkan.";
            return RedirectToAction("Index");
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(MachineCategoriesModel category)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Terjadi kesalahan.";
                return View(category);
            }

            var existingCategory = await _context.MachineCategories.FindAsync(category.Id);
            if (existingCategory == null)
            {
                return NotFound();
            }

            // Update hanya CategoryName
            existingCategory.CategoryName = category.CategoryName;
            existingCategory.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Data berhasil diubah.";
            return RedirectToAction("Index");
        }

    }
}
