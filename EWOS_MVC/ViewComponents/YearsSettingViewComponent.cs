using EWOS_MVC.Services;
using Microsoft.AspNetCore.Mvc;

namespace EWOS_MVC.ViewComponents
{
    public class YearsSettingViewComponent : ViewComponent
    {
        private readonly YearsHelper _yearsHelper;

        public YearsSettingViewComponent(YearsHelper yearsHelper)
        {
            _yearsHelper = yearsHelper;
        }

        public async Task<IViewComponentResult> InvokeAsync(int? SelectedId = null)
        {

            var years = await _yearsHelper.GetAllYearsAsync();

            ViewData["SelectedId"] = SelectedId;
            return View(years);
        }
    }
}
