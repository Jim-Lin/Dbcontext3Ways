namespace Dbcontext3Ways.DAL.Repo.Explicit
{
    using System;
    using System.Collections.Generic;
    using System.Data.Entity;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Text;
    using System.Threading.Tasks;

    abstract class GenericRepository<TEntity> : IGenericRepository<TEntity> where TEntity : class
    {
        public GenericRepository(CoreDbContext db)
        {
            this.DbContext = db;
            this.DbSet = db.Set<TEntity>();
        }

        protected CoreDbContext DbContext { get; set; }

        protected DbSet<TEntity> DbSet { get; set; }

        public virtual IEnumerable<TEntity> Get(
            Expression<Func<TEntity, bool>> filter = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
            string includeProperties = "")
        {
            IQueryable<TEntity> query = this.DbSet;

            if (filter != null)
            {
                query = query.Where(filter);
            }

            foreach (var includeProperty in includeProperties.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                query = query.Include(includeProperty);
            }

            if (orderBy != null)
            {
                return orderBy(query).ToList();
            }
            else
            {
                return query.ToList();
            }
        }

        public virtual TEntity GetByID(object id)
        {
            return this.DbSet.Find(id);
        }

        public virtual void Insert(TEntity entity)
        {
            this.DbSet.Add(entity);
        }

        public virtual void InsertRange(IEnumerable<TEntity> entities)
        {
            this.DbSet.AddRange(entities);
        }

        public virtual void DeleteByID(object id)
        {
            TEntity entityToDelete = this.DbSet.Find(id);
            this.Delete(entityToDelete);
        }

        public virtual void Delete(TEntity entityToDelete)
        {
            if (this.DbContext.Entry(entityToDelete).State == EntityState.Detached)
            {
                this.DbSet.Attach(entityToDelete);
            }

            this.DbSet.Remove(entityToDelete);
        }

        public virtual void Update(TEntity entityToUpdate)
        {
            this.DbSet.Attach(entityToUpdate);
            this.DbContext.Entry(entityToUpdate).State = EntityState.Modified;
        }
    }
}
