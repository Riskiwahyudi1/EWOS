using EWOS_MVC.Models;
using EWOS_MVC.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EWOS_MVC.Areas.Requestor.Controllers
{
    [Authorize(Roles = "Requestor,AdminFabrication,AdminSystem,Supervisor")]
    public class FabricationHistoryController : BaseController
    {
        private readonly AppDbContext _context;
        private readonly WeekHelper _weekHelper;
        public FabricationHistoryController(AppDbContext context, WeekHelper weekHelper)
        {
            _context = context;
            _weekHelper = weekHelper;
        }

        public async Task<IActionResult> Index()
        {
            //dapatkan tahun
            int tahunSekarang = DateTime.Now.Year;
            var getTahun = await _context.YearsSetting
                .FirstOrDefaultAsync(y => y.Year == tahunSekarang);

            if (getTahun == null)
            {
                TempData["Error"] = "Tahun Fabrikasi sekarang belum tersedia, minta admin sistem menambahkan!! ";
                return RedirectToAction("Index");
            }

            //ambil week sekarang
            var mingguSekarang = await _context.WeeksSetting
                .FirstOrDefaultAsync(w =>
                    w.YearSettingId == getTahun.Id &&
                    DateTime.Now >= w.StartDate &&
                    DateTime.Now <= w.EndDate);

            if (mingguSekarang == null)
            {
                TempData["Error"] = "Minggu aktif tidak ditemukan.";
                return RedirectToAction("Index");
            }

            //ambil list data fabrikasi
            var fabricationList = await _context.ItemFabrications
                    .Include(ir => ir.ItemRequest)
                        .ThenInclude(u => u.Users)
                    .Include(ro => ro.RepeatOrder)
                        .ThenInclude(u => u.Users)
                    .Include(m => m.Machine)
                        .ThenInclude(m => m.MachineCategories)
                    .Where(wk => wk.WeeksSettingId == mingguSekarang.Id)
                    .ToListAsync();

            //total jam fabrikasi mesin
            decimal totalTimeFabrikasi = await _context.ItemFabrications
                    .Where(wk => wk.WeeksSettingId == mingguSekarang.Id)
                    .SumAsync(m => m.FabricationTime);

            //ambil data semua mesin
            int jumlahMesin = await _context.Machines
                .Where(m => m.MachineCategoryId != 3)
                .CountAsync();

            decimal totalJam = mingguSekarang.WorkingDays * 24 * jumlahMesin;
            decimal percentase = 0;

            if (totalJam > 0)
            {
                percentase = (totalTimeFabrikasi / totalJam) * 100;
            }
            percentase = Math.Round(percentase, 2);
            ViewBag.MachineUtilization = percentase;
            return View(fabricationList);
        }

        // search
        [HttpGet]
        public IActionResult Search(string keyword, int? categoryId, string status, int? weekSettingId, int? MachineId, int? yearSettingId)
        {
            var query = _context.ItemFabrications
                .Include(m => m.Machine)
                .Include(w => w.WeeksSetting)
                    .ThenInclude(y => y.YearsSetting)
                .Include(i => i.ItemRequest)
                    .ThenInclude(m => m.MachineCategories)
                .AsQueryable();

            // Filter keyword (PartName dan Status)
            if (!string.IsNullOrEmpty(keyword))
            {
                keyword = keyword.ToLower();
                query = query.Where(r =>
                    (r.ItemRequest != null && r.ItemRequest.PartName.ToLower().Contains(keyword)) ||
                    (r.Status != null && r.Status.ToLower().Contains(keyword))
                );
            }

            // Filter kategori
            if (categoryId.HasValue)
            {
                query = query.Where(r =>
                    r.ItemRequest != null && r.ItemRequest.MachineCategoryId == categoryId.Value
                );
            }

            // Filter mesin
            if (MachineId.HasValue)
            {
                query = query.Where(r =>
                    r.MachineId != null && r.MachineId == MachineId.Value
                );
            }

            // Filter week
            if (weekSettingId.HasValue)
            {
                query = query.Where(r =>
                     r.WeeksSettingId == weekSettingId.Value
                );
            }
            // Filter year
            if (yearSettingId.HasValue)
            {
                query = query.Where(r =>
                     r.WeeksSetting.YearSettingId == yearSettingId.Value
                );
            }

            // Filter status (exact match, misalnya dari dropdown)
            if (!string.IsNullOrEmpty(status))
            {
                status = status.ToLower();
                query = query.Where(r => r.Status.ToLower() == status);
            }

            // =========================
            // Hitung Machine Utilization
            // =========================

            //ambil week sekarang
            var mingguSekarang = _context.WeeksSetting
                .FirstOrDefault(w =>
                    DateTime.Now >= w.StartDate &&
                    DateTime.Now <= w.EndDate);

            var week = _context.WeeksSetting.FirstOrDefault(w =>
                    (weekSettingId.HasValue && w.Id == weekSettingId.Value) ||
                    (!weekSettingId.HasValue && mingguSekarang != null && w.Id == mingguSekarang.Id)
                );
            if (week == null)
            {
                return Json(new { error = "Week Setting tidak ditemukan" });
            }

            List<int> mesinIds = new List<int>();
            string machineName = null;
            string categoryName = null;

            if (MachineId.HasValue)
            {
                // Ambil hanya satu mesin
                var mesin = _context.Machines
                    .Where(m => m.Id == MachineId.Value)
                    .Select(m => new { m.Id, m.MachineName, m.MachineCategoryId })
                    .FirstOrDefault();

                if (mesin != null)
                {
                    mesinIds = new List<int> { mesin.Id };
                    machineName = mesin.MachineName;
                }
            }
            else if (categoryId.HasValue)
            {
                // Ambil semua mesin dalam kategori
                mesinIds = _context.Machines
                    .Where(m => m.MachineCategoryId == categoryId.Value)
                    .Select(m => m.Id)
                    .ToList();

                var kategori = _context.MachineCategories
                    .Where(c => c.Id == categoryId.Value)
                    .Select(c => c.CategoryName)
                    .FirstOrDefault();

                categoryName = kategori;
            }
            else
            {
                // Semua mesin
                mesinIds = _context.Machines
                    .Select(m => m.Id)
                    .ToList();
            }

            // Hitung Utilization
            decimal totalTimeFabrikasi = _context.ItemFabrications
                .Where(m => mesinIds.Contains(m.MachineId) &&
                            m.WeeksSettingId == week.Id)
                .Sum(m => (decimal?)m.FabricationTime ?? 0);


            int jumlahMesin = mesinIds.Count;
            decimal totalJam = week.WorkingDays * 24 * jumlahMesin;

            decimal percentase = totalJam > 0
                ? Math.Round((totalTimeFabrikasi / totalJam) * 100, 2)
                : 0;

            // Ambil data item
            var result = query
                .Select(r => new
                {
                    r.Id,
                    PartName = r.ItemRequest.PartName,
                    r.ItemRequest.CRD,
                    r.Status,
                    r.TotalSaving,
                    r.FabricationTime,
                    r.Quantity,
                    r.CreatedAt
                })
                .ToList();

            // Return JSON lengkap
            return Json(new
            {
                data = result,
                utilization = percentase,
                machineName = machineName,
                categoryName = categoryName,

            });
        }

        // Proses Finish
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Finish(int Id, long ItemRequestId, long? RepeatOrderId)
        {
            if (Id <= 0)
            {
                TempData["Error"] = "Id fabrikasi tidak valid.";
                return Redirect("index");
            }

            if (ItemRequestId <= 0)
            {
                TempData["Error"] = "ItemRequestId tidak valid.";
                return Redirect("index");
            }

            // Ambil data new ItemRequest
            var requestData = await _context.ItemRequests.FindAsync(ItemRequestId);
            if (requestData == null)
            {
                TempData["Error"] = "Data ItemRequest tidak ditemukan.";
                return Redirect("index");
            }

            // Ambil data RepeatOrder hanya jika RepeatOrderId ada
            RepeatOrderModel? requestDataRo = null;
            if (RepeatOrderId.HasValue)
            {
                requestDataRo = await _context.RepeatOrders.FindAsync(RepeatOrderId.Value);
                if (requestDataRo == null)
                {
                    TempData["Error"] = "Data Repeat Order tidak ditemukan.";
                    return Redirect("index");
                }
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Cari data fabrikasi
                var fabricationData = await _context.ItemFabrications
                    .FirstOrDefaultAsync(f => f.Id == Id);

                if (fabricationData == null)
                {
                    TempData["Warning"] = "Tidak ditemukan data ItemFabrication yang terkait.";
                    return Redirect("index");
                }

                // Jika ini request dari RepeatOrder
                if (RepeatOrderId.HasValue)
                {
                    requestDataRo.QuantityDone = (requestDataRo.QuantityDone ?? 0) + fabricationData.Quantity;

                    // finish jika sudah habis
                    if(requestDataRo.QuantityReq == 0)
                    {
                        requestDataRo.Status = "Done";
                    }
                    _context.RepeatOrders.Update(requestDataRo);
                }
                else
                {
                    // Jika request biasa
                    requestData.Status = "WaitingBuyoff";
                    requestData.UpdatedAt = DateTime.Now;

                    _context.ItemRequests.Update(requestData);
                }

                // Update data fabrikasi
                  fabricationData.Status = "FabricationDone";
                _context.ItemFabrications.Update(fabricationData);

                // Simpan seluruh perubahan
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["Success"] = "Data berhasil disimpan.";
                return Redirect("index");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["Error"] = "Terjadi kesalahan saat memproses: " + ex.Message;
                return Redirect("index");
            }
        }


        //Proses cancel
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(long ItemRequestId, long itemFabId, long? RepeatOrderId)
        {
            if (ItemRequestId <= 0)
            {
                TempData["Error"] = "ItemRequestId tidak valid.";
                return Redirect("index");
            }

            //cari request baru
            var requestData = await _context.ItemRequests.FindAsync(ItemRequestId);
            if (requestData == null)
            {
                TempData["Error"] = "Data ItemRequest tidak ditemukan.";
                return Redirect("index");
            }
            //cari requesst ulang

            var repeatOrderData = await _context.RepeatOrders.FirstOrDefaultAsync(r => r.Id == RepeatOrderId);


            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Cari  data ItemFabrication 
                var fabricationData = await _context.ItemFabrications
                    .FirstOrDefaultAsync(f => f.Id == itemFabId);

                if (fabricationData != null)
                {

                    // Hapus data fabrication
                    _context.ItemFabrications.Remove(fabricationData);

                    //cek request baru atau repeat
                    if (RepeatOrderId == null)
                    {
                        //jika baru
                        requestData.UpdatedAt = DateTime.Now;
                        requestData.Status = "WaitingFabrication";

                        _context.ItemRequests.Update(requestData);
                        await _context.SaveChangesAsync();

                        await transaction.CommitAsync();
                        TempData["Success"] = "Fabrikasi berhasil dibatalkan .";

                    }
                    else
                    {

                        repeatOrderData.QuantityReq += fabricationData.Quantity;
                        repeatOrderData.UpdatedAt = DateTime.Now;

                        _context.RepeatOrders.Update(repeatOrderData);
                        await _context.SaveChangesAsync();

                        await transaction.CommitAsync();
                        TempData["Success"] = "Fabrikasi berhasil dibatalkan .";
                    }

                }
                else
                {
                    TempData["Warning"] = "Tidak ditemukan data ItemFabrication yang terkait.";
                }

                return Redirect("index");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["Error"] = "Terjadi kesalahan saat membatalkan data: " + ex.Message;
                return Redirect("index");
            }
        }

        //Edit quantity
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int Quantity, int ItemFabricationId, long RepeatOrderId)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);

                TempData["Error"] = "Terjadi kesalahan: " + string.Join(", ", errors);
                return View("Index");
            }

            var existingData = await _context.ItemFabrications.FindAsync(ItemFabricationId);
            if (existingData == null)
            {
                return NotFound();
            }
            var exitingRepeatOdr = await _context.RepeatOrders.FindAsync(RepeatOrderId);
            if (exitingRepeatOdr == null)
            {
                return NotFound();
            }

            //var weekToday = await _weekHelper.GetMingguAktifTodayAsync();

            ////data week

            //var weekId = WeekSettingId ?? weekToday.Id;

            //Data saving edit
            var bagiSaving = (existingData.TotalSaving / existingData.Quantity);
            var DataSavingBaru = bagiSaving * Quantity;

            //data time fab edit
            var bagiWkt = (existingData.FabricationTime / existingData.Quantity);
            var dataWktBaru = bagiWkt * Quantity;

            //Edit quantity fabrikasi
            var selisih = Quantity - existingData.Quantity;

            if (selisih > 0)
            {

                exitingRepeatOdr.QuantityReq -= selisih;
            }
            else if (selisih < 0)
            {
                exitingRepeatOdr.QuantityReq += Math.Abs(selisih);
            }
            existingData.TotalSaving = DataSavingBaru;
            existingData.Quantity = Quantity;
            existingData.FabricationTime = dataWktBaru;
            //existingData.WeeksSettingId = weekId;
            existingData.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Data berhasil diubah.";
            return Redirect("index");
        }
    }
}