using EWOS_MVC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace EWOS_MVC.Areas.AdminFabrication.Controllers
{
    [Authorize(Roles = "AdminSystem,AdminFabrication")]
    [Area("AdminFabrication")]

    public class NewRequestController : BaseController
    {
        private readonly AppDbContext _context;

        public NewRequestController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var requestList = await _context.ItemRequests
                                .Include(mc => mc.MachineCategories)
                                .Include(u => u.Users)
                                .Include(rs => rs.RequestStatus)
                                    .ThenInclude(u => u.Users)
                                .Where(rq => rq.Status == "FabricationApproval")
                                .ToListAsync();

            // --- Data modal untuk semua status ---
            var modalData = await _context.ItemRequests
                .Include(mc => mc.MachineCategories)
                .Include(u => u.Users)
                .Include(rs => rs.RequestStatus)
                    .ThenInclude(u => u.Users)
                .ToListAsync();

            // --- Summary semua status ---
            var statusSummary = await _context.ItemRequests
                .GroupBy(r => r.Status ?? "Unknown")
                .ToDictionaryAsync(g => g.Key, g => g.Count());

            ViewBag.StatusSummary = statusSummary;
            ViewBag.CurrentStatus = "FabricationApproval";
            ViewBag.ModalData = modalData;
            return View(requestList);
        }

        // search
        [HttpGet]
        public IActionResult Search(string keyword, int? categoryId, string status)
        {
            var query = _context.ItemRequests
                .Include(m => m.MachineCategories)
                .Include(u => u.Users)
                .AsQueryable();

            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(r => r.PartName.Contains(keyword));
            }

            if (categoryId.HasValue)
            {
                query = query.Where(r => r.MachineCategoryId == categoryId.Value);
            }

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(r => r.Status == status);
            }

            var result = query
                .Select(r => new
                {
                    r.Id,
                    r.PartName,
                    CategoryName = r.MachineCategories.CategoryName,
                    Users = r.Users.Name,
                    r.CRD,
                    r.Status,
                    r.CreatedAt
                })
                .ToList();

            return Json(result);
        }

        //Approve Request
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(long? ItemRequestId, DateTime OCD)
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
            findRequest.Status = "WaitingFabrication";
            findRequest.OCD = OCD;
            findRequest.UpdatedAt = DateTime.Now;

            var approvalRequest = new RequestStatusModel
            {
                ItemRequestId = ItemRequestId,
                Status = "FabricationApproved",
                UserId = userId,
                CreatedAt = DateTime.Now
            };

            _context.RequestStatus.Add(approvalRequest);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Berhasil melakukan Approve.";
            return Redirect("index");
        }

        //Reject Request
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(long? ItemRequestId, string Reason)
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
            findRequest.Status = "Reject";
            findRequest.UpdatedAt = DateTime.Now;

            var approvalRequest = new RequestStatusModel
            {
                ItemRequestId = ItemRequestId,
                Status = "RejectByFabricationTeam",
                Reason = Reason,
                UserId = userId,
                CreatedAt = DateTime.Now
            };

            _context.RequestStatus.Add(approvalRequest);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Berhasil melakukan Reject.";
            return Redirect("index");
        }

        //Edit Data
        [HttpPost]
        [Route("/AdminFabrication/NewRequest/Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ItemRequestModel itemRequest)
        {
            // Validasi model
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage);

                TempData["Error"] = "Terjadi kesalahan: " + string.Join(", ", errors);
                return Redirect("/AdminFabrication/NewRequest/Approved");
            }

            var existingData = await _context.ItemRequests.FindAsync(itemRequest.Id);
            if (existingData == null)
            {
                return NotFound();
            }

            // Update data
            existingData.PartName = itemRequest.PartName;
            existingData.RawMaterialId = itemRequest.RawMaterialId;
            existingData.MachineCategoryId = itemRequest.MachineCategoryId;
            existingData.ExternalFabCost = itemRequest.ExternalFabCost;
            existingData.FabricationTime = itemRequest.FabricationTime;
            existingData.PartCode = itemRequest.PartCode;
            existingData.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Data berhasil diubah.";
            return Redirect("/AdminFabrication/NewRequest/Approved");
        }

    }
}
