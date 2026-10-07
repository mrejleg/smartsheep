using Microsoft.AspNetCore.Http;
using Project.Core.Extentions;
using System.Security.Claims;
using Web.Domain.Models;

namespace Web.Domain.Constants
{
    public static class GeneralConsts
    {
        public const string ActionNameLogout = "Logout";
        public const string ControllerNameLogout = "Account";
        public const string ViewNameUnauthorize = "_NotAuthorize";

        public static string GetUserName(IHttpContextAccessor _httpContextAccessor)
        {
            return _httpContextAccessor.HttpContext.User?.Claims?.FirstOrDefault(x => x.Type == "name")?.Value;
        }

        public static string GetUserSessionName(IHttpContextAccessor _httpContextAccessor, AppSetting _appSettings)
        {
            return TextExtentions.Md5Encrypt(ApiTokens.ApplicationCookiePrefix + "." + _appSettings.ApplicationCookieName + ".User." + GetUserName(_httpContextAccessor), _appSettings.SecurityEncryptKeyMD5);
        }

        public static string GetApiAccessTokenSessionName(IHttpContextAccessor _httpContextAccessor, string applicationCookieName, string securityEncryptKeyMD5)
        {
            return TextExtentions.Md5Encrypt(ApiTokens.ApplicationCookiePrefix + "." + applicationCookieName + "." + ApiTokens.AccessTokenName + "." + GetUserName(_httpContextAccessor), securityEncryptKeyMD5);
        }

        public static string GetApiRefreshTokenSessionName(IHttpContextAccessor _httpContextAccessor, string applicationCookieName, string securityEncryptKeyMD5)
        {
            return TextExtentions.Md5Encrypt(ApiTokens.ApplicationCookiePrefix + "." + applicationCookieName + "." + ApiTokens.RefreshTokenName + "." + GetUserName(_httpContextAccessor), securityEncryptKeyMD5);
        }
    }
}
