using Api.ApplicationCore.Interfaces.Repositories;
using Api.Domain.Entities.MasterDatas;
using Api.Infrastructure.Data.DbContexts;
using Project.Core.Interfaces.Repositories;
using Project.Core.Repositories;

namespace Api.ApplicationCore.Services.Repositories
{
    public class MasterDataRepositories : IMasterDataRepositories
    {
        public ApplicationDbContext Contexts { get; private set; }
        public IRepositoryPattern<Farm, Guid> Farms { get; private set; }
        public IRepositoryPattern<Barn, Guid> Barns { get; private set; }
        public IRepositoryPattern<Livestock, Guid> Livestocks { get; private set; }
        public IRepositoryPattern<SourceVideo, Guid> SourceVideos { get; private set; }

        public MasterDataRepositories(ApplicationDbContext applicationDbContext)
        {
            Contexts = applicationDbContext;
            Farms = new RepositoryPattern<Farm, Guid>(applicationDbContext);
            Barns = new RepositoryPattern<Barn, Guid>(applicationDbContext);
            Livestocks = new RepositoryPattern<Livestock, Guid>(applicationDbContext);
            SourceVideos = new RepositoryPattern<SourceVideo, Guid>(applicationDbContext);
        }
    }
}
