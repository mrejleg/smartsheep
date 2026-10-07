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
    public class ReminderLogService : IReminderLogService
    {
        private readonly ISmartSheepRepositories _smartSheepRepository;

        public ReminderLogService(ISmartSheepRepositories repository)
        {
            _smartSheepRepository = repository;
        }

        public async Task<LoadResult> DxGridAsync(DataSourceLoadOptions param)
        {
            var datas = GetAll();
            return await DxDataGridExtentions.DxDataGridAsync(datas, param);
        }

        public IQueryable<ReminderLog> GetAll()
        {
            return _smartSheepRepository.ReminderLogs.GetAll()
                .Include(x => x.ReminderTemplate)
                .Include(x => x.SucklingStatistic);
        }

        public IQueryable<ReminderLog> GetAllByReminderTemplate(Guid fkReminderTemplateId)
        {
            return GetAll().Where(x => x.FkReminderTemplateId == fkReminderTemplateId);
        }

        public IQueryable<ReminderLog> GetAllBySucklingStatistic(Guid fkSucklingStatisticId)
        {
            return GetAll().Where(x => x.FkSucklingStatisticId == fkSucklingStatisticId);
        }

        public async Task<DataPagedResults<ReminderLog>> GetAsync(DataParameter param)
        {
            var data = GetAll();
            return await _smartSheepRepository.ReminderLogs.GetByPagingAsync(data, param.Start, param.Length);
        }

        public async Task<ReminderLog> GetAsync(Guid id)
        {
            return await GetAll().FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<Guid?> PostAsync(ReminderLog model)
        {
            var data = await _smartSheepRepository.ReminderLogs.AddAsync(model);
            return data.Id;
        }

        public async Task<Guid?> PutAsync(ReminderLog model)
        {
            await _smartSheepRepository.ReminderLogs.UpdateAsync(model);
            return model.Id;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            bool result = false;
            var data = await GetAsync(id);

            if (data != null)
            {
                await _smartSheepRepository.ReminderLogs.DeleteAsync(data);
                result = true;
            }

            return result;
        }
    }
}
