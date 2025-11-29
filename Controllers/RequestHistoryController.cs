using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EWOS_MVC.Controllers
{
    public class RequestHistoryController : BaseController
    {
        private readonly AppDbContext _context;
        public RequestHistoryController(AppDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> NewRequest()
        {
            var newRequest = await _context.ItemRequests
                               .Include(mc => mc.MachineCategories)
                               .Include(u => u.Users)
                               .Include(rs => rs.RequestStatus)
                                   .ThenInclude(u => u.Users)
                               .ToListAsync();
            return View(newRequest);
        }
        public async Task<IActionResult> RepeatOrder()
        {

            var Ro = await _context.RepeatOrders
                               .Include(ir => ir.ItemRequests)
                                    .ThenInclude(mc => mc.MachineCategories)
                               .Include(u => u.Users)
                               .Include(rs => rs.RequestStatus)
                                   .ThenInclude(u => u.Users)
                               .ToListAsync();
            return View(Ro);
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
                .ThenInclude(u => u.Users)
                .Include(wk => wk.WeeksSetting)
                .Where(IdReq => IdReq.ItemRequestId == id && IdReq.RepeatOrderId == null)
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
                .ThenInclude(u => u.Users)
                .Include(wk => wk.WeeksSetting)
                .Where(IdReq => IdReq.RepeatOrderId == id )
                .ToList();

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
        public IActionResult SearchNew(string keyword, int? categoryId)
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

    }
}
