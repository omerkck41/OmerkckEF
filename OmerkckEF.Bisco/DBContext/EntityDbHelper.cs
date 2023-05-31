﻿using OmerkckEF.Biscom.ToolKit;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using static OmerkckEF.Biscom.ToolKit.Enums;
using static OmerkckEF.Biscom.ToolKit.Tools;

namespace OmerkckEF.Biscom.DBContext
{
	public class EntityDbHelper : Bisco
	{
        public Bisco bisco { get; set; }
		private string? QueryString { get; set; }


		public EntityDbHelper(DbServer DbServer) : base(DbServer) => bisco = this;


		#region Mapping Methods /// CRUD = RCUD :)) Read, Create, Update, Delete ///

		#region Read		
		public Result<List<T>> GetMappedClass<T>(string? QueryString = null, Dictionary<string, object>? Parameters = null, string? schema = null, CommandType CommandType = CommandType.Text) where T : class
		{
			try
			{
				if (!OpenConnection(schema)) return new Result<List<T>> { IsSuccess = false, Message = "The connection couldn't be opened or created." };

				QueryString ??= $"Select * from {ConnSchemaName}.{typeof(T).Name}";

				using var connection = MyConnection;
				using var command = ExeCommand(QueryString, Parameters, CommandType);
				using var reader = command.ExecuteReader();
				var entities = new List<T>();

				while (reader.Read())
				{
					var entity = Activator.CreateInstance<T>();

					for (int i = 0; i < reader.FieldCount; i++)
					{
						var propertyName = reader.GetName(i);
						var propertyValue = reader.GetValue(i);
						var propertyInfo = typeof(T).GetProperty(propertyName);
						if (propertyName != null && propertyValue != null && propertyInfo != null)
							ParsePrimitive(propertyInfo, entity, propertyValue);
					}

					entities.Add(entity);
				}

				return new Result<List<T>> { IsSuccess = true, Data = entities };
			}
			catch (DbException ex)
			{
				CloseConnection();
				return new Result<List<T>> { IsSuccess = false, Message = $"Executing Get Mapped Class Error: {ex.GetType().FullName}: {ex.Message}" };
			}
		}
		public Result<List<T>> GetMappedClass<T>(Expression<Func<T, bool>> filter) where T : class
		{
			QueryString = $"Select * from {ConnSchemaName}.{typeof(T).Name} where {filter.ConvertExpressionToQueryString()}";

			return GetMappedClass<T>(QueryString);
		}
		public Result<T> GetMappedClassById<T>(object Id, Dictionary<string, object>? Parameters = null, CommandType CommandType = CommandType.Text) where T : class
		{
			var entity = Activator.CreateInstance<T>();
			QueryString = $"Select * from {ConnSchemaName}.{typeof(T).Name} where {entity.GetKeyAttribute<T>()}={Id}";

			var exeResult = GetMappedClass<T>(QueryString, Parameters,null, CommandType);
			if (exeResult.IsSuccess)
				return new Result<T> { IsSuccess = true, Data = exeResult.Data?.FirstOrDefault() };

			return new Result<T> { IsSuccess = false, Message = "The data is incorrect or not found" };
		}
		public Result<List<T>> GetMappedClassByWhere<T>(string WhereCond, Dictionary<string, object>? Parameters = null, CommandType CommandType = CommandType.Text) where T : class
		{
			QueryString = $"Select * from {ConnSchemaName}.{typeof(T).Name} {WhereCond}";

			return GetMappedClass<T>(QueryString, Parameters, null, CommandType);
		}
		public Result<List<T>> GetMappedClassBySchema<T>(string schema, string? WhereCond = null, Dictionary<string, object>? Parameters = null, CommandType CommandType = CommandType.Text) where T : class
		{
			QueryString = $"Select * from {schema}.{typeof(T).Name} {WhereCond}";

			return GetMappedClass<T>(QueryString, Parameters, schema, CommandType);
		}
		#endregion
		#region Create
		private Result<int> DoInsert<T>(string? schema, T entity, bool getById = false, bool transaction = false) where T : class
		{
			try
			{
				if (entity == null) return new Result<int> { IsSuccess = false, Message = "Entity Null" };

				
				if (!OpenConnection(schema)) return new Result<int> { IsSuccess = false, Message = "The connection couldn't be opened or created." };
				using var connection = MyConnection;

				var getColmAndParams = GetInsertColmAndParams<T>(entity);
				Dictionary<string, object> parameters = getColmAndParams?.Item2 ?? new();

				var identityColumn = entity.GetKeyAttribute<T>();
				var ReturnIdentity = DbServer?.DataBaseType switch
				{
					DataBaseType.MySql => "; SELECT @@Identity;",
					DataBaseType.Sql => "; SELECT SCOPE_IDENTITY();",
					DataBaseType.Oracle => $" RETURNING {identityColumn} INTO :new_id;",
					DataBaseType.PostgreSQL => "; SELECT LASTVAL();",
					DataBaseType.None => "; SELECT @@Identity;",
					null => "; SELECT @@Identity;",
					_ => "; SELECT @@Identity;",
				};
				var sqlQuery = $"Insert Into {ConnSchemaName}.{typeof(T).Name} {getColmAndParams?.Item1}";


				if (getById)
				{
					var exeResult = RunScaler(sqlQuery, parameters, transaction);
					if (exeResult.IsSuccess == false) return new Result<int> { IsSuccess = false, Message = "Database DoInset RunScaler error." };

					return new Result<int> { IsSuccess = true, Data = exeResult.Data?.MyToInt() ?? 0 };
				}
				else
				{
					var affectedRows = RunNonQuery(sqlQuery, parameters, transaction);
					if (affectedRows.IsSuccess == false) return new Result<int> { IsSuccess = false, Message = "Database DoInset RunNonQuery error." };

					return new Result<int> { IsSuccess = true, Data = affectedRows.Data.MyToInt() ?? 0 };
				}
			}
			catch (Exception ex)
			{
				CloseConnection();
				return new Result<int> { IsSuccess = false, Message = $"Executing DoInsert Class Error: {ex.GetType().FullName}: {ex.Message}" };
			}
		}

