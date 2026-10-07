using Microsoft.AspNetCore.Http;
using Web.ApplicationCore.Interfaces.MasterDatas;
using Web.ApplicationCore.Interfaces.Repositories;
using Web.ApplicationCore.Services.MasterDatas;
using Web.Domain.Models;

namespace Web.ApplicationCore.Services.Repositories
{
    public class MasterDataRepositories : IMasterDataRepositories
    {
        public AppSetting AppSettings { get; private set; }
        public IHttpContextAccessor HttpContextAccessors { get; private set; }
        public IFarmService Farms { get; private set; }
        public IBarnService Barns { get; private set; }
        public ILivestockService Livestocks { get; private set; }
        public ISourceVideoService SourceVideos { get; private set; }

        private readonly AppSetting _appSetting;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public MasterDataRepositories(AppSetting appSetting, IHttpContextAccessor httpContextAccessor)
        {
            _appSetting = appSetting;
            _httpContextAccessor = httpContextAccessor;
            AppSettings = _appSetting;
            HttpContextAccessors = new HttpContextAccessor();
            Farms = new FarmService(_appSetting);
            Barns = new BarnService(_appSetting);
            Livestocks = new LivestockService(_appSetting);
            SourceVideos = new SourceVideoService(_appSetting);
        }
    }
}
