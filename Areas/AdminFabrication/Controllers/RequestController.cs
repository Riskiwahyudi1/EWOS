using EWOS_MVC.Models;
using EWOS_MVC.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace EWOS_MVC.Areas.AdminFabrication.Controllers
{
    [Authorize(Roles = "AdminSystem,AdminFabrication")]
    [Area("AdminFabrication")]

    public class RequestController : BaseController
    {
        private readonly AppDbContext _context;
        private readonly WeekHelper _weekHelper;
        private readonly YearsHelper _yearHelper;

        public RequestController(AppDbContext context, WeekHelper weekHelper, YearsHelper yearsHelper)
        {
            _context = context;
            _weekHelper = weekHelper;
            _yearHelper = yearsHelper;
        }

        //request baru(Evaluasi)
        public async Task<IActionResult> Evaluation()
        {
            var requestList = await _context.ItemRequests
                                .Include(mc => mc.MachineCategories)
                                .Include(u => u.Users)
                                .Include(rs => rs.RequestStatus)
                                    .ThenInclude(u => u.Users)
                                .Where(rq => rq.Status == "WaitingApproval")
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
            ViewBag.CurrentStatus = "FabricationApproval"; //!!
            ViewBag.ModalData = modalData;
            return View(requestList);
        }

        //request lama(Repeat Order)
        public async Task<IActionResult> RepeatOrder()
        {
            var requestList = await _context.RepeatOrders
                .Include(rq => rq.ItemRequests)
                    .ThenInclude(mc => mc.MachineCategories)
                .Include(u => u.Users)
                .Where(rq => rq.Status == "WaitingApproval" && rq.QuantityReq >= 0)
                .ToListAsync();

            //data modal
            var modalData = await _context.RepeatOrders
                .Include(rq => rq.ItemRequests)
                    .ThenInclude(mc => mc.MachineCategories)
                .Include(rq => rq.ItemRequests)
                    .ThenInclude(rm => rm.RawMaterials)
                .Include(u => u.Users)
                .Include(rs => rs.RequestStatus)
                    .ThenInclude(u => u.Users)
                .ToListAsync();


            var statusSummary = await _context.RepeatOrders
               .Where(qty => qty.QuantityReq > 0)
               .GroupBy(r => r.Status ?? "Unknown")
               .ToDictionaryAsync(g => g.Key, g => g.Count());

            ViewBag.StatusSummary = statusSummary;
            ViewBag.CurrentStatus = "FabricationApproval"; //!!
            ViewBag.ModalData = modalData;
            return View(requestList);
        }

        // search req baru
        [HttpGet]
        public IActionResult SearchNew(string keyword, int? categoryId, string status)
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

        // search RO
        [HttpGet]
        public IActionResult SearchRO(string keyword, int? categoryId, string status)
        {
            var query = _context.RepeatOrders
                 .Include(rq => rq.ItemRequests)
                    .ThenInclude(mc => mc.MachineCategories)
                .Include(u => u.Users)
                .Where(qty => qty.QuantityReq > 0)
                .AsQueryable();

            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(r => r.ItemRequests.PartName.Contains(keyword));
            }

            if (categoryId.HasValue)
            {
                query = query.Where(r => r.ItemRequests.MachineCategoryId == categoryId.Value);
            }

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(r => r.Status == status);
            }

            var result = query
                .Select(r => new
                {
                    r.Id,
                    partName = r.ItemRequests.PartName,
                    CategoryName = r.ItemRequests.MachineCategories.CategoryName,
                    machineCategoryId = r.ItemRequests.MachineCategories.Id,
                    Users = r.Users.Name,
                    r.QuantityReq,
                    r.QuantityDone,
                    r.CRD,
                    r.Status,
                    r.CreatedAt
                })
                .ToList();

            return Json(result);
        }

        //Approve request
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(long ItemRequestId, DateTime OCD, long? repeatOrderId)
        {
            int userId = ViewBag.Id != null ? Convert.ToInt32(ViewBag.Id) : 0;

            //validasi
            if (ItemRequestId <= 0)
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

            string redirectUrl = "/AdminFabrication/Request/Evaluation";

            //menerima data dari file AdminFabrication/Request/RepeatOrder.cshtml
            if (repeatOrderId.HasValue)
            {
                var repeatOrder = await _context.RepeatOrders.FindAsync(repeatOrderId.Value);
                if (repeatOrder == null)
                    return NotFound();
                repeatOrder.OCD = OCD;
                repeatOrder.Status = "WaitingFabrication";
                repeatOrder.UpdatedAt = DateTime.Now;

                redirectUrl = "/AdminFabrication/Request/RepeatOrder";
            }
            //menerima data dari file AdminFabrication/Request/Evaluation.cshtml
            else
            {
                var itemRequest = await _context.ItemRequests.FindAsync(ItemRequestId);
                if (itemRequest == null)
                    return NotFound();

                itemRequest.OCD = OCD;
                itemRequest.Status = "WaitingFabrication";
                itemRequest.UpdatedAt = DateTime.Now;
            }

            var approvalRequest = new RequestStatusModel
            {
                ItemRequestId = ItemRequestId,
                RepeatOrderId = repeatOrderId,
                Status = "FabricationApproved",
                UserId = userId,
                CreatedAt = DateTime.Now
            };

            _context.RequestStatus.Add(approvalRequest);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Berhasil melakukan Approve.";
            return Redirect(redirectUrl);
        }

        //Reject Request
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(long ItemRequestId, string Reason)
        {
            int userId = ViewBag.Id != null ? Convert.ToInt32(ViewBag.Id) : 0;
            if (ItemRequestId >= 0)
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

        //edit data
        [HttpPost]
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
                return RedirectToAction("Evaluation");
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
                return RedirectToAction("Evaluation");
            }

            // Update data lain
            existingData.PartName = itemRequest.PartName;
            existingData.RawMaterialId = itemRequest.RawMaterialId;
            existingData.MachineCategoryId = itemRequest.MachineCategoryId;
            existingData.ExternalFabCost = itemRequest.ExternalFabCost;
            existingData.FabricationTime = itemRequest.FabricationTime;
            existingData.Weight = itemRequest.Weight;
            existingData.Unit = itemRequest.Unit;
            existingData.PartCode = itemRequest.PartCode;
            existingData.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Data dan file berhasil diupdate.";
            return RedirectToAction("Evaluation");
        }

        //tambah fabrikasi
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddFabrikasi(int MachineId, long ItemRequestId, long? RepeatOrderId, int? Quantity)
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

            //  Ambil tahun aktif
            int tahunSekarang = DateTime.Now.Year;
            var getTahun = await _yearHelper.GetCurrentYearAsync();

            // buat default jika belum ada
            if (getTahun == null)
            {
                TempData["Error"] = "Tahun Fabrikasi sekarang belum tersedia, minta admin sistem menambahkan!! ";
                return Redirect("index");

            }

            

            //  get mg aktif dari helper
            var mingguSekarang = await _weekHelper.GetMingguAktifAsync(getTahun.Id);

            if (mingguSekarang == null)
            {
                TempData["Error"] = "Minggu aktif tidak ditemukan.";
                return Redirect("index");
            }

            // Ambil data request baru
            var requestData = await _context.ItemRequests
                 .Include(r => r.RawMaterials)
                 .FirstOrDefaultAsync(r => r.Id == ItemRequestId);

            if (requestData == null)
                return NotFound();

            // Ambil data RO
            var requestDataRo = await _context.RepeatOrders
                 .Include(ir => ir.ItemRequests)
                    .ThenInclude(r => r.RawMaterials)
                 .FirstOrDefaultAsync(r => r.Id == RepeatOrderId);

            //ambil data mesin
            var machine = await _context.Machines.FindAsync(MachineId);
            if (machine == null)
                return NotFound();




            //pisah req baru dan RO
            decimal totalSaving = 0m;
            decimal fabricationTime = requestData.FabricationTime ?? 0m;
            var RedirectUrl = "/AdminFabrication/Request/Evaluation";

            //hitung saving RO
            if (RepeatOrderId.HasValue)
            {
                if (requestDataRo.ItemRequests.IsCalculateSaving)
                {
                    decimal rawMaterialCost = 0m;
                    decimal inhouseCost = 0m;
                    decimal externalCost = (requestDataRo.ItemRequests.ExternalFabCost ?? 0m) * (Quantity ?? 0);
                    fabricationTime = (requestDataRo.ItemRequests.FabricationTime ?? 0m) * (Quantity ?? 0);

                    if (requestDataRo.ItemRequests.MachineCategoryId == 1)
                    {
                        // Perhitungan untuk kategori mesin CNC
                        rawMaterialCost = (requestDataRo.ItemRequests.RawMaterials?.Price ?? 0m) * (Quantity ?? 0m);
                        inhouseCost = fabricationTime * (getTahun.ElectricalCost ?? 0m) * (machine.MachinePower);
                        totalSaving = externalCost - (inhouseCost + rawMaterialCost);

                    }
                    if (requestDataRo.ItemRequests.MachineCategoryId == 2)
                    {
                        // Perhitungan untuk kategori mesin 3d Printing
                        rawMaterialCost = ((requestDataRo.ItemRequests.Weight ?? 0m) / 1000) * (requestDataRo.ItemRequests.RawMaterials?.Price ?? 0m);
                        inhouseCost = ((requestDataRo.ItemRequests.FabricationTime ?? 0m)) * ((getTahun.ElectricalCost ?? 0m) * (machine.MachinePower));
                        totalSaving = (requestDataRo.ItemRequests.ExternalFabCost ?? 0m) - (inhouseCost + rawMaterialCost);

                    }
                }

                RedirectUrl = "/AdminFabrication/Request/RepeatOrder";
            }
            else
            {
                //hitung saving New Request

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
            }

            //  Jalankan semua perubahan dalam 1 transaksi
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {

                var cekItemFab = await _context.ItemFabrications
                    .FirstOrDefaultAsync(r => r.RepeatOrderId == RepeatOrderId && r.MachineId == MachineId);

                if(cekItemFab == null)
                {
                    // buat record baru
                    var item = new ItemFabricationModel
                    {
                        ItemRequestId = ItemRequestId,
                        RepeatOrderId = RepeatOrderId,
                        MachineId = MachineId,
                        TotalSaving = totalSaving,
                        WeeksSettingId = mingguSekarang.Id,
                        Status = "Onprogress",
                        Quantity = Quantity ?? 1,
                        FabricationTime = fabricationTime,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    };

                    _context.ItemFabrications.Add(item);

                }
                else
                {
                    var savingCostActual = cekItemFab.TotalSaving;

                    // Update jika sudah ada
                    cekItemFab.TotalSaving = savingCostActual + totalSaving;
                    cekItemFab.Quantity += Quantity ?? 0;
                    cekItemFab.UpdatedAt = DateTime.Now;
                    _context.ItemFabrications.Update(cekItemFab);
                }

                if (RepeatOrderId.HasValue)
                {
                    requestDataRo.QuantityReq -= Quantity ?? 0;
                }
                else
                {
                    requestData.Status = "InFabrication";

                }
                var statusLog = new RequestStatusModel
                {
                    ItemRequestId = ItemRequestId,
                    RepeatOrderId = RepeatOrderId,
                    Status = "FabricationStarted",
                    UserId = userId,
                    CreatedAt = DateTime.Now,
                };

                _context.RequestStatus.Add(statusLog);

                requestData.UpdatedAt = DateTime.Now;

                _context.ItemRequests.Update(requestData);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["Success"] = "Plan has been created.";
                return Redirect(RedirectUrl);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["Error"] = "Terjadi kesalahan: " + ex.Message;
                return Redirect(RedirectUrl);
            }


        }


        
    }
}