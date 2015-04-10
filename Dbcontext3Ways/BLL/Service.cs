namespace Dbcontext3Ways.BLL
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Dbcontext3Ways.DAL;
    using Dbcontext3Ways.DAL.Repo;
    using Dbcontext3Ways.DAL.Repo.Ambient.DbContextScope;
    using Dbcontext3Ways.DAL.Repo.Ambient.UnitOfWork;
    using Dbcontext3Ways.EFModel;

    class Service : IService
    {
        private IRepoCompany repoComp;
        private IRepoUser repoUser;

        public Service()
        { }

        public Service(IRepoCompany repoComp, IRepoUser repoUser)
        {
            this.repoComp = repoComp;
            this.repoUser = repoUser;
        }

        public void AddUsingExplicitDbContext(Company comp, User user)
        {
            CoreDbContext context = new CoreDbContext();
            IRepoCompany repoComp = new Dbcontext3Ways.DAL.Repo.Explicit.RepoCompany(context);
            IRepoUser repoUser = new Dbcontext3Ways.DAL.Repo.Explicit.RepoUser(context);

            repoComp.Insert(comp);
            user.Company = comp;
            repoUser.Insert(user);

            context.SaveChanges();
        }

        public void AddUsingInjectedDbContext(Company comp, User user)
        {
            this.repoComp.Insert(comp);
            user.Company = comp;
            this.repoUser.Insert(user);
        }


        public void AddUsingAmbientDbContextUoW(Company comp, User user)
        {
            using (var scope = new UnitOfWorkScope<CoreDbContext>(UnitOfWorkScopePurpose.Writing))
            {
                this.repoComp.Insert(comp);
                user.Company = comp;
                this.repoUser.Insert(user);

                scope.SaveChanges();
            }
        }

        public void AddUsingAmbientDbContextScope(Company comp, User user)
        {
            using (var scope = new DbContextScope(DbContextScopePurpose.Writing))
            {
                this.repoComp.Insert(comp);
                user.Company = comp;
                this.repoUser.Insert(user);

                scope.SaveChanges();
            }
        }
    }
}
