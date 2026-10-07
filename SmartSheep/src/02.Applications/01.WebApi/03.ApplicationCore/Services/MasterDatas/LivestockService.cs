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
    public class LivestockService : ILivestockService
    {
        private readonly IMasterDataRepositories _masterDataRepository;

        public LivestockService(IMasterDataRepositories repository)
        {
            _masterDataRepository = repository;
        }

        public async Task<LoadResult> DxGridAsync(DataSourceLoadOptions param)
        {
            var datas = GetAll();
            return await DxDataGridExtentions.DxDataGridAsync(datas, param);
        }

        public IQueryable<Livestock> GetAll()
        {
            return _masterDataRepository.Livestocks.GetAll();
        }

        public async Task<DataPagedResults<Livestock>> GetAsync(DataParameter param)
        {
            var data = GetAll();
            return await _masterDataRepository.Livestocks.GetByPagingAsync(data, param.Start, param.Length);
        }

        public async Task<Livestock> GetAsync(Guid id)
        {
            return await GetAll().FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<Guid?> PostAsync(Livestock model)
        {
            var data = await _masterDataRepository.Livestocks.AddAsync(model);
            return data.Id;
        }

        public async Task<Guid?> PutAsync(Livestock model)
        {
            await _masterDataRepository.Livestocks.UpdateAsync(model);
            return model.Id;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            bool result = false;
            var data = await GetAsync(id);

            if (data != null)
            {
                await _masterDataRepository.Livestocks.DeleteAsync(data);
                result = true;
            }

            return result;
        }
    }
}
