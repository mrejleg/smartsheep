using Api.ApplicationCore.Interfaces.ApiManagements;
using Api.ApplicationCore.Interfaces.Repositories;
using Api.Domain.ApiManagements;
using DevExtreme.AspNet.Data.ResponseModel;
using Microsoft.EntityFrameworkCore;
using Project.Base.Models;
using Project.Base.Models.DevExpress.DxDropDownBoxs;
using Project.Base.Models.DevExpress.DxGrids;
using Project.Base.Models.JQueries.Datatables;
using Project.Core.DevExpress;

namespace Api.ApplicationCore.Services.ApiManagements
{
    public class AppClientService : IAppClientService
    {
        private readonly IConfigRepositories _repository;

        public AppClientService(IConfigRepositories repository)
        {
            _repository = repository;
        }

        public async Task<LoadResult> DxGridAsync(DataSourceLoadOptions param)
        {
            var datas = _repository.AppClients.GetAll();
            return await DxDataGridExtentions.DxDataGridAsync(datas, param);
        }

        public IQueryable<AppClient> GetAll()
        {
            var data = _repository.AppClients.GetAll();
            return data;
        }
        public async Task<IReadOnlyList<AppClient>> GetAllAsync()
        {
            var data = await _repository.AppClients.GetAllAsync();
            return data;
        }

        public async Task<DataPagedResults<AppClient>> GetAsync(DataParameter param)
        {
            var data = GetAll();
            return await _repository.AppClients.GetByPagingAsync(data, param.Start, param.Length);
        }

        public async Task<AppClient> GetAsync(Guid id)
        {
            var data = await _repository.AppClients.GetAsync(id);
            return data;
        }

        public async Task<AppClient> GetAsync(string clientId)
        {
            var data = await _repository.AppClients.DataContext().FirstOrDefaultAsync(x => x.ClientId == clientId);
            return data;
        }

        public async Task<Guid?> PostAsync(AppClient model)
        {
            Guid? id = null;

            var isExist = await _repository.AppClients.DataContext().AnyAsync(x => x.Name == model.Name || x.ClientId == model.ClientId);

            if (!isExist)
            {
                await _repository.AppClients.AddAsync(model);
                id = model.Id;
            }

            return id;
        }

        public async Task<Guid?> PutAsync(AppClient model)
        {
            Guid? id = null;
            var isExist = await _repository.AppClients.DataContext().AnyAsync(x => (x.Name == model.Name || x.ClientId == model.ClientId) && x.Id != model.Id);

            if (!isExist)
            {
                await _repository.AppClients.UpdateAsync(model);
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
                var isAppScope = await _repository.Contexts.AppScopeClient.AnyAsync(x => x.FkAppClientId == id);

                if (!isAppScope)
                {
                    //delete icon
                    await _repository.AppClients.DeleteAsync(data);
                    result = true;
                }
            }

            return result;
        }

        public async Task<IReadOnlyList<DxDropDownBoxBinding>> BindingDxDropDownBoxAsync()
        {
            var data = await _repository.AppClients.GetAll()
                .Where(x => x.IsActive == true)
                .OrderBy(x => x.Name)
                .Select(x => new DxDropDownBoxBinding
                {
                    Id = x.Id,
                    Text = x.ClientId + " - " + x.Name
                }).ToListAsync();

            return data;
        }
    }
}
