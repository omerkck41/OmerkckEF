using OmerkckEF.Biscom.DBContext;
using System.ComponentModel.DataAnnotations;
using System.Data;
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



		/// <summary>
		/// Attachments for Entity use
		/// </summary>
		#region Entity ToolKit

		/// <summary>
		/// type and content control of properties
		/// </summary>
		/// <param name="Prop"></param>
		/// <param name="Entity"></param>
		/// <param name="Value"></param>
		public static void ParsePrimitive(PropertyInfo Prop, object Entity, object Value)
        {
            if (Prop == null || Entity == null || (Value == null || Value == DBNull.Value)) return;

            if (Prop.PropertyType == typeof(string))
            {
                Prop.SetValue(Entity, ((string)Value).Trim(), null);
            }
            else if (Prop.PropertyType == typeof(char) || Prop.PropertyType == typeof(char?))
            {
                if (Value == null)
                    Prop.SetValue(Entity, null, null);
                else
                    Prop.SetValue(Entity, char.Parse((string)Value), null);
            }
            else if (Prop.PropertyType == typeof(int) || Prop.PropertyType == typeof(int?))
            {
                if (Value == null)
                    Prop.SetValue(Entity, null, null);
                else
                    Prop.SetValue(Entity, int.Parse((string)Value), null);
            }
            else if (Prop.PropertyType == typeof(bool) || Prop.PropertyType == typeof(bool?))
            {
                if (Value == null)
                    Prop.SetValue(Entity, null, null);
                else
                    Prop.SetValue(Entity, bool.Parse((string)Value), null);
            }
            else if (Prop.PropertyType == typeof(DateTime) || Prop.PropertyType == typeof(DateTime?))
            {
                if (Value == null)
                    Prop.SetValue(Entity, null, null);
                else
                    Prop.SetValue(Entity, DateTime.Parse((string)Value), null);
            }
            else if (Prop.PropertyType == typeof(decimal) || Prop.PropertyType == typeof(decimal?))
            {
                if (Value == null)
                    Prop.SetValue(Entity, null, null);
                else
                    Prop.SetValue(Entity, decimal.Parse((string)Value), null);
            }
            else if (Prop.PropertyType == typeof(byte[]))
            {
                if (Value == null)
                    Prop.SetValue(Entity, null, null);
                else
                    Prop.SetValue(Entity, (byte[])Value, null);
            }
		}

		/// <summary>
		/// Listing Properties by AttirbuteType
		/// </summary>
		/// <param name="ClassType"></param>
		/// <param name="AttirbuteType"></param>
		/// <param name="IsKeyAttirbute"></param>
		/// <returns></returns>
		public static IEnumerable<PropertyInfo> GetProperties(Type ClassType, Type AttirbuteType, bool IsKeyAttirbute = true)
		{
			if (ClassType == null) return new List<PropertyInfo>();
			if (AttirbuteType == null) return ClassType.GetProperties();

			return ClassType.GetProperties().Where(x => IsKeyAttirbute ? x.GetCustomAttributes(AttirbuteType, true).Any()
																	   : x.GetCustomAttributes(AttirbuteType, true).Any() && !x.GetCustomAttributes(typeof(KeyAttribute), true).Any());
		}
        public static string GetKeyAttribute<T>() where T : class
		{
            return typeof(T).GetProperties().Where(x => x.GetCustomAttributes(typeof(KeyAttribute), true).Any()).Select(p => p.Name).FirstOrDefault()!;
		}
		public static string GetColumnNames<T>(bool IsKeyAttirbute = true) where T : class
		{
			var keys = GetProperties(typeof(T), typeof(DataNameAttribute), IsKeyAttirbute).Select(x => x.Name);

			return $"{string.Join(", ", keys)}";
		}
		public static string GetParameterNames<T>(bool IsKeyAttirbute = true) where T : class
		{
            var keys = GetProperties(typeof(T), typeof(DataNameAttribute), IsKeyAttirbute).Select(x => $"@{x.Name}");

			return $"{string.Join(", ", keys)}";
		}
		public static string GetUpdateSetClause<T>() where T : class
		{
            var keys = GetProperties(typeof(T), typeof(DataNameAttribute), false).Select(p => $"{p.Name} = @{p.Name}");

			return $"{string.Join(", ", keys)}";
		}
		public static Dictionary<string, object> GetDbPrameters<T>(T Entity)
		{
			Dictionary<string, object> dict = new();
			try
			{
                dict = GetProperties(typeof(T), typeof(DataNameAttribute))
                       .Select(x => new KeyValuePair<string, object>($"@{x.Name}", x.GetValue(Entity) ?? DBNull.Value))
                       .ToDictionary(x => x.Key, x => x.Value);

				return dict;
			}
			catch
			{
				return dict;
			}
		}

        public static string GetIEnumerablePairs(IEnumerable<object> keys, string format = "{0}", string separator = ", ")
		{
			var pairs = keys.Select(key => string.Format(format, key)).ToList();
			return string.Join(separator, pairs);
		}

		public static string CheckAttributeColumn<T>(T Entity, Bisco bisco) where T : class
        {
            try
            {
                string MsgError = string.Empty;
                string RequiredError = string.Empty;
				string UniqueError = string.Empty;
				string? IdentityName = null;
				var IdentValue = 0;
                Dictionary<string, object> dict = new();

				Type type = Entity.GetType();


				///Controls of Required fields
				var required = type.GetProperties()
	                                               .Where(x => x.GetCustomAttribute<RequiredAttribute>() is RequiredAttribute attribute &&
				                                               (x.GetValue(Entity) is null || string.IsNullOrEmpty(x.GetValue(Entity)!.ToString())))
	                                               .Select(s => s.Name + " = " + s.GetCustomAttribute<RequiredAttribute>()?.ErrorMessage).ToArray();

				RequiredError = required.Length > 0 ? $"- {string.Join("\n- ", required)}\n\n{required.Length} column(s) cannot be null!!!" : "";


                ///Controls of Unique fields
                type.GetProperties()
                                    .Where(x => x.GetCustomAttributes(true).Any(a => a is UniqueAttribute || a is KeyAttribute) && x.GetValue(Entity) is not null).ToList()
                                    .ForEach(f =>
                                    {
										var colmValue = f.GetValue(Entity) ?? string.Empty;

										if (f.GetCustomAttribute(typeof(KeyAttribute)) != null)
										{
											IdentityName = f.Name;
											IdentValue = (int)f.GetValue(Entity)!;
										}

                                        if (f.GetCustomAttribute(typeof(UniqueAttribute)) != null)
                                        {
                                            if (string.IsNullOrEmpty((string)colmValue)) return;

                                            string sqlQuery = $"Select {f.Name} from {type.Name} where {IdentityName}!={IdentValue} and {f.Name}=@{f.Name}";
                                            var UniqueMsg = f.GetCustomAttribute<UniqueAttribute>()?.ErrorMessage ?? null;

                                            dict.Add($"@{f.Name}", colmValue);
                                            var ctrl = bisco.RunScaler(sqlQuery, dict);

                                            UniqueError += ctrl != null ? f.Name + " : " + colmValue : null;
											UniqueError += UniqueMsg != null ? (". ErrorMessage : " + UniqueMsg) : null;
										}
									});
				UniqueError += UniqueError != null ? "\ncolumn(s) cannot be Unique!!!" : null;



				return MsgError = RequiredError +"\n"+ UniqueError;
            }
            catch
            {
                return "Syntax Error";
            }
		}
		#endregion
	}
}