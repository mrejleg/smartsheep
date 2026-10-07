using DevExtreme.AspNet.Data.ResponseModel;
using Project.Base.Models.DevExpress.DxGrids;
using Project.Base.Models.RestApis;
using Web.Domain.Models.MasterDatas;

namespace Web.ApplicationCore.Interfaces.MasterDatas
{
    public interface IBarnService
    {
        Task<LoadResult> DxGridAsync(DataSourceLoadOptions param);
        Task<RestApiResult<List<Barn>>> GetAllAsync();
        Task<RestApiResult<List<Barn>>> GetAllByFarmAsync(Guid fkFarmId);
        Task<RestApiResult<List<Barn>>> GetAllByLivestockAsync(Guid fkLivestockId);
        Task<RestApiResult<Barn>> GetAsync(Guid id);
        Task<RestApiResult<Barn>> PostAsync(Barn model);
        Task<RestApiResult<Barn>> PutAsync(Barn model);
        Task<RestApiResult<Barn>> DeleteAsync(Guid id);
    }
}
