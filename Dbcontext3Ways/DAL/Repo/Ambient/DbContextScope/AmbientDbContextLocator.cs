namespace Dbcontext3Ways.DAL.Repo.Ambient.DbContextScope
{
    using System;
    using System.Collections.Generic;
    using System.Data.Entity;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    class AmbientDbContextLocator : IAmbientDbContextLocator
    {
        public TDbContext Get<TDbContext>() where TDbContext : DbContext
        {
            var ambientScope = DbContextScope.GetAmbientScope();
            return ambientScope == null ? null : ambientScope.DbContext.Get<TDbContext>();
        }
    }
}
