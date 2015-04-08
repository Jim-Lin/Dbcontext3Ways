namespace Dbcontext3Ways
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Dbcontext3Ways.BLL;
    using Dbcontext3Ways.DAL;
    using Dbcontext3Ways.DAL.Repo;
    using Dbcontext3Ways.EFModel;
    using TinyIoC;

    class Program
    {
        static void Main(string[] args)
        {
            var compKg = new Company { Name = "kgmarket", DisplayName = "Kuan-Guang Supermarket" };
            var userKg = new User { Username = "kgmarket", Password = "Kgmarket123", Email = "admin@kgmarket.com", Name = "kgmarket" };

            Console.WriteLine("managing DbContext way with Entity Framework 6:");
            Console.WriteLine("1 => Explicit");
            Console.WriteLine("2 => Injected");
            Console.WriteLine("3 => Ambient UoW");
            Console.WriteLine("4 => Ambient DbContextScope");
            Console.WriteLine("others => exit");
            string way = Console.ReadLine();

            switch (way)
            {
                case "1":
                    // Explicit
                    compKg.Name += "Explicit";
                    userKg.Username += "Explicit";
                    Explicit(compKg, userKg);
                    break;
                case "2":
                    // Injected
                    compKg.Name += "Injected";
                    userKg.Username += "Injected";
                    Injected(compKg, userKg);
                    break;
                case "3":
                    // Ambient UoW
                    compKg.Name += "AmbientUoW";
                    userKg.Username += "AmbientUoW";
                    AmbientUoW(compKg, userKg);
                    break;
                case "4":
                    // Ambient DbContextScope
                    compKg.Name += "AmbientDbContextScope";
                    userKg.Username += "AmbientDbContextScope";
                    AmbientDbContextScope(compKg, userKg);
                    break;
                default:
                    break;
            }

            Console.WriteLine("press any key");
            Console.ReadKey();
        }

        static void Explicit(dynamic compKg, dynamic userKg)
        {
            /*
             * Good:
             * 直接取用DbContext
             * 很直覺又明確
             * There's a clear and obvious place where the context is created.
             * 
             * Bad:
             * 1.很多層時
             * 必須一直傳遞DbContext參數
             * these methods will only require to be provided with a DbContext parameter so that they can pass it down the line until it eventually reaches whatever method actually uses it.
             * 2.多個DbContext時
             * 必須傳遞多個DbContext參數
             * It can get quite ugly. Particularly if your application uses multiple DbContext, resulting in service methods potentially requiring two or more mandatory DbContext parameters.
             */
            IService serviceExplicit = new Service();
            serviceExplicit.AddUsingExplicitDbContext(compKg, userKg);
        }

        static void Injected(dynamic compKg, dynamic userKg)
        {
            /*
             * Good:
             * 封裝特性不直接取用DbContext
             * there is no DbContext to be seen anywhere in the service code. The service is completely oblivious of Entity Framework.
             * 
             * Bad:
             * DI掌握度不好會發生無法預期的結果
             * A lot of magic
             * Where do these DbContext instances come from? How and where is the business transaction boundary defined? If a service depends on two different repositories, will they both have access to the same DbContext instance or will they each have their own instance?
             */
            var ctx = TinyIoCContainer.Current;
            ctx.Register<CoreDbContext>().AsSingleton(); // AsMultiInstance()
            ctx.Register<IRepoCompany, Dbcontext3Ways.DAL.Repo.Injected.RepoCompany>();
            ctx.Register<IRepoUser, Dbcontext3Ways.DAL.Repo.Injected.RepoUser>();
            ctx.Register<IService, Service>();
            IService serviceInjected = ctx.Resolve<IService>();
            serviceInjected.AddUsingInjectedDbContext(compKg, userKg);
        }

        static void AmbientUoW(dynamic compKg, dynamic userKg)
        {
            /*
             * Good:
             * BLL DAL切割
             * Your service and repository methods are now free of DbContext parameters, making your interfaces cleaner and your method contracts clearer as they can now only request the parameters that they actually need to do their job.
             * 
             * Bad:
             * 1.多一層建構及傳遞DbContext的架構不易懂
             * a certain amount of magic which can certainly make the code more difficult to understand and maintain. When looking at the data access code, it's not necessarily easy to figure out where the ambient DbContext is coming from. You just have to hope that someone somehow registered it before calling the data access code.
             * 2.連線多個資料庫或複雜多層架構時
             * 無法確定是哪個DbContext被創建及使用
             * if it connects to multiple databases or if you have split your domain model into separate model groups, it can be difficult for the top-level service method to know which DbContext object(s) it must create and register.
             */
            Dbcontext3Ways.DAL.Repo.Ambient.UnitOfWork.IAmbientDbContextLocator abtUoWDbLocator = new Dbcontext3Ways.DAL.Repo.Ambient.UnitOfWork.AmbientDbContextLocator();
            IRepoCompany repoUoWComp = new Dbcontext3Ways.DAL.Repo.Ambient.UnitOfWork.RepoCompany(abtUoWDbLocator);
            IRepoUser repoUoWUser = new Dbcontext3Ways.DAL.Repo.Ambient.UnitOfWork.RepoUser(abtUoWDbLocator);
            IService serviceAmbientUow = new Service(repoUoWComp, repoUoWUser);
            serviceAmbientUow.AddUsingAmbientDbContextUoW(compKg, userKg);
        }

        static void AmbientDbContextScope(dynamic compKg, dynamic userKg)
        {
            /*
             * as above
             */
            Dbcontext3Ways.DAL.Repo.Ambient.DbContextScope.IAmbientDbContextLocator abtScopeDbLocator = new Dbcontext3Ways.DAL.Repo.Ambient.DbContextScope.AmbientDbContextLocator();
            IRepoCompany repoScopeComp = new Dbcontext3Ways.DAL.Repo.Ambient.DbContextScope.RepoCompany(abtScopeDbLocator);
            IRepoUser repoScopeUser = new Dbcontext3Ways.DAL.Repo.Ambient.DbContextScope.RepoUser(abtScopeDbLocator);
            IService serviceAmbientScope = new Service(repoScopeComp, repoScopeUser);
            serviceAmbientScope.AddUsingAmbientDbContextScope(compKg, userKg);
        }
    }
}
