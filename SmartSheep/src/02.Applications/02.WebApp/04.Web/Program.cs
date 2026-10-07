using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authentication;
using Newtonsoft.Json;
using Project.Core.Extentions;
using Project.Core.HttpClients;
using Project.Core.Storages;
using Project.Core.Toasts.Extensions;
using Project.Core.Toasts.Notyf.Enums;
using System.Net.Http.Json;
using System.Security.Claims;
using Web.ApplicationCore;
using Web.Domain.Models;
using Web.Domain.Models.Auths;
using Web.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

#region ReadAppSettingJson
Action<AppSetting> appSettings = (opt =>
{
    opt.ApplicationUrlPublish = builder.Configuration["Application:UrlPublish"];
    opt.ApplicationIsUnderMaintenance = builder.Configuration["Application:IsUnderMaintenance"];
    opt.ApplicationIsHttpsRequired = builder.Configuration["Application:IsHttpsRequired"];
    opt.ApplicationCookieName = builder.Configuration["Application:CookieName"];
    opt.ApplicationCookieExpiresInDays = builder.Configuration["Application:CookieExpiresInDays"];
    opt.ApplicationUrl = builder.Configuration["Application:Url"];
    opt.ApplicationName = builder.Configuration["Application:Name"];
    opt.ApplicationClientId = builder.Configuration["Application:ClientId"];
    opt.ApplicationIsByPassWhitelist = builder.Configuration["Application:IsByPassWhitelist"];
    opt.ApplicationCompanyCode = builder.Configuration["Application:CompanyCode"];

    opt.SecurityEncryptKeyBase64 = builder.Configuration["Security:EncryptKeyBase64"];
    opt.SecurityEncryptKeyMD5 = builder.Configuration["Security:EncryptKeyMD5"];

    opt.ApiUrl = builder.Configuration["Api:Url"];
    opt.ApiClientId = builder.Configuration["Api:ClientId"];
    opt.ApiClientSecret = builder.Configuration["Api:ClientSecret"];
    opt.ApiCookiePrefix = builder.Configuration["Api:CookiePrefix"];

    opt.YoloAppSettings = builder.Configuration["Yolo:AppSettings"];
    opt.YoloModel = builder.Configuration["Yolo:Model"];
    opt.YoloLabels = builder.Configuration["Yolo:Labels"];
    opt.YoloUseCoreMl = builder.Configuration["Yolo:UseCoreMl"];
    opt.YoloPendingFolder = builder.Configuration["Yolo:PendingFolder"];

});

builder.Services.Configure(appSettings);
builder.Services.AddSingleton(resolver => resolver.GetRequiredService<IOptions<AppSetting>>().Value);
#endregion

#region JsonResultToPascalCase
builder.Services.AddMvc()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });
#endregion

#region LocalAuthentication
var authenticationScheme = TextExtentions.Md5Encrypt(builder.Configuration["Api:CookiePrefix"] + builder.Configuration["Application:CookieName"], builder.Configuration["Security:EncryptKeyMD5"]);
builder.Services.AddAuthentication(authenticationScheme)
    .AddCookie(TextExtentions.Md5Encrypt(builder.Configuration["Api:CookiePrefix"] + builder.Configuration["Application:CookieName"], builder.Configuration["Security:EncryptKeyMD5"]),
     options =>
     {
         options.ExpireTimeSpan = TimeSpan.FromDays(int.TryParse(builder.Configuration["Application:CookieExpiresInDays"], out var expiresInDays) ? expiresInDays : 30);
         options.SlidingExpiration = true;
         options.Cookie.IsEssential = true;
         options.LoginPath = "/Account";
         options.AccessDeniedPath = "/Account";
    });
#endregion

builder.Services.AddHttpContextAccessor();
builder.Services.AddControllersWithViews().AddRazorRuntimeCompilation();
builder.Services.AddHttpClient();

