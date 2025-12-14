using EWOS_MVC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EWOS_MVC.Areas.AdminSystem.Controllers
{
    [Authorize(Roles = "AdminSystem")]
    [Area("AdminSystem")]
    public class MachineController : BaseController
    {
        private readonly AppDbContext _context;
        public MachineController(AppDbContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            var machines = _context.Machines
                            .Include(m => m.MachineCategories)
                            .ToList();
            return View(machines);

        }

        // load data modal
        [HttpGet]
        public async Task<IActionResult> LoadData(long id, string type)
        {
            var data = await _context.Machines
                .FirstOrDefaultAsync(i => i.Id == id);

            if (data == null) return NotFound();

            return type switch
            {
                "Edit" => PartialView("~/Views/modals/AdminSystem/EditMcModal.cshtml", data),
                _ => BadRequest("Unknown modal type")
            };

            ;
        }

        //Seaching
        [HttpGet]
        public IActionResult Search(string keyword)
        {
            var hasil = _context.Machines
                .Include(m => m.MachineCategories)
                .Where(s =>
                    string.IsNullOrEmpty(keyword) ||
                    s.MachineName.Contains(keyword) ||
                    s.MachineCategories.CategoryName.Contains(keyword)
                )
                .Select(s => new {
                    s.Id,
                    s.MachineName,
                    s.MachinePower,
                    s.MachineCategories.CategoryName,
                    s.IsActive,
                    CreatedAt = s.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                    UpdatedAt = s.UpdatedAt.ToString("dd/MM/yyyy HH:mm")
                })
                .ToList();

            return Json(hasil);
        }

        //Menambahkan data
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MachineModel machine)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);

                TempData["Error"] = "Terjadi kesalahan: " + string.Join(", ", errors);
                return View("Index");
            }

            machine.IsActive = true;
            machine.CreatedAt = DateTime.Now;
            machine.UpdatedAt = DateTime.Now;

            _context.Machines.Add(machine);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Machine has been created.";
            return RedirectToAction("Index");
        }

        //Edit data
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(MachineModel machine)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);

                TempData["Error"] = "Terjadi kesalahan: " + string.Join(", ", errors);
                return View("Index");
            }

            var existingMachine = await _context.Machines.FindAsync(machine.Id);
            if (existingMachine == null)
            {
                return NotFound();
            }

            // Update hanya CategoryName
            existingMachine.MachineName = machine.MachineName;
            existingMachine.MachinePower = machine.MachinePower;
            existingMachine.MachineCategoryId = machine.MachineCategoryId;
            existingMachine.IsActive = machine.IsActive;
            existingMachine.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Data berhasil diubah.";
            return RedirectToAction("Index");
        }
    }
}
