using System.Reflection;

namespace OmerkckEF.Biscom.Interfaces
{
    public interface ISqlGenerator
    {
        string GetColumnNames<T>(T entity) where T : class;
        string GetParameterNames<T>(T entity, int rowCount = -1) where T : class;
        string GetUpdateSetClause<T>(T entity) where T : class;
        string GetDbDataType(PropertyInfo property);
        string GetConstraints(PropertyInfo property);
        string GetDefaultValue(PropertyInfo property);
        
        (string sql, Dictionary<string, object> parameters) BuildInsertQuery<T>(T entity) where T : class;
        (string sql, Dictionary<string, object> parameters) BuildUpdateQuery<T>(T entity, IEnumerable<string> fields) where T : class;
        Dictionary<string, object> GetParameters<T>(T entity, IEnumerable<string>? fields = null);
    }
}
