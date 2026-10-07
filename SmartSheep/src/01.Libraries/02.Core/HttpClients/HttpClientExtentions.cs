using DevExtreme.AspNet.Data.ResponseModel;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Project.Base.Models.DevExpress.DxGrids;
using Project.Base.Models.Jwts;
using Project.Base.Models.RestApis;
using Project.Core.Extentions;
using Project.Core.RestApis;
using Project.Core.Storages;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;

namespace Project.Core.HttpClients
{
    public static class HttpClientExtentions
    {
        public static async Task<LoadResult> SecuredDxGridAsync<TEntity>(HttpClient httpClient, string routingApi, DataSourceLoadOptions param)
        {
            var response = await httpClient.PostAsync(httpClient.BaseAddress.ToString() + routingApi, new StringContent(JsonConvert.SerializeObject(param), Encoding.UTF8, MediaTypeNames.Application.Json));
            try
            {
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    if (await IsTokenExpiredAsync(httpClient))
                    {
                        await ClientRefreshBearearAsync(httpClient);
                        response.Dispose();
                        response = await httpClient.PostAsync(httpClient.BaseAddress.ToString() + routingApi, new StringContent(JsonConvert.SerializeObject(param), Encoding.UTF8, MediaTypeNames.Application.Json));
                    }
                }

                var result = new LoadResult
                {
                    groupCount = -1,
                    totalCount = -1,
                    data = null
                };

                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var contents = await response.Content.ReadAsStringAsync();
                    var results = contents.GetApiResultObject(response);

                    if (results.Data != null)
                    {
                        var convertDictionary = JsonConvert.DeserializeObject<IDictionary<string, object>>(results.Data.ToString());

                        if (convertDictionary["groupCount"] != null)
                        {
                            if (TextExtentions.IsNumeric(convertDictionary["groupCount"].ToString()))
                            {
                                result.groupCount = int.Parse(convertDictionary["groupCount"]?.ToString());
                            }
                        }

                        if (convertDictionary["totalCount"] != null)
                        {
                            if (TextExtentions.IsNumeric(convertDictionary["totalCount"].ToString()))
                            {
                                result.totalCount = int.Parse(convertDictionary["totalCount"]?.ToString());
                            }
                        }

                        if (convertDictionary["data"] != null)
                        {
                            if (!string.IsNullOrEmpty(param.Group))
                            {
                                var dataObject = JsonConvert.DeserializeObject<IList<GroupResponseModel>>(convertDictionary["data"]?.ToString());
                                if (dataObject != null)
                                {
                                    result.data = dataObject;
                                }
                            }
                            else
                            {
                                var dataObject = JsonConvert.DeserializeObject<IList<TEntity>>(convertDictionary["data"]?.ToString());
                                if (dataObject != null)
                                {
                                    result.data = dataObject;
                                }
                            }
                        }
                    }
                }