		public Result<int> DoMapInsert<T>(string? schema, T entity, bool getById = false, bool transaction = false) where T : class
		{
			return DoInsert<T>(schema, entity, getById, transaction);
		}
		public Result<int> DoMapInsert<T>(T entity, bool getById = false, bool transaction = false) where T : class
		{
			return DoInsert<T>(null, entity, getById, transaction);
		}

		public Result<bool> DoMultiMapInsert<T>(string? schema, IEnumerable<T> entityList) where T : class
		{
			try
			{
				if (entityList is null || !entityList.Any()) return new Result<bool> { IsSuccess = false, Message = "entityList Null or Count = 0" };

				
				if (!OpenConnection(schema)) return new Result<bool> { IsSuccess = false, Message = "The connection couldn't be opened or created." };

				using var connection = MyConnection;

				var getMultiInsertColmParams = GetInsertColmAndParamList<T>(entityList);

				var queryString = $"INSERT INTO {ConnSchemaName}.{typeof(T).Name} {getMultiInsertColmParams?.Item1}";

				var exeResult = RunNonQuery(queryString, getMultiInsertColmParams?.Item2, true);
				if (exeResult.IsSuccess == false) return new Result<bool> { IsSuccess = false, Message = "Database DoMultiMapInsert RunNonQuery error." };

				return new Result<bool> { IsSuccess = true, Data = exeResult.Data > 0 };
			}
			catch (Exception ex)
			{
				CloseConnection();
				return new Result<bool> { IsSuccess = false, Message = $"Executing DoMultiMapInsert Class Error: {ex.GetType().FullName}: {ex.Message}" };
			}
		}
		public Result<bool> DoMultiMapInsert<T>(IEnumerable<T> entityList) where T : class
		{
			return DoMultiMapInsert<T>(null, entityList);
		}
		#endregion
		#region Update
		private Result<bool> DoUpdate<T>(string? schema, T entity, IEnumerable<string> fields, bool transaction = false) where T : class
		{
			try
			{
				if (entity == null || !fields.Any()) return new Result<bool> { IsSuccess = false, Message = "Entity or Fields Null" };

				if (!OpenConnection(schema)) return new Result<bool> { IsSuccess = false, Message = "The connection couldn't be opened or created." };

				using var connection = MyConnection;

				var identityColumn = entity.GetKeyAttribute<T>();

				var _fields = string.Join(",", fields.Select(x => string.Format("{0}=@{0}", x.ToString())).ToList());


				var getUpdateColmParams = GetUpdateColmAndParams<T>(entity, fields);

				var sqlQuery = $"Update {ConnSchemaName}.{typeof(T).Name} set {_fields} where {identityColumn}=@{identityColumn};";

				var exeResult = RunNonQuery(sqlQuery, getUpdateColmParams?.Item2, transaction);

				if (exeResult.IsSuccess == false) return new Result<bool> { IsSuccess = false, Message = "Database DoUpdate RunNonQuery error." };

				return new Result<bool> { IsSuccess = true, Data = exeResult.Data > 0 };
			}
			catch (Exception ex)
			{
				CloseConnection();
				return new Result<bool> { IsSuccess = false, Message = $"Executing DoUpdate Class Error: {ex.GetType().FullName}: {ex.Message}" };
			}
		}
		public Result<bool> DoMapUpdate<T>(string? schema, T currentT, bool transaction = false) where T : class
		{
			var identityValue = typeof(T).GetProperties()
										 .Where(x => x.GetCustomAttributes(typeof(KeyAttribute), true).Any())
										 .Select(s => $"where {s.Name}={s.GetValue(currentT)}").FirstOrDefault();

			var entity = GetMappedClassByWhere<T>(identityValue ?? "").Data?.FirstOrDefault();

			if (entity == null) return new Result<bool> { IsSuccess = false, Message = "Entity Null" };

			List<string> fields = GetChangedFields<T>(currentT, entity);

			if (!fields.Any()) return new Result<bool> { IsSuccess = false, Message = "Fields Null" };

			return DoUpdate<T>(schema, currentT, fields, transaction);
		}
		public Result<bool> DoMapUpdate<T>(T currentT, bool transaction = false) where T : class
		{
			var identityValue = typeof(T).GetProperties()
										 .Where(x => x.GetCustomAttributes(typeof(KeyAttribute), true).Any())
										 .Select(s => $"where {s.Name}={s.GetValue(currentT)}").FirstOrDefault();

			var entity = GetMappedClassByWhere<T>(identityValue ?? "").Data?.FirstOrDefault();

			if (entity == null) return new Result<bool> { IsSuccess = false, Message = "Entity Null" };

			List<string> fields = GetChangedFields<T>(currentT, entity);

			if (!fields.Any()) return new Result<bool> { IsSuccess = false, Message = "Fields Null" };

			return DoUpdate<T>(null, currentT, fields, transaction);
		}
		#endregion
		#region Delete
		public Result<bool> DoMapDelete<T>(string? schema, T entity, bool transaction = false) where T : class
		{
			try
			{
				if (entity == null) return new Result<bool> { IsSuccess = false, Message = "Entity Null" };

				if (!OpenConnection(schema)) return new Result<bool> { IsSuccess = false, Message = "The connection couldn't be opened or created." };

				using var connection = MyConnection;

				var identityColumn = entity.GetKeyAttribute<T>();
				var identityValue = typeof(T).GetProperties()
										 .Where(x => x.GetCustomAttributes(typeof(KeyAttribute), true).Any())
										 .Select(s => s.GetValue(entity)).FirstOrDefault();

				var sqlQuery = $"Delete from {ConnSchemaName}.{typeof(T).Name} where {identityColumn}=@{identityColumn};";

				var exeResult = RunNonQuery(sqlQuery, identityValue?.CreateParameters(identityColumn.ToString() ?? "Id"), transaction);

				if (exeResult.IsSuccess == false) return new Result<bool> { IsSuccess = false, Message = "Database DoMapDelete RunNonQuery error." };

				return new Result<bool> { IsSuccess = true, Data = exeResult.Data > 0 };
			}
			catch (Exception ex)
			{
				CloseConnection();
				return new Result<bool> { IsSuccess = false, Message = $"Executing DoMapDelete Class Error: {ex.GetType().FullName}: {ex.Message}" };
			}
		}
		public Result<bool> DoMapDelete<T>(T entity, bool transaction = false) where T : class
		{
			return DoMapDelete<T>(null, entity, transaction);
		}

