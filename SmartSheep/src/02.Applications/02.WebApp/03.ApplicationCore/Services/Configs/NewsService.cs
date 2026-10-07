using DevExtreme.AspNet.Data.ResponseModel;
using Microsoft.AspNetCore.Authorization;
using Project.Base.Models.DevExpress.DxGrids;
using Project.Base.Models.RestApis;
using Project.Core.HttpClients;
using Web.ApplicationCore.Interfaces.Configs;
using Web.Domain.Models;
using Web.Domain.Models.Configs;
using Web.Infrastructure;
using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;

namespace Web.ApplicationCore.Services.Configs
{
    [Authorize(Policy = "Bearer")]
    public class NewsService : INewsService
    {
        private readonly HttpClient _client;
        private readonly AppSetting _appSettings;

        public NewsService(AppSetting appSettings)
        {
            _appSettings = appSettings;
            _client = HttpClientFactoryExtentions.ClientApiBearear(MapperAppSettings.ParamAppSetting(_appSettings));
        }

        public async Task<LoadResult> DxGridAsync(DataSourceLoadOptions param)
        {
            return await HttpClientExtentions.SecuredDxGridAsync<News>(_client, "News/DxGrid", param);
        }

        public async Task<RestApiResult<List<News>>> GetAllAsync()
        {
            return await HttpClientExtentions.SecuredGetAsync<List<News>>(_client, "News/All");
        }

        public async Task<RestApiResult<News>> GetAsync(Guid id)
        {
            return await HttpClientExtentions.SecuredGetAsync<News>(_client, "News/" + id);
        }

        public async Task<RestApiResult<News>> PostAsync(News model)
        {
            return await HttpClientExtentions.SecuredPostAsync<News>(_client, "News", model);
        }

        public async Task<RestApiResult<News>> PutAsync(News model)
        {
            return await HttpClientExtentions.SecuredPutAsync<News>(_client, "News", model);
        }

        public async Task<RestApiResult<News>> DeleteAsync(Guid id)
        {
            return await HttpClientExtentions.SecuredDeleteAsync<News>(_client, "News/" + id);
        }

        public async Task<RestApiResult<NewsImageUploadResult>> UploadImageAsync(IFormFile file)
        {
            using var form = new MultipartFormDataContent();
            await using var stream = file.OpenReadStream();
            using var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType ?? "application/octet-stream");
            form.Add(fileContent, "file", Path.GetFileName(file.FileName));
            return await HttpClientExtentions.SecuredPostAsync<NewsImageUploadResult>(_client, "News/UploadImage", form);
        }
    }
}
