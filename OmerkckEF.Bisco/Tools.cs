﻿using OmerkckEF.Biscom.DBContext;
using SharpCompress.Common;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.Common;
using System.Reflection;
using static Dapper.SqlMapper;
using static MongoDB.Driver.WriteConcern;

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
        public static object GetKeyAttribute<T>(this T obj) where T : class
		{
            return typeof(T).GetProperties().Where(x => x.GetCustomAttributes(typeof(KeyAttribute), true).Any())
                                            .Select(p => p.Name).FirstOrDefault()!;
		}



		public static string GetColumnNames<T>() where T : class
		{
			var keys = GetProperties(typeof(T), typeof(DataNameAttribute), false).Select(x => x.Name);

			return $"{string.Join(", ", keys)}";
		}
		public static string GetParameterNames<T>(int RowCount = -1) where T : class
		{
            var keys = GetProperties(typeof(T), typeof(DataNameAttribute), false)
                       .Select(x => RowCount >= 0 ? $"@{RowCount + x.Name}" : $"@{x.Name}");

			return $"{string.Join(", ", keys)}";
		}
		public static string GetUpdateSetClause<T>() where T : class
		{
            var keys = GetProperties(typeof(T), typeof(DataNameAttribute), false)
                       .Select(p => $"{p.Name} = @{p.Name}");

			return $"{string.Join(", ", keys)}";
		}


        public static Tuple<string, Dictionary<string, object>>? GetInsertColumnAndParameter<T>(T entity)
        {            
            try
            {
                Dictionary<string, object> dict = new();
                dict = GetProperties(typeof(T), typeof(DataNameAttribute), false)
                       .Where(x => x.GetValue(entity) != null)
                       .Select(x => new KeyValuePair<string, object>($"@{x.Name}", x.GetValue(entity) ?? DBNull.Value))
                       .ToDictionary(x => x.Key, x => x.Value);

                var keys = GetProperties(typeof(T), typeof(DataNameAttribute), false)
                           .Where(x => x.GetValue(entity) != null)
                           .Select(x => x.Name);

                string queryInsert = $"({string.Join(", ", keys)}) values (@{string.Join(", @", keys)})";

                return new Tuple<string, Dictionary<string, object>>(queryInsert, dict);
            }
            catch
            {
                return null;
            }
        }
        public static Tuple<string, Dictionary<string, object>>? GetInsertColumnAndParameterList<T>(IEnumerable<T> list)
        {
            try
            {
                Dictionary<string, object> dict = new();
                dict = list.SelectMany((item, index) => GetProperties(typeof(T), typeof(DataNameAttribute), false)
                           .Where(x => x.GetValue(item) != null)
                           .Select(property => new KeyValuePair<string, object>($"@{index + property.Name}", property.GetValue(item) ?? DBNull.Value)))
                           .ToDictionary(x => x.Key, x => x.Value);


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


        public static Dictionary<string, object> GetDbParametersList<T>(IEnumerable<T> list)
		{
            return list.SelectMany((item, index) => GetProperties(typeof(T), typeof(DataNameAttribute),false)
                    .Where(x => x.GetValue(item) != null)
                    .Select(property => new KeyValuePair<string, object>($"@{index + property.Name}", property.GetValue(item) ?? DBNull.Value)))
                    .ToDictionary(x => x.Key, x => x.Value);
        }
        public static Dictionary<string, object> GetDbParameters<T>(T entity)
        {
            Dictionary<string, object> dict = new();
            try
            {
                dict = GetProperties(typeof(T), typeof(DataNameAttribute))
                       .Where(x => x.GetValue(entity) != null)
                       .Select(x => new KeyValuePair<string, object>($"@{x.Name}", x.GetValue(entity) ?? DBNull.Value))
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
                string RequiredError = string.Empty;
				string UniqueError = string.Empty;
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

                                            if (!string.IsNullOrEmpty(UniqueMsg))
                                                UniqueError += UniqueMsg != null ? ("ErrorMessage : " + UniqueMsg) + "\n" : null;
                                            else
                                                UniqueError += ctrl != null ? "- " + f.Name + " : " + colmValue + "\n" : null;
											
										}
									});
				UniqueError += !string.IsNullOrEmpty(UniqueError) ? $"{UniqueError.Split('\n').Length-1} column(s) must be Unique. The entered values are available.!!!" : null;


				return RequiredError += !string.IsNullOrEmpty(UniqueError) ? "\n<----------------->\n" + UniqueError : null;
            }
            catch
            {
                return "Syntax Error";
            }
		}
		#endregion
	}
}