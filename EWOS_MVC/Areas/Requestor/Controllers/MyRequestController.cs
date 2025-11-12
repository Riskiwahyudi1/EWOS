using EWOS_MVC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EWOS_MVC.Areas.Requestor.Controllers
{
    [Authorize(Roles = "Requestor,AdminFabrication,AdminSystem,Supervisor")]
    [Area("Requestor")]
    public class MyRequestController : BaseController
    {
        private readonly AppDbContext _context;
        public MyRequestController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string status)
        {
            int userId = ViewBag.Id != null ? Convert.ToInt32(ViewBag.Id) : 0;

            // --- Data tabel hanya untuk status awal ---
            var tableData = await _context.ItemRequests
                .Include(mc => mc.MachineCategories)
                .Include(u => u.Users)
                .Include(rs => rs.RequestStatus)
                    .ThenInclude(u => u.Users)
                .Where(u => u.UserId == userId)
                .Where(s => s.Status == "WaitingApproval" || s.Status == "FabricationApproval")
                .ToListAsync();

            // --- Data modal untuk semua status ---
            var modalData = await _context.ItemRequests
                .Include(mc => mc.MachineCategories)
                .Include(u => u.Users)
                .Include(rs => rs.RequestStatus)
                    .ThenInclude(u => u.Users)
                .Where(u => u.UserId == userId)
                .ToListAsync();

            // --- Summary semua status ---
            var statusSummary = await _context.ItemRequests
                .Where(u => u.UserId == userId)
                .GroupBy(r => r.Status ?? "Unknown")
                .ToDictionaryAsync(g => g.Key, g => g.Count());

            ViewBag.StatusSummary = statusSummary;
            ViewBag.CurrentStatus = "WaitingApproval";
            ViewBag.ModalData = modalData;

            return View(tableData);
        }


        // Search request
        [HttpGet]
        public async Task<IActionResult> Search(string keyword, int? categoryId, List<string> status)
        {
            // Base query
            var query = _context.ItemRequests
                .Include(m => m.MachineCategories)
                .AsQueryable();

            // Filter by keyword
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(r => r.PartName.Contains(keyword));
            }

            // Filter by category
            if (categoryId.HasValue && categoryId.Value > 0)
            {
                query = query.Where(r => r.MachineCategoryId == categoryId.Value);
            }

            // Filter by multiple statuses
            if (status != null && status.Any())
            {
                query = query.Where(r => r.Status != null && status.Contains(r.Status));
            }

            // Execute query asynchronously
            var result = await query
                .Select(r => new
                {
                    r.Id,
                    r.PartName,
                    r.Weight,
                    r.ExternalFabCost,
                    r.FabricationTime,
                    r.RawMaterialId,
                    r.MachineCategoryId,
                    CategoryName = r.MachineCategories.CategoryName,
                    r.CRD,
                    r.Status,
                    r.CreatedAt
                })
                .ToListAsync();

            return Json(result);
        }


        //Result Pass
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Pass(long? ItemRequestId)
        {
            int userId = ViewBag.Id != null ? Convert.ToInt32(ViewBag.Id) : 0;

            if (ItemRequestId == null)
            {
                TempData["Error"] = "ItemRequestId tidak boleh kosong.";
                return RedirectToAction("Index");
            }
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);

                TempData["Error"] = "Terjadi kesalahan: " + string.Join(", ", errors);
                return View("Index");
            }

            var findRequest = await _context.ItemRequests.FindAsync(ItemRequestId);
            if (findRequest == null)
            {
                return NotFound();
            }
            // Update proses
            findRequest.Status = "Maspro";
            findRequest.UpdatedAt = DateTime.Now;

            var approvalRequest = new RequestStatusModel
            {
                ItemRequestId = ItemRequestId,
                Status = "ResultPass",
                UserId = userId,
                CreatedAt = DateTime.Now
            };

            _context.RequestStatus.Add(approvalRequest);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Berhasil Update Data.";
            return Redirect("/MyRequest/WaitingConfirmation");
        }

        // result fail
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Fail(long? ItemRequestId, string Reason)
        {
            int userId = ViewBag.Id != null ? Convert.ToInt32(ViewBag.Id) : 0;
            if (ItemRequestId == null)
            {
                TempData["Error"] = "ItemRequestId tidak boleh kosong.";
                return RedirectToAction("Index");
            }
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);

                TempData["Error"] = "Terjadi kesalahan: " + string.Join(", ", errors);
                return View("Index");
            }

            var findRequest = await _context.ItemRequests.FindAsync(ItemRequestId);
            if (findRequest == null)
            {
                return NotFound();
            }

            // Update proses
            findRequest.Status = "Fail";
            findRequest.UpdatedAt = DateTime.Now;

            var approvalRequest = new RequestStatusModel
            {
                ItemRequestId = ItemRequestId,
                Status = "ResultFail",
                Reason = Reason,
                UserId = userId,
                CreatedAt = DateTime.Now
            };

            _context.RequestStatus.Add(approvalRequest);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Berhasil Update Data.";
            return Redirect("index");
        }

    }
}