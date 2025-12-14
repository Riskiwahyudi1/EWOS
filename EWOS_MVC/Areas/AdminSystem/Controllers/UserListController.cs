using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EWOS_MVC.Models.ViewModels;
using EWOS_MVC.Models;

namespace EWOS_MVC.Areas.AdminSystem.Controllers
{
    [Authorize(Roles = "AdminSystem")]
    [Area("AdminSystem")]
    public class UserListController : BaseController
    {
        private readonly AppDbContext _context;

        public UserListController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            // Ambil semua user
            var users = _context.Users.ToList();

            // Ambil semua roles
            var roles = _context.Roles.ToList();

            // Ambil semua userRoles
            var userRoles = _context.UserRoles.ToList();

            // Gabungkan ke ViewModel
            var model = new UserRolesViewModel
            {
                Users = users,
                Roles = roles,
                UserRoles = userRoles
            };

            return View(model);
        }

        // load data modal
        [HttpGet]
        public async Task<IActionResult> LoadData(long id, string type)
        {
            // Ambil user (WAJIB await)
            var user = await _context.Users
                .FirstOrDefaultAsync(i => i.Id == id);

            if (user == null)
                return NotFound();

            // Ambil semua roles
            var roles = await _context.Roles.ToListAsync();

            // Ambil role user terkait saja
            var userRoles = await _context.UserRoles
                .Where(ur => ur.UserId == id)
                .ToListAsync();

            // Gabungkan ke ViewModel
            var model = new UserRolesViewModel
            {
                SelectedUser = user,
                Roles = roles,
                UserRoles = userRoles
            };

            return type switch
            {
                "Edit" => PartialView("~/Views/modals/AdminSystem/EditUserModal.cshtml", model),
                _ => BadRequest("Unknown modal type")
            };
        }

        //Seaching
        [HttpGet]
        public IActionResult Search(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return Json(Array.Empty<object>());

            keyword = keyword.Trim();

            var query = _context.Users
                .Include(u => u.UserRoles)
                .Where(u =>
                    (u.UserName != null && EF.Functions.Like(u.UserName, $"%{keyword}%")) ||
                    (u.Name != null && EF.Functions.Like(u.Name, $"%{keyword}%")) ||
                    (u.Badge != null && EF.Functions.Like(u.Badge, $"%{keyword}%"))
                );

            var result = query
                .Select(u => new
                {
                    u.Id,
                    u.UserName,
                    u.Name,
                    u.Badge,
                    u.Email,
                    u.IsActive,
                    CreatedAt = u.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                    UpdatedAt = u.UpdatedAt.ToString("dd/MM/yyyy HH:mm")
                })
                .ToList();

            return Json(result);
        }




        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int Id, int[] Roles, bool IsActive)
        {
            var user = _context.Users.FirstOrDefault(u => u.Id == Id);
            if (user == null)
                return NotFound();

            // Update status aktif
            user.IsActive = IsActive;

            // Hapus semua UserRoles lama
            var oldRoles = _context.UserRoles.Where(ur => ur.UserId == Id);
            _context.UserRoles.RemoveRange(oldRoles);

            // Tambah UserRoles baru
            foreach (var roleId in Roles)
            {
                _context.UserRoles.Add(new UserRoleModel
                {
                    UserId = Id,
                    RoleId = roleId
                });
            }

            _context.SaveChanges();

            TempData["Success"] = "Data user berhasil diperbarui.";
            return RedirectToAction("Index");
        }

    }
}
