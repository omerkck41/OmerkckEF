using System.Reflection;

namespace OmerkckEF.Biscom.Interfaces
{
    public interface IMetadataProvider
    {
        IEnumerable<PropertyInfo> GetProperties(Type classType, Type? attributeType = null, bool isKeyAttribute = true);
        string GetKeyAttributeName<T>(T obj) where T : class;
        object? GetEntityValue<T, TAttribute>(T entity, string? propertyName = null) where T : class where TAttribute : class;
        List<string> GetChangedFields<T>(T newEntity, T oldEntity) where T : class;
        void ParsePrimitive(PropertyInfo prop, object entity, object? value);
        Guid CreateSequentialGuid();
    }
}
