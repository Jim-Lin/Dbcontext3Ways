namespace Dbcontext3Ways.DAL
{
    using System;
    using System.Collections.Generic;
    using System.Data.Entity;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Dbcontext3Ways.EFModel;

    class CoreDbContext : DbContext
    {
        public CoreDbContext()
            : base("core-db-context")
        {
            Database.SetInitializer<CoreDbContext>(new DataInitializer());
        }

        public DbSet<User> Users { get; set; }

        public DbSet<Company> Companies { get; set; }
    }
}
