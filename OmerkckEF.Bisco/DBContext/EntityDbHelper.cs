using static OmerkckEF.Biscom.Enums;
using static OmerkckEF.Biscom.Tools;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.Common;
using MySqlX.XDevAPI;

namespace OmerkckEF.Biscom.DBContext
{
	public partial class EntityDbHelper : Bisco
	{
        public Bisco bisco { get; set; }
		private string? QueryString { get; set; }

		public EntityDbHelper(DbServer DbServer) : base(DbServer) => bisco = this;

		#region Mapping Methods /// CRUD = RCUD :)) Read, Create, Update, Delete ///

		#region Read
		public List<T>? GetMappedClass<T>(string? QueryString = null, Dictionary<string, object>? Parameters = null, CommandType CommandType = CommandType.Text) where T : class
		{
			try
			{
				QueryString ??= $"Select * from {ConnSchemaName}.{typeof(T).Name}";

				if (!OpenConnection(ConnSchemaName))return null;

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
							Tools.ParsePrimitive(propertyInfo, entity, propertyValue);
					}

					entities.Add(entity);
				}

				return entities;
			}
			catch (DbException ex)
			{
				throw new Exception("Executing Get Mapped Class Error: " + ex.Message);
			}
		}
		public List<T>? GetMappedClassByWhere<T>(string WhereCond, Dictionary<string, object>? Parameters = null, CommandType CommandType = CommandType.Text) where T : class
		{
			this.QueryString = $"Select * from {ConnSchemaName}.{typeof(T).Name} {WhereCond}";

			return GetMappedClass<T>(QueryString, Parameters, CommandType);
		}
		public List<T>? GetMappedClassBySchema<T>(string schema, string? WhereCond = null, Dictionary<string, object>? Parameters = null, CommandType CommandType = CommandType.Text) where T : class
		{
			this.QueryString = $"Select * from {schema}.{typeof(T).Name} {WhereCond}";
			ConnSchemaName = schema;

			return GetMappedClass<T>(QueryString, Parameters, CommandType);
		}
		#endregion
		#region Create
		private int DoInsert<T>(string schema, T entity, bool getById = false, bool transaction = false) where T : class
		{
			try
			{
				if (entity == null) return -1;

				ConnSchemaName = string.IsNullOrEmpty(schema) ? ConnSchemaName : schema;
				if (!OpenConnection(ConnSchemaName)) return -1;
				using var connection = this.MyConnection;

				var getColmAndParams = GetInsertColmAndParams<T>(entity);
				Dictionary<string, object> parameters = getColmAndParams?.Item2 ?? new();

				var identityColumn = entity.GetKeyAttribute<T>();
				var ReturnIdentity = this.DbServer?.DataBaseType switch
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
					object result = RunScaler(sqlQuery, parameters, transaction) ?? -1;
					if (result == null) return -1;

					return Convert.ToInt32(result);
				}
				else
				{
					int affectedRows = RunNonQuery(sqlQuery, parameters, transaction);
					if (affectedRows <= 0) return -1;

					return affectedRows;
				}
			}
			catch (Exception ex)
			{
				return -1;
				throw new Exception("Executing Do Insert Error: " + ex.Message);
			}
		}

		public int DoMapInsert<T>(string schema, T entity, bool getById = false, bool transaction = false) where T : class
		{
			return DoInsert<T>(schema, entity, getById, transaction);
		}
		public int DoMapInsert<T>(T entity, bool getById = false, bool transaction = false) where T : class
		{
			return DoInsert<T>(ConnSchemaName, entity, getById, transaction);
		}

		public bool DoMultiMapInsert<T>(string schema, IEnumerable<T> entityList) where T : class
		{
			try
			{
				if (entityList is null || !entityList.Any()) return false;

				ConnSchemaName = string.IsNullOrEmpty(schema) ? ConnSchemaName : schema;
				if (!OpenConnection(ConnSchemaName)) return false;
				using var connection = this.MyConnection;

				var getMultiInsertColmParams = GetInsertColmAndParamList<T>(entityList);

				var queryString = $"INSERT INTO {ConnSchemaName}.{typeof(T).Name} {getMultiInsertColmParams?.Item1}";

				return RunNonQuery(queryString, getMultiInsertColmParams?.Item2, true) > 0;
			}
			catch (Exception ex)
			{
				return false;
				throw new Exception("Executing DoMultiMapInsert Error: " + ex.Message);
			}
		}
		public bool DoMultiMapInsert<T>(IEnumerable<T> entityList) where T : class
		{
			return DoMultiMapInsert<T>(ConnSchemaName, entityList);
		}
		#endregion
		#region Update
		private bool DoUpdate<T>(string schema, T entity, IEnumerable<string> fields, bool transaction = false) where T : class
		{
			try
			{
				if (entity == null || !fields.Any()) return false;

				ConnSchemaName = string.IsNullOrEmpty(schema) ? ConnSchemaName : schema;
				if (!OpenConnection(ConnSchemaName)) return false;
				using var connection = this.MyConnection;

				var identityColumn = entity.GetKeyAttribute<T>();

				var _fields = string.Join(",", fields.Select(x => string.Format("{0}=@{0}", x.ToString())).ToList());


				var getUpdateColmParams = GetUpdateColmAndParams<T>(entity, fields);

				var sqlQuery = $"Update {ConnSchemaName}.{typeof(T).Name} set {_fields} where {identityColumn}=@{identityColumn};";

				var result = RunNonQuery(sqlQuery, getUpdateColmParams?.Item2, transaction);

				return result > 0;
			}
			catch
			{
				return false;
			}
		}
		public bool DoMapUpdate<T>(string schema, T currentT, bool transaction = false) where T : class
		{
			var identityValue = typeof(T).GetProperties()
										 .Where(x => x.GetCustomAttributes(typeof(KeyAttribute), true).Any())
										 .Select(s => $"where {s.Name}={s.GetValue(currentT)}").FirstOrDefault();

			var entity = GetMappedClassByWhere<T>(identityValue ?? "")?.FirstOrDefault();

			if (entity == null) return false;

			List<string> fields = GetChangedFields<T>(currentT, entity);

			if (!fields.Any()) return false;

			return DoUpdate<T>(schema, currentT, fields, transaction);

		}
		public bool DoMapUpdate<T>(T currentT, bool transaction = false) where T : class
		{
			var identityValue = typeof(T).GetProperties()
										 .Where(x => x.GetCustomAttributes(typeof(KeyAttribute), true).Any())
										 .Select(s => $"where {s.Name}={s.GetValue(currentT)}").FirstOrDefault();

			var entity = GetMappedClassByWhere<T>(identityValue ?? "")?.FirstOrDefault();

			if (entity == null) return false;

			List<string> fields = GetChangedFields<T>(currentT, entity);

			if (!fields.Any()) return false;

			return DoUpdate<T>(ConnSchemaName, currentT, fields, transaction);
		}
		#endregion
		#region Delete
		public bool DoMapDelete<T>(string schema, T entity, bool transaction = false) where T : class
		{
			try
			{
				if (entity == null) return false;

				ConnSchemaName = string.IsNullOrEmpty(schema) ? ConnSchemaName : schema;
				if (!OpenConnection(ConnSchemaName)) return false;
				using var connection = this.MyConnection;

				var identityColumn = entity.GetKeyAttribute<T>();
				var identityValue = typeof(T).GetProperties()
										 .Where(x => x.GetCustomAttributes(typeof(KeyAttribute), true).Any())
										 .Select(s => s.GetValue(entity)).FirstOrDefault();

				var sqlQuery = $"Delete from {ConnSchemaName}.{typeof(T).Name} where {identityColumn}=@{identityColumn};";

				var result = RunNonQuery(sqlQuery, identityValue?.CreateParameters(identityColumn.ToString() ?? "Id"), transaction);

				return result > 0;
			}
			catch
			{
				return false;
			}
		}
		public bool DoMapDelete<T>(T entity, bool transaction = false) where T : class
		{
			return DoMapDelete<T>(ConnSchemaName, entity, transaction);
		}

		public bool DoMapDeleteAll<T>(string schema, IEnumerable<T> entityList, bool transaction = false) where T : class
		{
			try
			{
				if (entityList is null || !entityList.Any()) return false;

				ConnSchemaName = string.IsNullOrEmpty(schema) ? ConnSchemaName : schema;
				if (!OpenConnection(ConnSchemaName)) return false;
				using var connection = this.MyConnection;

				string identityColumn = entityList.FirstOrDefault()?.GetKeyAttribute<T>().ToString() ?? "";

				string paramsColm = string.Join(", ", entityList.Select((x, index) => $"@{index}{identityColumn}"));

				Dictionary<string, object> dictParams = entityList.SelectMany((item, index) => GetProperties(typeof(T), typeof(KeyAttribute), true)
					.Select(property => new KeyValuePair<string, object>($"@{index + property.Name}", property.GetValue(item) ?? DBNull.Value)))
					.ToDictionary(x => x.Key, x => x.Value);

				var sqlQuery = $"Delete from {ConnSchemaName}.{typeof(T).Name} where {identityColumn} IN ({paramsColm});";

				var result = RunNonQuery(sqlQuery, dictParams, transaction);

				return result > 0;
			}
			catch
			{
				return false;
			}
		}
		public bool DoMapDeleteAll<T>(IEnumerable<T> entityList, bool transaction = false) where T : class
		{
			return DoMapDeleteAll(ConnSchemaName, entityList, transaction);
		}

		public bool DoMapDeleteWithField<T>(string schema, string fieldName, object fieldValue, bool transaction = false) where T : class
		{
			try
			{
				if (string.IsNullOrEmpty(fieldName) || fieldValue.In("", null)) return false;

				ConnSchemaName = string.IsNullOrEmpty(schema) ? ConnSchemaName : schema;
				if (!OpenConnection(ConnSchemaName)) return false;
				using var connection = this.MyConnection;

				var dataName = typeof(T).GetProperties()
										.FirstOrDefault(x => x.GetCustomAttributes(typeof(DataNameAttribute), true).Any() && x.Name == fieldName)?.Name.ToString();

				if (dataName == null) return false;

				var sqlQuery = $"Delete from {ConnSchemaName}.{typeof(T).Name} where {dataName}=@{dataName};";

				var result = RunNonQuery(sqlQuery, fieldValue?.CreateParameters(dataName.ToString() ?? "Id"), transaction);

				return result > 0;
			}
			catch
			{
				return false;
			}
		}
		public bool DoMapDeleteWithField<T>(string fieldName, object fieldValue, bool transaction = false) where T : class
		{
			return DoMapDeleteWithField<T>(ConnSchemaName, fieldName, fieldValue, transaction);
		}

		public bool DoMapDeleteCompositeTable<T>(string schema, Dictionary<string, object> parameters, bool transaction = false) where T : class
		{
			try
			{
				if (parameters == null || parameters.Count == 0) return false;

				ConnSchemaName = string.IsNullOrEmpty(schema) ? ConnSchemaName : schema;
				if (!OpenConnection(ConnSchemaName)) return false;
				using var connection = this.MyConnection;

				var WhereClause = parameters.Select(x => $"{x.Key.Replace("@", "")}=@{x.Key.Replace("@", "")}").ToList();

				var sqlQuery = $"Delete from {ConnSchemaName}.{typeof(T).Name} where {string.Join(" and ", WhereClause)};";

				var result = RunNonQuery(sqlQuery, parameters, transaction);

				return result > 0;
			}
			catch
			{
				return false;
			}
		}
		public bool DoMapDeleteCompositeTable<T>(Dictionary<string, object> parameters, bool transaction = false) where T : class
		{
			return DoMapDeleteCompositeTable<T>(ConnSchemaName, parameters, transaction);
		}

		#endregion

		#endregion


		#region ASYNC Mapping Methods /// CRUD = RCUD :)) Read, Create, Update, Delete ///

		#region Read
		public async Task<List<T>?> GetMapClassAsync<T>(string? QueryString = null, Dictionary<string, object>? Parameters = null, CommandType CommandType = CommandType.Text) where T : class
		{
			try
			{
				QueryString ??= $"Select * from {ConnSchemaName}.{typeof(T).Name}";
				
				
				if (!await OpenConnectionAsync(ConnSchemaName)) return null;

				using var connection = this.MyConnection;
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
							Tools.ParsePrimitive(propertyInfo, entity, propertyValue);
					}

					entities.Add(entity);
				}

				return entities;
			}
			catch (DbException ex)
			{
				throw new Exception("Executing Get Mapped Class Error: " + ex.Message);
			}
		}
		public async Task<T?> GetMapClassByIdAsync<T>(object Id, Dictionary<string, object>? Parameters = null, CommandType CommandType = CommandType.Text) where T : class
		{
			var entity = Activator.CreateInstance<T>();
			this.QueryString = $"Select * from {ConnSchemaName}.{typeof(T).Name} where {entity.GetKeyAttribute<T>()}={Id}";

			var result = await GetMapClassAsync<T>(QueryString, Parameters, CommandType);
			return  result?.FirstOrDefault() ?? null;
		}
		public async Task<List<T>?> GetMapClassByWhereAsync<T>(string WhereCond, Dictionary<string, object>? Parameters = null, CommandType CommandType = CommandType.Text) where T : class
		{
			this.QueryString = $"Select * from {ConnSchemaName}.{typeof(T).Name} {WhereCond}";

			return await GetMapClassAsync<T>(QueryString, Parameters, CommandType);
		}
		public async Task<List<T>?> GetMapClassBySchemaAsync<T>(string schema, string? WhereCond = null, Dictionary<string, object>? Parameters = null, CommandType CommandType = CommandType.Text) where T : class
		{
			this.QueryString = $"Select * from {schema}.{typeof(T).Name} {WhereCond}";
			ConnSchemaName = schema;

			return await GetMapClassAsync<T>(QueryString, Parameters, CommandType);
		}
		#endregion
		#region Create
		private async Task<int> DoInsertAsync<T>(string schema, T entity, bool getById = false, bool transaction = false) where T : class
		{
			try
			{
				if (entity == null) return -1;

				ConnSchemaName = string.IsNullOrEmpty(schema) ? ConnSchemaName : schema;
				if (!await OpenConnectionAsync(ConnSchemaName))	return -1;
				using var connection = this.MyConnection;

				var getColmAndParams = GetInsertColmAndParams<T>(entity);
				Dictionary<string, object> parameters = getColmAndParams?.Item2 ?? new();

				var identityColumn = entity.GetKeyAttribute<T>();
				var ReturnIdentity = this.DbServer?.DataBaseType switch
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
					var result = await RunNonQueryAsync(sqlQuery, parameters, transaction).ConfigureAwait(false);
					if (Convert.ToBoolean(result) == false) return -1;

					return Convert.ToInt32(result);
				}
				else
				{
					int affectedRows = await RunNonQueryAsync(sqlQuery, parameters, transaction);
					if (affectedRows <= 0) return -1;

					return affectedRows;
				}
			}
			catch (Exception ex)
			{
				return -1;
				throw new Exception("Executing Do Insert Error: " + ex.Message);
			}
		}

		public async Task<int> DoMapInsertAsync<T>(string schema, T entity, bool getById = false, bool transaction = false) where T : class
		{
			return await DoInsertAsync<T>(schema, entity, getById, transaction);
		}
		public async Task<int> DoMapInsertAsync<T>(T entity, bool getById = false, bool transaction = false) where T : class
		{
			return await DoInsertAsync<T>(ConnSchemaName, entity, getById, transaction);
		}

		public async Task<bool> DoMultiMapInsertAsync<T>(string schema, IEnumerable<T> entityList) where T : class
		{
			try
			{
				if (entityList is null || !entityList.Any()) return false;

				ConnSchemaName = string.IsNullOrEmpty(schema) ? ConnSchemaName : schema;
				if (!await OpenConnectionAsync(ConnSchemaName)) return false;
				using var connection = this.MyConnection;

				var getMultiInsertColmParams = GetInsertColmAndParamList<T>(entityList);

				var queryString = $"INSERT INTO {ConnSchemaName}.{typeof(T).Name} {getMultiInsertColmParams?.Item1}";

				return await RunNonQueryAsync(queryString, getMultiInsertColmParams?.Item2, true) > 0;
			}
			catch (Exception ex)
			{
				return false;
				throw new Exception("Executing DoMultiMapInsert Error: " + ex.Message);
			}
		}
		public async Task<bool> DoMultiMapInsertAsync<T>(IEnumerable<T> entityList) where T : class
		{
			return await DoMultiMapInsertAsync<T>(ConnSchemaName, entityList);
		}
		#endregion
		#region Update
		private async Task<bool> DoUpdateAsync<T>(string schema, T entity, IEnumerable<string> fields, bool transaction = false) where T : class
		{
			try
			{
				if (entity == null || !fields.Any()) return false;

				ConnSchemaName = string.IsNullOrEmpty(schema) ? ConnSchemaName : schema;
				if (!await OpenConnectionAsync(ConnSchemaName)) return false;
				using var connection = this.MyConnection;

				var identityColumn = entity.GetKeyAttribute<T>();

				var _fields = string.Join(",", fields.Select(x => string.Format("{0}=@{0}", x.ToString())).ToList());


				var getUpdateColmParams = GetUpdateColmAndParams<T>(entity, fields);

				var sqlQuery = $"Update {ConnSchemaName}.{typeof(T).Name} set {_fields} where {identityColumn}=@{identityColumn};";

				var result = await RunNonQueryAsync(sqlQuery, getUpdateColmParams?.Item2, transaction);

				return result > 0;
			}
			catch
			{
				return false;
			}
		}
		public async Task<bool> DoMapUpdateAsync<T>(string schema, T currentT, bool transaction = false) where T : class
		{
			var identityValue = typeof(T).GetProperties()
										 .Where(x => x.GetCustomAttributes(typeof(KeyAttribute), true).Any())
										 .Select(s => $"where {s.Name}={s.GetValue(currentT)}").FirstOrDefault();

			var entity = await GetMapClassByWhereAsync<T>(identityValue ?? "");

			if (entity == null) return false;

			List<string> fields = GetChangedFields<T>(currentT, entity.FirstOrDefault()!);

			if (!fields.Any()) return false;

			return await DoUpdateAsync<T>(schema, currentT, fields, transaction);

		}
		public async Task<bool> DoMapUpdateAsync<T>(T currentT, bool transaction = false) where T : class
		{
			var identityValue = typeof(T).GetProperties()
										 .Where(x => x.GetCustomAttributes(typeof(KeyAttribute), true).Any())
										 .Select(s => $"where {s.Name}={s.GetValue(currentT)}").FirstOrDefault();

			var entity = await GetMapClassByWhereAsync<T>(identityValue ?? "");

			if (entity == null) return false;

			List<string> fields = GetChangedFields<T>(currentT, entity.FirstOrDefault()!);

			if (!fields.Any()) return false;

			return await DoUpdateAsync<T>(ConnSchemaName, currentT, fields, transaction);
		}
		#endregion
		#region Delete
		public async Task<bool> DoMapDeleteAsync<T>(string schema, T entity, bool transaction = false) where T : class
		{
			try
			{
				if (entity == null) return false;

				ConnSchemaName = string.IsNullOrEmpty(schema) ? ConnSchemaName : schema;
				if (!await OpenConnectionAsync(ConnSchemaName)) return false;
				using var connection = this.MyConnection;

				var identityColumn = entity.GetKeyAttribute<T>();
				var identityValue = typeof(T).GetProperties()
										 .Where(x => x.GetCustomAttributes(typeof(KeyAttribute), true).Any())
										 .Select(s => s.GetValue(entity)).FirstOrDefault();

				var sqlQuery = $"Delete from {ConnSchemaName}.{typeof(T).Name} where {identityColumn}=@{identityColumn};";

				var result = await RunNonQueryAsync(sqlQuery, identityValue?.CreateParameters(identityColumn.ToString() ?? "Id"), transaction);

				return result > 0;
			}
			catch
			{
				return false;
			}
		}
		public async Task<bool> DoMapDeleteAsync<T>(T entity, bool transaction = false) where T : class
		{
			return await DoMapDeleteAsync<T>(ConnSchemaName, entity, transaction);
		}

		public async Task<bool> DoMapDeleteAllAsync<T>(string schema, IEnumerable<T> entityList, bool transaction = false) where T : class
		{
			try
			{
				if (entityList is null || !entityList.Any()) return false;

				ConnSchemaName = string.IsNullOrEmpty(schema) ? ConnSchemaName : schema;
				if (!await OpenConnectionAsync(ConnSchemaName)) return false;
				using var connection = this.MyConnection;

				string identityColumn = entityList.FirstOrDefault()?.GetKeyAttribute<T>().ToString() ?? "";

				string paramsColm = string.Join(", ", entityList.Select((x, index) => $"@{index}{identityColumn}"));

				Dictionary<string, object> dictParams = entityList.SelectMany((item, index) => GetProperties(typeof(T), typeof(KeyAttribute), true)
					.Select(property => new KeyValuePair<string, object>($"@{index + property.Name}", property.GetValue(item) ?? DBNull.Value)))
					.ToDictionary(x => x.Key, x => x.Value);

				var sqlQuery = $"Delete from {ConnSchemaName}.{typeof(T).Name} where {identityColumn} IN ({paramsColm});";

				var result = await RunNonQueryAsync(sqlQuery, dictParams, transaction);

				return result > 0;
			}
			catch
			{
				return false;
			}
		}
		public async Task<bool> DoMapDeleteAllAsync<T>(IEnumerable<T> entityList, bool transaction = false) where T : class
		{
			return await DoMapDeleteAllAsync(ConnSchemaName, entityList, transaction);
		}

		public async Task<bool> DoMapDeleteWithFieldAsync<T>(string schema, string fieldName, object fieldValue, bool transaction = false) where T : class
		{
			try
			{
				if (string.IsNullOrEmpty(fieldName) || fieldValue.In("", null)) return false;

				ConnSchemaName = string.IsNullOrEmpty(schema) ? ConnSchemaName : schema;
				if (!await OpenConnectionAsync(ConnSchemaName)) return false;
				using var connection = this.MyConnection;

				var dataName = typeof(T).GetProperties()
										.FirstOrDefault(x => x.GetCustomAttributes(typeof(DataNameAttribute), true).Any() && x.Name == fieldName)?.Name.ToString();

				if (dataName == null) return false;

				var sqlQuery = $"Delete from {ConnSchemaName}.{typeof(T).Name} where {dataName}=@{dataName};";

				var result = await RunNonQueryAsync(sqlQuery, fieldValue?.CreateParameters(dataName.ToString() ?? "Id"), transaction);

				return result > 0;
			}
			catch
			{
				return false;
			}
		}
		public async Task<bool> DoMapDeleteWithFieldAsync<T>(string fieldName, object fieldValue, bool transaction = false) where T : class
		{
			return await DoMapDeleteWithFieldAsync<T>(ConnSchemaName, fieldName, fieldValue, transaction);
		}

		public async Task<bool> DoMapDeleteCompositeTableAsync<T>(string schema, Dictionary<string, object> parameters, bool transaction = false) where T : class
		{
			try
			{
				if (parameters == null || parameters.Count == 0) return false;

				ConnSchemaName = string.IsNullOrEmpty(schema) ? ConnSchemaName : schema;
				if (!await OpenConnectionAsync(ConnSchemaName)) return false;
				using var connection = this.MyConnection;

				var WhereClause = parameters.Select(x => $"{x.Key.Replace("@", "")}=@{x.Key.Replace("@", "")}").ToList();

				var sqlQuery = $"Delete from {ConnSchemaName}.{typeof(T).Name} where {string.Join(" and ", WhereClause)};";

				var result = await RunNonQueryAsync(sqlQuery, parameters, transaction);

				return result > 0;
			}
			catch
			{
				return false;
			}
		}
		public async Task<bool> DoMapDeleteCompositeTableAsync<T>(Dictionary<string, object> parameters, bool transaction = false) where T : class
		{
			return await DoMapDeleteCompositeTableAsync<T>(ConnSchemaName, parameters, transaction);
		}

		#endregion

		#endregion
	}
}