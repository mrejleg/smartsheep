using Microsoft.AspNetCore.Http;
using Web.ApplicationCore.Interfaces.MasterDatas;
using Web.Domain.Models;

namespace Web.ApplicationCore.Interfaces.Repositories
{
    public interface IMasterDataRepositories
    {
        AppSetting AppSettings { get; }
        IHttpContextAccessor HttpContextAccessors { get; }
        IFarmService Farms { get; }
        IBarnService Barns { get; }
        ILivestockService Livestocks { get; }
        ISourceVideoService SourceVideos { get; }
    }
}
