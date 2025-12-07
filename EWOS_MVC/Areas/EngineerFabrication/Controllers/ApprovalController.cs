using EWOS_MVC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace EWOS_MVC.Areas.Supervisor.Controllers
{
    [Authorize(Roles = "AdminSystem,EngineerFabrication")]
    [Area("EngineerFabrication")]
    public class ApprovalController : BaseController
    {
        private readonly AppDbContext _context;
        public ApprovalController(AppDbContext contex)
        {
            _context = contex;
        }
        //show new order
        [HttpGet]
        public async Task<IActionResult> Evaluation()
        {
            // get ItemRequests yang menunggu approval
            var waitingApprovalEval = await _context.ItemRequests
                                     .Include(u => u.Users)
                                     .Include(i => i.MachineCategories)
                                     .Where(i => i.Status == "WaitingApproval")
                                     .ToListAsync();

            // Hitung RepeatOrders yang menunggu approval
            var waitingApprovalROCount = await _context.RepeatOrders
                                     .Where(s => s.Status == "WaitingApproval")
                                     .CountAsync();

            // Hitung ItemRequests yang menunggu approval
            var waitingApprovalEvalCount = waitingApprovalEval.Count();

            ViewBag.ApprovalROCount = waitingApprovalROCount;
            ViewBag.ApprovalEvalCount = waitingApprovalEvalCount;

            return View(waitingApprovalEval);
        }

        // Show Repeat Order
        [HttpGet]
        public async Task<IActionResult> RepeatOrder()
        {
            // get RepeatOrders yang menunggu approval
            var waitingApprovalRepeat = await _context.RepeatOrders
                                     .Include(u => u.Users)
                                     .Include(i => i.ItemRequests)
                                         .ThenInclude(mc => mc.MachineCategories)
                                     .Where(i => i.Status == "WaitingApproval")
                                     .ToListAsync();

            // Hitung ItemRequests yang menunggu approval
            var waitingApprovalEvalCount = await _context.ItemRequests
                                     .Where(s => s.Status == "WaitingApproval")
                                     .CountAsync();

            // Hitung RepeatOrders yang menunggu approval
            var waitingApprovalRepeatCount = waitingApprovalRepeat.Count();

            ViewBag.ApprovalROCount = waitingApprovalRepeatCount;
            ViewBag.ApprovalEvalCount = waitingApprovalEvalCount;

            return View(waitingApprovalRepeat);
        }


        //load data modal evaluasi
        [HttpGet]
        public async Task<IActionResult> LoadData(long id, string type)
        {
            var data = await _context.ItemRequests
                .Include(m => m.MachineCategories)
                .Include(u => u.Users)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (data == null) return NotFound();

            return type switch
            {
                "Approve" => PartialView("~/Views/modals/EngineerFabrication/Evaluation/ApproveModal.cshtml", data),
                "Reject" => PartialView("~/Views/modals/EngineerFabrication/Evaluation/RejectModal.cshtml", data),
                "Detail" => PartialView("~/Views/modals/EngineerFabrication/Evaluation/DetailModal.cshtml", data),
                _ => BadRequest("Unknown modal type")
            };

            ;
        }

        //load data modal RO
        [HttpGet]
        public async Task<IActionResult> LoadDataRo(long id, string type)
        {
            var data = await _context.RepeatOrders
                .Include(i => i.ItemRequests)
                    .ThenInclude(mc => mc.MachineCategories)
                .Include(u => u.Users)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (data == null) return NotFound();

            return type switch
            {
                "Approve" => PartialView("~/Views/modals/EngineerFabrication/RepeatOrder/ApproveModal.cshtml", data),
                "Detail" => PartialView("~/Views/modals/EngineerFabrication/RepeatOrder/DetailModal.cshtml", data),
                _ => BadRequest("Unknown modal type")
            };

            ;
        }


        //search baru
        [HttpGet]
        public IActionResult SearchNew(string keyword, int? categoryId)
        {
            var query = _context.ItemRequests
                .Include(m => m.MachineCategories)
                .Include(u => u.Users)
                .Where(s => s.Status == "WaitingApproval")
                .AsQueryable();

            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(r => r.PartName.Contains(keyword));
            }

            if (categoryId.HasValue)
            {
                query = query.Where(r => r.MachineCategoryId == categoryId.Value);
            }

            var result = query
                .Select(r => new
                {
                    r.Id,
                    r.PartName,
                    CategoryName = r.MachineCategories.CategoryName,
                    Users = r.Users.Name,
                    r.MachineCategoryId,
                    r.CreatedAt
                })
                .ToList();

            return Json(result);
        }
        //search Ro
        [HttpGet]
        public IActionResult SearchRo(string keyword, int? categoryId)
        {
            var query = _context.RepeatOrders
                .Include(i => i.ItemRequests)
                    .ThenInclude(mc => mc.MachineCategories)
                .Include(u => u.Users)
                .Include(u => u.Users)
                .Where(s => s.Status == "WaitingApproval")
                .AsQueryable();

            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(r => r.ItemRequests.PartName.Contains(keyword));
            }

            if (categoryId.HasValue)
            {
                query = query.Where(r => r.ItemRequests.MachineCategoryId == categoryId.Value);
            }

            var result = query
                .Select(r => new
                {
                    r.Id,
                    r.ItemRequests.PartName,
                    CategoryName = r.ItemRequests.MachineCategories.CategoryName,
                    Users = r.Users.Name,
                    r.ItemRequests.MachineCategoryId,
                    r.CreatedAt
                })
                .ToList();

            return Json(result);
        }

        // approve request
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(long itemRequestId, long? repeatOrderId)
        {
            int userId = ViewBag.Id != null ? Convert.ToInt32(ViewBag.Id) : 0;
            string redirectUrl = "/EngineerFabrication/Approval/Evaluation";

            //validasi
            if (itemRequestId <= 0)
            {
                TempData["Error"] = "ItemRequestId tidak boleh kosong.";
                return RedirectToAction(redirectUrl);
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                TempData["Error"] = "Terjadi kesalahan: " + string.Join(", ", errors);
                return View(redirectUrl);
            }


            //menerima data dari file Supervisor/Approval/RepeatOrder.cshtml
            if (repeatOrderId.HasValue)
            {
                var repeatOrder = await _context.RepeatOrders.FindAsync(repeatOrderId.Value);
                if (repeatOrder == null)
                    return NotFound();

                repeatOrder.Status = "FabricationApproval";
                repeatOrder.UpdatedAt = DateTime.Now;

                redirectUrl = "/EngineerFabrication/Approval/RepeatOrder";
            }

            //menerima data dari file Supervisor/Approval/Evaluation.cshtml
            else
            {
                var itemRequest = await _context.ItemRequests.FindAsync(itemRequestId);
                if (itemRequest == null)
                    return NotFound();

                itemRequest.Status = "FabricationApproval";
                itemRequest.UpdatedAt = DateTime.Now;
            }

            var approvalRequest = new RequestStatusModel
            {
                ItemRequestId = itemRequestId,
                RepeatOrderId = repeatOrderId,
                Status = "EngineerApproved",
                UserId = userId,
                CreatedAt = DateTime.Now
            };

            _context.RequestStatus.Add(approvalRequest);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Berhasil melakukan Approve.";
            return Redirect(redirectUrl);
        }


        // Reject Request
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(long itemRequestId, long? repeatOrderId, string reason)
        {
            int userId = ViewBag.Id != null ? Convert.ToInt32(ViewBag.Id) : 0;
            string redirectUrl = "/EngineerFabrication/Approval/Evaluation";

            // Validasi input
            if (itemRequestId <= 0 )
            {
                TempData["Error"] = "ItemRequestId atau RepeatOrderId harus diisi.";
                return RedirectToAction(redirectUrl);
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors)
                                              .Select(e => e.ErrorMessage);
                TempData["Error"] = "Terjadi kesalahan: " + string.Join(", ", errors);
                return View(redirectUrl);
            }


            //menerima data dari file Supervisor/Approval/RepeatOrder.cshtml
            if (repeatOrderId.HasValue)
            {
                var repeatOrder = await _context.RepeatOrders.FindAsync(repeatOrderId.Value);
                if (repeatOrder == null) return NotFound();

                repeatOrder.Status = "Reject";
                repeatOrder.UpdatedAt = DateTime.Now;

                redirectUrl = "/EngineerFabrication/Approval/RepeatOrder";
            }
            //menerima data dari file Supervisor/Approval/Evaluation.cshtml
            else
            {
                var itemRequest = await _context.ItemRequests.FindAsync(itemRequestId);
                if (itemRequest == null) return NotFound();

                itemRequest.Status = "Reject";
                itemRequest.UpdatedAt = DateTime.Now;
            }

            // Catat status reject di RequestStatusModel
            var rejectRequest = new RequestStatusModel
            {
                ItemRequestId = itemRequestId,
                RepeatOrderId = repeatOrderId,
                Status = "EngineerReject",
                Reason = reason,
                UserId = userId,
                CreatedAt = DateTime.Now
            };

            _context.RequestStatus.Add(rejectRequest);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Berhasil melakukan Reject.";
            return Redirect(redirectUrl);
        }

    }
}