using Project.Core.Interfaces.Repositories.Dappers;

namespace Project.Core.Interfaces.Repositories
{
    public interface IDapperRepo
    {
        IDapperRepository DapperRepositories { get; }
        IDapperRepositoryPattern DapperRepositoryPatterns { get; }
    }
}
