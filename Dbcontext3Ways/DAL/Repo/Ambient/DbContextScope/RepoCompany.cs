namespace Dbcontext3Ways.DAL.Repo.Ambient.DbContextScope
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Dbcontext3Ways.EFModel;

    class RepoCompany : GenericRepository<Company>, IRepoCompany
    {
        public RepoCompany(IAmbientDbContextLocator ambientDbContextLocator)
            : base(ambientDbContextLocator)
        {
        }
    }
}
