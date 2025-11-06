using EWOS_MVC.Models;
using Microsoft.AspNetCore.Mvc;

public class BaseController : Controller
{
    protected UserModel? CurrentUser => HttpContext.Items["CurrentUser"] as UserModel;

    public override void OnActionExecuting(Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext context)
    {
        var user = CurrentUser;

        if (user != null)
        {
            ViewBag.UserName = user.UserName;
            ViewBag.Name = user.Name;
            ViewBag.Roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        }
        else
        {
            ViewBag.UserName = "Guest";
            ViewBag.Name = "Guest";
            ViewBag.Roles = new List<string>();
        }

        base.OnActionExecuting(context);
    }
}
