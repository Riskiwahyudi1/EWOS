using EWOS_MVC.Models;
using Microsoft.EntityFrameworkCore;

namespace EWOS_MVC.Services
{
    public class YearsHelper
    {
        private readonly AppDbContext _context;

        public YearsHelper(AppDbContext context)
        {
            _context = context;
        }

        // Ambil semua tahun
        public async Task<List<YearsSettingModel>> GetAllYearsAsync()
        {
            return await _context.YearsSetting
                .OrderByDescending(y => y.Year)   // yg terbaru di atas
                .ToListAsync();
        }

        // Ambil 1 tahun berdasarkan Id
        public async Task<YearsSettingModel?> GetYearByIdAsync(int? id)
        {
            return await _context.YearsSetting
                .FirstOrDefaultAsync(y => y.Id == id);
        }

        // Ambil tahun berdasarkan nilai tahun
        public async Task<YearsSettingModel?> GetYearByValueAsync(int yearValue)
        {
            return await _context.YearsSetting
                .FirstOrDefaultAsync(y => y.Year == yearValue);
        }

        // Ambil tahun berjalan
        public async Task<YearsSettingModel?> GetCurrentYearAsync()
        {
            int nowYear = DateTime.Now.Year;

            return await _context.YearsSetting
                .FirstOrDefaultAsync(y => y.Year == nowYear);
        }
    }

}
