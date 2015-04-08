namespace Dbcontext3Ways.DAL.Repo.Ambient.UnitOfWork
{
    using System;
    using System.Collections.Generic;
    using System.Data.Entity;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    class AmbientDbContextLocator : IAmbientDbContextLocator
    {
        public TDbContext Get<TDbContext>() where TDbContext : DbContext, new()
        {
            var ambientDbContext = UnitOfWorkScope<TDbContext>.DbContext;
            return ambientDbContext == null ? null : ambientDbContext;
        }
    }
}
