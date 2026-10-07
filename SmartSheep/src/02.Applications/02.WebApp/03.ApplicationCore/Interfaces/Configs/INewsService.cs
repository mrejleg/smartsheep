using DevExtreme.AspNet.Data.ResponseModel;
using Project.Base.Models.DevExpress.DxGrids;
using Project.Base.Models.RestApis;
using Web.Domain.Models.Configs;
using Microsoft.AspNetCore.Http;

namespace Web.ApplicationCore.Interfaces.Configs
{
    public interface INewsService
    {
        Task<LoadResult> DxGridAsync(DataSourceLoadOptions param);
        Task<RestApiResult<List<News>>> GetAllAsync();
        Task<RestApiResult<News>> GetAsync(Guid id);
        Task<RestApiResult<News>> PostAsync(News model);
        Task<RestApiResult<News>> PutAsync(News model);
        Task<RestApiResult<News>> DeleteAsync(Guid id);
        Task<RestApiResult<NewsImageUploadResult>> UploadImageAsync(IFormFile file);
    }
}
