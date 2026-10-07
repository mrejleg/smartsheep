using Project.Core.HttpClients;
using Web.Domain.Models;

namespace Web.Infrastructure
{
    public static class MapperAppSettings
    {
        public static ParamSettings ParamAppSetting(AppSetting appSettings)
        {
            return new ParamSettings
            {
                ApiClientId = appSettings.ApiClientId,
                ApiClientSecret = appSettings.ApiClientSecret,
                ApiUrl = appSettings.ApiUrl,
                ApiCookiePrefix = appSettings.ApiCookiePrefix,
                ApplicationCookieExpiresInDays = appSettings.ApplicationCookieExpiresInDays,
                ApplicationCookieName = appSettings.ApplicationCookieName,
                SecurityEncryptKeyBase64 = appSettings.SecurityEncryptKeyBase64,
                SecurityEncryptKeyMD5 = appSettings.SecurityEncryptKeyMD5,
                Roles = appSettings.Roles
            };
        }
    }
}
