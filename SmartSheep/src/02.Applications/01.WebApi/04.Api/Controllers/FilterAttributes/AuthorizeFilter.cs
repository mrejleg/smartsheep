using Api.ApplicationCore.Interfaces.Auths;
using Microsoft.AspNetCore.Mvc.Filters;
using Project.Core.Interfaces.Jwt;
using Project.Core.RestApis;
using Project.Core.RestApis.Response;

namespace Api.Controllers.FilterAttributes
{
    public class AuthorizeFilter : ActionFilterAttribute
    {
        public string Scope { get; set; } = string.Empty;

        public override async Task OnActionExecutionAsync(
            ActionExecutingContext filterContext,
            ActionExecutionDelegate next)
        {
            var jwtService = filterContext.HttpContext.RequestServices
                .GetService(typeof(IJwtService)) as IJwtService;
            var clientId = jwtService?.GetClientId();

            var isAuthorized = false;
            if (!string.IsNullOrWhiteSpace(Scope) && !string.IsNullOrWhiteSpace(clientId))
            {
                if (!Api.ApplicationCore.Services.Auths.ApiScopeAuthorizationCache.TryAuthorize(clientId, Scope, out isAuthorized))
                {
                    var authService = filterContext.HttpContext.RequestServices
                        .GetService(typeof(IAuthApiService)) as IAuthApiService;
                    isAuthorized = authService != null
                        && await authService.IsAuthorizeApiAsync(Scope, clientId);
                }
            }

            if (!isAuthorized)
            {
                filterContext.Result = new Unauthorized("Unauthorized Scope", Scope).ReturnResponse();
                return;
            }

            await next();
        }
    }
}
