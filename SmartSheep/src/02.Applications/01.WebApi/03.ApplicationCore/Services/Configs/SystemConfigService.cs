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
    public class SystemConfigService : ISystemConfigService
    {
        private readonly IConfigRepositories _configRepository;

        public SystemConfigService(IConfigRepositories repository)
        {
            _configRepository = repository;
        }

        public async Task<LoadResult> DxGridAsync(DataSourceLoadOptions param)
        {
            var datas = GetAll();
            return await DxDataGridExtentions.DxDataGridAsync(datas, param);
        }

        public IQueryable<SystemConfig> GetAll()
        {
            return _configRepository.SystemConfigs.GetAll();
        }

        public async Task<DataPagedResults<SystemConfig>> GetAsync(DataParameter param)
        {
            var data = GetAll();
            return await _configRepository.SystemConfigs.GetByPagingAsync(data, param.Start, param.Length);
        }

        public async Task<SystemConfig> GetAsync(Guid id)
        {
            return await GetAll().FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<Guid?> PostAsync(SystemConfig model)
        {
            var data = await _configRepository.SystemConfigs.AddAsync(model);
            return data.Id;
        }

        public async Task<Guid?> PutAsync(SystemConfig model)
        {
            await _configRepository.SystemConfigs.UpdateAsync(model);
            return model.Id;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            bool result = false;
            var data = await GetAsync(id);

            if (data != null)
            {
                await _configRepository.SystemConfigs.DeleteAsync(data);
                result = true;
            }

            return result;
        }
    }
}
