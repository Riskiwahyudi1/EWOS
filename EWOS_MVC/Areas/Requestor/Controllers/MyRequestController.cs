using EWOS_MVC.Models;
using EWOS_MVC.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace EWOS_MVC.Areas.Requestor.Controllers
{
    [Authorize(Roles = "Requestor,AdminFabrication,AdminSystem,EngineerFabrication")]
    [Area("Requestor")]
    public class MyRequestController : BaseController
    {
        private readonly AppDbContext _context;
        public MyRequestController(AppDbContext context)
        {
            _context = context;
        }
        //new order
        [HttpGet]
        public async Task<IActionResult> Evaluation(int page = 1)
        {
            int userId = CurrentUser.Id;
            int pageSize = 20;

            // --- Data tabel hanya untuk status awal ---
            var tableData = _context.ItemRequests
                .Include(mc => mc.MachineCategories)
                .Include(u => u.Users)
                .Include(rs => rs.RequestStatus)
                    .ThenInclude(u => u.Users)
                .Where(u => u.UserId == userId)
                .Where(s => s.Status == "WaitingApproval" || s.Status == "FabricationApproval")
                .OrderByDescending(r => r.CreatedAt);

            var paginatedData = await PaginatedHelper<ItemRequestModel>
                .CreateAsync(tableData, page, pageSize);

            // --- Summary semua status ---
            var statusSummaryRepeat = await _context.RepeatOrders
                .Where(u => u.UsersId == userId)
                .GroupBy(r => r.Status ?? "Unknown")
                .ToDictionaryAsync(g => g.Key, g => g.Count());

            // --- Summary semua status evaluasi---
            var statusSummaryEval = await _context.ItemRequests
                .Where(u => u.UserId == userId)
                .GroupBy(r => r.Status ?? "Unknown")
                .ToDictionaryAsync(g => g.Key, g => g.Count());

            ViewBag.StatusSummaryRepeat = statusSummaryRepeat;
            ViewBag.StatusSummaryEval = statusSummaryEval;

            return View(paginatedData);
        }

        //RepeatOrder 
        [HttpGet]
        public async Task<IActionResult> RepeatOrder(int page = 1)
        {
            int userId = CurrentUser.Id;
            int pageSize = 20;

            // --- Data tabel hanya untuk status awal ---
            var tableData = _context.RepeatOrders
                .Include(ir => ir.ItemRequests)
                    .ThenInclude(mc => mc.MachineCategories)
                .Where(u => u.UsersId == userId)
                .Where(s => s.Status == "WaitingApproval" || s.Status == "FabricationApproval")
                .OrderByDescending(r => r.CreatedAt);

            var paginatedData = await PaginatedHelper<RepeatOrderModel>
                .CreateAsync(tableData, page, pageSize);


            // --- Summary semua status ---
            var statusSummaryRepeat = await _context.RepeatOrders
                .Where(u => u.UsersId == userId)
                .GroupBy(r => r.Status ?? "Unknown")
                .ToDictionaryAsync(g => g.Key, g => g.Count());

            // --- Summary semua status evaluasi---
            var statusSummaryEval = await _context.ItemRequests
                .Where(u => u.UserId == userId)
                .GroupBy(r => r.Status ?? "Unknown")
                .ToDictionaryAsync(g => g.Key, g => g.Count());

            ViewBag.StatusSummaryRepeat = statusSummaryRepeat;
            ViewBag.StatusSummaryEval = statusSummaryEval;


            return View(paginatedData);
        }

        //load modal new request
        [HttpGet]
        public IActionResult LoadData(long id, string type)
        {
            var data = _context.ItemRequests
                .Include(mc => mc.MachineCategories)
                .Include(rs => rs.RequestStatus)
                .Include(u => u.Users)
                .FirstOrDefault(i => i.Id == id);

            if (data == null) return NotFound();

            return type switch
            {
                "Pass" => PartialView("~/Views/modals/Requestor/Evaluation/BuyOffPassModal.cshtml", data),
                "Status" => PartialView("~/Views/modals/General/DetailStatus/StatusRequestModal.cshtml", data),
                "Fail" => PartialView("~/Views/modals/Requestor/Evaluation/BuyOffFailModal.cshtml", data),
                "Detail" => PartialView("~/Views/modals/General/DetailRequest/RequestDetailSummaryModal.cshtml", data),
                _ => BadRequest("Unknown modal type")
            };

            ;
        }

        //load modal Ro
        [HttpGet]
        public IActionResult LoadDataRo(long id, string type)
        {
            var data = _context.RepeatOrders
                .Include(ir => ir.ItemRequests)
                    .ThenInclude(mc => mc.MachineCategories)
                .Include(ir => ir.ItemRequests)
                    .ThenInclude(rm => rm.RawMaterials)
                .Include(u => u.Users)
                .Include(rs => rs.RequestStatus)
                    .ThenInclude(u => u.Users)
                .FirstOrDefault(i => i.Id == id);

            if (data == null) return NotFound();

            return type switch
            {
                "Recived" => PartialView("~/Views/modals/Requestor/RepeatOrder/ReciveModal.cshtml", data),
                "Detail" => PartialView("~/Views/modals/General/DetailRequest/ItemRequestRoDetailModal.cshtml", data),
                "Status" => PartialView("~/Views/modals/General/DetailStatus/StatusRequestRoModal.cshtml", data),
                _ => BadRequest("Unknown modal type")
            };

            ;
        }

        // Search request baru
        [HttpGet]
        public async Task<IActionResult> SearchNew(string keyword, int? categoryId, List<string> status)
        {
            int userId = CurrentUser.Id;
            // Base query
            var query = _context.ItemRequests
                .Include(m => m.MachineCategories)
                .Where(u => u.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
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
        // Search RO
        [HttpGet]
        public async Task<IActionResult> SearchRo(string keyword, int? categoryId, List<string> status)
        {
            int userId = CurrentUser.Id;
            // Base query
            var query = _context.RepeatOrders
                .Include(ir => ir.ItemRequests)
                    .ThenInclude(m => m.MachineCategories)
                .Where(u => u.UsersId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .AsQueryable();

            // Filter by keyword
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(r => r.ItemRequests.PartName.Contains(keyword));
            }

            // Filter by category
            if (categoryId.HasValue && categoryId.Value > 0)
            {
                query = query.Where(r => r.ItemRequests.MachineCategoryId == categoryId.Value);
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
                    r.ItemRequests.PartName,
                    r.ItemRequests.Weight,
                    r.ItemRequests.ExternalFabCost,
                    r.ItemRequests.FabricationTime,
                    CategoryName = r.ItemRequests.MachineCategories.CategoryName,
                    r.CRD,
                    r.QuantityReq,
                    r.QuantityDone,
                    r.Status,
                    r.CreatedAt
                })
                .ToListAsync();

            return Json(result);
        }


        //Result Pass
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Pass(long ItemRequestId, string Reason)
        {
            int userId = CurrentUser.Id;

            if (ItemRequestId == null)
            {
                TempData["Error"] = "ItemRequestId tidak boleh kosong.";
                return RedirectToAction("Evaluation");
            }
            if (Reason == null)
            {
                TempData["Error"] = "Reason tidak boleh kosong.";
                return RedirectToAction("Evaluation");
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);

                TempData["Error"] = "Terjadi kesalahan: " + string.Join(", ", errors);
                return View("Evaluation");
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
                Status = "Buyoff Pass",
                Reason = Reason,
                UserId = userId,
                CreatedAt = DateTime.Now
            };

            _context.RequestStatus.Add(approvalRequest);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Berhasil Update Data.";
            return Redirect("Evaluation");
        }

        //Result Close
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Close(long ItemRequestId, long RepeatOrderId, string Reason)
        {
            int userId = CurrentUser.Id;

            if (RepeatOrderId <= 0)
            {
                TempData["Error"] = "RepearOrderId tidak boleh kosong.";
                return RedirectToAction("RepeatOrder");
            }
            if (ItemRequestId <= 0)
            {
                TempData["Error"] = "RepeatOrderId tidak boleh kosong.";
                return RedirectToAction("RepeatOrder");
            }
            if (Reason == null)
            {
                TempData["Error"] = "Reason tidak boleh kosong.";
                return RedirectToAction("RepeatOrder");
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);

                TempData["Error"] = "Terjadi kesalahan: " + string.Join(", ", errors);
                return View("RepeatOrder");
            }

            var findRequest = await _context.RepeatOrders.FindAsync(RepeatOrderId);
            if (findRequest == null)
            {
                return NotFound();
            }
            // Update proses
            findRequest.Status = "Close";
            findRequest.UpdatedAt = DateTime.Now;

            var approvalRequest = new RequestStatusModel
            {
                ItemRequestId = ItemRequestId,
                RepeatOrderId = RepeatOrderId,
                Status = "Close",
                Reason = Reason,
                UserId = userId,
                CreatedAt = DateTime.Now
            };

            _context.RequestStatus.Add(approvalRequest);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Berhasil Update Data.";
            return Redirect("RepeatOrder");
        }

        // result fail
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Fail(long ItemRequestId, string Reason)
        {
            int userId = CurrentUser.Id;
            if (ItemRequestId == null)
            {
                TempData["Error"] = "ItemRequestId tidak boleh kosong.";
                return RedirectToAction("Evaluation");
            }
            if (Reason == null)
            {
                TempData["Error"] = "Reason tidak boleh kosong.";
                return RedirectToAction("Evaluation");
            }
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);

                TempData["Error"] = "Terjadi kesalahan: " + string.Join(", ", errors);
                return View("Evaluation");
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
                Status = "Buyoff Fail",
                Reason = Reason,
                UserId = userId,
                CreatedAt = DateTime.Now
            };

            _context.RequestStatus.Add(approvalRequest);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Berhasil Update Data.";
            return Redirect("Evaluation");
        }

        //Revisi 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Revisi(long? ItemRequestId, string Reason)
        {
            int userId = CurrentUser.Id;

            if (ItemRequestId == null)
            {
                TempData["Error"] = "ItemRequestId tidak boleh kosong.";
                return RedirectToAction("Evaluation");
            }

            if (string.IsNullOrWhiteSpace(Reason))
            {
                TempData["Error"] = "Reason wajib diisi.";
                return RedirectToAction("Evaluation");
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);

                TempData["Error"] = "Terjadi kesalahan: " + string.Join(", ", errors);
                return View("Evaluation");
            }

            var findRequest = await _context.ItemRequests.FindAsync(ItemRequestId);
            if (findRequest == null)
            {
                return NotFound();
            }

            //tambahkan revisi ke data request

            if (findRequest.Status?.Equals("Fail", StringComparison.OrdinalIgnoreCase) == true)
            {
                findRequest.RevisiNo = (findRequest.RevisiNo ?? 0) + 1;
            }

            // Update proses
            findRequest.Status = "WaitingApproval";
            findRequest.Description = Reason;
            findRequest.UpdatedAt = DateTime.Now;

            await _context.RequestStatus
                        .Where(rs => rs.ItemRequestId == ItemRequestId)
                        .ExecuteDeleteAsync();
            await _context.SaveChangesAsync();

            TempData["Success"] = "Berhasil Mengajukan revisi.";
            return Redirect("Evaluation");
        }
    }
}