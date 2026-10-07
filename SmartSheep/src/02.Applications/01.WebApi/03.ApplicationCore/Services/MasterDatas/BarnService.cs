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
    public class BarnService : IBarnService
    {
        private readonly IMasterDataRepositories _masterDataRepository;

        public BarnService(IMasterDataRepositories repository)
        {
            _masterDataRepository = repository;
        }

        public async Task<LoadResult> DxGridAsync(DataSourceLoadOptions param)
        {
            var datas = GetAll();
            return await DxDataGridExtentions.DxDataGridAsync(datas, param);
        }

        public IQueryable<Barn> GetAll()
        {
            return _masterDataRepository.Barns.GetAll()
                .Include(x => x.Farm)
                .Include(x => x.Livestock);
        }

        public IQueryable<Barn> GetAllByFarm(Guid fkFarmId)
        {
            return GetAll().Where(x => x.FkFarmId == fkFarmId);
        }

        public IQueryable<Barn> GetAllByLivestock(Guid fkLivestockId)
        {
            return GetAll().Where(x => x.FkLivestockId == fkLivestockId);
        }

        public async Task<DataPagedResults<Barn>> GetAsync(DataParameter param)
        {
            var data = GetAll();
            return await _masterDataRepository.Barns.GetByPagingAsync(data, param.Start, param.Length);
        }

        public async Task<Barn> GetAsync(Guid id)
        {
            return await GetAll().FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<Guid?> PostAsync(Barn model)
        {
            var data = await _masterDataRepository.Barns.AddAsync(model);
            return data.Id;
        }

        public async Task<Guid?> PutAsync(Barn model)
        {
            await _masterDataRepository.Barns.UpdateAsync(model);
            return model.Id;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            bool result = false;
            var data = await GetAsync(id);

            if (data != null)
            {
                await _masterDataRepository.Barns.DeleteAsync(data);
                result = true;
            }

            return result;
        }
    }
}
