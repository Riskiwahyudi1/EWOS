using EWOS_MVC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EWOS_MVC.Areas.Requestor.Controllers
{
    [Authorize(Roles = "Requestor,AdminFabrication,AdminSystem,Supervisor")]
    [Area("Requestor")]
    public class NewRequestController : BaseController
    {
        private readonly AppDbContext _context;
        public NewRequestController(AppDbContext context)
        {
            _context = context;
        }
        //view form
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        //new request post
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateNewRequest(
           ItemRequestModel itemRequest,
           IFormFile? fileDesign,
           IFormFile? fileDrawing,
           IFormFile? fileQuotation)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                TempData["Error"] = "Terjadi kesalahan: " + string.Join(", ", errors);
                return View("Index");
            }
            string userName = ViewBag.UserName?.ToString();
            var findUser = await _context.Users.FirstOrDefaultAsync(u => u.UserName == userName);

            if (findUser == null)
            {
                TempData["Error"] = "User tidak ditemukan.";
                return View("Index");
            }

            async Task<string?> SaveFileAsync(IFormFile? file, string folderName, string allowedExt, long maxSizeBytes)
            {
                //validasi file
                if (file == null || file.Length == 0)
                    return null;

                var ext = Path.GetExtension(file.FileName).ToLower();
                if (!allowedExt.Split(',').Contains(ext))
                    throw new Exception($"Format file {ext} tidak diizinkan untuk {folderName}.");

                if (file.Length > maxSizeBytes)
                    throw new Exception($"Ukuran file {file.FileName} melebihi batas.");

                // cek folder upload
                var uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), $"wwwroot/uploads/{folderName}");
                if (!Directory.Exists(uploadFolder))
                    Directory.CreateDirectory(uploadFolder);

                //set nama file
                var invalidChars = Path.GetInvalidFileNameChars();
                var safePartName = new string(itemRequest.PartName
                    .Where(c => !invalidChars.Contains(c))
                    .ToArray())
                    .Replace(" ", "_");

                var fileName = $"{safePartName}-{Guid.NewGuid()}{ext}";

                var filePath = Path.Combine(uploadFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                return $"/uploads/{folderName}/{fileName}";
            }

            try
            {
                // Upload file dan simpan path
                itemRequest.DesignPath = await SaveFileAsync(fileDesign, "design", ".zip", 1 * 1024 * 1024);
                itemRequest.DrawingPath = await SaveFileAsync(fileDrawing, "drawing", ".pdf", 1 * 1024 * 1024);
                itemRequest.QuantationPath = await SaveFileAsync(fileQuotation, "quotation", ".pdf", 1 * 1024 * 1024);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Gagal upload file: " + ex.Message;
                return View("Index");
            }

            // Data lain
            itemRequest.UserId = findUser.Id;
            itemRequest.Status = "WaitingApproval";
            itemRequest.IsCalculateSaving = true;
            itemRequest.CreatedAt = DateTime.Now;
            itemRequest.UpdatedAt = DateTime.Now;

            _context.ItemRequests.Add(itemRequest);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Request has been created.";
            return RedirectToAction("Index");
        }
    }
}
