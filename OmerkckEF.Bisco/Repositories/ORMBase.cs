using OmerkckEF.Biscom.DBContext;
using OmerkckEF.Biscom.Interfaces;
using OmerkckEF.Biscom.ToolKit;
using System.Data;
using System.Linq.Expressions;
using static OmerkckEF.Biscom.ToolKit.Enums;

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
                if (DBServer.DBServerInfo?.DbSchema != DBContext?.DBSchemaName) { DBContext?.Dispose(); }

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
        public virtual Result<bool> UpdateCompositeTable(T currentT, params object[] args) => DBContext.DoMapUpdateCompositeTable(currentT, args);
        public virtual Result<bool> UpdateCompositeTable(string? schema, T currentT, params object[] args) => DBContext.DoMapUpdateCompositeTable(schema, currentT, args);
        public virtual Result<bool> UpdateQuery(Dictionary<string, object> prms, Expression<Func<T, bool>> filter) => DBContext.DoUpdateQuery<T>(prms, filter);


        /// Delete
        public virtual Result<bool> Delete(T entity) => DBContext.DoMapDelete(entity);
        public virtual Result<bool> Delete(Expression<Func<T, bool>> filter) => DBContext.DoMapDelete(filter);
        public virtual Result<bool> Delete(T entity, bool transaction) => DBContext.DoMapDelete(entity, transaction);
        public virtual Result<bool> Delete(string? schema, T entity, bool transaction = false) => DBContext.DoMapDelete(schema, entity, transaction);
        public virtual Result<bool> Delete(IEnumerable<T> entityList, bool transaction = false) => DBContext.DoMapDeleteAll(entityList, transaction);
        public virtual Result<bool> Delete(string? schema, IEnumerable<T> entityList, bool transaction = false) => DBContext.DoMapDeleteAll(schema, entityList, transaction);


        //Table CRUD
        /// <summary>
        /// Creates a new table in the specified schema (defaults to the current database schema).
        /// </summary>
        /// <typeparam name="T">The type of the table.</typeparam>
        /// <param name="schema">The schema of the table (optional). Defaults to the current database schema.</param>
        /// <returns>A result indicating whether the creation was successful and providing information about the operation.</returns>
        /// <example>
        /// var result = CreateTable<YourTableType>("your_schema_name");
        /// </example>
        public virtual Result<bool> CreateTable() => DBContext.CreateTable<T>();
        /// <summary>
        /// Drops a table in the specified schema (defaults to the current database schema).
        /// </summary>
        /// <typeparam name="T">The type of the table.</typeparam>
        /// <param name="schema">The schema of the table (optional). Defaults to the current database schema.</param>
        /// <returns>A result indicating whether the deletion was successful and providing information about the operation.</returns>
        /// <example>
        /// var result = DropTable<YourTableType>("your_schema_name");
        /// </example>
        public virtual Result<bool> DropTable() => DBContext.DropTable<T>();
        /// <summary>
        /// Updates a table in the specified schema (defaults to the current database schema).
        /// </summary>
        /// <typeparam name="T">The type of the table.</typeparam>
        /// <param name="schema">The schema of the table (optional). Defaults to the current database schema.</param>
        /// <returns>A result indicating whether the update was successful and providing information about the operation.</returns>
        /// <example>
        /// var result = UpdateTable<YourTableType>("your_schema_name");
        /// </example>
        public virtual Result<bool> UpdateTable() => DBContext.UpdateTable<T>();
        /// <summary>
        /// Removes a column from a table in the specified schema (defaults to the current database schema), or removes all columns.
        /// </summary>
        /// <typeparam name="T">The type of the table.</typeparam>
        /// <param name="columnName">The name of the column to remove (optional). If left empty, removes all columns.</param>
        /// <param name="schema">The schema of the table (optional). Defaults to the current database schema.</param>
        /// <returns>A result indicating whether the removal was successful and providing information about the operation.</returns>
        /// <example>
        /// var result = RemoveTableColumn<YourTableType>("column_name", "your_schema_name");
        /// </example>
        public virtual Result<bool> RemoveTableColumn(string? columnName = null) => DBContext.RemoveTableColumn<T>(columnName);
        /// <summary>
        /// Adds an attribute to a column in the specified table schema (defaults to the current database schema).
        /// </summary>
        /// <typeparam name="T">The type of the table.</typeparam>
        /// <param name="attribute">The attribute to add to the column.</param>
        /// <param name="propertyName">The name of the property representing the column.</param>
        /// <param name="schema">The schema of the table (optional). Defaults to the current database schema.</param>
        /// <returns>A result indicating whether the addition was successful and providing information about the operation.</returns>
        /// <example>
        /// var result = AddAttributeToTableColumn<YourTableType>(TableColumnAttribute.PrimaryKey, "column_name", "your_schema_name");
        /// </example>
        public virtual Result<bool> AddAttributeToTableColumn(TableColumnAttribute attribute, string propertyName) => DBContext.AddAttributeToTableColumn<T>(attribute, propertyName);

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