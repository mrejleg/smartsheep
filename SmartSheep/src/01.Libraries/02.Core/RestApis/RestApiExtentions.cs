using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Project.Base.Models.RestApis;

namespace Project.Core.RestApis
{
    public static class RestApiExtentions
    {
        public static ObjectResult ReturnResponse(this RestApiResponse value)
        {
            var objectResult = new ObjectResult(value)
            {
                Value = value,
                StatusCode = value.StatusCode
            };

            return objectResult;
        }

        public static RestApiResult<TEntity> GetApiResult<TEntity>(this string contents, HttpResponseMessage httpResponseMessage) where TEntity : new()
        {
            try
            {
                var settings = new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.All,
                    PreserveReferencesHandling = PreserveReferencesHandling.Objects
                };

                if (string.IsNullOrEmpty(contents))
                {
                    var model = new RestApiResult<TEntity>
                    {
                        Message = httpResponseMessage.ReasonPhrase,
                        StatusCode = (int)httpResponseMessage.StatusCode,
                        StatusText = httpResponseMessage.StatusCode.ToString()
                    };

                    return model;
                }

                var results = JsonConvert.DeserializeObject<RestApiResult>(contents, settings);
                var result = new RestApiResult<TEntity>()
                {
                    StatusCode = results.StatusCode,
                    StatusText = results.StatusText,
                    Message = results.Message
                };

                if (results.Data != null)
                {
                    result.Data = JsonConvert.DeserializeObject<TEntity>(JsonConvert.SerializeObject(results.Data), settings);
                }

                return result;
            }
            catch (Exception ex)
            {
                var result = new RestApiResult<TEntity>
                {
                    Message = httpResponseMessage.ReasonPhrase + " - " + ex.Message + " - " + ex.ToString(),
                    StatusCode = (int)httpResponseMessage.StatusCode,
                    StatusText = httpResponseMessage.StatusCode.ToString()
                };

                return result;
            }
        }

        public static RestApiResult<TEntity> GetApiResultMapping<TEntity>(this string contents, HttpResponseMessage httpResponseMessage) where TEntity : new()
        {
            try
            {
                var settings = new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.All,
                    PreserveReferencesHandling = PreserveReferencesHandling.Objects
                };

                if (string.IsNullOrEmpty(contents))
                {
                    var model = new RestApiResult<TEntity>
                    {
                        Message = httpResponseMessage.ReasonPhrase,
                        StatusCode = (int)httpResponseMessage.StatusCode,
                        StatusText = httpResponseMessage.StatusCode.ToString(),
                        Data = new TEntity()
                    };

                    return model;
                }

                var results = JsonConvert.DeserializeObject<RestApiResult>(contents, settings);
                var result = new RestApiResult<TEntity>()
                {
                    StatusCode = results.StatusCode,
                    StatusText = results.StatusText,
                    Message = results.Message
                };

                if (results.Data != null)
                {
                    result.Data = JsonConvert.DeserializeObject<TEntity>(JsonConvert.SerializeObject(results.Data), settings);
                }
                else
                {
                    result.Data = new TEntity();
                }

                return result;
            }
            catch (Exception ex)
            {
                var result = new RestApiResult<TEntity>
                {
                    Message = httpResponseMessage.ReasonPhrase + " - " + ex.Message + " - " + ex.ToString(),
                    StatusCode = (int)httpResponseMessage.StatusCode,
                    StatusText = httpResponseMessage.StatusCode.ToString(),
                    Data = new TEntity()
                };

                return result;
            }
        }

        public static RestApiResult GetApiResultObject(this string contents, HttpResponseMessage httpResponseMessage)
        {
            try
            {
                var settings = new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.All,
                    PreserveReferencesHandling = PreserveReferencesHandling.Objects
                };

                if (string.IsNullOrEmpty(contents))
                {
                    var model = new RestApiResult
                    {
                        Message = httpResponseMessage.ReasonPhrase,
                        StatusCode = (int)httpResponseMessage.StatusCode,
                        StatusText = httpResponseMessage.StatusCode.ToString(),
                        Data = null
                    };

                    return model;
                }

                var results = JsonConvert.DeserializeObject<RestApiResult>(contents, settings);
                var result = new RestApiResult()
                {
                    StatusCode = results.StatusCode,
                    StatusText = results.StatusText,
                    Message = results.Message,
                    Data = results.Data
                };

                return result;
            }
            catch (Exception ex)
            {
                var result = new RestApiResult
                {
                    Message = httpResponseMessage.ReasonPhrase + " - " + ex.Message + " - " + ex.ToString(),
                    StatusCode = (int)httpResponseMessage.StatusCode,
                    StatusText = httpResponseMessage.StatusCode.ToString(),
                    Data = null
                };

                return result;
            }
        }

        public static async Task<RestApiResultFile> GetApiResultFileAsync(this string contents, HttpResponseMessage httpResponseMessage)
        {
            try
            {
                var settings = new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.All,
                    PreserveReferencesHandling = PreserveReferencesHandling.Objects
                };

                var file = await httpResponseMessage.Content.ReadAsStreamAsync();

                if (string.IsNullOrEmpty(contents))
                {
                    var model = new RestApiResultFile
                    {
                        Message = httpResponseMessage.ReasonPhrase,
                        StatusCode = (int)httpResponseMessage.StatusCode,
                        StatusText = httpResponseMessage.StatusCode.ToString(),
                        Data = file,
                        FileName = httpResponseMessage.Content.Headers.ContentDisposition?.FileName,
                        FileType = httpResponseMessage.Content.Headers.ContentType?.MediaType
                    };

                    return model;
                }

                var result = new RestApiResultFile
                {
                    Message = httpResponseMessage.ReasonPhrase,
                    StatusCode = (int)httpResponseMessage.StatusCode,
                    StatusText = httpResponseMessage.StatusCode.ToString(),
                    Data = file,
                    FileName = httpResponseMessage.Content?.Headers?.ContentDisposition?.FileName,
                    FileType = httpResponseMessage.Content?.Headers?.ContentType?.MediaType
                };

                return result;
            }
            catch (Exception ex)
            {
                var result = new RestApiResultFile
                {
                    Message = httpResponseMessage.ReasonPhrase + " - " + ex.Message + " - " + ex.ToString(),
                    StatusCode = (int)httpResponseMessage.StatusCode,
                    StatusText = httpResponseMessage.StatusCode.ToString(),
                    Data = null,
                    FileName = null,
                    FileType = null
                };

                return result;
            }
        }
    }
}
