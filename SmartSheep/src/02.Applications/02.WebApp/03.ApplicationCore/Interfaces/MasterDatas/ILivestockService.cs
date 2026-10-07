using DevExtreme.AspNet.Data.ResponseModel;
using Project.Base.Models.DevExpress.DxGrids;
using Project.Base.Models.RestApis;
using Web.Domain.Models.MasterDatas;

namespace Web.ApplicationCore.Interfaces.MasterDatas
{
    public interface ILivestockService
    {
        Task<LoadResult> DxGridAsync(DataSourceLoadOptions param);
        Task<RestApiResult<List<Livestock>>> GetAllAsync();
        Task<RestApiResult<Livestock>> GetAsync(Guid id);
        Task<RestApiResult<Livestock>> PostAsync(Livestock model);
        Task<RestApiResult<Livestock>> PutAsync(Livestock model);
        Task<RestApiResult<Livestock>> DeleteAsync(Guid id);
    }
}
