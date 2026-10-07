using DevExtreme.AspNet.Data.ResponseModel;
using Project.Base.Models.DevExpress.DxDropDownBoxs;
using Project.Base.Models.DevExpress.DxGrids;
using Project.Base.Models.RestApis;
using Web.Domain.Models.Configs;

namespace Web.ApplicationCore.Interfaces.Configs
{
    public interface IEmailTemplateService
    {
        Task<LoadResult> DxGridAsync(DataSourceLoadOptions param);
        Task<RestApiResult<List<EmailTemplate>>> GetAllAsync();
        Task<RestApiResult<EmailTemplate>> GetAsync(string code);
        Task<RestApiResult<EmailTemplate>> GetAsync(Guid id);
        Task<RestApiResult<EmailTemplate>> PostAsync(EmailTemplate model);
        Task<RestApiResult<EmailTemplate>> PutAsync(EmailTemplate model);
        Task<RestApiResult<EmailTemplate>> DeleteAsync(Guid id);

        Task<RestApiResult<List<DxDropDownBoxBinding>>> BindingDxDropDownBoxAsync();
    }
}
