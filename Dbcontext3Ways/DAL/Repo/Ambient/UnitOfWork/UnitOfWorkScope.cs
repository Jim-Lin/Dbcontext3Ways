namespace Dbcontext3Ways.DAL.Repo.Ambient.UnitOfWork
{
    using System;
    using System.Collections.Generic;
    using System.Data.Entity;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Purpose of a UnitOfWorkScope.
    /// </summary>
    public enum UnitOfWorkScopePurpose
    {
        /// <summary>
        /// This unit of work scope will only be used for reading.
        /// </summary>
        Reading,

        /// <summary>
        /// This unit of work scope will be used for writing. If SaveChanges
        /// isn't called, it cancels the entire unit of work.
        /// </summary>
        Writing
    }

    /// <summary>
    /// Scoped unit of work, that merges with any existing scoped unit of work
    /// activated by a previous function in the call chain.
    /// </summary>
    /// <typeparam name="TDbContext">The type of the DbContext</typeparam>
    class UnitOfWorkScope<TDbContext> : IDisposable
        where TDbContext : DbContext, new()
    {
        [ThreadStatic]
        private static UnitOfWorkScopeContext<TDbContext> scopedDbContext;

        private bool isRoot = false;

        private bool saveChangesCalled = false;

        private UnitOfWorkScopePurpose purpose;

        /// <summary>
        /// Initializes a new instance of the <see cref="UnitOfWorkScope{TDbContext}" /> class.
        /// </summary>
        /// <param name="purpose">Will this unit of work scope be used for reading or writing?</param>
        public UnitOfWorkScope(UnitOfWorkScopePurpose purpose)
        {
            this.purpose = purpose;

            if (scopedDbContext == null)
            {
                scopedDbContext = new UnitOfWorkScopeContext<TDbContext>(purpose == UnitOfWorkScopePurpose.Writing);
                this.isRoot = true;
            }

            if (purpose == UnitOfWorkScopePurpose.Writing && !scopedDbContext.ForWriting)
            {
                throw new InvalidOperationException(
                    "Can't open a child UnitOfWorkScope for writing when the root scope " +
                    "is opened for reading.");
            }
        }

        /// <summary>
        /// Gets Access the ambient DbContext that this unit of work uses.
        /// </summary>
        public static TDbContext DbContext
        {
            get
            {
                return (scopedDbContext == null) ? null : scopedDbContext.DbContext;
            }
        }

        /// <summary>
        /// For child unit of work scopes: Mark for saving. For the root: Do actually save.
        /// </summary>
        public void SaveChanges()
        {
            if (this.purpose != UnitOfWorkScopePurpose.Writing)
            {
                throw new InvalidOperationException(
                    "Can't save changes on a UnitOfWorkScope with Reading purpose.");
            }

            if (scopedDbContext.BlockSave)
            {
                throw new InvalidOperationException(
                    "Saving of changes is blocked for this unit of work scope. An enclosed " +
                    "scope was disposed without calling SaveChanges.");
            }

            this.saveChangesCalled = true;

            if (!this.isRoot)
            {
                return;
            }

            scopedDbContext.AllowSaving = true;
            scopedDbContext.DbContext.SaveChanges();
            scopedDbContext.AllowSaving = false;
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose implementation, checking post conditions for purpose and saving.
        /// </summary>
        /// <param name="disposing">Are we disposing?</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // We're disposing and SaveChanges wasn't called. That usually
                // means we're exiting the scope with an exception. Block saves
                // of the entire unit of work.
                if (this.purpose == UnitOfWorkScopePurpose.Writing && !this.saveChangesCalled)
                {
                    scopedDbContext.BlockSave = true;

                    // Don't throw here - it would mask original exception when exiting
                    // a using block.
                }

                if (scopedDbContext != null && this.isRoot)
                {
                    scopedDbContext.Dispose();
                    scopedDbContext = null;
                }
            }
        }
    }
}