		public Result<bool> DoMapDelete<T>(Expression<Func<T, bool>> filter) where T : class
		{
			try
			{
				if (filter == null) return new Result<bool> { IsSuccess = false, Message = "Filter not found." };

				if (!OpenConnection()) return new Result<bool> { IsSuccess = false, Message = "The connection couldn't be opened or created." };

				using var connection = MyConnection;

				var WhereClause = filter.ConvertExpressionToQueryString();

				var sqlQuery = $"Delete from {ConnSchemaName}.{typeof(T).Name} where {WhereClause};";

				var exeResult = RunNonQuery(sqlQuery);

				if (exeResult.IsSuccess == false) return new Result<bool> { IsSuccess = false, Message = "Database DoMapDeleteFilter RunNonQuery error." };

				return new Result<bool> { IsSuccess = true, Data = exeResult.Data > 0 };
			}
			catch (Exception ex)
			{
				CloseConnection();
				return new Result<bool> { IsSuccess = false, Message = $"Executing DoMapDeleteFilter Class Error: {ex.GetType().FullName}: {ex.Message}" };
			}
		}

		public Result<bool> DoMapDeleteAll<T>(string? schema, IEnumerable<T> entityList, bool transaction = false) where T : class
		{
			try
			{
				if (entityList is null || !entityList.Any()) return new Result<bool> { IsSuccess = false, Message = "Entity or Fields Null" };

				if (!OpenConnection(schema)) return new Result<bool> { IsSuccess = false, Message = "The connection couldn't be opened or created." };

				using var connection = MyConnection;

				string identityColumn = entityList.FirstOrDefault()?.GetKeyAttribute<T>().ToString() ?? "";

				string paramsColm = string.Join(", ", entityList.Select((x, index) => $"@{index}{identityColumn}"));

				Dictionary<string, object> dictParams = entityList.SelectMany((item, index) => GetProperties(typeof(T), typeof(KeyAttribute), true)
					.Select(property => new KeyValuePair<string, object>($"@{index + property.Name}", property.GetValue(item) ?? DBNull.Value)))
					.ToDictionary(x => x.Key, x => x.Value);

				var sqlQuery = $"Delete from {ConnSchemaName}.{typeof(T).Name} where {identityColumn} IN ({paramsColm});";

				var exeResult = RunNonQuery(sqlQuery, dictParams, transaction);

				if (exeResult.IsSuccess == false) return new Result<bool> { IsSuccess = false, Message = "Database DoMapDeleteAll RunNonQuery error." };

				return new Result<bool> { IsSuccess = true, Data = exeResult.Data > 0 };
			}
			catch (Exception ex)
			{
				CloseConnection();
				return new Result<bool> { IsSuccess = false, Message = $"Executing DoMapDeleteAll Class Error: {ex.GetType().FullName}: {ex.Message}" };
			}
		}
		public Result<bool> DoMapDeleteAll<T>(IEnumerable<T> entityList, bool transaction = false) where T : class
		{
			return DoMapDeleteAll(null, entityList, transaction);
		}

