using OmerkckEF.Biscom.DBContext;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.Common;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;

namespace OmerkckEF.Biscom.ToolKit
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
		public static void ParsePrimitive(PropertyInfo prop, object entity, object value)
        {
			if (prop == null || entity == null || (value == null || value == DBNull.Value)) return;

			switch (prop.PropertyType)
			{
				case Type t when t == typeof(string):
					prop.SetValue(entity, value.ToString()?.Trim());
					break;

				case Type t when t == typeof(char) || t == typeof(char?):
					prop.SetValue(entity, char.Parse(value.ToString() ?? ""));
					break;

				case Type t when t == typeof(int) || t == typeof(int?):
					prop.SetValue(entity, int.Parse(value.ToString() ?? ""));
					break;

				case Type t when t == typeof(bool) || t == typeof(bool?):
					prop.SetValue(entity, bool.Parse(value.ToString() ?? ""));
					break;

				case Type t when t == typeof(DateTime) || t == typeof(DateTime?):
					prop.SetValue(entity, DateTime.Parse(value.ToString() ?? ""));
					break;

				case Type t when t == typeof(decimal) || t == typeof(decimal?):
					prop.SetValue(entity, decimal.Parse(value.ToString() ?? ""));
					break;

				case Type t when t == typeof(byte[]):
					if (value is byte[] byteArray)
						prop.SetValue(entity, byteArray);
					break;

				default:
					throw new NotSupportedException($"Type {prop.PropertyType} is not supported");
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
        public static object GetKeyAttribute<T>(this T obj) where T : class
		{
            return typeof(T).GetProperties().Where(x => x.GetCustomAttributes(typeof(KeyAttribute), true).Any())
                                            .Select(p => p.Name).FirstOrDefault()!;
		}
		public static object? GetEntityValue<T, TAttribute>(this T entity, string propertyName = null) where T : class where TAttribute : class
		{
            PropertyInfo? property;

            if (!string.IsNullOrEmpty(propertyName))
            {
                property = typeof(T).GetProperties()
                                    .Where(x => x.GetCustomAttributes(typeof(TAttribute), true).Any())
                                    .FirstOrDefault(x => x.Name == propertyName);
            }
            else
            {
                property = typeof(T).GetProperties()
                                    .Where(x => x.GetCustomAttributes(typeof(TAttribute), true).Any())
                                    .FirstOrDefault();
            }

            if (property != null)
            {
                return property.GetValue(entity);
            }

            return null;
        }
		private static string GetColumnNames<T>(T entity) where T : class
		{
			var keys = GetProperties(typeof(T), typeof(DataNameAttribute), false)
				       .Where(x => x.GetValue(entity) != null)
				       .Select(x => x.Name);

			return $"{string.Join(", ", keys)}";
		}
		private static string GetParameterNames<T>(T entity, int RowCount = -1) where T : class
		{
            var keys = GetProperties(typeof(T), typeof(DataNameAttribute), false)
                       .Where(x => x.GetValue(entity) != null)
					   .Select(x => RowCount >= 0 ? $"@{RowCount + x.Name}" : $"@{x.Name}");

			return $"{string.Join(", ", keys)}";
		}
		private static string GetUpdateSetClause<T>(T entity) where T : class
		{
            var keys = GetProperties(typeof(T), typeof(DataNameAttribute), false)
				       .Where(x => x.GetValue(entity) != null)
				       .Select(p => $"{p.Name} = @{p.Name}");

			return $"{string.Join(", ", keys)}";
		}


        public static Tuple<string, Dictionary<string, object>>? GetInsertColmAndParams<T>(T entity) where T : class
        {            
            try
            {
                Dictionary<string, object> dict = GetDbParameters<T>(entity);

                string queryInsert = $"({GetColumnNames<T>(entity)}) values ({GetParameterNames<T>(entity)})";

                return new Tuple<string, Dictionary<string, object>>(queryInsert, dict);
            }
            catch
            {
                return null;
            }
        }
        public static Tuple<string, Dictionary<string, object>>? GetInsertColmAndParamList<T>(IEnumerable<T> list)
        {
            try
            {
                Dictionary<string, object> dict = GetDbParametersList<T>(list);

				var propertyInfos = GetProperties(typeof(T), typeof(DataNameAttribute), false);

                var keys = list.SelectMany((item, index) =>
                                                propertyInfos.Where(p => p.GetValue(item) != null)
                                                .Select(p => p.Name)
                                          ).Distinct();

                var valuesList = list.Select((item, index) => {
                    var validProperties = propertyInfos.Where(p => p.GetValue(item) != null);
                    var values = validProperties.Select(p => $"@{index}{p.Name}");
                    return "(" + string.Join(", ", values) + ")";
                });

                string queryInsert = $"({string.Join(", ", keys)}) VALUES {string.Join(", ", valuesList)}";


                return new Tuple<string, Dictionary<string, object>>(queryInsert, dict);
            }
            catch (Exception)
            {
                return null;
            }            
        }
        public static Tuple<string, Dictionary<string, object>>? GetUpdateColmAndParams<T>(T entity, IEnumerable<string> fields) where T : class
        {
            try
            {
                Dictionary<string, object> dict = GetDbParameters<T>(entity, fields);

				string editColm = GetUpdateSetClause<T>(entity);

				return new Tuple<string, Dictionary<string, object>>(editColm, dict);
			}
            catch
            {
                return null;
            }
        }


		public static Dictionary<string, object> GetDbParameters<T>(T entity, IEnumerable<string>? fields = null)
		{
			Dictionary<string, object> dict = new();
			try
			{
				dict = GetProperties(typeof(T), typeof(DataNameAttribute))
					   .Where(x => x.GetValue(entity) != null && ((fields?.Contains(x.Name) ?? true) || x.GetCustomAttributes(typeof(KeyAttribute), true).Any()))
					   .Select(x => new KeyValuePair<string, object>($"@{x.Name}", x.GetValue(entity) ?? DBNull.Value))
					   .ToDictionary(x => x.Key, x => x.Value);

				return dict;
			}
			catch
			{
				return dict;
			}
		}
		public static Dictionary<string, object> GetDbParametersList<T>(IEnumerable<T> list)
		{
            return list.SelectMany((item, index) => GetProperties(typeof(T), typeof(DataNameAttribute),false)
                    .Where(x => x.GetValue(item) != null)
                    .Select(property => new KeyValuePair<string, object>($"@{index + property.Name}", property.GetValue(item) ?? DBNull.Value)))
                    .ToDictionary(x => x.Key, x => x.Value);
        }


		public static List<T> ToList<T>(this DataTable dt) where T : class
		{
			try
			{
				Type type = typeof(T);
				List<T> list = new();

				PropertyInfo[] properties = type.GetProperties();
				foreach (DataRow dr in dt.Rows)
				{
					T obj = Activator.CreateInstance<T>();
					foreach (PropertyInfo pi in properties)
					{
						if (dt.Columns.Contains(pi.Name))
						{
							object value = dr[pi.Name];
							if (value != DBNull.Value)
							{
								if (Nullable.GetUnderlyingType(pi.PropertyType) != null)
									pi.SetValue(obj, Convert.ChangeType(value, Nullable.GetUnderlyingType(pi.PropertyType) ?? pi.PropertyType));
								else
									pi.SetValue(obj, Convert.ChangeType(value, pi.PropertyType));
							}
						}
					}
					list.Add(obj);
				}
				return list;
			}
			catch (Exception ex)
			{
				throw new Exception("Executing DataTable to Class Error: ", ex);
			}
		}
		public static string GetIEnumerablePairs(IEnumerable<object> keys, string format = "{0}", string separator = ", ")
		{
			var pairs = keys.Select(key => string.Format(format, key)).ToList();
			return string.Join(separator, pairs);
		}


		public static List<string> GetChangedFields<T>(T newT, T oldT) where T : class
		{
            try
            {
				if (newT == null || oldT == null) return new();

				List<string> fields = new();

				var propertiesWithAttribute = newT.GetType()
												  .GetProperties()
												  .Where(x => Attribute.IsDefined(x, typeof(DataNameAttribute)) && !x.GetCustomAttributes(typeof(KeyAttribute), true).Any())
												  .ToList();

				foreach (var prop in propertiesWithAttribute)
				{
					if (prop.PropertyType.Namespace == "System.Collections.Generic") continue;

					object oldValue = prop.GetValue(oldT) ?? string.Empty;
					object newValue = prop.GetValue(newT) ?? string.Empty;

					if (prop.PropertyType == typeof(byte[]))
					{
						byte[] oldBytes = oldValue as byte[] ?? new byte[] { 0 };
						byte[] newBytes = newValue as byte[] ?? new byte[] { 0 };

						if (!oldBytes.SequenceEqual(newBytes))
							fields.Add(prop.Name);

					}
					else if (prop.PropertyType == typeof(SecureString))
					{ 
						var oldString = ((SecureString)oldValue).ConvertToUnSecurestring();
						var newString = ((SecureString)newValue).ConvertToUnSecurestring();

						if(!oldString.Equals(newString))
							fields.Add(prop.Name);
					}
					else if (!newValue.Equals(oldValue))
						fields.Add(prop.Name);
				}

				return fields;
			}
            catch
            {
                return new();
            }
		}
        public static SecureString ConvertToSecureString(this string value)
        {
            var secureString = new SecureString();

            if (value.Length > 0)
                value.ToCharArray().ToList().ForEach(x => secureString.AppendChar(x));

            secureString.MakeReadOnly();
            return secureString;
        }
        public static string ConvertToUnSecurestring(this SecureString value)
        {
            var result = Marshal.SecureStringToBSTR(value);
            return Marshal.PtrToStringAuto(result) ?? "";
        }

        public static string CheckAttributeColumn<T>(T Entity, Bisco bisco) where T : class
        {
            try
            {
                string RequiredError = string.Empty;
				string UniqueError = string.Empty;
                string MaxLengthError = string.Empty;
                string IdentityName = string.Empty;
				var IdentValue = 0;
                Dictionary<string, object> dict = new();

				Type type = Entity.GetType();


				///Controls of Required fields
				var required = type.GetProperties()
	                                               .Where(x => x.GetCustomAttribute<RequiredAttribute>() is RequiredAttribute attribute &&
				                                               (x.GetValue(Entity) is null || string.IsNullOrEmpty(x.GetValue(Entity)!.ToString())))
	                                               .Select(s => s.Name + " = " + s.GetCustomAttribute<RequiredAttribute>()?.ErrorMessage).ToArray();

                RequiredError = required.Length > 0 ? $"- {string.Join("\n- ", required)}\n{required.Length} column(s) cannot be null!!!" : "";

                
				///Controls of MaxLength fields
                type.GetProperties()
                    .Where(x => x.GetCustomAttribute(typeof(MaxLengthAttribute)) != null && x.GetValue(Entity) is string value && value.Length > x.GetCustomAttribute<MaxLengthAttribute>()!.Length)
                    .Select(s => s.Name + " = " + s.GetCustomAttribute<MaxLengthAttribute>()?.ErrorMessage).ToList()
                    .ForEach(f =>
                    {
                        MaxLengthError += "- " + f + "\n";
                    });

                MaxLengthError += !string.IsNullOrEmpty(MaxLengthError) ? $"{MaxLengthError.Split('\n').Length - 1} column(s) exceed maximum length!!!" : "";


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
                                            var ctrl = bisco.RunScaler(sqlQuery, dict).Data;
											
                                            if (!string.IsNullOrEmpty(UniqueMsg))
                                                UniqueError += UniqueMsg != null ? ("ErrorMessage : " + UniqueMsg) + "\n" : null;
                                            else
                                                UniqueError += ctrl != null ? "- " + f.Name + " : " + colmValue + "\n" : null;
										}
									});
				UniqueError += !string.IsNullOrEmpty(UniqueError) ? $"{UniqueError.Split('\n').Length-1} column(s) must be Unique. The entered values are available.!!!" : null;

								
                return RequiredError += !string.IsNullOrEmpty(UniqueError) || !string.IsNullOrEmpty(MaxLengthError)
										? (!string.IsNullOrEmpty(RequiredError) ? "\n<-------->\n" + 
																	UniqueError + "\n<-------->\n" + 
												   MaxLengthError : UniqueError + "\n<-------->\n" + MaxLengthError)
										: null;

            }
            catch
            {
                return "Syntax Error";
            }
		}
		#endregion
	}
}