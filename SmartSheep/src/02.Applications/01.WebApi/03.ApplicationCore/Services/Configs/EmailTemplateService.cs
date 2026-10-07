using Api.ApplicationCore.Interfaces.Configs;
using Api.ApplicationCore.Interfaces.Repositories;
using Api.Domain.Entities.Configs;
using DevExtreme.AspNet.Data.ResponseModel;
using Microsoft.EntityFrameworkCore;
using Project.Base.Models;
using Project.Base.Models.DevExpress.DxDropDownBoxs;
using Project.Base.Models.DevExpress.DxGrids;
using Project.Base.Models.JQueries.Datatables;
using Project.Core.DevExpress;

namespace Api.ApplicationCore.Services.Configs
{
    public class EmailTemplateService : IEmailTemplateService
    {
        private readonly IConfigRepositories _configRepository;

        public EmailTemplateService(IConfigRepositories configRepository)
        {
            _configRepository = configRepository;
        }

        public async Task<LoadResult> DxGridAsync(DataSourceLoadOptions param)
        {
            var datas = _configRepository.EmailTemplates.GetAll();
            return await DxDataGridExtentions.DxDataGridAsync(datas, param);
        }

        public IQueryable<EmailTemplate> GetAll()
        {
            var data = _configRepository.EmailTemplates.GetAll();
            return data;
        }

        public async Task<DataPagedResults<EmailTemplate>> GetAsync(DataParameter param)
        {
            var data = GetAll();
            return await _configRepository.EmailTemplates.GetByPagingAsync(data, param.Start, param.Length);
        }

        public async Task<EmailTemplate> GetAsync(Guid id)
        {
            return await _configRepository.EmailTemplates.DataContext().FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<EmailTemplate> GetAsync(string code)
        {
            return await _configRepository.EmailTemplates.DataContext().FirstOrDefaultAsync(x => x.Name == code);
        }

        public async Task<Guid?> PostAsync(EmailTemplate model)
        {
            Guid? id = null;
            var isExist = await _configRepository.EmailTemplates.DataContext().AnyAsync(x => x.Name == model.Name);

            if (!isExist)
            {
                var data = await _configRepository.EmailTemplates.AddAsync(model);
                id = data.Id;
            }

            return id;
        }

        public async Task<Guid?> PutAsync(EmailTemplate model)
        {
            Guid? id = null;
            var isExist = await _configRepository.EmailTemplates.DataContext().AnyAsync(x => x.Name == model.Name && x.Id != model.Id);

            if (!isExist)
            {
                await _configRepository.EmailTemplates.UpdateAsync(model);
                id = model.Id;
            }

            return id;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            bool result = false;

            var data = await GetAsync(id);

            if (data != null)
            {
                await _configRepository.EmailTemplates.DeleteAsync(data);
                result = true;
            }

            return result;
        }

        public async Task<IReadOnlyList<DxDropDownBoxBinding>> BindingDxDropDownBoxAsync()
        {
            var data = await _configRepository.EmailTemplates.GetAll()
                .Where(x => x.IsActive == true)
                .OrderBy(x => x.Name)
                .Select(x => new DxDropDownBoxBinding
                {
                    Id = x.Id,
                    Text = x.Name
                }).ToListAsync();

            return data;
        }
    }
}