		public Result<bool> DoMapDeleteWithField<T>(string? schema, string fieldName, object fieldValue, bool transaction = false) where T : class
		{
			try
			{
				if (string.IsNullOrEmpty(fieldName) || fieldValue.In("", null)) return new Result<bool> { IsSuccess = false, Message = "Entity or Fields Null" };

				if (!OpenConnection(schema)) return new Result<bool> { IsSuccess = false, Message = "The connection couldn't be opened or created." };

				using var connection = MyConnection;

				var dataName = typeof(T).GetProperties()
										.FirstOrDefault(x => x.GetCustomAttributes(typeof(DataNameAttribute), true).Any() && x.Name == fieldName)?.Name.ToString();

				if (dataName == null) return new Result<bool> { IsSuccess = true, Message = "Fields not found." };

				var sqlQuery = $"Delete from {ConnSchemaName}.{typeof(T).Name} where {dataName}=@{dataName};";

				var exeResult = RunNonQuery(sqlQuery, fieldValue?.CreateParameters(dataName.ToString() ?? "Id"), transaction);

				if (exeResult.IsSuccess == false) return new Result<bool> { IsSuccess = false, Message = "Database DoMapDeleteWithField RunNonQuery error." };

				return new Result<bool> { IsSuccess = true, Data = exeResult.Data > 0 };
			}
			catch (Exception ex)
			{
				CloseConnection();
				return new Result<bool> { IsSuccess = false, Message = $"Executing DoMapDeleteWithField Class Error: {ex.GetType().FullName}: {ex.Message}" };
			}
		}
		public Result<bool> DoMapDeleteWithField<T>(string fieldName, object fieldValue, bool transaction = false) where T : class
		{
			return DoMapDeleteWithField<T>(null, fieldName, fieldValue, transaction);
		}

		public Result<bool> DoMapDeleteCompositeTable<T>(string? schema, Dictionary<string, object> parameters, bool transaction = false) where T : class
		{
			try
			{
				if (parameters == null || parameters.Count == 0) return new Result<bool> { IsSuccess = false, Message = "Parameters not found." };

				if (!OpenConnection(schema)) return new Result<bool> { IsSuccess = false, Message = "The connection couldn't be opened or created." };

				using var connection = MyConnection;

				var WhereClause = parameters.Select(x => $"{x.Key.Replace("@", "")}=@{x.Key.Replace("@", "")}").ToList();

				var sqlQuery = $"Delete from {ConnSchemaName}.{typeof(T).Name} where {string.Join(" and ", WhereClause)};";

				var exeResult = RunNonQuery(sqlQuery, parameters, transaction);

				if (exeResult.IsSuccess == false) return new Result<bool> { IsSuccess = false, Message = "Database DoMapDeleteWithField RunNonQuery error." };

				return new Result<bool> { IsSuccess = true, Data = exeResult.Data > 0 };
			}
			catch (Exception ex)
			{
				CloseConnection();
				return new Result<bool> { IsSuccess = false, Message = $"Executing DoMapDeleteCompositeTable Class Error: {ex.GetType().FullName}: {ex.Message}" };
			}
		}
		public Result<bool> DoMapDeleteCompositeTable<T>(Dictionary<string, object> parameters, bool transaction = false) where T : class
		{
			return DoMapDeleteCompositeTable<T>(null, parameters, transaction);
		}

		#endregion

		#endregion


		#region ASYNC Mapping Methods /// CRUD = RCUD :)) Read, Create, Update, Delete ///

