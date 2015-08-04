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
    /// Purpose of a DbContextScopePurpose.
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
    public class DbContextScope : IDisposable
    {
        private static readonly string AmbientDbContextScopeKey = "AmbientDbcontext_" + Guid.NewGuid();

        private static readonly ConditionalWeakTable<InstanceIdentifier, DbContextScope> DbContextScopeInstances = new ConditionalWeakTable<InstanceIdentifier, DbContextScope>();

        // [ThreadStatic]
        private DbContextCollection scopedDbContexts;

        private InstanceIdentifier instanceIdentifier = new InstanceIdentifier();

        private bool isRoot = false;

        private bool saveChangesCalled = false;

        private DbContextScopePurpose purpose;

        private DbContextScope parentScope;

        /// <summary>
        /// Initializes a new instance of the <see cref="DbContextScope" /> class.
        /// </summary>
        /// <param name="purpose">Will this dbcontext scope be used for reading or writing?</param>
        /// <param name="isNewScope">Will this dbcontext scope be a new DbContextScope.</param>
        public DbContextScope(DbContextScopePurpose purpose, bool isNewScope = false)
        {
            this.purpose = purpose;

            this.parentScope = GetAmbientScope();
            if (this.parentScope != null && !isNewScope)
            {
                this.scopedDbContexts = this.parentScope.DbContext;
            }
            else
            {
                this.scopedDbContexts = new DbContextCollection(purpose == DbContextScopePurpose.Writing);
                this.isRoot = true;
            }

            this.SetAmbientScope(this);

            if (purpose == DbContextScopePurpose.Writing && !this.scopedDbContexts.ForWriting)
            {
                throw new InvalidOperationException(
                    "Can't open a child DbContextScope for writing when the root scope " +
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
                return this.scopedDbContexts;
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

            if (this.scopedDbContexts.BlockSave)
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

            this.scopedDbContexts.AllowSaving = true;
            this.scopedDbContexts.SaveChanges();
            this.scopedDbContexts.AllowSaving = false;
        }

        public void Dispose()
        {
            if (this.isRoot)
            {
                this.Dispose(true);
                GC.SuppressFinalize(this);
            }

            // Pop ourself from the ambient scope stack
            var currentAmbientScope = GetAmbientScope();
            if (currentAmbientScope != this)
            {
                // This is a serious programming error. Worth throwing here.
                throw new InvalidOperationException("DbContextScope instances must be disposed of in the order in which they were created!");
            }

            RemoveAmbientScope();

            if (this.parentScope != null)
            {
                if (this.parentScope.DbContext == null)
                {
                    /*
                     * If our parent scope has been disposed before us, it can only mean one thing:
                     * someone started a parallel flow of execution and forgot to suppress the
                     * ambient context before doing so. And we've been created in that parallel flow.
                     * 
                     * Since the CallContext flows through all async points, the ambient scope in the 
                     * main flow of execution ended up becoming the ambient scope in this parallel flow
                     * of execution as well. So when we were created, we captured it as our "parent scope". 
                     * 
                     * The main flow of execution then completed while our flow was still ongoing. When 
                     * the main flow of execution completed, the ambient scope there (which we think is our 
                     * parent scope) got disposed of as it should.
                     * 
                     * So here we are: our parent scope isn't actually our parent scope. It was the ambient
                     * scope in the main flow of execution from which we branched off. We should never have seen 
                     * it. Whoever wrote the code that created this parallel task should have suppressed
                     * the ambient context before creating the task - that way we wouldn't have captured
                     * this bogus parent scope.
                     * 
                     * While this is definitely a programming error, it's not worth throwing here. We can only 
                     * be in one of two scenario:
                     * 
                     * - If the developer who created the parallel task was mindful to force the creation of 
                     * a new scope in the parallel task (with IDbContextScopeFactory.CreateNew() instead of 
                     * JoinOrCreate()) then no harm has been done. We haven't tried to access the same DbContext
                     * instance from multiple threads.
                     * 
                     * - If this was not the case, they probably already got an exception complaining about the same
                     * DbContext or ObjectContext being accessed from multiple threads simultaneously (or a related
                     * error like multiple active result sets on a DataReader, which is caused by attempting to execute
                     * several queries in parallel on the same DbContext instance). So the code has already blow up.
                     * 
                     * So just record a warning here. Hopefully someone will see it and will fix the code.
                     */

                    var message = @"PROGRAMMING ERROR - When attempting to dispose a DbContextScope, we found that our parent DbContextScope has already been disposed! This means that someone started a parallel flow of execution (e.g. created a TPL task, created a thread or enqueued a work item on the ThreadPool) within the context of a DbContextScope without suppressing the ambient context first. 

In order to fix this:
1) Look at the stack trace below - this is the stack trace of the parallel task in question.
2) Find out where this parallel task was created.
3) Change the code so that the ambient context is suppressed before the parallel task is created. You can do this with IDbContextScopeFactory.SuppressAmbientContext() (wrap the parallel task creation code block in this). 

Stack Trace:
" + Environment.StackTrace;
                }
                else
                {
                    this.SetAmbientScope(this.parentScope);
                }
            }
        }

        /// <summary>
        /// Clears the ambient scope from the CallContext and stops tracking its instance. 
        /// Call this when a DbContextScope is being disposed.
        /// </summary>
        internal static void RemoveAmbientScope()
        {
            var current = CallContext.LogicalGetData(AmbientDbContextScopeKey) as InstanceIdentifier;
            CallContext.LogicalSetData(AmbientDbContextScopeKey, null);

            // If there was an ambient scope, we can stop tracking it now
            if (current != null)
            {
                DbContextScopeInstances.Remove(current);
            }
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
                    this.scopedDbContexts.BlockSave = true;

                    // Don't throw here - it would mask original exception when exiting
                    // a using block.
                }

                if (this.scopedDbContexts != null && this.isRoot)
                {
                    this.scopedDbContexts.Dispose();
                    this.scopedDbContexts = null;
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
