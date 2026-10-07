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
    public class FarmService : IFarmService
    {
        private readonly IMasterDataRepositories _masterDataRepository;

        public FarmService(IMasterDataRepositories repository)
        {
            _masterDataRepository = repository;
        }

        public async Task<LoadResult> DxGridAsync(DataSourceLoadOptions param)
        {
            var datas = GetAll();
            return await DxDataGridExtentions.DxDataGridAsync(datas, param);
        }

        public IQueryable<Farm> GetAll()
        {
            return _masterDataRepository.Farms.GetAll();
        }

        public async Task<DataPagedResults<Farm>> GetAsync(DataParameter param)
        {
            var data = GetAll();
            return await _masterDataRepository.Farms.GetByPagingAsync(data, param.Start, param.Length);
        }

        public async Task<Farm> GetAsync(Guid id)
        {
            return await GetAll().FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<Guid?> PostAsync(Farm model)
        {
            var data = await _masterDataRepository.Farms.AddAsync(model);
            return data.Id;
        }

        public async Task<Guid?> PutAsync(Farm model)
        {
            await _masterDataRepository.Farms.UpdateAsync(model);
            return model.Id;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            bool result = false;
            var data = await GetAsync(id);

            if (data != null)
            {
                await _masterDataRepository.Farms.DeleteAsync(data);
                result = true;
            }

            return result;
        }
    }
}
