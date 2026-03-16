using System.Buffers;
using System.Reflection;
using System.Text;
using OmerkckEF.Biscom.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace OmerkckEF.Biscom.ToolKit
{
    public class SqlGenerator(IMetadataProvider metadataProvider) : ISqlGenerator
    {
        private static readonly SearchValues<char> _sqlSpecialChars = SearchValues.Create("';--");

        public string GetColumnNames<T>(T entity) where T : class
        {
            var keys = metadataProvider.GetProperties(typeof(T), typeof(DataNameAttribute), false)
                       .Where(x => x.GetValue(entity) != null)
                       .Select(x => Sanitize(x.Name));

            return string.Join(", ", keys);
        }

        private string Sanitize(string value)
        {
            // TIER 3: High-performance scanning with .NET 10 SearchValues
            if (value.AsSpan().ContainsAny(_sqlSpecialChars))
            {
                // Simple protection: removing special chars
                var sb = new StringBuilder(value.Length);
                foreach (var c in value)
                {
                    if (!"';--".Contains(c)) sb.Append(c);
                }
                return sb.ToString();
            }
            return value;
        }

        public string GetParameterNames<T>(T entity, int rowCount = -1) where T : class
        {
            var keys = metadataProvider.GetProperties(typeof(T), typeof(DataNameAttribute), false)
                       .Where(x => x.GetValue(entity) != null)
                       .Select(x => rowCount >= 0 ? $"@{rowCount + x.Name}" : $"@{x.Name}");

            return string.Join(", ", keys);
        }

        public string GetUpdateSetClause<T>(T entity) where T : class
        {
            var keys = metadataProvider.GetProperties(typeof(T), typeof(DataNameAttribute), false)
                       .Where(x => x.GetValue(entity) != null)
                       .Select(p => $"{p.Name} = @{p.Name}");

            return string.Join(", ", keys);
        }

        public string GetDbDataType(PropertyInfo property)
        {
            Type? type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

            return type switch
            {
                _ when type == typeof(int) => "INT",
                _ when type == typeof(long) => "BIGINT",
                _ when type == typeof(string) => "VARCHAR(255)", // Default size
                _ when type == typeof(DateTime) => "DATETIME",
                _ when type == typeof(decimal) => "DECIMAL(18,2)",
                _ when type == typeof(bool) => "TINYINT(1)",
                _ when type == typeof(byte[]) => "BLOB",
                _ when type == typeof(Guid) => "CHAR(36)",
                _ => "VARCHAR(255)"
            };
        }

        public string GetConstraints(PropertyInfo property)
        {
            var constraints = new List<string>();

            if (property.GetCustomAttribute<KeyAttribute>() != null)
                constraints.Add("NOT NULL AUTO_INCREMENT UNIQUE");

            if (property.GetCustomAttribute<RequiredAttribute>() != null)
                constraints.Add("NOT NULL");

            return string.Join(" ", constraints);
        }

        public string GetDefaultValue(PropertyInfo property)
        {
            if (property.DeclaringType == null) return string.Empty;
            
            // TIER 3: Activator.CreateInstance saniyede binlerce kez çağrılırsa performans kaybı yaratır.
            // Bu kısım ileride Cached Expression Trees ile optimize edilecek.
            var instance = Activator.CreateInstance(property.DeclaringType);
            var value = property.GetValue(instance);
            
            if (value == null) return string.Empty;

            return property.PropertyType == typeof(string) 
                ? $"DEFAULT '{value}'" 
                : $"DEFAULT {value}";
        }

        public (string sql, Dictionary<string, object> parameters) BuildInsertQuery<T>(T entity) where T : class
        {
            var parameters = GetParameters(entity);
            var columns = GetColumnNames(entity);
            var paramNames = GetParameterNames(entity);
            
            var sql = $"INSERT INTO {typeof(T).Name} ({columns}) VALUES ({paramNames})";
            return (sql, parameters);
        }

        public (string sql, Dictionary<string, object> parameters) BuildUpdateQuery<T>(T entity, IEnumerable<string> fields) where T : class
        {
            var parameters = GetParameters(entity, fields);
            var identityColumn = metadataProvider.GetKeyAttributeName(entity);
            
            var setClause = string.Join(", ", fields.Select(f => $"{f} = @{f}"));
            var sql = $"UPDATE {typeof(T).Name} SET {setClause} WHERE {identityColumn} = @{identityColumn}";
            
            return (sql, parameters);
        }

        public Dictionary<string, object> GetParameters<T>(T entity, IEnumerable<string>? fields = null)
        {
            return metadataProvider.GetProperties(typeof(T), typeof(DataNameAttribute))
                       .Where(x => x.GetValue(entity) != null && ((fields?.Contains(x.Name) ?? true) || x.GetCustomAttributes(typeof(KeyAttribute), true).Length > 0))
                       .ToDictionary(x => $"@{x.Name}", x => x.GetValue(entity) ?? DBNull.Value);
        }
    }
}