builder.Services.AddSession(options =>
{
    options.Cookie.Name = TextExtentions.Md5Encrypt(builder.Configuration["Api:CookiePrefix"] + ".SESSION." + builder.Configuration["Application:CookieName"], builder.Configuration["Security:EncryptKeyMD5"]);
    options.IdleTimeout = TimeSpan.FromMinutes(10);
    options.Cookie.IsEssential = true;
    options.Cookie.HttpOnly = true;
});

builder.Services.AddNotyf(config =>
{
    config.DurationInSeconds = 5;
    config.Position = NotyfPosition.TopRight;
    config.IsDismissable = true;
    config.HasRippleEffect = true;
});

builder.Services.AddApplicationCores();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

if (bool.Parse(builder.Configuration["Application:IsHttpsRequired"]))
{
    app.UseCookiePolicy(
        new CookiePolicyOptions
        {
            Secure = CookieSecurePolicy.Always
        });

    app.UseHttpsRedirection();
}
else
{
    app.UseCookiePolicy(
        new CookiePolicyOptions
        {
            Secure = CookieSecurePolicy.SameAsRequest,
            MinimumSameSitePolicy = SameSiteMode.Strict
        });
}

app.UseStaticFiles();
app.UseNotyf();
app.UseRouting();
app.UseSession();
app.UseAuthentication();

if (app.Environment.IsDevelopment() && builder.Configuration.GetValue<bool>("Authentication:BypassLogin"))
{
    app.Use(async (context, next) =>
    {
        const string manualLogoutCookie = "SmartSheep.ManualLogout";

        // BypassLogin is only a development convenience. Once a user explicitly
        // signs out, do not let this middleware immediately authenticate them again.
        if (context.User.Identity?.IsAuthenticated != true &&
            !context.Request.Cookies.ContainsKey(manualLogoutCookie))
        {
            var appSetting = context.RequestServices.GetRequiredService<AppSetting>();
            using var client = new HttpClient { BaseAddress = new Uri(appSetting.ApiUrl) };
            var response = await client.PostAsJsonAsync("Auth/Login", new { Username = "SuperAdmin", Password = "12345678" });

            if (response.IsSuccessStatusCode)
            {
                var profile = await response.Content.ReadFromJsonAsync<AppUserResponseDto>();
                if (profile?.IsSuccessResponse == true)
                {
                    var claims = new List<Claim>
                    {
                        new("name", profile.Username ?? string.Empty),
                        new(ClaimTypes.Name, profile.Username ?? string.Empty),
                        new(ClaimTypes.Email, profile.Email ?? string.Empty),
                        new("display_name", profile.FullName ?? profile.Username ?? string.Empty)
                    };
                    claims.AddRange(profile.ListRoles.Select(role => new Claim(ClaimTypes.Role, role)));

                    var cookieName = (appSetting.ApiCookiePrefix + appSetting.ApplicationCookieName).Md5Encrypt(appSetting.SecurityEncryptKeyMD5);
                    var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, cookieName));
                    await context.SignInAsync(cookieName, principal, new AuthenticationProperties
                    {
                        IsPersistent = false,
                        AllowRefresh = true
                    });
                    context.User = principal;

                    var httpContextAccessor = context.RequestServices.GetRequiredService<IHttpContextAccessor>();
                    var sessionKey = HttpClientFactoryExtentions.GetApiUserSessionName(httpContextAccessor, MapperAppSettings.ParamAppSetting(appSetting));
                    var profileSession = JsonConvert.SerializeObject(profile).ToBase64Encode();
                    SessionDataExtentions.SetSession(sessionKey, profileSession, httpContextAccessor);
                    SessionDataExtentions.SetSession("SmartSheep.LocalUserProfile", profileSession, httpContextAccessor);
                }
            }
        }

        await next();
    });
}

app.UseAuthorization();

if (bool.Parse(builder.Configuration["Application:IsUnderMaintenance"]))
{
    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=UnderMaintenance}/{action=Index}/{id?}");
}
else
{
    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Account}/{action=Index}/{id?}");
}

app.Run();
