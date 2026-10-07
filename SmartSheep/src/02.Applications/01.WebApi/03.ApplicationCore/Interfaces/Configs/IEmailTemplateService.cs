using Api.Domain.Entities.Configs;
using DevExtreme.AspNet.Data.ResponseModel;
using Project.Base.Models;
using Project.Base.Models.DevExpress.DxDropDownBoxs;
using Project.Base.Models.DevExpress.DxGrids;
using Project.Base.Models.JQueries.Datatables;

namespace Api.ApplicationCore.Interfaces.Configs
{
    public interface IEmailTemplateService
    {
        Task<LoadResult> DxGridAsync(DataSourceLoadOptions param);
        IQueryable<EmailTemplate> GetAll();
        Task<EmailTemplate> GetAsync(Guid id);
        Task<EmailTemplate> GetAsync(string code);
        Task<DataPagedResults<EmailTemplate>> GetAsync(DataParameter param);
        Task<Guid?> PostAsync(EmailTemplate model);
        Task<Guid?> PutAsync(EmailTemplate model);
        Task<bool> DeleteAsync(Guid id);
        Task<IReadOnlyList<DxDropDownBoxBinding>> BindingDxDropDownBoxAsync();
    }
}
