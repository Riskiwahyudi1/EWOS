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
                .Include(rm => rm.RawMaterials)
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
                    r.MachineCategoryId,
                    r.ExternalFabCost,
                    r.FabricationTime,
                    r.Weight,
                    r.RawMaterialId,
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

        [HttpPost]
        [Route("/AdminFabrication/NewRequest/Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
        ItemRequestModel itemRequest,
        IFormFile? fileDesign,
        IFormFile? fileDrawing,
        IFormFile? fileQuotation)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                TempData["Error"] = "Terjadi kesalahan: " + string.Join(", ", errors);
                return RedirectToAction("Index");
            }

            var existingData = await _context.ItemRequests.FindAsync(itemRequest.Id);
            if (existingData == null)
            {
                TempData["Error"] = "Data tidak ditemukan.";
                return RedirectToAction("Index");
            }

            async Task<string?> SaveFileAsync(IFormFile? file, string folderName, string allowedExt, long maxSizeBytes)
            {
                if (file == null || file.Length == 0) return null;

                var ext = Path.GetExtension(file.FileName).ToLower();
                if (!allowedExt.Split(',').Contains(ext))
                    throw new Exception($"Format file {ext} tidak diizinkan untuk {folderName}.");

                if (file.Length > maxSizeBytes)
                    throw new Exception($"Ukuran file {file.FileName} melebihi batas.");

                var uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), $"wwwroot/uploads/{folderName}");
                if (!Directory.Exists(uploadFolder))
                    Directory.CreateDirectory(uploadFolder);

                var invalidChars = Path.GetInvalidFileNameChars();
                var safePartName = new string(itemRequest.PartName
                    .Where(c => !invalidChars.Contains(c))
                    .ToArray())
                    .Replace(" ", "_");

                var fileName = $"{safePartName}-{Guid.NewGuid()}{ext}";
                var filePath = Path.Combine(uploadFolder, fileName);

                // Hapus file lama jika ada
                string? oldFilePath = folderName switch
                {
                    "design" => existingData.DesignPath != null ? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", existingData.DesignPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)) : null,
                    "drawing" => existingData.DrawingPath != null ? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", existingData.DrawingPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)) : null,
                    "quotation" => existingData.QuantationPath != null ? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", existingData.QuantationPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)) : null,
                    _ => null
                };

                if (oldFilePath != null && System.IO.File.Exists(oldFilePath))
                    System.IO.File.Delete(oldFilePath);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                return $"/uploads/{folderName}/{fileName}";
            }

            try
            {
                // Update file jika ada
                var designPath = await SaveFileAsync(fileDesign, "design", ".zip", 1 * 1024 * 1024);
                var drawingPath = await SaveFileAsync(fileDrawing, "drawing", ".pdf", 1 * 1024 * 1024);
                var quotationPath = await SaveFileAsync(fileQuotation, "quotation", ".pdf", 1 * 1024 * 1024);

                if (designPath != null) existingData.DesignPath = designPath;
                if (drawingPath != null) existingData.DrawingPath = drawingPath;
                if (quotationPath != null) existingData.QuantationPath = quotationPath;
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Gagal upload file: " + ex.Message;
                return RedirectToAction("Index");
            }

            // Update data lain
            existingData.PartName = itemRequest.PartName;
            existingData.RawMaterialId = itemRequest.RawMaterialId;
            existingData.MachineCategoryId = itemRequest.MachineCategoryId;
            existingData.ExternalFabCost = itemRequest.ExternalFabCost;
            existingData.FabricationTime = itemRequest.FabricationTime;
            existingData.Unit = itemRequest.Unit;
            existingData.PartCode = itemRequest.PartCode;
            existingData.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Data dan file berhasil diupdate.";
            return RedirectToAction("Index");
        }

        //tambah fabrikasi
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddFabrikasi(int MachineId, int ItemRequestId)
        {
            int userId = ViewBag.Id != null ? Convert.ToInt32(ViewBag.Id) : 0;
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Data tidak valid: " +
                    string.Join(", ", ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage));
                return Redirect("index");
            }

            // 1️⃣ Ambil tahun aktif
            int tahunSekarang = DateTime.Now.Year;
            var getTahun = await _context.YearsSetting
                .Where(y => y.StartDate <= DateTime.Now)
                .OrderByDescending(y => y.StartDate)
                .FirstOrDefaultAsync();

            // buat default jika belum ada
            if (getTahun == null)
            {
                TempData["Error"] = "Tahun Fabrikasi sekarang belum tersedia, minta admin sistem menambahkan!! ";
                return Redirect("index");

            }

            // generate minggu jika belum ada
            if (!await _context.WeeksSetting.AnyAsync(w => w.YearSettingId == getTahun.Id))
            {
                var weekStart = getTahun.StartDate;
                int weekNumber = 1;
                var akhirTahun = new DateTime(tahunSekarang, 12, 31, 23, 59, 59);

                while (weekStart <= akhirTahun)
                {
                    var nextWeekDate = weekStart.AddDays(7);
                    DateTime weekEnd = new DateTime(
                        nextWeekDate.Year, nextWeekDate.Month, nextWeekDate.Day,
                        6, 59, 59
                    );
                    if (weekEnd > akhirTahun)
                        weekEnd = akhirTahun;

                    _context.WeeksSetting.Add(new WeeksSettingModel
                    {
                        YearSettingId = getTahun.Id,
                        Week = weekNumber,
                        Month = weekStart.Month,
                        WorkingDays = 5.5m,
                        StartDate = weekStart,
                        EndDate = weekEnd
                    });

                    weekNumber++;
                    weekStart = weekEnd.AddSeconds(1);
                }

                await _context.SaveChangesAsync();
            }

            //  Dapatkan minggu aktif
            var mingguSekarang = await _context.WeeksSetting
                .Where(w => w.YearSettingId == getTahun.Id &&
                            DateTime.Now >= w.StartDate &&
                            DateTime.Now <= w.EndDate)
                .FirstOrDefaultAsync();

            if (mingguSekarang == null)
            {
                TempData["Error"] = "Minggu aktif tidak ditemukan.";
                return Redirect("index");
            }

            // Ambil data request
            var requestData = await _context.ItemRequests
                 .Include(r => r.RawMaterials)
                 .FirstOrDefaultAsync(r => r.Id == ItemRequestId);

            if (requestData == null)
                return NotFound();


            //ambil data mesin
            var machine = await _context.Machines.FindAsync(MachineId);


            decimal totalSaving = 0m;
            if (requestData.IsCalculateSaving)
            {
                decimal rawMaterialCost = 0m;
                decimal inhouseCost = 0m;

                if (requestData.MachineCategoryId == 1)
                {
                    // Perhitungan untuk kategori mesin CNC
                    rawMaterialCost = (requestData.RawMaterials?.Price ?? 0m);
                    inhouseCost =
                        (requestData.FabricationTime ?? 0m)
                        * (getTahun.ElectricalCost ?? 0m)
                        * (machine.MachinePower);

                    totalSaving = (requestData.ExternalFabCost ?? 0m) - (inhouseCost + rawMaterialCost);
                }
                if (requestData.MachineCategoryId == 2)
                {
                    // Perhitungan untuk kategori mesin 3d Printing
                    rawMaterialCost = ((requestData.Weight ?? 0m) / 1000) * (requestData.RawMaterials?.Price ?? 0m);
                    inhouseCost = ((requestData.FabricationTime ?? 0m)) * ((getTahun.ElectricalCost ?? 0m) * (machine.MachinePower));
                    totalSaving = (requestData.ExternalFabCost ?? 0m) - (inhouseCost + rawMaterialCost);


                }
            }

            //  Jalankan semua perubahan dalam 1 transaksi
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // buat record baru
                var item = new ItemFabricationModel
                {
                    ItemRequestId = ItemRequestId,
                    MachineId = MachineId,
                    TotalSaving = totalSaving,
                    WeeksSettingId = mingguSekarang.Id,
                    Status = "Onprogress",
                    Quantity = 1,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                _context.ItemFabrications.Add(item);
                
                var StatusLog = new RequestStatusModel
                {
                    ItemRequestId = ItemRequestId,
                    Status = "FabricationStarted",
                    UserId = userId,
                    CreatedAt = DateTime.Now,
                };

                _context.RequestStatus.Add(StatusLog);

                requestData.Status = "InFabrication";
                requestData.UpdatedAt = DateTime.Now;
                _context.ItemRequests.Update(requestData);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["Success"] = "Plan has been created.";
                return Redirect("index");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["Error"] = "Terjadi kesalahan: " + ex.Message;
                return Redirect("index");
            }


        }
    }
}
