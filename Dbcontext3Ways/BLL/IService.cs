namespace Dbcontext3Ways.BLL
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Dbcontext3Ways.EFModel;

    interface IService
    {
        void AddUsingExplicitDbContext(Company comp, User user);

        void AddUsingInjectedDbContext(Company comp, User user);

        void AddUsingAmbientDbContextUoW(Company comp, User user);

        void AddUsingAmbientDbContextScope(Company comp, User user);
    }
}
