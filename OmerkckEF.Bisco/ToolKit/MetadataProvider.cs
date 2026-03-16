using OmerkckEF.Biscom.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace OmerkckEF.Biscom.ToolKit
{
    public class MetadataProvider : IMetadataProvider
    {
        public IEnumerable<PropertyInfo> GetProperties(Type classType, Type? attributeType = null, bool isKeyAttribute = true)
        {
            if (classType == null) return [];
            if (attributeType == null) return classType.GetProperties();

            return classType.GetProperties().Where(x => isKeyAttribute ? x.GetCustomAttributes(attributeType, true).Length != 0
                                                                       : x.GetCustomAttributes(attributeType, true).Length != 0 && x.GetCustomAttributes(typeof(KeyAttribute), true).Length == 0);
        }

        public string GetKeyAttributeName<T>(T obj) where T : class
        {
            if (obj is null) return string.Empty;

            return typeof(T).GetProperties().Where(x => x.GetCustomAttributes(typeof(KeyAttribute), true).Length > 0)
                                            .Select(p => p.Name).FirstOrDefault() ?? string.Empty;
        }

        public object? GetEntityValue<T, TAttribute>(T entity, string? propertyName = null) where T : class where TAttribute : class
        {
            PropertyInfo? property;

            if (!string.IsNullOrEmpty(propertyName))
            {
                property = typeof(T).GetProperties()
                                    .Where(x => x.GetCustomAttributes(typeof(TAttribute), true).Length > 0)
                                    .FirstOrDefault(x => x.Name == propertyName);
            }
            else
            {
                property = typeof(T).GetProperties()
                                    .Where(x => x.GetCustomAttributes(typeof(TAttribute), true).Length > 0)
                                    .FirstOrDefault();
            }

            return property?.GetValue(entity);
        }

        public List<string> GetChangedFields<T>(T newEntity, T oldEntity) where T : class
        {
            if (newEntity == null || oldEntity == null) return [];

            List<string> fields = [];

            var propertiesWithAttribute = typeof(T).GetProperties()
                                                  .Where(x => Attribute.IsDefined(x, typeof(DataNameAttribute)) && x.GetCustomAttributes(typeof(KeyAttribute), true).Length <= 0)
                                                  .ToList();

            foreach (var prop in propertiesWithAttribute)
            {
                if (prop.PropertyType.Namespace == "System.Collections.Generic") continue;

                object oldValue = prop.GetValue(oldEntity) ?? string.Empty;
                object newValue = prop.GetValue(newEntity) ?? string.Empty;

                if (prop.PropertyType == typeof(byte[]))
                {
                    byte[] oldBytes = oldValue as byte[] ?? [0];
                    byte[] newBytes = newValue as byte[] ?? [0];

                    if (!oldBytes.SequenceEqual(newBytes))
                        fields.Add(prop.Name);
                }
                else if (!newValue.Equals(oldValue))
                    fields.Add(prop.Name);
            }

            return fields;
        }

        public void ParsePrimitive(PropertyInfo prop, object entity, object? value)
        {
            if (prop == null || entity == null || value == null || value == DBNull.Value) return;

            var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

            try
            {
                if (targetType == typeof(bool))
                {
                    if (bool.TryParse(value.ToString(), out bool parsedBool))
                        prop.SetValue(entity, parsedBool);
                    else if (int.TryParse(value.ToString(), out int parsedInt))
                        prop.SetValue(entity, parsedInt != 0);
                }
                else if (targetType == typeof(Guid))
                {
                    prop.SetValue(entity, Guid.Parse(value.ToString() ?? ""));
                }
                else
                {
                    prop.SetValue(entity, Convert.ChangeType(value, targetType));
                }
            }
            catch
            {
                // TIER 3: Buraya bir logging eklenebilir veya hata yönetimi özelleştirilebilir.
            }
        }

        public Guid CreateSequentialGuid() => Guid.CreateVersion7();
    }
}
