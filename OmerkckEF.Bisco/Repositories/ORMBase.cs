using OmerkckEF.Biscom.DBContext;
using OmerkckEF.Biscom.Interfaces;
using OmerkckEF.Biscom.ToolKit;
using System.Data;
using System.Linq.Expressions;

namespace OmerkckEF.Biscom.Repositories
{
    public class ORMBase<T, OT> : IORM<T>, IDisposable
        where T : class, new()
        where OT : class, new()
    {
        private static OT? _current;
        public static OT Current
        {
            get
            {
                if (DBServer.DBServerInfo?.DbSchema != DBContext?.ConnSchemaName) { DBContext?.Dispose(); }

                if (_current != null) return _current as OT;

                ORMBase<T, OT>._current = ORMBase<T, OT>._current ?? Activator.CreateInstance<OT>();
                return ORMBase<T, OT>._current;
            }
        }


        #region Properties
        private static EntityContext DBContext { get; } = new(DBServer.DBServerInfo ?? new());
        #endregion


        /// Select
        public virtual Result<List<T>> GetAll() => DBContext.GetMapClass<T>();
        public virtual Result<List<T>> GetAll(Expression<Func<T, bool>> filter) => DBContext.GetMapClass<T>(filter);
        public virtual Result<List<T>> GetAll(string? queryString = null, Dictionary<string, object>? parameters = null, string? schema = null)
            => DBContext.GetMapClass<T>(queryString, parameters, schema, CommandType.Text);
        public virtual Result<List<T>> GetAllByWhere(string whereCond, Dictionary<string, object>? parameters = null)
            => DBContext.GetMapClassByWhere<T>(whereCond, parameters, CommandType.Text);
        public virtual Result<List<T>> GetAllBySchema(string schema, string? whereCond = null, Dictionary<string, object>? parameters = null)
            => DBContext.GetMapClassBySchema<T>(schema, whereCond, parameters, CommandType.Text);


        public virtual Result<T> GetById(object id) => DBContext.GetMapClassById<T>(id);


        /// Insert
        public virtual Result<int> Insert(T entity)
            => DBContext.DoMapInsert(entity);
        public virtual Result<int> Insert(T entity, bool getById = false, bool transaction = false)
            => DBContext.DoMapInsert(entity, getById, transaction);
        public virtual Result<int> Insert(string? schema, T entity, bool getById = false, bool transaction = false)
            => DBContext.DoMapInsert<T>(schema, entity, getById, transaction);


        /// Multi Insert
        public virtual Result<bool> MultiInsert(IEnumerable<T> entityList) => DBContext.DoMapMultiInsert<T>(entityList);
        public virtual Result<bool> MultiInsert(string? schema, IEnumerable<T> entityList) => DBContext.DoMapMultiInsert(schema, entityList);


        /// Update
        public virtual Result<bool> Update(T currentT) => DBContext.DoMapUpdate(currentT);
        public virtual Result<bool> Update(T currentT, bool transaction) => DBContext.DoMapUpdate(currentT, transaction);
        public virtual Result<bool> Update(string? schema, T currentT, bool transaction = false) => DBContext.DoMapUpdate(schema, currentT, transaction);


        /// Delete
        public virtual Result<bool> Delete(T entity) => DBContext.DoMapDelete(entity);
        public virtual Result<bool> Delete(Expression<Func<T, bool>> filter) => DBContext.DoMapDelete(filter);
        public virtual Result<bool> Delete(T entity, bool transaction) => DBContext.DoMapDelete(entity, transaction);
        public virtual Result<bool> Delete(string? schema, T entity, bool transaction = false) => DBContext.DoMapDelete(schema, entity, transaction);
        public virtual Result<bool> Delete(IEnumerable<T> entityList, bool transaction = false) => DBContext.DoMapDeleteAll(entityList, transaction);
        public virtual Result<bool> Delete(string? schema, IEnumerable<T> entityList, bool transaction = false) => DBContext.DoMapDeleteAll(schema, entityList, transaction);

        #region IDisposable Members
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private bool disposed = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // Managed resources are released here.
                    _current = null;
                    DBContext?.Dispose();
                }

                // Unmanaged resources are released here.
                disposed = true;
            }
        }
        ~ORMBase() { Dispose(false); }
        #endregion
    }
}