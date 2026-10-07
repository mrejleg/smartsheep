using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Project.Core.Extentions;
using Project.Core.Storages;
using Project.Core.Toasts.Abstractions;
using Web.ApplicationCore.Interfaces.Auths;

namespace Web.Controllers.ActionFilters
{
    public class ActionFilter : ActionFilterAttribute
    {
        public const string AccessTypeItemKey = "ActionFilter.AccessType";
        public const string ControllerNameItemKey = "ActionFilter.ControllerName";

        public override async Task OnActionExecutionAsync(
            ActionExecutingContext filterContext,
            ActionExecutionDelegate next)
        {
            if (filterContext.Controller is not Controller)
            {
                await next();
                return;
            }

            var authService = filterContext.HttpContext.RequestServices.GetRequiredService<IAuthService>();
            var appSetting = filterContext.HttpContext.RequestServices.GetRequiredService<Web.Domain.Models.AppSetting>();
            var notyfService = filterContext.HttpContext.RequestServices.GetRequiredService<INotyfService>();

            if (!await authService.GetAccessApplicationAsync())
            {
                await ClearAuthenticationAsync(filterContext.HttpContext, appSetting);
                notyfService.Error("The Web application access does not match the API configuration. Please sign in again.");
                filterContext.Result = new RedirectToActionResult("Index", "Account", null);
                return;
            }

            if (!authService.IsAuthorizeSession())
            {
                await ClearAuthenticationAsync(filterContext.HttpContext, appSetting);
                notyfService.Error("Your API session is no longer valid. Please sign in again.");
                filterContext.Result = new RedirectToActionResult("Index", "Account", null);
                return;
            }

            var executedContext = await next();
            var routeControllerName = ((Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor)filterContext.ActionDescriptor).ControllerName;
            var controllerName = filterContext.HttpContext.Items[ControllerNameItemKey] as string ?? routeControllerName;
            var accessType = filterContext.HttpContext.Items[AccessTypeItemKey] as string;

            if (controllerName != "Home" && controllerName != "Account" && controllerName != "Document" && controllerName != "Notification")
            {
                if (!string.IsNullOrEmpty(accessType))
                {
                    var isAuth = await authService.GetAccessMenuAsync(controllerName, accessType);

                    if (!isAuth)
                    {
                        var actionName = filterContext.ActionDescriptor.RouteValues["action"];
                        await ClearAuthenticationAsync(filterContext.HttpContext, appSetting);
                        notyfService.Error($"Access to {controllerName} - {actionName} is not available in the API access configuration. Please sign in again.");
                        executedContext.Result = new RedirectToActionResult("Index", "Account", null);
                    }
                }
            }
        }

        private static async Task ClearAuthenticationAsync(HttpContext httpContext, Web.Domain.Models.AppSetting appSetting)
        {
            var httpContextAccessor = httpContext.RequestServices.GetRequiredService<IHttpContextAccessor>();
            SessionDataExtentions.RemoveAllSession(httpContextAccessor);
            var cookieName = (appSetting.ApiCookiePrefix + appSetting.ApplicationCookieName).Md5Encrypt(appSetting.SecurityEncryptKeyMD5);
            await httpContext.SignOutAsync(cookieName);
        }
    }
}