		#region Read
		public async Task<Result<List<T>>> GetMapClassAsync<T>(string? QueryString = null, Dictionary<string, object>? Parameters = null, CommandType CommandType = CommandType.Text) where T : class
		{
			try
			{
				QueryString ??= $"Select * from {ConnSchemaName}.{typeof(T).Name}";
				
				
				if (!await OpenConnectionAsync(ConnSchemaName)) return new Result<List<T>> { IsSuccess = false, Message = "The connection couldn't be opened or created." };

				using var connection = MyConnection;
				using var command = ExeCommand(QueryString, Parameters, CommandType);
				using var reader = await command.ExecuteReaderAsync();
				var entities = new List<T>();

				while (await reader.ReadAsync())
				{
					var entity = Activator.CreateInstance<T>();

					for (int i = 0; i < reader.FieldCount; i++)
					{
						var propertyName = reader.GetName(i);
						var propertyValue = reader.GetValue(i);
						var propertyInfo = typeof(T).GetProperty(propertyName);
						if (propertyName != null && propertyValue != null && propertyInfo != null)
							ParsePrimitive(propertyInfo, entity, propertyValue);
					}

					entities.Add(entity);
				}

				return new Result<List<T>> { IsSuccess = true, Data = entities };
			}
			catch (Exception ex)
			{
				CloseConnection();
				return new Result<List<T>> { IsSuccess = false, Message = $"Executing GetMapClassAsync Class Error: {ex.GetType().FullName}: {ex.Message}" };
			}
		}
		public async Task<Result<List<T>>> GetMapClassAsync<T>(Expression<Func<T, bool>> filter) where T : class
		{
			QueryString = $"Select * from {ConnSchemaName}.{typeof(T).Name} where {filter.ConvertExpressionToQueryString()}";

			return await GetMapClassAsync<T>(QueryString);
		}
		public async Task<Result<T>> GetMapClassByIdAsync<T>(object Id, Dictionary<string, object>? Parameters = null, CommandType CommandType = CommandType.Text) where T : class
		{
			var entity = Activator.CreateInstance<T>();
			QueryString = $"Select * from {ConnSchemaName}.{typeof(T).Name} where {entity.GetKeyAttribute<T>()}={Id}";

			var exeResult = await GetMapClassAsync<T>(QueryString, Parameters, CommandType);
			if (exeResult.IsSuccess)
				return new Result<T> { IsSuccess = true, Data = exeResult.Data?.FirstOrDefault() };

			return new Result<T> { IsSuccess = false, Message = "The data is incorrect or not found" };
		}
		public async Task<Result<List<T>>> GetMapClassByWhereAsync<T>(string WhereCond, Dictionary<string, object>? Parameters = null, CommandType CommandType = CommandType.Text) where T : class
		{
			QueryString = $"Select * from {ConnSchemaName}.{typeof(T).Name} {WhereCond}";

			return await GetMapClassAsync<T>(QueryString, Parameters, CommandType);
		}
		public async Task<Result<List<T>>> GetMapClassBySchemaAsync<T>(string schema, string? WhereCond = null, Dictionary<string, object>? Parameters = null, CommandType CommandType = CommandType.Text) where T : class
		{
			QueryString = $"Select * from {schema}.{typeof(T).Name} {WhereCond}";
			ConnSchemaName = schema;

			return await GetMapClassAsync<T>(QueryString, Parameters, CommandType);
		}
		#endregion
		#region Create
		private async Task<Result<int>> DoInsertAsync<T>(string? schema, T entity, bool getById = false, bool transaction = false) where T : class
		{
			try
			{
				if (entity == null) return new Result<int> { IsSuccess = false, Message = "Entity Null" };

				if (!await OpenConnectionAsync(schema)) return new Result<int> { IsSuccess = false, Message = "The connection couldn't be opened or created." };

				using var connection = MyConnection;

				var getColmAndParams = GetInsertColmAndParams<T>(entity);
				Dictionary<string, object> parameters = getColmAndParams?.Item2 ?? new();

				var identityColumn = entity.GetKeyAttribute<T>();
				var ReturnIdentity = DbServer?.DataBaseType switch
				{
					DataBaseType.MySql => "; SELECT @@Identity;",
					DataBaseType.Sql => "; SELECT SCOPE_IDENTITY();",
					DataBaseType.Oracle => $" RETURNING {identityColumn} INTO :new_id;",
					DataBaseType.PostgreSQL => "; SELECT LASTVAL();",
					DataBaseType.None => "; SELECT @@Identity;",
					null => "; SELECT @@Identity;",
					_ => "; SELECT @@Identity;",
				};
				var sqlQuery = $"Insert Into {ConnSchemaName}.{typeof(T).Name} {getColmAndParams?.Item1}";


				if (getById)
				{
					var exeResult = await RunScalerAsync(sqlQuery, parameters, transaction).ConfigureAwait(false);
					if (exeResult.IsSuccess == false) return new Result<int> { IsSuccess = false, Message = "Database DoInsertAsync RunScalerAsync error." };

					return new Result<int> { IsSuccess = true, Data = exeResult.Data?.MyToInt() ?? 0 };
				}
				else
				{
					var affectedRows = await RunNonQueryAsync(sqlQuery, parameters, transaction);
					if (affectedRows.IsSuccess == false) return new Result<int> { IsSuccess = false, Message = "Database DoInsertAsync RunNonQueryAsync error." };

					return new Result<int> { IsSuccess = true, Data = affectedRows.Data.MyToInt() ?? 0 };
				}
			}
			catch (Exception ex)
			{
				CloseConnection();
				return new Result<int> { IsSuccess = false, Message = $"Executing DoInsertAsync Class Error: {ex.GetType().FullName}: {ex.Message}" };
			}
		}