                response.Dispose();
                return result;
            }
            catch (Exception)
            {
                return new LoadResult
                {
                    groupCount = -1,
                    totalCount = -1,
                    data = null
                };
            }
        }

        public static async Task<TEntity> SecuredGetEntityAsync<TEntity>(HttpClient httpClient, string routingApi) where TEntity : new()
        {
            var response = await httpClient.GetAsync(httpClient.BaseAddress.ToString() + routingApi);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                if (await IsTokenExpiredAsync(httpClient))
                {
                    await ClientRefreshBearearAsync(httpClient);
                    response.Dispose();
                    response = await httpClient.GetAsync(httpClient.BaseAddress.ToString() + routingApi);
                }
            }

            var contents = await response.Content.ReadAsStringAsync();
            var results = JsonConvert.DeserializeObject<RestApiResponse>(contents);
            var result = JsonConvert.DeserializeObject<TEntity>(results.Data.ToString());
            response.Dispose();
            return result;
        }

        public static async Task<RestApiResult<TEntity>> SecuredGetAsync<TEntity>(HttpClient httpClient, string routingApi) where TEntity : new()
        {
            var response = await httpClient.GetAsync(httpClient.BaseAddress.ToString() + routingApi);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                if (await IsTokenExpiredAsync(httpClient))
                {
                    await ClientRefreshBearearAsync(httpClient);
                    response.Dispose();
                    response = await httpClient.GetAsync(httpClient.BaseAddress.ToString() + routingApi);
                }
            }

            var contents = await response.Content.ReadAsStringAsync();
            var result = contents.GetApiResult<TEntity>(response);
            response.Dispose();
            return result;
        }

        public static async Task<Stream> SecuredDownloadAsync(HttpClient httpClient, string routingApi)
        {
            var response = await httpClient.GetAsync(httpClient.BaseAddress.ToString() + routingApi);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                if (await IsTokenExpiredAsync(httpClient))
                {
                    await ClientRefreshBearearAsync(httpClient);
                    response = await httpClient.GetAsync(httpClient.BaseAddress.ToString() + routingApi);
                }
            }

            var contents = await response.Content.ReadAsStreamAsync();

            return contents;
        }

        public static async Task<RestApiResult<TEntity>> SecuredPostAsync<TEntity>(HttpClient httpClient, string routingApi, object param) where TEntity : new()
        {
            var response = await httpClient.PostAsync(httpClient.BaseAddress.ToString() + routingApi, new StringContent(JsonConvert.SerializeObject(param), Encoding.UTF8, MediaTypeNames.Application.Json));

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                if (await IsTokenExpiredAsync(httpClient))
                {
                    await ClientRefreshBearearAsync(httpClient);
                    response.Dispose();
                    response = await httpClient.PostAsync(httpClient.BaseAddress.ToString() + routingApi, new StringContent(JsonConvert.SerializeObject(param), Encoding.UTF8, MediaTypeNames.Application.Json));
                }
            }

            var contents = await response.Content.ReadAsStringAsync();
            var result = contents.GetApiResult<TEntity>(response);
            response.Dispose();
            return result;
        }

        public static async Task<RestApiResult<TEntity>> SecuredPostAsync<TEntity>(HttpClient httpClient, string routingApi, HttpContent content) where TEntity : new()
        {
            var response = await httpClient.PostAsync(httpClient.BaseAddress.ToString() + routingApi, content);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                if (await IsTokenExpiredAsync(httpClient))
                {
                    await ClientRefreshBearearAsync(httpClient);
                    response.Dispose();
                    response = await httpClient.PostAsync(httpClient.BaseAddress.ToString() + routingApi, content);
                }
            }

            var contents = await response.Content.ReadAsStringAsync();
            var result = contents.GetApiResult<TEntity>(response);
            response.Dispose();
            return result;
        }

        public static async Task<RestApiResult<TEntity>> SecuredPutAsync<TEntity>(HttpClient httpClient, string routingApi, object param) where TEntity : new()
        {
            var response = await httpClient.PutAsync(httpClient.BaseAddress.ToString() + routingApi, new StringContent(JsonConvert.SerializeObject(param), Encoding.UTF8, MediaTypeNames.Application.Json));

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                if (await IsTokenExpiredAsync(httpClient))
                {
                    await ClientRefreshBearearAsync(httpClient);
                    response.Dispose();
                    response = await httpClient.PutAsync(httpClient.BaseAddress.ToString() + routingApi, new StringContent(JsonConvert.SerializeObject(param), Encoding.UTF8, MediaTypeNames.Application.Json));
                }
            }

            var contents = await response.Content.ReadAsStringAsync();
            var result = contents.GetApiResult<TEntity>(response);
            response.Dispose();
            return result;
        }

        public static async Task<RestApiResult<TEntity>> SecuredPutAsync<TEntity>(HttpClient httpClient, string routingApi, HttpContent content) where TEntity : new()
        {
            var response = await httpClient.PutAsync(httpClient.BaseAddress.ToString() + routingApi, content);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                if (await IsTokenExpiredAsync(httpClient))
                {
                    await ClientRefreshBearearAsync(httpClient);
                    response.Dispose();
                    response = await httpClient.PutAsync(httpClient.BaseAddress.ToString() + routingApi, content);
                }
            }

            var contents = await response.Content.ReadAsStringAsync();
            var result = contents.GetApiResult<TEntity>(response);
            response.Dispose();
            return result;
        }

        public static async Task<RestApiResult<TEntity>> SecuredDeleteAsync<TEntity>(HttpClient httpClient, string routingApi) where TEntity : new()
        {
            var response = await httpClient.DeleteAsync(httpClient.BaseAddress.ToString() + routingApi);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                if (await IsTokenExpiredAsync(httpClient))
                {
                    await ClientRefreshBearearAsync(httpClient);
                    response.Dispose();
                    response = await httpClient.DeleteAsync(httpClient.BaseAddress.ToString() + routingApi);
                }
            }

            var contents = await response.Content.ReadAsStringAsync();
            var result = contents.GetApiResult<TEntity>(response);
            response.Dispose();
            return result;
        }

        public static async Task<object> SecuredGetObjectAsync(HttpClient httpClient, string routingApi)
        {
            var response = await httpClient.GetAsync(httpClient.BaseAddress.ToString() + routingApi);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                if (await IsTokenExpiredAsync(httpClient))
                {
                    await ClientRefreshBearearAsync(httpClient);
                    response.Dispose();
                    response = await httpClient.GetAsync(httpClient.BaseAddress.ToString() + routingApi);
                }
            }

            var contents = await response.Content.ReadAsStringAsync();
            var results = JsonConvert.DeserializeObject<RestApiResponse>(contents);
            var result = JsonConvert.DeserializeObject<object>(results.Data.ToString());
            response.Dispose();
            return result;
        }

        public static async Task<RestApiResult> SecuredObjectGetAsync(HttpClient httpClient, string routingApi)
        {
            var response = await httpClient.GetAsync(httpClient.BaseAddress.ToString() + routingApi);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                if (await IsTokenExpiredAsync(httpClient))
                {
                    await ClientRefreshBearearAsync(httpClient);
                    response.Dispose();
                    response = await httpClient.GetAsync(httpClient.BaseAddress.ToString() + routingApi);
                }
            }

            var contents = await response.Content.ReadAsStringAsync();
            var result = contents.GetApiResultObject(response);
            response.Dispose();
            return result;
        }

        public static async Task<RestApiResult> SecuredObjectPostAsync(HttpClient httpClient, string routingApi, object param)
        {
            var response = await httpClient.PostAsync(httpClient.BaseAddress.ToString() + routingApi, new StringContent(JsonConvert.SerializeObject(param), Encoding.UTF8, MediaTypeNames.Application.Json));

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                if (await IsTokenExpiredAsync(httpClient))
                {
                    await ClientRefreshBearearAsync(httpClient);
                    response.Dispose();
                    response = await httpClient.PostAsync(httpClient.BaseAddress.ToString() + routingApi, new StringContent(JsonConvert.SerializeObject(param), Encoding.UTF8, MediaTypeNames.Application.Json));
                }
            }

            var contents = await response.Content.ReadAsStringAsync();
            var result = contents.GetApiResultObject(response);
            response.Dispose();
            return result;
        }

        public static async Task<RestApiResult> SecuredObjectPostAsync(HttpClient httpClient, string routingApi, HttpContent content)
        {
            var response = await httpClient.PostAsync(httpClient.BaseAddress.ToString() + routingApi, content);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                if (await IsTokenExpiredAsync(httpClient))
                {
                    await ClientRefreshBearearAsync(httpClient);
                    response.Dispose();
                    response = await httpClient.PostAsync(httpClient.BaseAddress.ToString() + routingApi, content);
                }
            }

            var contents = await response.Content.ReadAsStringAsync();
            var result = contents.GetApiResultObject(response);
            response.Dispose();
            return result;
        }

        public static async Task<RestApiResult> SecuredObjectPutAsync(HttpClient httpClient, string routingApi, object param)
        {
            var response = await httpClient.PutAsync(httpClient.BaseAddress.ToString() + routingApi, new StringContent(JsonConvert.SerializeObject(param), Encoding.UTF8, MediaTypeNames.Application.Json));

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                if (await IsTokenExpiredAsync(httpClient))
                {
                    await ClientRefreshBearearAsync(httpClient);
                    response.Dispose();
                    response = await httpClient.PutAsync(httpClient.BaseAddress.ToString() + routingApi, new StringContent(JsonConvert.SerializeObject(param), Encoding.UTF8, MediaTypeNames.Application.Json));
                }
            }

            var contents = await response.Content.ReadAsStringAsync();
            var result = contents.GetApiResultObject(response);
            response.Dispose();
            return result;
        }

        public static async Task<RestApiResult> SecuredObjectPutAsync(HttpClient httpClient, string routingApi, HttpContent content)
        {
            var response = await httpClient.PutAsync(httpClient.BaseAddress.ToString() + routingApi, content);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                if (await IsTokenExpiredAsync(httpClient))
                {
                    await ClientRefreshBearearAsync(httpClient);
                    response.Dispose();
                    response = await httpClient.PutAsync(httpClient.BaseAddress.ToString() + routingApi, content);
                }
            }

            var contents = await response.Content.ReadAsStringAsync();
            var result = contents.GetApiResultObject(response);
            response.Dispose();
            return result;
        }

        public static async Task<RestApiResult> SecuredObjectDeleteAsync(HttpClient httpClient, string routingApi)
        {
            var response = await httpClient.DeleteAsync(httpClient.BaseAddress.ToString() + routingApi);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                if (await IsTokenExpiredAsync(httpClient))
                {
                    await ClientRefreshBearearAsync(httpClient);
                    response.Dispose();
                    response = await httpClient.DeleteAsync(httpClient.BaseAddress.ToString() + routingApi);
                }
            }

            var contents = await response.Content.ReadAsStringAsync();
            var result = contents.GetApiResultObject(response);
            response.Dispose();
            return result;
        }

        public static async Task<RestApiResultFile> SecuredGetFileAsync(HttpClient httpClient, string routingApi)
        {
            var response = await httpClient.GetAsync(httpClient.BaseAddress.ToString() + routingApi);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                if (await IsTokenExpiredAsync(httpClient))
                {
                    await ClientRefreshBearearAsync(httpClient);
                    response = await httpClient.GetAsync(httpClient.BaseAddress.ToString() + routingApi);
                }
            }

            var file = await response.Content.ReadAsStreamAsync();
            var result = new RestApiResultFile()
            {
                StatusCode = (int)response.StatusCode,
                Data = file,
                Message = response.StatusCode.ToString(),
                FileName = response.Content.Headers.ContentDisposition?.FileName,
                FileType = response.Content.Headers.ContentType?.MediaType
            };

            return result;
        }


        #region Tokens
        private static async Task<bool> IsTokenExpiredAsync(HttpClient httpClient)
        {
            var result = false;
            var httpContextAccessor = new HttpContextAccessor();
            var encryptKeyMD5 = httpClient.DefaultRequestHeaders.GetValues(GeneralConstants.EncryptKeyName)?.FirstOrDefault();
            var applicationCookieName = httpClient.DefaultRequestHeaders.GetValues(GeneralConstants.ApplicationCookieName)?.FirstOrDefault();
            var paramSetting = new ParamSettings
            {
                ApplicationCookieName = applicationCookieName,
                SecurityEncryptKeyMD5 = encryptKeyMD5
            };

            var keyAccessTokenSessionName = HttpClientFactoryExtentions.GetApiAccessTokenSessionName(httpContextAccessor, paramSetting);
            var isAccessToken = SessionDataExtentions.IsExistSession(keyAccessTokenSessionName, httpContextAccessor);
            if (isAccessToken)
            {
                var token = SessionDataExtentions.GetSession(keyAccessTokenSessionName, httpContextAccessor).ToBase64Decode();
                if (!string.IsNullOrEmpty(token))
                {
                    var handler = new JwtSecurityTokenHandler();
                    var tokenSecurity = handler.ReadToken(token) as JwtSecurityToken;
                    DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(tokenSecurity.Payload.Expiration.Value);
                    if ((dateTimeOffset.LocalDateTime - DateTimeOffset.Now).TotalSeconds <= 0)
                    {
                        result = true;
                    }
                }
            }
            else
            {
                result = true;
            }

            return result;
        }

        private static async Task ClientRefreshBearearAsync(HttpClient httpClient)
        {
            var httpContextAccessor = new HttpContextAccessor();

            await RefreshTokenApiAsync(httpClient, httpContextAccessor);

        }

        private static async Task RefreshTokenApiAsync(HttpClient httpClient, HttpContextAccessor httpContextAccessor)
        {
            var encryptKeyMD5 = httpClient.DefaultRequestHeaders.GetValues(GeneralConstants.EncryptKeyName)?.FirstOrDefault();
            var applicationCookieName = httpClient.DefaultRequestHeaders.GetValues(GeneralConstants.ApplicationCookieName)?.FirstOrDefault();
            var applicationCookieExpiresInDays = httpClient.DefaultRequestHeaders.GetValues(GeneralConstants.ApplicationCookieExpiresInDays)?.FirstOrDefault();
            var paramSetting = new ParamSettings
            {
                ApplicationCookieName = applicationCookieName,
                SecurityEncryptKeyMD5 = encryptKeyMD5
            };

            var keyAccessTokenSessionName = HttpClientFactoryExtentions.GetApiAccessTokenSessionName(httpContextAccessor, paramSetting);
            var keyRefreshTokenSessionName = HttpClientFactoryExtentions.GetApiRefreshTokenSessionName(httpContextAccessor, paramSetting);

            var accessToken = SessionDataExtentions.GetSession(keyAccessTokenSessionName, httpContextAccessor).ToBase64Decode();
            var handlerAccessToken = new JwtSecurityTokenHandler();
            if (!string.IsNullOrEmpty(accessToken))
            {
                var jwtAccessToken = handlerAccessToken.ReadJwtToken(accessToken);

                var clientId = jwtAccessToken?.Claims?.FirstOrDefault(claim => claim.Type == "ClientId")?.Value?.ToBase64Decode();
                var clientSecret = jwtAccessToken?.Claims?.FirstOrDefault(claim => claim.Type == "ClientSecret")?.Value?.ToBase64Decode();
                var username = jwtAccessToken?.Claims?.FirstOrDefault(claim => claim.Type == "Username")?.Value?.ToBase64Decode();
                var email = jwtAccessToken?.Claims?.FirstOrDefault(claim => claim.Type == "Email")?.Value?.ToBase64Decode();
                var roles = jwtAccessToken?.Claims?.FirstOrDefault(claim => claim.Type == "Roles")?.Value?.ToBase64Decode();

                var refreshToken = SessionDataExtentions.GetSession(keyRefreshTokenSessionName, httpContextAccessor).ToBase64Decode();
                var handlerRefreshToken = new JwtSecurityTokenHandler();
                var jwtRefreshToken = handlerRefreshToken.ReadJwtToken(refreshToken);

                var refreshTokenClaim = jwtRefreshToken?.Claims?.FirstOrDefault(claim => claim.Type == "RefreshTokenClaim")?.Value;

                var refreshTokenClaimEncode = (clientId + clientSecret + username + roles).ToBase64Encode();
                if (refreshTokenClaim == refreshTokenClaimEncode)
                {
                    var param = new TokenJwtRequest
                    {
                        ClientId = clientId,
                        ClientSecret = clientSecret,
                        Username = username,
                        Email = email,
                        Roles = roles,
                    };

                    var response = await SecuredPostAsync<JwtResponse>(httpClient, GeneralConstants.RoutingToken, param);
                    var newAccessToken = response?.Data?.Token;
                    var newRefreshToken = response?.Data?.RefreshToken;

                    if (!string.IsNullOrWhiteSpace(newAccessToken) && !string.IsNullOrWhiteSpace(newRefreshToken))
                    {
                        SessionDataExtentions.SetSession(keyAccessTokenSessionName, newAccessToken.ToBase64Encode(), httpContextAccessor);
                        SessionDataExtentions.SetSession(keyRefreshTokenSessionName, newRefreshToken.ToBase64Encode(), httpContextAccessor);
                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(GeneralConstants.HeaderScheme, newAccessToken);
                    }
                }
            }
        }

        #endregion
    }
}
