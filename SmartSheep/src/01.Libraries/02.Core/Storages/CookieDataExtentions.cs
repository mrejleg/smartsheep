using Microsoft.AspNetCore.Http;

namespace Project.Core.Storages
{
    public static class CookieDataExtentions
    {
        public static bool IsExistCookie(this string key, IHttpContextAccessor httpContextAccessor)
        {
            return httpContextAccessor.HttpContext.Request.Cookies.TryGetValue(key, out string _);
        }

        public static string GetCookie(this string key, IHttpContextAccessor httpContextAccessor)
        {
            return httpContextAccessor.HttpContext.Request.Cookies[key];
        }

        public static void SetCookie(string key, string value, double? expireTimeInDays, IHttpContextAccessor httpContextAccessor)
        {
            var option = new CookieOptions();

            if (expireTimeInDays.HasValue)
            {
                option.Expires = DateTime.Now.AddDays(expireTimeInDays.Value);
            }
            else
            {
                option.Expires = DateTime.Now.AddMilliseconds(10);
            }

            httpContextAccessor.HttpContext.Response.Cookies.Append(key, value, option);
        }

        public static void RemoveCookie(string key, IHttpContextAccessor httpContextAccessor)
        {
            httpContextAccessor.HttpContext.Response.Cookies.Delete(key);
        }
    }
}
