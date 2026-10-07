using Project.Core.Interfaces.Repositories;
using Project.Core.Interfaces.Repositories.Dappers;
using Project.Core.Repositories.Dappers;

namespace Project.Core.Repositories
{
    public class DapperRepo : IDapperRepo
    {
        public IDapperRepository DapperRepositories { get; private set; }
        public IDapperRepositoryPattern DapperRepositoryPatterns { get; private set; }

        public DapperRepo()
        {
            DapperRepositories = new DapperRepository();
            DapperRepositoryPatterns = new DapperRepositoryPattern();
        }
    }
}
