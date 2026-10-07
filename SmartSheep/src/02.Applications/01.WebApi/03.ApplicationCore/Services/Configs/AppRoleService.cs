using Api.ApplicationCore.Interfaces.Configs;
using Api.ApplicationCore.Interfaces.Repositories;
using Api.Domain.Entities.Configs;
using Api.Domain.Models;
using DevExtreme.AspNet.Data.ResponseModel;
using Microsoft.EntityFrameworkCore;
using Project.Base.Models;
using Project.Base.Models.DevExpress.DxDropDownBoxs;
using Project.Base.Models.DevExpress.DxGrids;
using Project.Base.Models.JQueries.Datatables;
using Project.Core.DevExpress;
using Project.Core.Interfaces.Repositories;

namespace Api.ApplicationCore.Services.Configs
{
    public class AppRoleService : IAppRoleService
    {
        private readonly IConfigRepositories _repository;
        private readonly IDapperRepo _dapperRepository;
        private readonly AppSetting _appSetting;

        public AppRoleService(IConfigRepositories repository, AppSetting appSetting, IDapperRepo dapperRepository)
        {
            _repository = repository;
            _appSetting = appSetting;

            _dapperRepository = dapperRepository;
            _dapperRepository.DapperRepositories.ConnectionString = _appSetting.ConnectionStringsDefaultConnection;
        }

        public async Task<LoadResult> DxGridAsync(DataSourceLoadOptions param)
        {
            var datas = _repository.AppRoles.DataContext().AsNoTracking();

            return await DxDataGridExtentions.DxDataGridAsync(datas, param);
        }

        public IQueryable<AppRole> GetAll()
        {
            var data = _repository.AppRoles.GetAll().AsNoTracking();
            return data;
        }

        public async Task<DataPagedResults<AppRole>> GetAsync(DataParameter param)
        {
            var data = GetAll();
            return await _repository.AppRoles.GetByPagingAsync(data, param.Start, param.Length);
        }

        public async Task<AppRole> GetAsync(Guid id)
        {
            return await _repository.Contexts.AppRole.FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<AppRole> GetByNameAsync(string name)
        {
            return await _repository.Contexts.AppRole.AsNoTracking().FirstOrDefaultAsync(x => x.Name == name);
        }

        public async Task<Guid?> PostAsync(AppRole model)
        {
            Guid? id = null;
            var isExist = await _repository.AppRoles.DataContext().AnyAsync(x => x.Name == model.Name);

            if (!isExist)
            {
                var data = await _repository.AppRoles.AddAsync(model);
                id = data.Id;
            }

            return id;
        }

        public async Task<Guid?> PutAsync(AppRole model)
        {
            Guid? id = null;

            var isExist = await _repository.AppRoles.DataContext().AnyAsync(x => x.Name == model.Name && x.Id != model.Id);

            if (!isExist)
            {
                await _repository.AppRoles.UpdateAsync(model);
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
                var isAppUserRole = await _repository.Contexts.AppMenuRole.AnyAsync(x => x.FkAppRoleId == id);
                if (!isAppUserRole)
                {
                    await _repository.AppRoles.DeleteAsync(data);
                    result = true;
                }
            }

            return result;
        }

        public async Task<IReadOnlyList<DxDropDownBoxBinding>> BindingDxDropDownBoxAsync()
        {
            var data = await _repository.AppRoles.GetAll()
                .AsNoTracking()
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
