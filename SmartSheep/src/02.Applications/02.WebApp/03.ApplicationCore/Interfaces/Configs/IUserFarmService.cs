using DevExtreme.AspNet.Data.ResponseModel;
using Project.Base.Models.DevExpress.DxGrids;
using Project.Base.Models.RestApis;
using Web.Domain.Models.Configs;

namespace Web.ApplicationCore.Interfaces.Configs
{
    public interface IUserFarmService
    {
        Task<LoadResult> DxGridAsync(DataSourceLoadOptions param);
        Task<RestApiResult<UserFarmOptions>> GetOptionsAsync();
        Task<RestApiResult<UserFarm>> GetAsync(Guid id);
        Task<RestApiResult<UserFarm>> PostAsync(UserFarm model);
        Task<RestApiResult<UserFarm>> PutAsync(UserFarm model);
        Task<RestApiResult<UserFarm>> DeleteAsync(Guid id);
    }
}
