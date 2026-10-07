using Api.ApplicationCore.Interfaces.ApiManagements;
using Api.ApplicationCore.Interfaces.Repositories;
using Api.Domain.ApiManagements;
using Api.Domain.Models;
using DevExtreme.AspNet.Data.ResponseModel;
using Microsoft.EntityFrameworkCore;
using Project.Base.Models;
using Project.Base.Models.DevExpress.DxDropDownBoxs;
using Project.Base.Models.DevExpress.DxGrids;
using Project.Base.Models.JQueries.Datatables;
using Project.Core.DevExpress;
using Project.Core.Interfaces.Repositories;

namespace Api.ApplicationCore.Services.ApiManagements
{
    public class ScopeClientService : IScopeClientService
    {
        private readonly IApiManagementRepositories _repository;
        private readonly IDapperRepo _dapperRepository;
        private readonly AppSetting _appSetting;

        public ScopeClientService(IApiManagementRepositories repository, AppSetting appSetting, IDapperRepo dapperRepository)
        {
            _repository = repository;
            _appSetting = appSetting;
            _dapperRepository = dapperRepository;
            _dapperRepository.DapperRepositories.ConnectionString = _appSetting.ConnectionStringsDefaultConnection;
        }

        public async Task<LoadResult> DxGridAsync(DataSourceLoadOptions param)
        {
            var datas = _repository.ScopeClients.GetAll();
            return await DxDataGridExtentions.DxDataGridAsync(datas, param);
        }

        public IQueryable<ScopeClient> GetAll()
        {
            var data = _repository.ScopeClients.GetAll();
            return data;
        }

        public async Task<DataPagedResults<ScopeClient>> GetAsync(DataParameter param)
        {
            var data = GetAll();
            return await _repository.ScopeClients.GetByPagingAsync(data, param.Start, param.Length);
        }

        public async Task<ScopeClient> GetAsync(Guid id)
        {
            return await _repository.Contexts.ScopeClient.FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<ScopeClient> GetAsync(string key)
        {
            return await _repository.Contexts.ScopeClient.FirstOrDefaultAsync(x => x.Name == key);
        }

        public async Task<Guid?> PostAsync(ScopeClient model)
        {
            Guid? id = null;
            var isExist = await _repository.ScopeClients.DataContext().AnyAsync(x => x.Name == model.Name);

            if (!isExist)
            {
                var data = await _repository.ScopeClients.AddAsync(model);
                id = data.Id;
            }

            return id;
        }

        public async Task<Guid?> PutAsync(ScopeClient model)
        {
            Guid? id = null;
            var isExist = await _repository.ScopeClients.DataContext().AnyAsync(x => x.Name == model.Name && x.Id != model.Id);

            if (!isExist)
            {
                await _repository.ScopeClients.UpdateAsync(model);
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
                await _repository.ScopeClients.DeleteAsync(data);
                result = true;
            }

            return result;
        }

        public async Task<IReadOnlyList<DxDropDownBoxBinding>> BindingDxDropDownBoxAsync()
        {
            var data = await _repository.ScopeClients.GetAll()
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
