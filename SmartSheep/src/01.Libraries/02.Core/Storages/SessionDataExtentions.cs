using Microsoft.AspNetCore.Http;

namespace Project.Core.Storages
{
    public static class SessionDataExtentions
    {
        public static bool IsExistSession(this string key, IHttpContextAccessor httpContextAccessor)
        {
            var data = httpContextAccessor.HttpContext.Session.GetString(key);
            if (string.IsNullOrEmpty(data))
            {
                return false;
            }

            return true;
        }

        public static string GetSession(this string key, IHttpContextAccessor httpContextAccessor)
        {
            return httpContextAccessor.HttpContext.Session.GetString(key);
        }

        public static void SetSession(string key, string value, IHttpContextAccessor httpContextAccessor)
        {
            httpContextAccessor.HttpContext.Session.SetString(key, value);
        }

        public static void RemoveSession(string key, IHttpContextAccessor httpContextAccessor)
        {
            httpContextAccessor.HttpContext.Session.Remove(key);
        }

        public static void RemoveAllSession(IHttpContextAccessor httpContextAccessor)
        {
            httpContextAccessor.HttpContext.Session.Clear();
            var sessionKeys = httpContextAccessor.HttpContext.Session.Keys;
            foreach (string sessionKey in sessionKeys)
            {
                httpContextAccessor.HttpContext.Session.Remove(sessionKey);
            }
        }
    }
}
