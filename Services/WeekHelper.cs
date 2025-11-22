using EWOS_MVC.Models;
using Microsoft.EntityFrameworkCore;

namespace EWOS_MVC.Services
{
    public class WeekHelper
    {
        private readonly AppDbContext _context;
        private readonly YearsHelper _yearsHelper;
        public WeekHelper(AppDbContext context, YearsHelper yearsHelper)
        {
            _context = context;
            _yearsHelper = yearsHelper;
        }

        /// Mengambil minggu aktif berdasarkan tanggal hari ini.
        public async Task<WeeksSettingModel?> GetMingguAktifAsync(int tahunId)
        {
            DateTime now = DateTime.Now;

            return await _context.WeeksSetting
                .FirstOrDefaultAsync(w =>
                    w.YearSettingId == tahunId &&
                    now >= w.StartDate &&
                    now <= w.EndDate);

        }
        /// Mengambil minggu aktif berdasarkan tanggal hari ini.
        public async Task<WeeksSettingModel?> GetMingguAktifTodayAsync()
        {
            var years = await _yearsHelper.GetCurrentYearAsync();
            DateTime now = DateTime.Now;

            return await _context.WeeksSetting
                .FirstOrDefaultAsync(w =>
                    w.YearSettingId == years.Id &&
                    now >= w.StartDate &&
                    now <= w.EndDate);
        }

      
        /// Mengambil list minggu berdasarkan tahun & bulan.
        public async Task<List<WeeksSettingModel>> GetMingguByTahunBulanAsync(int tahunId, int bulan)
        {
            return await _context.WeeksSetting
                .Where(w =>
                    w.YearSettingId == tahunId &&
                    w.Month == bulan)
                .ToListAsync();
        }
    }

}
