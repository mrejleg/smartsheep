using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Project.Core.Extentions;
using Project.Core.HttpClients;
using Project.Core.Storages;
using Project.Core.Toasts.Abstractions;
using System.Net.Http.Json;
using System.Security.Claims;
using Web.Domain.Models;
using Web.Domain.Models.Auths;
using Web.Infrastructure;

namespace Web.Controllers;

public class AccountController : Controller
{
    private const string ManualLogoutCookie = "SmartSheep.ManualLogout";
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly AppSetting _appSetting;
    private readonly INotyfService _notyf;

    public AccountController(IHttpContextAccessor httpContextAccessor, AppSetting appSetting, INotyfService notyf)
    {
        _httpContextAccessor = httpContextAccessor;
        _appSetting = appSetting;
        _notyf = notyf;
    }

    [AllowAnonymous, HttpGet]
    public IActionResult Index(string returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Home");
        return View(new LoginParameter { UrlAction = returnUrl });
    }

    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(LoginParameter model)
    {
        if (string.IsNullOrWhiteSpace(model.Username) || string.IsNullOrWhiteSpace(model.Password))
        {
            ModelState.AddModelError(string.Empty, "Username and password are required.");
            return View(model);
        }

        using var client = new HttpClient { BaseAddress = new Uri(_appSetting.ApiUrl) };
        var response = await client.PostAsJsonAsync("Auth/Login", new { model.Username, model.Password });
        if (!response.IsSuccessStatusCode)
        {
            ModelState.AddModelError(string.Empty, "The username or password is incorrect.");
            return View(model);
        }

        var profile = await response.Content.ReadFromJsonAsync<AppUserResponseDto>();
        if (profile?.IsSuccessResponse != true)
        {
            ModelState.AddModelError(string.Empty, "Sign-in failed. Please try again.");
            return View(model);
        }

        var claims = new List<Claim>
        {
            new("name", profile.Username ?? string.Empty),
            new(ClaimTypes.Name, profile.Username ?? string.Empty),
            new(ClaimTypes.Email, profile.Email ?? string.Empty),
            new("display_name", profile.FullName ?? profile.Username ?? string.Empty)
        };
        claims.AddRange(profile.ListRoles.Select(role => new Claim(ClaimTypes.Role, role)));
        var cookieName = (_appSetting.ApiCookiePrefix + _appSetting.ApplicationCookieName).Md5Encrypt(_appSetting.SecurityEncryptKeyMD5);
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, cookieName));
        var cookieExpiresInDays = int.TryParse(_appSetting.ApplicationCookieExpiresInDays, out var expiresInDays)
            ? expiresInDays
            : 30;
        var authenticationProperties = new AuthenticationProperties
        {
            IsPersistent = model.RememberMe,
            AllowRefresh = true,
            ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(cookieExpiresInDays) : null
        };
        await HttpContext.SignInAsync(cookieName, principal, authenticationProperties);
        HttpContext.User = principal;
        Response.Cookies.Delete(ManualLogoutCookie);

        var sessionKey = HttpClientFactoryExtentions.GetApiUserSessionName(_httpContextAccessor, MapperAppSettings.ParamAppSetting(_appSetting));
        SessionDataExtentions.SetSession(sessionKey, JsonConvert.SerializeObject(profile).ToBase64Encode(), _httpContextAccessor);
        SessionDataExtentions.SetSession("SmartSheep.LocalUserProfile", JsonConvert.SerializeObject(profile).ToBase64Encode(), _httpContextAccessor);

        // Discard messages queued before authentication (for example, an access
        // warning caused by opening a protected URL). Notifications raised by
        // user actions after this redirect continue to work normally.
        _notyf.RemoveAll();
        return LocalRedirect(Url.IsLocalUrl(model.UrlAction) ? model.UrlAction : Url.Action("Index", "Home"));
    }

    [Authorize]
    public async Task<IActionResult> Logout()
    {
        SessionDataExtentions.RemoveAllSession(_httpContextAccessor);
        var cookieName = (_appSetting.ApiCookiePrefix + _appSetting.ApplicationCookieName).Md5Encrypt(_appSetting.SecurityEncryptKeyMD5);
        await HttpContext.SignOutAsync(cookieName);
        Response.Cookies.Append(ManualLogoutCookie, "1", new CookieOptions
        {
            HttpOnly = true,
            IsEssential = true,
            SameSite = SameSiteMode.Strict,
            Secure = Request.IsHttps,
            Expires = DateTimeOffset.UtcNow.AddDays(1)
        });
        HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());
        _notyf.Success("You have signed out successfully.");
        return RedirectToAction(nameof(Index));
    }
}
