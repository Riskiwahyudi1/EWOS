using EWOS_MVC.Models;
using EWOS_MVC.Services;
using Microsoft.AspNetCore.Mvc;

namespace EWOS_MVC.ViewComponents
{
    public class WeeksSettingViewComponent : ViewComponent
    {
        private readonly WeekHelper _weekHelper;
        private readonly YearsHelper _yearHelper;

        public WeeksSettingViewComponent(WeekHelper weekHelper, YearsHelper yearHelper)
        {
            _weekHelper = weekHelper;
            _yearHelper = yearHelper;
        }

        public async Task<IViewComponentResult> InvokeAsync(int? YearSettingId, int? SelectedId = null, int? MonthSelect = null)
        {
            int currentYear = DateTime.Now.Year;

            // Ambil year berdasarkan helper
            var yearById = await _yearHelper.GetYearByValueAsync(currentYear);
            if (yearById == null)
            {
                TempData["Error"] = $"Tahun {currentYear} tidak ditemukan.";
                return View(new List<WeeksSettingModel>());
            }

            int year = yearById.Id;
            int month = MonthSelect ?? DateTime.Now.Month;

            // Ambil minggu berdasarkan helper
            var weeks = await _weekHelper.GetMingguByTahunBulanAsync(year, month);

            // Jika belum memilih week → otomatis ambil minggu aktif
            if (SelectedId == null)
            {
                var mingguAktif = await _weekHelper.GetMingguAktifAsync(year);
                if (mingguAktif != null)
                {
                    SelectedId = mingguAktif.Id;
                }
            }

            ViewData["SelectedId"] = SelectedId;

            return View(weeks);
        }

    }
}
