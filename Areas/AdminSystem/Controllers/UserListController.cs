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
