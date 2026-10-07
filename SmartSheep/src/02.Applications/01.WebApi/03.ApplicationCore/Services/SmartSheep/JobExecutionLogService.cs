using Api.ApplicationCore.Interfaces.SmartSheep;
using Api.ApplicationCore.Interfaces.Repositories;
using Api.Domain.Entities.SmartSheep;
using DevExtreme.AspNet.Data.ResponseModel;
using Microsoft.EntityFrameworkCore;
using Project.Base.Models;
using Project.Base.Models.DevExpress.DxGrids;
using Project.Base.Models.JQueries.Datatables;
using Project.Core.DevExpress;

namespace Api.ApplicationCore.Services.SmartSheep
{
    public class JobExecutionLogService : IJobExecutionLogService
    {
        private readonly ISmartSheepRepositories _smartSheepRepository;

        public JobExecutionLogService(ISmartSheepRepositories repository)
        {
            _smartSheepRepository = repository;
        }

        public async Task<LoadResult> DxGridAsync(DataSourceLoadOptions param)
        {
            var datas = GetAll();
            return await DxDataGridExtentions.DxDataGridAsync(datas, param);
        }

        public IQueryable<JobExecutionLog> GetAll()
        {
            return _smartSheepRepository.JobExecutionLogs.GetAll();
        }

        public async Task<DataPagedResults<JobExecutionLog>> GetAsync(DataParameter param)
        {
            var data = GetAll();
            return await _smartSheepRepository.JobExecutionLogs.GetByPagingAsync(data, param.Start, param.Length);
        }

        public async Task<JobExecutionLog> GetAsync(Guid id)
        {
            return await GetAll().FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<Guid?> PostAsync(JobExecutionLog model)
        {
            var data = await _smartSheepRepository.JobExecutionLogs.AddAsync(model);
            return data.Id;
        }

        public async Task<Guid?> PutAsync(JobExecutionLog model)
        {
            await _smartSheepRepository.JobExecutionLogs.UpdateAsync(model);
            return model.Id;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            bool result = false;
            var data = await GetAsync(id);

            if (data != null)
            {
                await _smartSheepRepository.JobExecutionLogs.DeleteAsync(data);
                result = true;
            }

            return result;
        }
    }
}