		public async Task<Result<int>> DoMapInsertAsync<T>(string? schema, T entity, bool getById = false, bool transaction = false) where T : class
		{
			return await DoInsertAsync<T>(schema, entity, getById, transaction);
		}
		public async Task<Result<int>> DoMapInsertAsync<T>(T entity, bool getById = false, bool transaction = false) where T : class
		{
			return await DoInsertAsync<T>(null, entity, getById, transaction);
		}

		public async Task<Result<bool>> DoMultiMapInsertAsync<T>(string? schema, IEnumerable<T> entityList) where T : class
		{
			try
			{
				if (entityList is null || !entityList.Any()) return new Result<bool> { IsSuccess = false, Message = "entityList Null" };

				if (!await OpenConnectionAsync(schema)) return new Result<bool> { IsSuccess = false, Message = "The connection couldn't be opened or created." };

				using var connection = MyConnection;

				var getMultiInsertColmParams = GetInsertColmAndParamList<T>(entityList);

				var queryString = $"INSERT INTO {ConnSchemaName}.{typeof(T).Name} {getMultiInsertColmParams?.Item1}";

				var exeResult = await RunNonQueryAsync(queryString, getMultiInsertColmParams?.Item2, true);

				if (exeResult.IsSuccess == false) return new Result<bool> { IsSuccess = false, Message = "Database DoMultiMapInsertAsync RunNonQueryAsync error." };

				return new Result<bool> { IsSuccess = true, Data = exeResult.Data > 0 };
			}
			catch (Exception ex)
			{
				CloseConnection();
				return new Result<bool> { IsSuccess = false, Message = $"Executing DoMultiMapInsertAsync Class Error: {ex.GetType().FullName}: {ex.Message}" };
			}
		}
		public async Task<Result<bool>> DoMultiMapInsertAsync<T>(IEnumerable<T> entityList) where T : class
		{
			return await DoMultiMapInsertAsync<T>(null, entityList);
		}
		#endregion
		#region Update
		private async Task<Result<bool>> DoUpdateAsync<T>(string? schema, T entity, IEnumerable<string> fields, bool transaction = false) where T : class
		{
			try
			{
				if (entity == null || !fields.Any()) return new Result<bool> { IsSuccess = false, Message = "Entity or Fields Null" };

				if (!await OpenConnectionAsync(schema)) return new Result<bool> { IsSuccess = false, Message = "The connection couldn't be opened or created." };

				using var connection = MyConnection;

				var identityColumn = entity.GetKeyAttribute<T>();

				var _fields = string.Join(",", fields.Select(x => string.Format("{0}=@{0}", x.ToString())).ToList());


				var getUpdateColmParams = GetUpdateColmAndParams<T>(entity, fields);

				var sqlQuery = $"Update {ConnSchemaName}.{typeof(T).Name} set {_fields} where {identityColumn}=@{identityColumn};";

				var exeResult = await RunNonQueryAsync(sqlQuery, getUpdateColmParams?.Item2, transaction);

				if (exeResult.IsSuccess == false) return new Result<bool> { IsSuccess = false, Message = "Database DoUpdateAsync RunNonQueryAsync error." };

				return new Result<bool> { IsSuccess = true, Data = exeResult.Data > 0 };
			}
			catch (Exception ex)
			{
				CloseConnection();
				return new Result<bool> { IsSuccess = false, Message = $"Executing DoUpdateAsync Class Error: {ex.GetType().FullName}: {ex.Message}" };
			}
		}
		public async Task<Result<bool>> DoMapUpdateAsync<T>(string? schema, T currentT, bool transaction = false) where T : class
		{
			var identityValue = typeof(T).GetProperties()
										 .Where(x => x.GetCustomAttributes(typeof(KeyAttribute), true).Any())
										 .Select(s => $"where {s.Name}={s.GetValue(currentT)}").FirstOrDefault();


			var entity = await GetMapClassByWhereAsync<T>(identityValue ?? "");

			if (entity.IsSuccess == false) return new Result<bool> { IsSuccess = false, Message = "Entity Null" };

			List<string> fields = GetChangedFields<T>(currentT, entity.Data?.FirstOrDefault()!);

			if (!fields.Any()) return new Result<bool> { IsSuccess = false, Message = "Fields Null" };

			return await DoUpdateAsync<T>(schema, currentT, fields, transaction);

		}
		public async Task<Result<bool>> DoMapUpdateAsync<T>(T currentT, bool transaction = false) where T : class
		{
			var identityValue = typeof(T).GetProperties()
										 .Where(x => x.GetCustomAttributes(typeof(KeyAttribute), true).Any())
										 .Select(s => $"where {s.Name}={s.GetValue(currentT)}").FirstOrDefault();

			var entity = await GetMapClassByWhereAsync<T>(identityValue ?? "");

			if (entity.IsSuccess == false) return new Result<bool> { IsSuccess = false, Message = "Entity Null" };

			List<string> fields = GetChangedFields<T>(currentT, entity.Data?.FirstOrDefault()!);

			if (!fields.Any()) return new Result<bool> { IsSuccess = false, Message = "Fields Null" };

			return await DoUpdateAsync<T>(null, currentT, fields, transaction);
		}
		#endregion
		#region Delete
		public async Task<Result<bool>> DoMapDeleteAsync<T>(string? schema, T entity, bool transaction = false) where T : class
		{
			try
			{
				if (entity == null) return new Result<bool> { IsSuccess = false, Message = "Entity Null" };

				if (!await OpenConnectionAsync(schema)) return new Result<bool> { IsSuccess = false, Message = "The connection couldn't be opened or created." };

				using var connection = MyConnection;

				var identityColumn = entity.GetKeyAttribute<T>();
				var identityValue = typeof(T).GetProperties()
										 .Where(x => x.GetCustomAttributes(typeof(KeyAttribute), true).Any())
										 .Select(s => s.GetValue(entity)).FirstOrDefault();

				var sqlQuery = $"Delete from {ConnSchemaName}.{typeof(T).Name} where {identityColumn}=@{identityColumn};";

				var exeResult = await RunNonQueryAsync(sqlQuery, identityValue?.CreateParameters(identityColumn.ToString() ?? "Id"), transaction);

				if (exeResult.IsSuccess == false) return new Result<bool> { IsSuccess = false, Message = "Database DoMapDeleteAsync RunNonQueryAsync error." };

				return new Result<bool> { IsSuccess = true, Data = exeResult.Data > 0 };
			}
			catch (Exception ex)
			{
				CloseConnection();
				return new Result<bool> { IsSuccess = false, Message = $"Executing DoMapDeleteAsync Class Error: {ex.GetType().FullName}: {ex.Message}" };
			}
		}
		public async Task<Result<bool>> DoMapDeleteAsync<T>(T entity, bool transaction = false) where T : class
		{
			return await DoMapDeleteAsync<T>(null, entity, transaction);
		}

