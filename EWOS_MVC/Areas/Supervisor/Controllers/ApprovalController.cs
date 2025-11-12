using EWOS_MVC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace EWOS_MVC.Areas.Supervisor.Controllers
{
    [Authorize(Roles = "AdminSystem,Supervisor")]
    [Area("Supervisor")]
    public class ApprovalController : BaseController
    {
        private readonly AppDbContext _context;
        public ApprovalController(AppDbContext contex)
        {
            _context = contex;
        }
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var waitingApproval = await _context.ItemRequests
                                    .Include(u => u.Users)
                                    .Include(i => i.MachineCategories)
                                    .Where(i =>i.Status == "WaitingApproval")
                                    .ToListAsync();

            return View(waitingApproval);
        }

        //Approve Request
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult>Approve(long ItemRequestId)
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
            findRequest.Status = "FabricationApproval";
            findRequest.UpdatedAt = DateTime.Now;

            var approvalRequest = new RequestStatusModel
            {
                ItemRequestId = ItemRequestId,
                Status = "SupervisorApproved",
                UserId = userId,
                CreatedAt = DateTime.Now
            };

            _context.RequestStatus.Add(approvalRequest);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Berhasil melakukan Approve.";
            return Redirect("/Supervisor/Approval");
        }

        //Reject Request
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(long ItemRequestId, string Reason)
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
                Status = "SupervisorReject",
                Reason = Reason,
                UserId = userId,
                CreatedAt = DateTime.Now
            };

            _context.RequestStatus.Add(approvalRequest);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Berhasil melakukan Reject.";
            return Redirect("/Supervisor/Approval");
        }
    }
}
