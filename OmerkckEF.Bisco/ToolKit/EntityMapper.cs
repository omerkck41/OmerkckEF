using System.Collections.Concurrent;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;
using OmerkckEF.Biscom.Interfaces;

namespace OmerkckEF.Biscom.ToolKit
{
    public static class EntityMapper
    {
        private static readonly ConcurrentDictionary<Type, object> _cachedMappers = new();

        public static List<T> MapList<T>(IDataReader reader, IMetadataProvider metadataProvider) where T : class, new()
        {
            var mapper = (Func<IDataReader, IMetadataProvider, T>)_cachedMappers.GetOrAdd(typeof(T), _ => CreateMapper<T>());
            var list = new List<T>();
            while (reader.Read())
            {
                list.Add(mapper(reader, metadataProvider));
            }
            return list;
        }

        private static Func<IDataReader, IMetadataProvider, T> CreateMapper<T>() where T : class, new()
        {
            var readerParam = Expression.Parameter(typeof(IDataReader), "reader");
            var metadataParam = Expression.Parameter(typeof(IMetadataProvider), "metadataProvider");
            
            var entityType = typeof(T);
            var entityVar = Expression.Variable(entityType, "entity");
            var constructor = Expression.New(entityType);
            
            var assignments = new List<Expression>
            {
                Expression.Assign(entityVar, constructor)
            };

            var properties = entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                       .Where(p => p.CanWrite);

            foreach (var prop in properties)
            {
                // reader.GetOrdinal check + reader.GetValue check
                // For simplicity in this first step, we use metadataProvider.ParsePrimitive
                // In a true TIER 3, we would generate direct calls to reader.GetInt32, etc.
                
                var propName = Expression.Constant(prop.Name);
                
                // Try-catch for each property to handle missing columns gracefully
                var getOrdinalMethod = typeof(IDataRecord).GetMethod("GetOrdinal");
                var getValueMethod = typeof(IDataRecord).GetMethod("GetValue");
                var parsePrimitiveMethod = typeof(IMetadataProvider).GetMethod("ParsePrimitive");

                var tryAssign = Expression.TryCatch(
                    Expression.Block(
                        Expression.Call(metadataParam, parsePrimitiveMethod!, 
                            Expression.Constant(prop), 
                            entityVar, 
                            Expression.Call(readerParam, getValueMethod!, 
                                Expression.Call(readerParam, getOrdinalMethod!, propName)))
                    ),
                    Expression.Catch(typeof(Exception), Expression.Empty())
                );

                assignments.Add(tryAssign);
            }

            assignments.Add(entityVar); // Return value

            var body = Expression.Block(new[] { entityVar }, assignments);
            return Expression.Lambda<Func<IDataReader, IMetadataProvider, T>>(body, readerParam, metadataParam).Compile();
        }
    }
}
