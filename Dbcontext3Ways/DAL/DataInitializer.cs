namespace Dbcontext3Ways.DAL
{
    using System;
    using System.Collections.Generic;
    using System.Data.Entity;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Dbcontext3Ways.EFModel;

    class DataInitializer : DropCreateDatabaseAlways<CoreDbContext>
    {
        protected override void Seed(CoreDbContext context)
        {
            var compSys = new Company { Name = "MIGO", DisplayName = "CRM領導者 -> MIGO功典資訊" };
            context.Companies.Add(compSys);

            var userSys = new User { Username = "admin", Password = "Admin1234", Email = "admin@migo.com", Name = "admin" };
            context.Users.Add(userSys);

            compSys.User.Add(userSys);

            context.SaveChanges();
        }
    }
}
