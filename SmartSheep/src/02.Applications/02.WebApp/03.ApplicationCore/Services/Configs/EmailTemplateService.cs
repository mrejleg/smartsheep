using DevExtreme.AspNet.Data.ResponseModel;
using Microsoft.AspNetCore.Authorization;
using Project.Base.Models.DevExpress.DxDropDownBoxs;
using Project.Base.Models.DevExpress.DxGrids;
using Project.Base.Models.RestApis;
using Project.Core.HttpClients;
using Web.ApplicationCore.Interfaces.Configs;
using Web.Domain.Models;
using Web.Domain.Models.Configs;
using Web.Infrastructure;

namespace Web.ApplicationCore.Services.Configs
{
    [Authorize(Policy = "Bearer")]
    public class EmailTemplateService : IEmailTemplateService
    {
        private readonly HttpClient _client;
        private readonly AppSetting _appSettings;


        public EmailTemplateService(AppSetting appSettings)
        {
            _appSettings = appSettings;
            _client = HttpClientFactoryExtentions.ClientApiBearear(MapperAppSettings.ParamAppSetting(_appSettings));
        }


        public async Task<LoadResult> DxGridAsync(DataSourceLoadOptions param)
        {
            return await HttpClientExtentions.SecuredDxGridAsync<EmailTemplate>(_client, "EmailTemplate/DxGrid", param);
        }


        public async Task<RestApiResult<List<EmailTemplate>>> GetAllAsync()
        {
            return await HttpClientExtentions.SecuredGetAsync<List<EmailTemplate>>(_client, "EmailTemplate/All");
        }


        public async Task<RestApiResult<EmailTemplate>> GetAsync(Guid id)
        {
            return await HttpClientExtentions.SecuredGetAsync<EmailTemplate>(_client, "EmailTemplate/" + id);
        }


        public async Task<RestApiResult<EmailTemplate>> GetAsync(string code)
        {
            return await HttpClientExtentions.SecuredGetAsync<EmailTemplate>(_client, "EmailTemplate/Code/" + code);
        }


        public async Task<RestApiResult<EmailTemplate>> PostAsync(EmailTemplate param)
        {
            return await HttpClientExtentions.SecuredPostAsync<EmailTemplate>(_client, "EmailTemplate", param);
        }


        public async Task<RestApiResult<EmailTemplate>> PutAsync(EmailTemplate param)
        {
            return await HttpClientExtentions.SecuredPutAsync<EmailTemplate>(_client, "EmailTemplate", param);
        }


        public async Task<RestApiResult<EmailTemplate>> DeleteAsync(Guid id)
        {
            return await HttpClientExtentions.SecuredDeleteAsync<EmailTemplate>(_client, "EmailTemplate/" + id);
        }


        public async Task<RestApiResult<List<DxDropDownBoxBinding>>> BindingDxDropDownBoxAsync()
        {
            return await HttpClientExtentions.SecuredGetAsync<List<DxDropDownBoxBinding>>(_client, "EmailTemplate/Binding/DxDropDownBox");
        }
    }
}
