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
    public class AppMenuService : IAppMenuService
    {
        private readonly IConfigRepositories _repository;
        private readonly IDapperRepo _dapperRepository;
        private readonly AppSetting _appSetting;

        public AppMenuService(IConfigRepositories repository, AppSetting appSetting, IDapperRepo dapperRepository)
        {
            _repository = repository;
            _appSetting = appSetting;

            _dapperRepository = dapperRepository;
            _dapperRepository.DapperRepositories.ConnectionString = _appSetting.ConnectionStringsDefaultConnection;
        }

        public async Task<LoadResult> DxGridAsync(DataSourceLoadOptions param)
        {
            var datas = GetAll();
            return await DxDataGridExtentions.DxDataGridAsync(datas, param);
        }

        public IQueryable<AppMenu> GetAll()
        {
            var data = _repository.AppMenus.GetAll().AsNoTracking().Include(x => x.AppMenuParent);
            return data;
        }

        public IQueryable<AppMenu> GetAllByParent(Guid fkParentId)
        {
            return GetAll().Where(x => x.FkParentId == fkParentId);
        }

        public async Task<DataPagedResults<AppMenu>> GetAsync(DataParameter param)
        {
            var data = GetAll();
            return await _repository.AppMenus.GetByPagingAsync(data, param.Start, param.Length);
        }

        public async Task<AppMenu> GetAsync(Guid id)
        {
            return await GetAll().FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<Guid?> PostAsync(AppMenu model)
        {
            Guid? id = null;
            var isExist = await _repository.AppMenus.DataContext().AnyAsync(x => x.Name == model.Name);

            if (!isExist)
            {
                var data = await _repository.AppMenus.AddAsync(model);
                id = data.Id;
            }

            return id;
        }

        public async Task<Guid?> PutAsync(AppMenu model)
        {
            Guid? id = null;
            var isExist = await _repository.AppMenus.DataContext().AnyAsync(x => x.Name == model.Name && x.Id != model.Id);

            if (!isExist)
            {
                await _repository.AppMenus.UpdateAsync(model);
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
                var AppMenuRoles = _repository.AppMenuRoles.GetAll().Where(x => x.FkAppMenuId == id).ToList();
                if (AppMenuRoles.Count == 0)
                {
                    var parents = GetAll().Where(x => x.FkParentId == id).ToList();
                    if (parents.Count == 0)
                    {
                        await _repository.AppMenus.DeleteAsync(data);

                        result = true;
                    }
                }
            }

            return result;
        }

        public async Task<IReadOnlyList<DxDropDownBoxBinding>> BindingDxDropDownBoxParentAsync()
        {
            var data = await _repository.AppMenus.GetAll()
                .AsNoTracking()
                .Where(x => x.IsActive == true && x.FkParentId == null && x.IsSection == false)
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
