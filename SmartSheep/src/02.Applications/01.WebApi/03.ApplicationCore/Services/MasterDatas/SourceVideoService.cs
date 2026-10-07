using Api.ApplicationCore.Interfaces.MasterDatas;
using Api.ApplicationCore.Interfaces.Repositories;
using Api.Domain.Entities.MasterDatas;
using DevExtreme.AspNet.Data.ResponseModel;
using Microsoft.EntityFrameworkCore;
using Project.Base.Models;
using Project.Base.Models.DevExpress.DxGrids;
using Project.Base.Models.JQueries.Datatables;
using Project.Core.DevExpress;

namespace Api.ApplicationCore.Services.MasterDatas
{
    public class SourceVideoService : ISourceVideoService
    {
        private readonly IMasterDataRepositories _masterDataRepository;

        public SourceVideoService(IMasterDataRepositories repository)
        {
            _masterDataRepository = repository;
        }

        public async Task<LoadResult> DxGridAsync(DataSourceLoadOptions param)
        {
            var datas = GetAll();
            return await DxDataGridExtentions.DxDataGridAsync(datas, param);
        }

        public IQueryable<SourceVideo> GetAll()
        {
            return _masterDataRepository.SourceVideos.GetAll()
                .Include(x => x.Barn);
        }

        public IQueryable<SourceVideo> GetAllByBarn(Guid fkBarnId)
        {
            return GetAll().Where(x => x.FkBarnId == fkBarnId);
        }

        public async Task<DataPagedResults<SourceVideo>> GetAsync(DataParameter param)
        {
            var data = GetAll();
            return await _masterDataRepository.SourceVideos.GetByPagingAsync(data, param.Start, param.Length);
        }

        public async Task<SourceVideo> GetAsync(Guid id)
        {
            return await GetAll().FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<Guid?> PostAsync(SourceVideo model)
        {
            var data = await _masterDataRepository.SourceVideos.AddAsync(model);
            return data.Id;
        }

        public async Task<Guid?> PutAsync(SourceVideo model)
        {
            await _masterDataRepository.SourceVideos.UpdateAsync(model);
            return model.Id;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            bool result = false;
            var data = await GetAsync(id);

            if (data != null)
            {
                await _masterDataRepository.SourceVideos.DeleteAsync(data);
                result = true;
            }

            return result;
        }
    }
}
