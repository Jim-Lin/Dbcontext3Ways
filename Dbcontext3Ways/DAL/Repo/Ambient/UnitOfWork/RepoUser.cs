namespace Dbcontext3Ways.DAL.Repo.Ambient.UnitOfWork
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Dbcontext3Ways.EFModel;

    class RepoUser : GenericRepository<User>, IRepoUser
    {
        public RepoUser(IAmbientDbContextLocator ambientDbContextLocator)
            : base(ambientDbContextLocator)
        {
        }
    }
}
