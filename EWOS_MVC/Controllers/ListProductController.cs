using EWOS_MVC.Models;
using EWOS_MVC.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EWOS_MVC.Controllers
{
    [Authorize(Roles = "Requestor,AdminFabrication,AdminSystem,EngineerFabrication")]

    public class ListProductController : BaseController
    {
        private readonly AppDbContext _context;
        private readonly EmailService _emailService;
        public ListProductController(AppDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }
        public async Task<IActionResult> Index(int page = 1)
        {
            int pageSize = 10;

            var query = _context.ItemRequests
                .Include(mc => mc.MachineCategories)
                .Include(u => u.Users)
                .Include(rs => rs.RequestStatus)
                    .ThenInclude(u => u.Users)
                .Where(s => s.Status == "Maspro")
                .OrderByDescending(r => r.CreatedAt);

            // PAGINATION
            var paginatedData = await PaginatedHelper<ItemRequestModel>
                .CreateAsync(query, page, pageSize);

            // --- Data modal untuk semua status ---
            var modalData = await _context.ItemRequests
                .Include(mc => mc.MachineCategories)
                .Include(u => u.Users)
                .Include(rm => rm.RawMaterials)
                .Include(rs => rs.RequestStatus)
                    .ThenInclude(u => u.Users)
                .ToListAsync();

            ViewBag.ModalData = modalData;
            ViewBag.PageSize = pageSize;
            ViewBag.CurrentStatus = "Maspro";

            return View(paginatedData);
        }


        // load data modal
        [HttpGet]
        public async Task<IActionResult> LoadData(long id, string type)
        {
            var data = await _context.ItemRequests
                .Include(m => m.MachineCategories)
                .Include(u => u.Users)
                .Include(rm => rm.RawMaterials)
                .Include(sr => sr.RequestStatus)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (data == null) return NotFound();

            return type switch
            {
                "Request" => PartialView("~/Views/modals/Requestor/ListProduct/RepeatOrderModal.cshtml", data),
                "Edit" => PartialView("~/Views/modals/Requestor/ListProduct/EditModal.cshtml", data),
               "Detail" => PartialView("~/Views/modals/General/DetailRequest/ItemRequestDetailModal.cshtml", data),
                "Status" => PartialView("~/Views/modals/General/DetailStatus/StatusRequestModal.cshtml", data),
                _ => BadRequest("Unknown modal type")
            };

            ;
        }
        // Search request
        [HttpGet]
        public async Task<IActionResult> Search(string keyword, int? categoryId, List<string> status)
        {
            //await Task.Delay(4000);
            // Base query
            var query = _context.ItemRequests
                .Include(m => m.MachineCategories)
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
                    r.MachineCategories.CategoryName,
                    Requestor = r.Users.Name,
                    r.CRD,
                    r.Status,
                    r.CreatedAt
                })
                .ToListAsync();

            return Json(result);
        }

        //request ulang
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RepeatOrder(RepeatOrderModel data)
        {
            int userId = ViewBag.Id != null ? Convert.ToInt32(ViewBag.Id) : 0;

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);

                TempData["Error"] = "Terjadi kesalahan: " + string.Join(", ", errors);
                return View("Index");
            }

            data.UsersId = userId;
            data.Status = "WaitingApproval";
            data.QuantityReq = data.QuantityReq;
            data.CRD = data.CRD;
            data.Description = data.Description;
            data.CreatedAt = DateTime.Now;
            data.UpdatedAt = DateTime.Now;

            _context.RepeatOrders.Add(data);
            await _context.SaveChangesAsync();

            var repeatOrder = await _context.RepeatOrders
                .Include(ro => ro.ItemRequests)
                    .ThenInclude(ir => ir.Users)
                .FirstOrDefaultAsync(ro => ro.Id == data.Id);

            if (repeatOrder == null)
            {
                TempData["Error"] = "Repeat order tidak ditemukan.";
                return RedirectToAction("Index");
            }
            // kirim email
            //await _emailService.SendNewRequestEmail(repeatOrder.ItemRequests, true, data.QuantityReq, data.CRD, data.Description);

            TempData["Success"] = "Request has been created.";
            return RedirectToAction("index");
        }
    }
}