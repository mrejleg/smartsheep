using Api.Domain.Entities.MasterDatas;
using Api.Infrastructure.Data.DbContexts;
using Project.Core.Interfaces.Repositories;

namespace Api.ApplicationCore.Interfaces.Repositories
{
    public interface IMasterDataRepositories
    {
        ApplicationDbContext Contexts { get; }
        IRepositoryPattern<Farm, Guid> Farms { get; }
        IRepositoryPattern<Barn, Guid> Barns { get; }
        IRepositoryPattern<Livestock, Guid> Livestocks { get; }
        IRepositoryPattern<SourceVideo, Guid> SourceVideos { get; }
    }
}
