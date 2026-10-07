using Api.ApplicationCore.Interfaces.Configs;
using Api.ApplicationCore.Interfaces.Repositories;
using Api.Domain.Entities.Configs;
using Api.Domain.Models;
using DevExtreme.AspNet.Data.ResponseModel;
using Microsoft.EntityFrameworkCore;
using Project.Base.Models;
using Project.Base.Models.DevExpress.DxGrids;
using Project.Base.Models.JQueries.Datatables;
using Project.Core.DevExpress;
using Project.Core.Interfaces.Repositories;

namespace Api.ApplicationCore.Services.Configs
{
    public class AppMenuRoleService : IAppMenuRoleService
    {
        private readonly IConfigRepositories _repository;
        private readonly IDapperRepo _dapperRepository;
        private readonly AppSetting _appSetting;

        public AppMenuRoleService(IConfigRepositories repository, AppSetting appSetting, IDapperRepo dapperRepository)
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

        public IQueryable<AppMenuRole> GetAll()
        {
            var data = _repository.AppMenuRoles.GetAll().AsNoTracking().Include(x => x.AppMenu).Include(x => x.AppRole);
            return data;
        }

        public IQueryable<AppMenuRole> GetAll(Guid fkAppRoleId)
        {
            return GetAllByAppRole(fkAppRoleId);
        }

        public IQueryable<AppMenuRole> GetAllByAppRole(Guid fkAppRoleId)
        {
            return GetAll().Where(x => x.FkAppRoleId == fkAppRoleId);
        }

        public IQueryable<AppMenuRole> GetAllByAppMenu(Guid fkAppMenuId)
        {
            return GetAll().Where(x => x.FkAppMenuId == fkAppMenuId);
        }

        public async Task<DataPagedResults<AppMenuRole>> GetAsync(DataParameter param)
        {
            var data = GetAll();
            return await _repository.AppMenuRoles.GetByPagingAsync(data, param.Start, param.Length);
        }

        public async Task<AppMenuRole> GetAsync(Guid id)
        {
            return await GetAll().FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<Guid?> PostAsync(AppMenuRole model)
        {
            Guid? id = null;

            var isExist = await _repository.AppMenuRoles.DataContext().AnyAsync(x => x.FkAppMenuId == model.FkAppMenuId && x.FkAppRoleId == model.FkAppRoleId);

            if (!isExist)
            {
                var data = await _repository.AppMenuRoles.AddAsync(model);
                id = data.Id;
            }

            return id;
        }

        public async Task<Guid?> PutAsync(AppMenuRole model)
        {
            Guid? id = null;

            var isExist = await _repository.AppMenuRoles.DataContext().AnyAsync(x => x.FkAppRoleId == model.FkAppRoleId && x.FkAppMenuId == model.FkAppMenuId && x.Id != model.Id);

            if (!isExist)
            {
                await _repository.AppMenuRoles.UpdateAsync(model);
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
                await _repository.AppMenuRoles.DeleteAsync(data);
                result = true;
            }

            return result;
        }

        public async Task<int?> ReplaceAsync(Guid fkAppRoleId, IReadOnlyCollection<AppMenuRole> models)
        {
            var roleExists = await _repository.AppRoles.DataContext()
                .AsNoTracking()
                .AnyAsync(x => x.Id == fkAppRoleId);
            if (!roleExists)
            {
                return null;
            }

            var requested = models
                .Where(x => x.FkAppMenuId.HasValue && !string.IsNullOrWhiteSpace(x.AccessTypes))
                .GroupBy(x => x.FkAppMenuId!.Value)
                .Select(group => new AppMenuRole
                {
                    Id = Guid.NewGuid(),
                    FkAppRoleId = fkAppRoleId,
                    FkAppMenuId = group.Key,
                    AccessTypes = string.Join(",", group
                        .SelectMany(x => x.AccessTypes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                        .Distinct(StringComparer.OrdinalIgnoreCase)),
                    IsActive = true
                })
                .ToList();

            var requestedMenuIds = requested.Select(x => x.FkAppMenuId!.Value).ToHashSet();
            if (requestedMenuIds.Count > 0)
            {
                var validMenuIds = await _repository.AppMenus.DataContext()
                    .AsNoTracking()
                    .Where(x => requestedMenuIds.Contains(x.Id))
                    .Select(x => x.Id)
                    .ToListAsync();

                if (validMenuIds.Count != requestedMenuIds.Count)
                {
                    return null;
                }
            }

            var executionStrategy = _repository.Contexts.Database.CreateExecutionStrategy();
            return await executionStrategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _repository.BeginTransactionAsync();
                try
                {
                    await _repository.AppMenuRoles.DataContext()
                        .Where(x => x.FkAppRoleId == fkAppRoleId)
                        .ExecuteDeleteAsync();

                    if (requested.Count > 0)
                    {
                        await _repository.Contexts.AppMenuRole.AddRangeAsync(requested);
                        await _repository.Contexts.SaveChangesAsync();
                    }

                    await transaction.CommitAsync();
                    return requested.Count;
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }
    }
}