		public async Task<Result<bool>> DoMapDeleteAsync<T>(Expression<Func<T, bool>> filter) where T : class
		{
			try
			{
				if (filter == null) return new Result<bool> { IsSuccess = false, Message = "Filter not found." };

				if (!await OpenConnectionAsync()) return new Result<bool> { IsSuccess = false, Message = "The connection couldn't be opened or created." };

				using var connection = MyConnection;

				var WhereClause = filter.ConvertExpressionToQueryString();

				var sqlQuery = $"Delete from {ConnSchemaName}.{typeof(T).Name} where {WhereClause};";

				var exeResult = await RunNonQueryAsync(sqlQuery);

				if (exeResult.IsSuccess == false) return new Result<bool> { IsSuccess = false, Message = "Database DoMapDeleteAsyncFilter RunNonQuery error." };

				return new Result<bool> { IsSuccess = true, Data = exeResult.Data > 0 };
			}
			catch (Exception ex)
			{
				CloseConnection();
				return new Result<bool> { IsSuccess = false, Message = $"Executing DoMapDeleteAsyncFilter Class Error: {ex.GetType().FullName}: {ex.Message}" };
			}
		}

		public async Task<Result<bool>> DoMapDeleteAllAsync<T>(string? schema, IEnumerable<T> entityList, bool transaction = false) where T : class
		{
			try
			{
				if (entityList is null || !entityList.Any()) return new Result<bool> { IsSuccess = false, Message = "EntityList Null" };

				if (!await OpenConnectionAsync(schema)) return new Result<bool> { IsSuccess = false, Message = "The connection couldn't be opened or created." };

				using var connection = MyConnection;

				string identityColumn = entityList.FirstOrDefault()?.GetKeyAttribute<T>().ToString() ?? "";

				string paramsColm = string.Join(", ", entityList.Select((x, index) => $"@{index}{identityColumn}"));

				Dictionary<string, object> dictParams = entityList.SelectMany((item, index) => GetProperties(typeof(T), typeof(KeyAttribute), true)
					.Select(property => new KeyValuePair<string, object>($"@{index + property.Name}", property.GetValue(item) ?? DBNull.Value)))
					.ToDictionary(x => x.Key, x => x.Value);

				var sqlQuery = $"Delete from {ConnSchemaName}.{typeof(T).Name} where {identityColumn} IN ({paramsColm});";

				var exeResult = await RunNonQueryAsync(sqlQuery, dictParams, transaction);

				if (exeResult.IsSuccess == false) return new Result<bool> { IsSuccess = false, Message = "Database DoMapDeleteAllAsync RunNonQueryAsync error." };

				return new Result<bool> { IsSuccess = true, Data = exeResult.Data > 0 };
			}
			catch (Exception ex)
			{
				CloseConnection();
				return new Result<bool> { IsSuccess = false, Message = $"Executing DoMapDeleteAllAsync Class Error: {ex.GetType().FullName}: {ex.Message}" };
			}
		}
		public async Task<Result<bool>> DoMapDeleteAllAsync<T>(IEnumerable<T> entityList, bool transaction = false) where T : class
		{
			return await DoMapDeleteAllAsync(null, entityList, transaction);
		}

