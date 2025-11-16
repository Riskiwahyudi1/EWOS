using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace EWOS_MVC.ViewComponents
{
    public class MonthSettingViewComponent : ViewComponent
    {
        public async Task<IViewComponentResult> InvokeAsync()
        {
            var currentMonth = DateTime.Now.Month;
            ViewBag.CurrentMonth = currentMonth;

            return View("Default");
        }
    }
}
