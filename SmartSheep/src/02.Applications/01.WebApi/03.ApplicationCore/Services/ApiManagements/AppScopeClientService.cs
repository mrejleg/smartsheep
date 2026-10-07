using Api.ApplicationCore.Interfaces.ApiManagements;
using Api.ApplicationCore.Interfaces.Repositories;
using Api.Domain.ApiManagements;
using Api.Domain.Models;
using DevExtreme.AspNet.Data.ResponseModel;
using Microsoft.EntityFrameworkCore;
using Api.ApplicationCore.Services.Auths;
using Project.Base.Models;
using Project.Base.Models.DevExpress.DxGrids;
using Project.Base.Models.JQueries.Datatables;
using Project.Core.DevExpress;
using Project.Core.Interfaces.Repositories;

namespace Api.ApplicationCore.Services.ApiManagements
{
    public class AppScopeClientService : IAppScopeClientService
    {
        private readonly IApiManagementRepositories _repository;
        private readonly IDapperRepo _dapperRepository;
        private readonly AppSetting _appSetting;

        public AppScopeClientService(IApiManagementRepositories repository, AppSetting appSetting, IDapperRepo dapperRepository)
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

        public IQueryable<AppScopeClient> GetAll()
        {
            var data = _repository.AppScopeClients.GetAll().AsNoTracking().Include(x => x.AppClient).Include(x => x.ScopeClient);
            return data;
        }

        public IQueryable<AppScopeClient> GetAll(Guid fkAppClientId)
        {
            return GetAllByAppClient(fkAppClientId);
        }

        public IQueryable<AppScopeClient> GetAllByAppClient(Guid fkAppClientId)
        {
            return GetAll().Where(x => x.FkAppClientId == fkAppClientId);
        }

        public IQueryable<AppScopeClient> GetAllByScopeClient(Guid fkScopeClientId)
        {
            return GetAll().Where(x => x.FkScopeClientId == fkScopeClientId);
        }

        public async Task<DataPagedResults<AppScopeClient>> GetAsync(DataParameter param)
        {
            var data = GetAll();
            return await _repository.AppScopeClients.GetByPagingAsync(data, param.Start, param.Length);
        }

        public async Task<AppScopeClient> GetAsync(Guid id)
        {
            return await GetAll().FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<Guid?> PostAsync(AppScopeClient model)
        {
            Guid? id = null;
            var isExist = await _repository.AppScopeClients.DataContext().AnyAsync(x => x.FkAppClientId == model.FkAppClientId && x.FkScopeClientId == model.FkScopeClientId);

            if (!isExist)
            {
                var data = await _repository.AppScopeClients.AddAsync(model);
                id = data.Id;
                InvalidateAuthorizationCache();
            }

            return id;
        }

        public async Task<Guid?> PutAsync(AppScopeClient model)
        {
            Guid? id = null;

            var isExist = await _repository.AppScopeClients.DataContext().AnyAsync(x => x.FkAppClientId == model.FkAppClientId && x.FkScopeClientId == model.FkScopeClientId && x.Id != model.Id);

            if (!isExist)
            {
                await _repository.AppScopeClients.UpdateAsync(model);
                id = model.Id;
                InvalidateAuthorizationCache();
            }

            return id;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            bool result = false;

            var data = await GetAsync(id);

            if (data != null)
            {
                await _repository.AppScopeClients.DeleteAsync(data);
                result = true;
                InvalidateAuthorizationCache();
            }

            return result;
        }

        public async Task<int?> ReplaceAsync(Guid fkAppClientId, IReadOnlyCollection<AppScopeClient> models)
        {
            var clientExists = await _repository.AppClients.DataContext()
                .AsNoTracking()
                .AnyAsync(x => x.Id == fkAppClientId);
            if (!clientExists)
            {
                return null;
            }

            var requestedScopeIds = models
                .Where(x => x.FkScopeClientId.HasValue)
                .Select(x => x.FkScopeClientId!.Value)
                .Distinct()
                .ToHashSet();

            if (requestedScopeIds.Count > 0)
            {
                var validScopeCount = await _repository.ScopeClients.DataContext()
                    .AsNoTracking()
                    .CountAsync(x => requestedScopeIds.Contains(x.Id));
                if (validScopeCount != requestedScopeIds.Count)
                {
                    return null;
                }
            }

            var replacements = requestedScopeIds.Select(scopeId => new AppScopeClient
            {
                Id = Guid.NewGuid(),
                FkAppClientId = fkAppClientId,
                FkScopeClientId = scopeId,
                IsActive = true
            }).ToList();

            var executionStrategy = _repository.Contexts.Database.CreateExecutionStrategy();
            return await executionStrategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _repository.BeginTransactionAsync();
                try
                {
                    await _repository.AppScopeClients.DataContext()
                        .Where(x => x.FkAppClientId == fkAppClientId)
                        .ExecuteDeleteAsync();

                    if (replacements.Count > 0)
                    {
                        await _repository.Contexts.AppScopeClient.AddRangeAsync(replacements);
                        await _repository.Contexts.SaveChangesAsync();
                    }

                    await transaction.CommitAsync();
                    InvalidateAuthorizationCache();
                    return replacements.Count;
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }

        private void InvalidateAuthorizationCache()
        {
            ApiScopeAuthorizationCache.Invalidate();
        }
    }
}
