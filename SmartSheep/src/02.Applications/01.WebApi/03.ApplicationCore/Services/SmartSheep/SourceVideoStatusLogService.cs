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
    public class SourceVideoStatusLogService : ISourceVideoStatusLogService
    {
        private readonly ISmartSheepRepositories _smartSheepRepository;

        public SourceVideoStatusLogService(ISmartSheepRepositories repository)
        {
            _smartSheepRepository = repository;
        }

        public async Task<LoadResult> DxGridAsync(DataSourceLoadOptions param)
        {
            var datas = GetAll();
            return await DxDataGridExtentions.DxDataGridAsync(datas, param);
        }

        public IQueryable<SourceVideoStatusLog> GetAll()
        {
            return _smartSheepRepository.SourceVideoStatusLogs.GetAll()
                .Include(x => x.SourceVideo);
        }

        public IQueryable<SourceVideoStatusLog> GetAllBySourceVideo(Guid fkIdSourceVideo)
        {
            return GetAll().Where(x => x.FkIdSourceVideo == fkIdSourceVideo);
        }

        public async Task<DataPagedResults<SourceVideoStatusLog>> GetAsync(DataParameter param)
        {
            var data = GetAll();
            return await _smartSheepRepository.SourceVideoStatusLogs.GetByPagingAsync(data, param.Start, param.Length);
        }

        public async Task<SourceVideoStatusLog> GetAsync(Guid id)
        {
            return await GetAll().FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<Guid?> PostAsync(SourceVideoStatusLog model)
        {
            var data = await _smartSheepRepository.SourceVideoStatusLogs.AddAsync(model);
            return data.Id;
        }

        public async Task<Guid?> PutAsync(SourceVideoStatusLog model)
        {
            await _smartSheepRepository.SourceVideoStatusLogs.UpdateAsync(model);
            return model.Id;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            bool result = false;
            var data = await GetAsync(id);

            if (data != null)
            {
                await _smartSheepRepository.SourceVideoStatusLogs.DeleteAsync(data);
                result = true;
            }

            return result;
        }
    }
}
