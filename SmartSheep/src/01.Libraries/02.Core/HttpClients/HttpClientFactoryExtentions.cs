using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Project.Base.Models.Jwts;
using Project.Core.Extentions;
using Project.Core.Storages;
using System.Net.Http.Headers;
using System.Net;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace Project.Core.HttpClients
{
    public static class HttpClientFactoryExtentions
    {
        // Reuse the connection pool while keeping headers isolated per request/user.
        // Creating HttpClient with its own handler in every service exhausts sockets
        // and repeatedly pays DNS/TLS connection setup costs.
        private static readonly SocketsHttpHandler SharedHandler = new()
        {
            UseCookies = false,
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli,
            PooledConnectionLifetime = TimeSpan.FromMinutes(10),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2)
        };

        private static HttpClient CreateClient(string baseAddress)
        {
            return new HttpClient(SharedHandler, disposeHandler: false)
            {
                BaseAddress = new Uri(baseAddress),
                Timeout = TimeSpan.FromSeconds(100)
            };
        }

        #region Api
        public static HttpClient ClientApiBearear(ParamSettings paramSettings)
        {
            var httpContextAccessor = new HttpContextAccessor();

            var client = CreateClient(paramSettings.ApiUrl);

            client.DefaultRequestHeaders.Add(GeneralConstants.ApplicationCookieName, paramSettings.ApplicationCookieName);
            client.DefaultRequestHeaders.Add(GeneralConstants.ApplicationCookieExpiresInDays, paramSettings.ApplicationCookieExpiresInDays);
            client.DefaultRequestHeaders.Add(GeneralConstants.EncryptKeyName, paramSettings.SecurityEncryptKeyMD5);
            client.DefaultRequestHeaders.Add(GeneralConstants.Source, "api");
            var token = GetTokenApiAsync(client, httpContextAccessor, paramSettings).GetAwaiter().GetResult();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(GeneralConstants.HeaderScheme, token);

            return client;
        }

        public static async Task<string> GetTokenApiAsync(HttpClient httpClient, HttpContextAccessor httpContextAccessor, ParamSettings paramSettings)
        {
            var keyAccessTokenSessionName = GetApiAccessTokenSessionName(httpContextAccessor, paramSettings);
            var keyRefreshTokenSessionName = GetApiRefreshTokenSessionName(httpContextAccessor, paramSettings);

            var token = SessionDataExtentions.GetSession(keyAccessTokenSessionName, httpContextAccessor).ToBase64Decode();

            var isExsistToken = SessionDataExtentions.IsExistSession(keyRefreshTokenSessionName, httpContextAccessor)
                && IsAccessTokenForClient(token, paramSettings.ApiClientId);
            if (!isExsistToken)
            {
                var userName = GetUserName(httpContextAccessor);
                var email = GetEmail(httpContextAccessor);
                var fullName = GetFullName(httpContextAccessor);
                var param = new TokenJwtRequest
                {
                    ClientId = paramSettings.ApiClientId,
                    ClientSecret = paramSettings.ApiClientSecret,
                    Username = userName,
                    Email = email,
                    FullName = fullName,
                    Roles = paramSettings.Roles,
                };

                var response = await HttpClientExtentions.SecuredPostAsync<JwtResponse>(httpClient, GeneralConstants.RoutingToken, param);
                token = response?.Data?.Token;
                var refreshToken = response?.Data?.RefreshToken;

                if (!string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(refreshToken))
                {
                    SessionDataExtentions.SetSession(keyAccessTokenSessionName, token.ToBase64Encode(), httpContextAccessor);
                    SessionDataExtentions.SetSession(keyRefreshTokenSessionName, refreshToken.ToBase64Encode(), httpContextAccessor);
                }
            }

            return token;
        }

        private static bool IsAccessTokenForClient(string token, string expectedClientId)
        {
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(expectedClientId))
            {
                return false;
            }

            try
            {
                var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
                var encodedClientId = jwt.Claims.FirstOrDefault(x => x.Type == "ClientId")?.Value;
                var clientId = encodedClientId?.ToBase64Decode();

                return string.Equals(clientId, expectedClientId, StringComparison.OrdinalIgnoreCase)
                    && jwt.ValidTo > DateTime.UtcNow.AddMinutes(1);
            }
            catch
            {
                return false;
            }
        }

        public static string GetApiUserSessionName(IHttpContextAccessor _httpContextAccessor, ParamSettings paramSettings)
        {
            return TextExtentions.Md5Encrypt(paramSettings.ApiCookiePrefix + "." + paramSettings.ApplicationCookieName + ".User." + GetUserName(_httpContextAccessor), paramSettings.SecurityEncryptKeyMD5);
        }

        public static string GetApiAccessTokenSessionName(IHttpContextAccessor _httpContextAccessor, ParamSettings paramSettings)
        {
            return TextExtentions.Md5Encrypt(paramSettings.ApiCookiePrefix + "." + paramSettings.ApplicationCookieName + ".V2." + GeneralConstants.AccessTokenName + "." + GetUserName(_httpContextAccessor), paramSettings.SecurityEncryptKeyMD5);
        }

        public static string GetApiRefreshTokenSessionName(IHttpContextAccessor _httpContextAccessor, ParamSettings paramSettings)
        {
            return TextExtentions.Md5Encrypt(paramSettings.ApiCookiePrefix + "." + paramSettings.ApplicationCookieName + ".V2." + GeneralConstants.RefreshTokenName + "." + GetUserName(_httpContextAccessor), paramSettings.SecurityEncryptKeyMD5);
        }
        #endregion

        public static string GetUserName(IHttpContextAccessor _httpContextAccessor)
        {
            return _httpContextAccessor.HttpContext?.User?.Claims?.FirstOrDefault(x => x.Type == "name")?.Value ?? string.Empty;
        }

        public static string GetEmail(IHttpContextAccessor _httpContextAccessor)
        {
            return _httpContextAccessor.HttpContext?.User?.Claims?.FirstOrDefault(x => x.Type.Equals(ClaimTypes.Email))?.Value ?? string.Empty;
        }

        public static string GetFullName(IHttpContextAccessor _httpContextAccessor)
        {
            return _httpContextAccessor.HttpContext?.User?.Claims?.FirstOrDefault(x => x.Type == "display_name")?.Value ?? string.Empty;
        }

    }
}
