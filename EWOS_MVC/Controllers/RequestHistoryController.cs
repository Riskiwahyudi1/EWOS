using EWOS_MVC.Models;
using EWOS_MVC.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace EWOS_MVC.Controllers
{
    public class RequestHistoryController : BaseController
    {
        private readonly AppDbContext _context;
        public RequestHistoryController(AppDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> NewRequest(int page = 1)
        {
            int pageSize = 10;
            var newRequest = _context.ItemRequests
                .Include(mc => mc.MachineCategories)
                .Include(u => u.Users)
                .Include(rs => rs.RequestStatus)
                    .ThenInclude(u => u.Users)
                .Where(rq => rq.Status == "Maspro"
                          || rq.Status == "Fail")
                .OrderBy(x => x.Id)
                 .OrderByDescending(r => r.CreatedAt)
                .AsQueryable();


            // PAGINATION
            var paginatedData = await PaginatedHelper<ItemRequestModel>
                .CreateAsync(newRequest, page, pageSize);

            // --- Summary semua status new request---
            var statusSummaryNew = await _context.ItemRequests
                .GroupBy(r => r.Status ?? "Unknown")
                .ToDictionaryAsync(g => g.Key, g => g.Count());

            ViewBag.StatusSummaryNew = statusSummaryNew;
            ViewBag.PageSize = pageSize;
            ViewBag.CurrentStatus = "WaitingApproval";
            return View(paginatedData);
        }

        public async Task<IActionResult> RepeatOrder(int page = 1)
        {
            int pageSize = 10;

            var Ro = _context.RepeatOrders
                .Include(ir => ir.ItemRequests)
                    .ThenInclude(mc => mc.MachineCategories)
                .Include(u => u.Users)
                .Where(rq => rq.Status == "Close")
                .Include(rs => rs.RequestStatus)
                    .ThenInclude(u => u.Users)
                .OrderBy(x => x.Id)
                 .OrderByDescending(r => r.CreatedAt)
                .AsQueryable();


            // PAGINATION
            var paginatedData = await PaginatedHelper<RepeatOrderModel>
                .CreateAsync(Ro, page, pageSize);

            // --- Summary semua status new request---
            var statusSummaryRo = await _context.RepeatOrders
                .GroupBy(r => r.Status ?? "Unknown")
                .ToDictionaryAsync(g => g.Key, g => g.Count());

            ViewBag.StatusSummaryRo = statusSummaryRo;
            ViewBag.PageSize = pageSize;

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
                "Status" => PartialView("~/Views/modals/General/DetailStatus/StatusFabrikasiModal.cshtml"),
                "Detail" => PartialView("~/Views/modals/General/DetailRequest/RequestDetailSummaryModal.cshtml", data),
                _ => BadRequest("Unknown modal type")
            };

            ;
        }
        //load modal progress fabrikasi new request
        [HttpGet]
        public IActionResult LoadDataFab(long id, string type)
        {
            var data = _context.ItemFabrications
                .Include(ir => ir.ItemRequest)
                .Include(u => u.Users)
                .Include(wk => wk.WeeksSetting)
                .Where(IdReq => IdReq.ItemRequestId == id && IdReq.RepeatOrderId == null && IdReq.Status == "FabricationDone")
                .ToList();

            if (data == null) return NotFound();

            return type switch
            {
                "Status" => PartialView("~/Views/modals/General/DetailStatus/StatusFabrikasiModal.cshtml", data),
                _ => BadRequest("Unknown modal type")
            };

            ;
        }
        //load modal progress fabrikasi new request
        [HttpGet]
        public IActionResult LoadDataRoFab(long id, string type)
        {
            var data = _context.ItemFabrications
                .Include(ir => ir.ItemRequest)
                .Include(u => u.Users)
                .Include(wk => wk.WeeksSetting)
                .Where(IdReq => IdReq.RepeatOrderId == id && IdReq.Status == "FabricationDone")
                .ToList(); ;

            if (data == null) return NotFound();

            return type switch
            {
                "Status" => PartialView("~/Views/modals/General/DetailStatus/StatusFabrikasiModal.cshtml", data),
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
                "Detail" => PartialView("~/Views/modals/General/DetailRequest/ItemRequestRoDetailModal.cshtml", data),
                "Status" => PartialView("~/Views/modals/General/DetailStatus/StatusRequestRoModal.cshtml", data),
                _ => BadRequest("Unknown modal type")
            };

            ;
        }


        //search baru
        [HttpGet]
        public IActionResult SearchNew(string keyword, int? categoryId, List<string> status)
        {
            var query = _context.ItemRequests
                .Include(m => m.MachineCategories)
                .Include(u => u.Users)
                 .OrderByDescending(r => r.CreatedAt)
                .AsQueryable();

            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(r => r.PartName.Contains(keyword));
            }

            if (categoryId.HasValue)
            {
                query = query.Where(r => r.MachineCategoryId == categoryId.Value);
            }
            if (status != null && status.Any())
            {
                query = query.Where(r => status.Contains(r.Status));
            }

            var result = query
                .Select(r => new
                {
                    r.Id,
                    r.PartName,
                    CategoryName = r.MachineCategories.CategoryName,
                    Users = r.Users.Name,
                    r.Unit,
                    r.Status,
                    r.MachineCategoryId,
                    r.CreatedAt
                })
                .ToList();

            return Json(result);
        }

        //search Ro
        [HttpGet]
        public IActionResult SearchRo(string keyword, int? categoryId, List<string> status)
        {
            var query = _context.RepeatOrders
                .Include(i => i.ItemRequests)
                    .ThenInclude(mc => mc.MachineCategories)
                .Include(u => u.Users)
                 .OrderByDescending(r => r.CreatedAt)
                .AsQueryable();

            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(r => r.ItemRequests.PartName.Contains(keyword));
            }

            if (categoryId.HasValue)
            {
                query = query.Where(r => r.ItemRequests.MachineCategoryId == categoryId.Value);
            }

            if (status != null && status.Any())
            {
                query = query.Where(r => status.Contains(r.Status));
            }

            var result = query
                .Select(r => new
                {
                    r.Id,
                    r.ItemRequests.PartName,
                    CategoryName = r.ItemRequests.MachineCategories.CategoryName,
                    Users = r.Users.Name,
                    r.QuantityReq,
                    Unit = r.ItemRequests.Unit,
                    r.ItemRequests.MachineCategoryId,
                    r.CreatedAt,
                    r.Status
                })
                .ToList();

            return Json(result);
        }


    }
}
