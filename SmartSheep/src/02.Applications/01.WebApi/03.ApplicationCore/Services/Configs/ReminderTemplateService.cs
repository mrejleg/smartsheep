using Api.ApplicationCore.Interfaces.Configs;
using Api.ApplicationCore.Interfaces.Repositories;
using Api.Domain.Entities.Configs;
using DevExtreme.AspNet.Data.ResponseModel;
using Microsoft.EntityFrameworkCore;
using Project.Base.Models;
using Project.Base.Models.DevExpress.DxGrids;
using Project.Base.Models.JQueries.Datatables;
using Project.Core.DevExpress;

namespace Api.ApplicationCore.Services.Configs
{
    public class ReminderTemplateService : IReminderTemplateService
    {
        private readonly IConfigRepositories _configRepository;

        public ReminderTemplateService(IConfigRepositories repository)
        {
            _configRepository = repository;
        }

        public async Task<LoadResult> DxGridAsync(DataSourceLoadOptions param)
        {
            var datas = GetAll();
            return await DxDataGridExtentions.DxDataGridAsync(datas, param);
        }

        public IQueryable<ReminderTemplate> GetAll()
        {
            return _configRepository.ReminderTemplates.GetAll();
        }

        public async Task<DataPagedResults<ReminderTemplate>> GetAsync(DataParameter param)
        {
            var data = GetAll();
            return await _configRepository.ReminderTemplates.GetByPagingAsync(data, param.Start, param.Length);
        }

        public async Task<ReminderTemplate> GetAsync(Guid id)
        {
            return await GetAll().FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<Guid?> PostAsync(ReminderTemplate model)
        {
            var data = await _configRepository.ReminderTemplates.AddAsync(model);
            return data.Id;
        }

        public async Task<Guid?> PutAsync(ReminderTemplate model)
        {
            await _configRepository.ReminderTemplates.UpdateAsync(model);
            return model.Id;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            bool result = false;
            var data = await GetAsync(id);

            if (data != null)
            {
                await _configRepository.ReminderTemplates.DeleteAsync(data);
                result = true;
            }

            return result;
        }
    }
}
