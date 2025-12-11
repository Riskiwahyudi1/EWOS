using EWOS_MVC.Models;
using EWOS_MVC.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EWOS_MVC.Areas.Requestor.Controllers
{
    [Authorize(Roles = "Requestor,AdminFabrication,AdminSystem,EngineerFabrication")]
    public class FabricationHistoryController : BaseController
    {
        private readonly AppDbContext _context;
        private readonly WeekHelper _weekHelper;
        private readonly YearsHelper _yearHelper;
        public FabricationHistoryController(AppDbContext context, WeekHelper weekHelper, YearsHelper yearHelper)
        {
            _context = context;
            _weekHelper = weekHelper;
            _yearHelper = yearHelper;
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
                    .Where(wk => wk.WeeksSettingId == mingguSekarang.Id && (wk.Status == "FabricationDone" || wk.Status == "Evaluation"))
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

        // load data modal
        public IActionResult LoadData(int id, string type)
        {
            var data = _context.ItemFabrications
                .Include(i => i.ItemRequest)
                .Include(m => m.Machine)
                .Include(i => i.RepeatOrder)
                .FirstOrDefault(i => i.Id == id);

            if (data == null) return NotFound();

            return type switch
            {
                "Edit" => PartialView("~/Views/modals/AdminFabrication/FabricationHistory/EditItemFabModal.cshtml", data),
                "Finish" => PartialView("~/Views/modals/AdminFabrication/FabricationHistory/FinishItemFabModal.cshtml", data),
                "updateCOC" => PartialView("~/Views/modals/AdminFabrication/FabricationHistory/UpdateCOCModal.cshtml", data),
                "evaluasi" => PartialView("~/Views/modals/AdminFabrication/FabricationHistory/EvaluasiModal.cshtml", data),
                "Cancel" => PartialView("~/Views/modals/AdminFabrication/FabricationHistory/CancelItemFabModal.cshtml", data),
                "CancelEval" => PartialView("~/Views/modals/AdminFabrication/FabricationHistory/CancelEvaluationModal.cshtml", data),
                "Detail" => PartialView("~/Views/modals/AdminFabrication/FabricationHistory/DetailItemFabModal.cshtml", data),
                _ => BadRequest("Unknown modal type")
            };

            ;
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
                .Where(y =>y.YearSettingId == yearSettingId )
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
                            m.WeeksSettingId == week.Id && m.WeeksSetting.YearSettingId == yearSettingId)
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
                    RepeatOrderId = r.RepeatOrderId,
                    r.ItemRequest.CRD,
                    r.Status,
                    machineCategory = r.ItemRequest.MachineCategoryId,
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

        // upload dan update COC
        private async Task<string?> UpdateCOCFileAsync(
          IFormFile? fileCOC,
          RepeatOrderModel? repeatOrder,
          ItemRequestModel? itemRequest,
          ItemFabricationModel itemFabrication,
          string? rawName
      )
        {
            if (fileCOC == null)
                return null;

            // Sanitasi nama file
            var invalidChars = Path.GetInvalidFileNameChars();
            var safeName = new string(rawName
                .Where(c => !invalidChars.Contains(c))
                .ToArray());

            safeName = safeName.Replace(" ", "-");

            if (string.IsNullOrWhiteSpace(safeName))
                safeName = "COC";

            // Prefix COC-
            safeName = $"COC-{safeName}";

            // Validasi file
            var ext = Path.GetExtension(fileCOC.FileName).ToLower();
            var allowedExt = ".pdf";

            if (!allowedExt.Contains(ext))
                throw new Exception($"Format file {ext} tidak diizinkan.");

            if (fileCOC.Length > (1 * 1024 * 1024))
                throw new Exception($"Ukuran file melebihi batas 1MB.");

            // Folder upload
            var uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "Storage/Uploads/COC");

            if (!Directory.Exists(uploadFolder))
                Directory.CreateDirectory(uploadFolder);

            // Tentukan target path dan hapus file lama
            if (itemFabrication.ItemRequestId == null)  
            {
                if (!string.IsNullOrEmpty(repeatOrder?.COCPath))
                {
                    var oldPath = Path.Combine(Directory.GetCurrentDirectory(), "Storage", repeatOrder.COCPath.TrimStart('/'));
                    if (System.IO.File.Exists(oldPath))
                        System.IO.File.Delete(oldPath);
                }
            }
            else  // ItemRequest
            {
                if (!string.IsNullOrEmpty(itemRequest?.COCPath))
                {
                    var oldPath = Path.Combine(Directory.GetCurrentDirectory(), "Storage", itemRequest.COCPath.TrimStart('/'));
                    if (System.IO.File.Exists(oldPath))
                        System.IO.File.Delete(oldPath);
                }
            }

            // Simpan file baru
            var fileName = $"{safeName}-{Guid.NewGuid()}{ext}";
            var filePath = Path.Combine(uploadFolder, fileName);

            using var stream = new FileStream(filePath, FileMode.Create);
            await fileCOC.CopyToAsync(stream);

            var finalPath = $"/uploads/COC/{fileName}";

            // Simpan path ke model yang benar
            if (itemFabrication.RepeatOrderId == null)
            {
                itemRequest.COCPath = finalPath;
            }
            else
            {
                repeatOrder.COCPath = finalPath;
            }

            return finalPath;
        }

        //update COC

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateCOC(int Id, long ItemRequestId, long? RepeatOrderId, IFormFile? fileCOC, string FabCode)
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
                requestDataRo = await _context.RepeatOrders.Include(ir => ir.ItemRequests).FirstOrDefaultAsync(r => r.Id == RepeatOrderId);
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
                // Cek request Ro || new request
                if (RepeatOrderId.HasValue)
                {
                    // Upload file dan simpan path
                    await UpdateCOCFileAsync(fileCOC, requestDataRo, requestData, fabricationData, requestDataRo.ItemRequests.PartName);

                    _context.RepeatOrders.Update(requestDataRo);
                }
                else
                {
                    await UpdateCOCFileAsync(fileCOC, requestDataRo, requestData, fabricationData, requestData.PartName);

                    _context.ItemRequests.Update(requestData);
                }


                fabricationData.FabCode = FabCode;
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


        // Proses Finish
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Finish(int Id, long ItemRequestId, long? RepeatOrderId, IFormFile? fileCOC, string FabCode)
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

                // Cek request Ro || new request
                if (RepeatOrderId.HasValue)
                {
                    // Upload file dan simpan path
                    await UpdateCOCFileAsync(fileCOC, requestDataRo, requestData, fabricationData, requestDataRo.ItemRequests.PartName);
                    //hitung apakah sudah di fabrikasi semua?
                    var calculateFabrication = (requestDataRo.QuantityDone ?? 0) + fabricationData.Quantity;

                    requestDataRo.QuantityDone = calculateFabrication;
                    requestDataRo.QtyOnFab = Math.Max(0, (requestDataRo.QtyOnFab ?? 0) - fabricationData.Quantity);


                    // Jika sudah selesai semau ubah status repeat order
                    if (requestDataRo.QuantityReq == calculateFabrication)
                    {
                        requestDataRo.Status = "Done";
                    }
                    _context.RepeatOrders.Update(requestDataRo);
                }
                else
                {
                    await UpdateCOCFileAsync(fileCOC, requestDataRo, requestData, fabricationData, requestData.PartName);

                    // Jika request baru 
                    requestData.Status = "WaitingBuyoff";
                    requestData.UpdatedAt = DateTime.Now;

                    _context.ItemRequests.Update(requestData);
                }

                // Update data fabrikasi
                fabricationData.Status = "FabricationDone";
                fabricationData.FabCode = FabCode;
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
                        repeatOrderData.UpdatedAt = DateTime.Now;
                        repeatOrderData.Status = "WaitingFabrication";
                        repeatOrderData.QtyOnFab = Math.Max(0, (repeatOrderData.QtyOnFab ?? 0) - fabricationData.Quantity);


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

        //Proses cancel
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelEval(long itemFabId)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Cari data Evaluasi
                var fabricationData = await _context.ItemFabrications
                    .FirstOrDefaultAsync(f => f.Id == itemFabId);

                if (fabricationData != null)
                {
                    // Hapus data evaluasi
                    _context.ItemFabrications.Remove(fabricationData);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync(); 

                    TempData["Success"] = "Evaluasi dibatalkan.";
                }
                else
                {
                    TempData["Error"] = "Tidak ditemukan data ItemFabrication yang terkait.";
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(); 
                TempData["Error"] = "Terjadi kesalahan saat membatalkan data: " + ex.Message;
            }

            return RedirectToAction("Index");
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

            //Data saving edit
            var bagiSaving = (existingData.TotalSaving / existingData.Quantity);
            var DataSavingBaru = bagiSaving * Quantity;

            //data time fab edit
            var bagiWkt = (existingData.FabricationTime / existingData.Quantity);
            var dataWktBaru = bagiWkt * Quantity;

            //Edit quantity fabrikasi
            exitingRepeatOdr.QtyOnFab ??= 0;

            var selisih = Math.Abs(Quantity - existingData.Quantity);

            if (Quantity > existingData.Quantity)
            {
                exitingRepeatOdr.QtyOnFab += selisih;
            }
            else if (Quantity < existingData.Quantity)
            {
                exitingRepeatOdr.QtyOnFab = Math.Max(0, exitingRepeatOdr.QtyOnFab.Value - selisih);
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

    //tambah fabrikasi request baru
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddEvaluasi(int MachineId, long ItemRequestId, decimal EvaluationTime)
        {
            int userId = 1;

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Data tidak valid: " +
                    string.Join(", ", ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage));
                return Redirect("/FabricationHistory");
            }

            //  Ambil tahun aktif
            int tahunSekarang = DateTime.Now.Year;
            var getTahun = await _yearHelper.GetCurrentYearAsync();

            // buat default jika belum ada
            if (getTahun == null)
            {
                TempData["Error"] = "Tahun Fabrikasi sekarang belum tersedia, minta admin sistem menambahkan!! ";
                return Redirect("/FabricationHistory");

            }
            //  get mg aktif dari helper
            var mingguSekarang = await _weekHelper.GetMingguAktifAsync(getTahun.Id);

            if (mingguSekarang == null)
            {
                TempData["Error"] = "Minggu aktif tidak ditemukan.";
                return Redirect("/FabricationHistory");
            }

            //ambil data mesin
            var machine = await _context.Machines.FindAsync(MachineId);
            if (machine == null)
                return NotFound();

            //  Jalankan semua perubahan dalam 1 transaksi
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // buat record baru
                var item = new ItemFabricationModel
                {
                    ItemRequestId = ItemRequestId,
                    MachineId = MachineId,
                    UserId = userId,
                    WeeksSettingId = mingguSekarang.Id,
                    Status = "Evaluation",
                    Quantity = 1,
                    FabricationTime = EvaluationTime,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                _context.ItemFabrications.Add(item);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["Success"] = "Evaluation has been created.";
                return Redirect("/FabricationHistory");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["Error"] = "Terjadi kesalahan: " + ex.Message;
                return Redirect("/FabricationHistory");
            }

        }

    }
}