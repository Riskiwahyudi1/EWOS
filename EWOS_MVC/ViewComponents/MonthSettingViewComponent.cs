using Microsoft.AspNetCore.Mvc;
using System;

namespace EWOS_MVC.ViewComponents
{
    public class MonthSettingViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(int? SelectedYear)
        {
            int currentYear = DateTime.Now.Year;
            int currentMonth = DateTime.Now.Month;

            // Tentukan batas bulan
            int maxMonth = (SelectedYear != null && SelectedYear == currentYear)
                ? currentMonth
                : 12;

            ViewData["MaxMonth"] = maxMonth;
            ViewData["SelectedYear"] = SelectedYear;

            return View();
        }
    }
}
