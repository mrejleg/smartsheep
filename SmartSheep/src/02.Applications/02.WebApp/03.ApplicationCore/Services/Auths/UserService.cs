using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Project.Core.Extentions;
using Project.Core.HttpClients;
using Project.Core.Storages;
using System.Net;
using System.Net.Http.Json;
using Web.ApplicationCore.Interfaces.Auths;
using Web.Domain.Models;
using Web.Domain.Models.Auths;
using Web.Infrastructure;

namespace Web.ApplicationCore.Services.Auths
{
    public class UserService : IUserService
    {
        private readonly HttpClient _client;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly AppSetting _appSettings;
        private AppUserResponseDto _requestProfile;
        private bool _requestProfileLoaded;

        public UserService(IHttpContextAccessor httpContextAccessor, AppSetting appSettings)
        {
            _httpContextAccessor = httpContextAccessor;
            _appSettings = appSettings;
            _client = HttpClientFactoryExtentions.ClientApiBearear(MapperAppSettings.ParamAppSetting(_appSettings));
        }

        public async Task<AppUserResponseDto> GetUserProfileAsync()
        {
            if (_requestProfileLoaded)
            {
                return _requestProfile;
            }

            const string localProfileSessionKey = "SmartSheep.LocalUserProfile";
            var username = HttpClientFactoryExtentions.GetUserName(_httpContextAccessor);
            if (!string.IsNullOrWhiteSpace(username))
            {
                // Auth/Profile returns the profile directly, not a RestApiResult.
                // Refresh once per request so farm revocations are visible immediately.
                using var response = await _client.GetAsync("Auth/Profile/" + Uri.EscapeDataString(username));

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    _requestProfile = await response.Content.ReadFromJsonAsync<AppUserResponseDto>();
                    if (_requestProfile != null)
                    {
                        SessionDataExtentions.SetSession(
                            localProfileSessionKey,
                            JsonConvert.SerializeObject(_requestProfile).ToBase64Encode(),
                            _httpContextAccessor);
                    }
                }
            }

            _requestProfileLoaded = true;
            return _requestProfile;
        }
    }
}
