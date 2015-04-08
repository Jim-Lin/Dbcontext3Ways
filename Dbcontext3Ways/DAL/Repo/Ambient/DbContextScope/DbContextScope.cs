namespace Dbcontext3Ways.DAL.Repo.Ambient.DbContextScope
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Runtime.Remoting.Messaging;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Purpose of a UnitOfWorkScope.
    /// </summary>
    public enum DbContextScopePurpose
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
    class DbContextScope : IDisposable
    {
        private static readonly string AmbientDbContextScopeKey = "AmbientDbcontext_" + Guid.NewGuid();

        private static readonly ConditionalWeakTable<InstanceIdentifier, DbContextScope> DbContextScopeInstances = new ConditionalWeakTable<InstanceIdentifier, DbContextScope>();

        [ThreadStatic]
        private static DbContextCollection scopedDbContexts;

        private InstanceIdentifier instanceIdentifier = new InstanceIdentifier();

        private bool isRoot = false;

        private bool saveChangesCalled = false;

        private DbContextScopePurpose purpose;

        /// <summary>
        /// Initializes a new instance of the <see cref="DbContextScope" /> class.
        /// </summary>
        /// <param name="purpose">Will this dbcontext scope be used for reading or writing?</param>
        public DbContextScope(DbContextScopePurpose purpose)
        {
            this.purpose = purpose;

            if (scopedDbContexts == null)
            {
                scopedDbContexts = new DbContextCollection(purpose == DbContextScopePurpose.Writing);
                this.SetAmbientScope(this);
                this.isRoot = true;
            }

            if (purpose == DbContextScopePurpose.Writing && !scopedDbContexts.ForWriting)
            {
                throw new InvalidOperationException(
                    "Can't open a child UnitOfWorkScope for writing when the root scope " +
                    "is opened for reading.");
            }
        }

        /// <summary>
        /// Gets Access the ambient DbContext that this unit of work uses.
        /// </summary>
        public DbContextCollection DbContext
        {
            get
            {
                return scopedDbContexts;
            }
        }

        public static DbContextScope GetAmbientScope()
        {
            // Retrieve the identifier of the ambient scope (if any)
            var instanceIdentifier = CallContext.LogicalGetData(AmbientDbContextScopeKey) as InstanceIdentifier;
            if (instanceIdentifier == null)
            {
                return null; // Either no ambient context has been set or we've crossed an app domain boundary and have (intentionally) lost the ambient context
            }

            // Retrieve the DbContextScope instance corresponding to this identifier
            DbContextScope ambientScope;
            if (DbContextScopeInstances.TryGetValue(instanceIdentifier, out ambientScope))
            {
                return ambientScope;
            }

            // We have an instance identifier in the CallContext but no corresponding instance
            // in our DbContextScopeInstances table. This should never happen! The only place where
            // we remove the instance from the DbContextScopeInstances table is in RemoveAmbientScope(),
            // which also removes the instance identifier from the CallContext. 
            //
            // There's only one scenario where this could happen: someone let go of a DbContextScope 
            // instance without disposing it. In that case, the CallContext
            // would still contain a reference to the scope and we'd still have that scope's instance
            // in our DbContextScopeInstances table. But since we use a ConditionalWeakTable to store 
            // our DbContextScope instances and are therefore only holding a weak reference to these instances, 
            // the GC would be able to collect it. Once collected by the GC, our ConditionalWeakTable will return
            // null when queried for that instance. In that case, we're OK. This is a programming error 
            // but our use of a ConditionalWeakTable prevented a leak.
            System.Diagnostics.Debug.WriteLine("Programming error detected. Found a reference to an ambient DbContextScope in the CallContext but didn't have an instance for it in our DbContextScopeInstances table. This most likely means that this DbContextScope instance wasn't disposed of properly. DbContextScope instance must always be disposed. Review the code for any DbContextScope instance used outside of a 'using' block and fix it so that all DbContextScope instances are disposed of.");
            return null;
        }

        /// <summary>
        /// For child unit of work scopes: Mark for saving. For the root: Do actually save.
        /// </summary>
        public void SaveChanges()
        {
            if (this.purpose != DbContextScopePurpose.Writing)
            {
                throw new InvalidOperationException(
                    "Can't save changes on a UnitOfWorkScope with Reading purpose.");
            }

            if (scopedDbContexts.BlockSave)
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

            scopedDbContexts.AllowSaving = true;
            scopedDbContexts.SaveChanges();
            scopedDbContexts.AllowSaving = false;
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
                if (this.purpose == DbContextScopePurpose.Writing && !this.saveChangesCalled)
                {
                    scopedDbContexts.BlockSave = true;

                    // Don't throw here - it would mask original exception when exiting
                    // a using block.
                }

                if (scopedDbContexts != null && this.isRoot)
                {
                    scopedDbContexts.Dispose();
                    scopedDbContexts = null;
                }
            }
        }

        private void SetAmbientScope(DbContextScope newAmbientScope)
        {
            if (newAmbientScope == null)
            {
                throw new ArgumentNullException("newAmbientScope");
            }

            var current = CallContext.LogicalGetData(AmbientDbContextScopeKey) as InstanceIdentifier;

            if (current == newAmbientScope.instanceIdentifier)
            {
                return;
            }

            // Store the new scope's instance identifier in the CallContext, making it the ambient scope
            CallContext.LogicalSetData(AmbientDbContextScopeKey, newAmbientScope.instanceIdentifier);

            // Keep track of this instance (or do nothing if we're already tracking it)
            DbContextScopeInstances.GetValue(newAmbientScope.instanceIdentifier, key => newAmbientScope);
        }

        internal class InstanceIdentifier : MarshalByRefObject
        {
        }
    }
}
