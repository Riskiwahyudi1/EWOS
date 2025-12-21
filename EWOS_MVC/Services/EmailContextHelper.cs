using EWOS_MVC.Models;
using Microsoft.EntityFrameworkCore;

namespace EWOS_MVC.Services
{
    public class EmailContextHelper
    {
        private readonly AppDbContext _context;

        public EmailContextHelper(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<string>> GetFabTeamEmailsAsync()
        {
            return await _context.Users
                .Include(u => u.UserRoles)
                .Where(u => u.UserRoles.Any(r => r.RoleId == 2))
                .Where(u => !string.IsNullOrEmpty(u.Email))
                .Select(u => u.Email)
                .ToListAsync();
        }

        public async Task<UserModel?> GetEngineerAsync()
        {
            return await _context.Users
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.UserRoles.Any(r => r.RoleId == 3));
        }
        public async Task<MachineCategoriesModel?> GetMachineCategoriesAsync(int categoryId)
        {
            return await _context.MachineCategories
                .FirstOrDefaultAsync(mc => mc.Id == categoryId);
        }

    }

}
