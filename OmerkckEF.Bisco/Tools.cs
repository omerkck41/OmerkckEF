using System.Data.Common;
using System.Reflection;

namespace OmerkckEF.Biscom
{
    public static class Tools
    {
        /// <summary>
        /// DataReader Extensions
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="columnName"></param>
        /// <returns></returns>
        public static bool HasColumn(this DbDataReader reader, string columnName)
        {
            try
            {
                return reader.GetOrdinal(columnName) >= 0;
            }
            catch (IndexOutOfRangeException)
            {
                return false;
            }
        }

        public static void ParsePrimitive(PropertyInfo prop, object entity, object value)
        {
            if (prop == null || entity == null || value == null) return;

            if (prop.PropertyType == typeof(string))
            {
                prop.SetValue(entity, value.ToString().Trim(), null);
            }
            else if (prop.PropertyType == typeof(char) || prop.PropertyType == typeof(char?))
            {
                if (value == null)
                    prop.SetValue(entity, null, null);
                else
                    prop.SetValue(entity, char.Parse(value.ToString()), null);
            }
            else if (prop.PropertyType == typeof(int) || prop.PropertyType == typeof(int?))
            {
                if (value == null)
                    prop.SetValue(entity, null, null);
                else
                    prop.SetValue(entity, int.Parse(value.ToString()), null);
            }
            else if (prop.PropertyType == typeof(bool) || prop.PropertyType == typeof(bool?))
            {
                if (value == null)
                    prop.SetValue(entity, null, null);
                else
                    prop.SetValue(entity, bool.Parse(value.ToString()), null);
            }
            else if (prop.PropertyType == typeof(DateTime) || prop.PropertyType == typeof(DateTime?))
            {
                if (value == null)
                    prop.SetValue(entity, null, null);
                else
                    prop.SetValue(entity, DateTime.Parse(value.ToString()), null);
            }
            else if (prop.PropertyType == typeof(decimal) || prop.PropertyType == typeof(decimal?))
            {
                if (value == null)
                    prop.SetValue(entity, null, null);
                else
                    prop.SetValue(entity, decimal.Parse(value.ToString()), null);
            }
            else if (prop.PropertyType == typeof(byte[]))
            {
                if (value == null)
                    prop.SetValue(entity, null, null);
                else
                    prop.SetValue(entity, (byte[])value, null);
            }
        }
    }
}