		public async Task<Result<bool>> DoMapDeleteWithFieldAsync<T>(string? schema, string fieldName, object fieldValue, bool transaction = false) where T : class
		{
			try
			{
				if (string.IsNullOrEmpty(fieldName) || fieldValue.In("", null)) return new Result<bool> { IsSuccess = false, Message = "fieldName or fieldValue Null" };

				if (!await OpenConnectionAsync(schema)) return new Result<bool> { IsSuccess = false, Message = "The connection couldn't be opened or created." };

				using var connection = MyConnection;

				var dataName = typeof(T).GetProperties()
										.FirstOrDefault(x => x.GetCustomAttributes(typeof(DataNameAttribute), true).Any() && x.Name == fieldName)?.Name.ToString();

				if (dataName == null) return new Result<bool> { IsSuccess = true, Message = "Fields not found." };

				var sqlQuery = $"Delete from {ConnSchemaName}.{typeof(T).Name} where {dataName}=@{dataName};";

				var exeResult = await RunNonQueryAsync(sqlQuery, fieldValue?.CreateParameters(dataName.ToString() ?? "Id"), transaction);

				if (exeResult.IsSuccess == false) return new Result<bool> { IsSuccess = false, Message = "Database DoMapDeleteWithFieldAsync RunNonQueryAsync error." };

				return new Result<bool> { IsSuccess = true, Data = exeResult.Data > 0 };
			}
			catch (Exception ex)
			{
				CloseConnection();
				return new Result<bool> { IsSuccess = false, Message = $"Executing DoMapDeleteWithFieldAsync Class Error: {ex.GetType().FullName}: {ex.Message}" };
			}
		}
		public async Task<Result<bool>> DoMapDeleteWithFieldAsync<T>(string fieldName, object fieldValue, bool transaction = false) where T : class
		{
			return await DoMapDeleteWithFieldAsync<T>(null, fieldName, fieldValue, transaction);
		}

		public async Task<Result<bool>> DoMapDeleteCompositeTableAsync<T>(string? schema, Dictionary<string, object> parameters, bool transaction = false) where T : class
		{
			try
			{
				if (parameters == null || parameters.Count == 0) return new Result<bool> { IsSuccess = false, Message = "Parameters Null" };

				if (!await OpenConnectionAsync(schema)) return new Result<bool> { IsSuccess = false, Message = "The connection couldn't be opened or created." };
				using var connection = MyConnection;

				var WhereClause = parameters.Select(x => $"{x.Key.Replace("@", "")}=@{x.Key.Replace("@", "")}").ToList();

				var sqlQuery = $"Delete from {ConnSchemaName}.{typeof(T).Name} where {string.Join(" and ", WhereClause)};";

				var exeResult = await RunNonQueryAsync(sqlQuery, parameters, transaction);

				if (exeResult.IsSuccess == false) return new Result<bool> { IsSuccess = false, Message = "Database DoMapDeleteCompositeTableAsync RunNonQueryAsync error." };

				return new Result<bool> { IsSuccess = true, Data = exeResult.Data > 0 };
			}
			catch (Exception ex)
			{
				CloseConnection();
				return new Result<bool> { IsSuccess = false, Message = $"Executing DoMapDeleteCompositeTableAsync Class Error: {ex.GetType().FullName}: {ex.Message}" };
			}
		}
		public async Task<Result<bool>> DoMapDeleteCompositeTableAsync<T>(Dictionary<string, object> parameters, bool transaction = false) where T : class
		{
			return await DoMapDeleteCompositeTableAsync<T>(null, parameters, transaction);
		}

		#endregion

		#endregion
	}
}