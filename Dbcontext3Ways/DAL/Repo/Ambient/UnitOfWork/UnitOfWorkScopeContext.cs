namespace Dbcontext3Ways.DAL.Repo.Ambient.UnitOfWork
{
    using System;
    using System.Collections.Generic;
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Handle class for holding the real DbContext and some state for it.
    /// </summary>
    /// <typeparam name="TDbContext">DbContext class</typeparam>
    class UnitOfWorkScopeContext<TDbContext> : IDisposable where TDbContext : DbContext, new()
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UnitOfWorkScopeContext{TDbContext}" /> class.
        /// </summary>
        /// <param name="forWriting">Is the root context opened for writing?</param>
        public UnitOfWorkScopeContext(bool forWriting)
        {
            this.ForWriting = forWriting;
            this.DbContext = new TDbContext();
            ((IObjectContextAdapter)this.DbContext).ObjectContext.SavingChanges += this.GuardAgainstDirectSaves;
        }

        /// <summary>
        /// Gets a value indicating whether The real DbContext.
        /// </summary>
        public TDbContext DbContext { get; private set; }

        /// <summary>
        /// Gets a value indicating whether Was any unit of work scope using this DbContext opened for writing?
        /// </summary>
        public bool ForWriting { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether Has there been a failure that should block saving?
        /// </summary>
        public bool BlockSave { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether Switch off guard for direct calls to SaveChanges.
        /// </summary>
        public bool AllowSaving { get; set; }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.DbContext != null)
                {
                    ((IObjectContextAdapter)this.DbContext).ObjectContext.SavingChanges -= this.GuardAgainstDirectSaves;

                    this.DbContext.Dispose();
                    this.DbContext = null;
                }
            }
        }

        private void GuardAgainstDirectSaves(object sender, EventArgs e)
        {
            if (!this.AllowSaving)
            {
                throw new InvalidOperationException(
                    "Don't call SaveChanges directly on a context owned by a UnitOfWorkScope. " +
                    "use UnitOfWorkScope.SaveChanges instead.");
            }
        }
    }
}
