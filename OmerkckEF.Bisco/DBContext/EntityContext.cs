using OmerkckEF.Biscom.ToolKit;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using static OmerkckEF.Biscom.ToolKit.Enums;
using static OmerkckEF.Biscom.ToolKit.Tools;

namespace OmerkckEF.Biscom.DBContext
{
    public class EntityContext : Bisco
    {
        private string? QueryString { get; set; }


        public EntityContext(DBServer dbServerInfo) : base(dbServerInfo) { }


        #region Mapping Methods /// CRUD = RCUD :)) Read, Create, Update, Delete ///

        #region Read		
        public Result<List<T>> GetMapClass<T>(string? queryString = null, Dictionary<string, object>? parameters = null, string? schema = null, CommandType commandType = CommandType.Text) where T : class
        {
            try
            {
                if (!OpenConnection(schema)) return new Result<List<T>> { IsSuccess = false, Message = "The connection couldn't be opened or created." };

                queryString ??= $"Select * from {schema ??= ConnSchemaName}.{typeof(T).Name}";

                using var connection = MyConnection;
                using var command = ExeCommand(queryString, parameters, commandType);
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
        public Result<List<T>> GetMapClass<T>(Expression<Func<T, bool>> filter) where T : class
        {
            QueryString = $"Select * from {ConnSchemaName}.{typeof(T).Name} where {filter.ConvertExpressionToQueryString()}";

            return GetMapClass<T>(QueryString);
        }
        public Result<T> GetMapClassById<T>(object id, Dictionary<string, object>? parameters = null, CommandType commandType = CommandType.Text) where T : class
        {
            var entity = Activator.CreateInstance<T>();
            QueryString = $"Select * from {ConnSchemaName}.{typeof(T).Name} where {entity.GetKeyAttribute<T>()}={id}";

            var exeResult = GetMapClass<T>(QueryString, parameters, ConnSchemaName, commandType);

            return exeResult.IsSuccess
                ? new Result<T> { IsSuccess = true, Data = exeResult.Data?.FirstOrDefault() }
                : new Result<T> { IsSuccess = false, Message = "The data is incorrect or not found.\n" + exeResult.Message };
        }
        public Result<List<T>> GetMapClassByWhere<T>(string whereCond, Dictionary<string, object>? parameters = null, CommandType commandType = CommandType.Text) where T : class
        {
            QueryString = $"Select * from {ConnSchemaName}.{typeof(T).Name} {whereCond}";

            return GetMapClass<T>(QueryString, parameters, ConnSchemaName, commandType);
        }
        public Result<List<T>> GetMapClassBySchema<T>(string schema, string? whereCond = null, Dictionary<string, object>? parameters = null, CommandType commandType = CommandType.Text) where T : class
        {
            QueryString = $"Select * from {(string.IsNullOrEmpty(schema) ? ConnSchemaName : schema)}.{typeof(T).Name} {whereCond}";

            return GetMapClass<T>(QueryString, parameters, (string.IsNullOrEmpty(schema) ? ConnSchemaName : schema), commandType);
        }
        #endregion
        #region Create
        private Result<int> DoInsert<T>(string? schema, T entity, bool getById = false, bool transaction = false) where T : class
        {
            try
            {
                if (entity == null) return new Result<int> { IsSuccess = false, Message = "Entity Null" };

                string check = CheckAttributeColumn<T>(entity, this);
                if (!string.IsNullOrEmpty(check)) return new Result<int> { IsSuccess = false, Message = check };

                var getColmAndParams = GetInsertColmAndParams<T>(entity);
                Dictionary<string, object> parameters = getColmAndParams?.Item2 ?? new();

                var identityColumn = entity.GetKeyAttribute<T>();
                var ReturnIdentity = DBServer.DBServerInfo?.DBModel switch
                {
                    DataBaseType.MySql => "; SELECT @@Identity;",
                    DataBaseType.Sql => "; SELECT SCOPE_IDENTITY();",
                    DataBaseType.Oracle => $" RETURNING {identityColumn} INTO :new_id;",
                    DataBaseType.PostgreSQL => "; SELECT LASTVAL();",
                    DataBaseType.None => "; SELECT @@Identity;",
                    null => "; SELECT @@Identity;",
                    _ => "; SELECT @@Identity;",
                };
                var sqlQuery = $"Insert Into {schema ??= ConnSchemaName}.{typeof(T).Name} {getColmAndParams?.Item1}";


                if (getById)
                {
                    var exeResult = RunScaler(schema, sqlQuery, parameters, transaction);
                    return !exeResult.IsSuccess
                           ? new Result<int> { IsSuccess = false, Message = "Database DoInsert RunScaler error.\n" + exeResult.Message }
                           : new Result<int> { IsSuccess = true, Data = exeResult.Data?.MyToInt() ?? 0 };
                }
                else
                {
                    var affectedRows = RunNonQuery(schema, sqlQuery, parameters, transaction);
                    return !affectedRows.IsSuccess
                           ? new Result<int> { IsSuccess = false, Message = "Database DoInsert RunNonQuery error.\n" + affectedRows.Message }
                           : new Result<int> { IsSuccess = true, Data = affectedRows.Data.MyToInt() ?? 0 };
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
            return DoInsert<T>(schema ?? ConnSchemaName, entity, getById, transaction);
        }
        public Result<int> DoMapInsert<T>(T entity, bool getById = false, bool transaction = false) where T : class
        {
            return DoInsert<T>(ConnSchemaName, entity, getById, transaction);
        }

        public Result<bool> DoMapMultiInsert<T>(string? schema, IEnumerable<T> entityList) where T : class
        {
            try
            {
                if (entityList is null || !entityList.Any()) return new Result<bool> { IsSuccess = false, Message = "entityList Null or Count = 0" };

                foreach (T entity in entityList)
                {
                    string check = CheckAttributeColumn<T>(entity, this);
                    if (!string.IsNullOrEmpty(check)) return new Result<bool> { IsSuccess = false, Message = "There are problems in the list.\n\n" + check };
                }

                var getMultiInsertColmParams = GetInsertColmAndParamList<T>(entityList);

                var queryString = $"INSERT INTO {schema ??= ConnSchemaName}.{typeof(T).Name} {getMultiInsertColmParams?.Item1}";

                var exeResult = RunNonQuery(schema, queryString, getMultiInsertColmParams?.Item2, true);


                return !exeResult.IsSuccess
                       ? new Result<bool> { IsSuccess = false, Message = "Database DoMultiMapInsert RunNonQuery error.\n" + exeResult.Message }
                       : new Result<bool> { IsSuccess = true, Data = exeResult.Data > 0 };
            }
            catch (Exception ex)
            {
                CloseConnection();
                return new Result<bool> { IsSuccess = false, Message = $"Executing DoMultiMapInsert Class Error: {ex.GetType().FullName}: {ex.Message}" };
            }
        }
        public Result<bool> DoMapMultiInsert<T>(IEnumerable<T> entityList) where T : class
        {
            return DoMapMultiInsert<T>(ConnSchemaName, entityList);
        }
        #endregion
        #region Update
        private Result<bool> DoUpdate<T>(string? schema, T entity, IEnumerable<string> fields, bool transaction = false) where T : class
        {
            try
            {
                if (entity == null || !fields.Any()) return new Result<bool> { IsSuccess = false, Message = "Entity or Fields Null" };

                string check = CheckAttributeColumn<T>(entity, this);
                if (!string.IsNullOrEmpty(check)) return new Result<bool> { IsSuccess = false, Message = check };


                var identityColumn = entity.GetKeyAttribute<T>();
                var _fields = string.Join(",", fields.Select(x => string.Format("{0}=@{0}", x.ToString())).ToList());
                var getUpdateColmParams = GetUpdateColmAndParams<T>(entity, fields);

                var sqlQuery = $"Update {schema ??= ConnSchemaName}.{typeof(T).Name} set {_fields} where {identityColumn}=@{identityColumn};";
                var exeResult = RunNonQuery(schema, sqlQuery, getUpdateColmParams?.Item2, transaction);


                return !exeResult.IsSuccess
                       ? new Result<bool> { IsSuccess = false, Message = "Database DoUpdate RunNonQuery error.\n" + exeResult.Message }
                       : new Result<bool> { IsSuccess = true, Data = exeResult.Data > 0 };
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

            var entity = GetMapClassByWhere<T>(identityValue ?? "").Data?.FirstOrDefault();

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

            var entity = GetMapClassByWhere<T>(identityValue ?? "").Data?.FirstOrDefault();

            if (entity == null) return new Result<bool> { IsSuccess = false, Message = "Entity Null" };

            List<string> fields = GetChangedFields<T>(currentT, entity);

            if (!fields.Any()) return new Result<bool> { IsSuccess = false, Message = "Fields Null" };

            return DoUpdate<T>(ConnSchemaName, currentT, fields, transaction);
        }
        #endregion
        #region Delete
        public Result<bool> DoMapDelete<T>(string? schema, T entity, bool transaction = false) where T : class
        {
            try
            {
                if (entity == null) return new Result<bool> { IsSuccess = false, Message = "Entity Null" };


                var identityColumn = entity.GetKeyAttribute<T>();
                var identityValue = typeof(T).GetProperties()
                                             .Where(x => x.GetCustomAttributes(typeof(KeyAttribute), true).Any())
                                             .Select(s => s.GetValue(entity)).FirstOrDefault();

                if (identityValue == null || (int)identityValue == 0)
                    return new Result<bool> { IsSuccess = false, Message = $"{identityColumn}; Identity Colum not found." };


                var sqlQuery = $"Delete from {schema ??= ConnSchemaName}.{typeof(T).Name} where {identityColumn}=@{identityColumn};";
                var exeResult = RunNonQuery(schema, sqlQuery, identityValue?.CreateParameters(identityColumn.ToString() ?? "Id"), transaction);


                return !exeResult.IsSuccess
                       ? new Result<bool> { IsSuccess = false, Message = "Database DoMapDelete RunNonQuery error.\n" + exeResult.Message }
                       : new Result<bool> { IsSuccess = true, Data = exeResult.Data > 0 };
            }
            catch (Exception ex)
            {
                CloseConnection();
                return new Result<bool> { IsSuccess = false, Message = $"Executing DoMapDelete Class Error: {ex.GetType().FullName}: {ex.Message}" };
            }
        }
        public Result<bool> DoMapDelete<T>(T entity, bool transaction = false) where T : class
        {
            return DoMapDelete<T>(ConnSchemaName, entity, transaction);
        }

        public Result<bool> DoMapDelete<T>(Expression<Func<T, bool>> filter) where T : class
        {
            try
            {
                if (filter == null) return new Result<bool> { IsSuccess = false, Message = "Filter not found." };


                var WhereClause = filter.ConvertExpressionToQueryString();

                var sqlQuery = $"Delete from {ConnSchemaName}.{typeof(T).Name} where {WhereClause};";

                var exeResult = RunNonQuery(sqlQuery);


                return !exeResult.IsSuccess
                       ? new Result<bool> { IsSuccess = false, Message = "Database DoMapDeleteFilter RunNonQuery error.\n" + exeResult.Message }
                       : new Result<bool> { IsSuccess = true, Data = exeResult.Data > 0 };
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


                string identityColumn = entityList.FirstOrDefault()?.GetKeyAttribute<T>().ToString() ?? "";
                string paramsColm = string.Join(", ", entityList.Select((x, index) => $"@{index}{identityColumn}"));

                Dictionary<string, object> dictParams = entityList.SelectMany((item, index) => GetProperties(typeof(T), typeof(KeyAttribute), true)
                    .Select(property => new KeyValuePair<string, object>($"@{index + property.Name}", property.GetValue(item) ?? DBNull.Value)))
                    .ToDictionary(x => x.Key, x => x.Value);

                var sqlQuery = $"Delete from {schema ??= ConnSchemaName}.{typeof(T).Name} where {identityColumn} IN ({paramsColm});";
                var exeResult = RunNonQuery(schema, sqlQuery, dictParams, transaction);


                return !exeResult.IsSuccess
                       ? new Result<bool> { IsSuccess = false, Message = "Database DoMapDeleteAll RunNonQuery error.\n" + exeResult.Message }
                       : new Result<bool> { IsSuccess = true, Data = exeResult.Data > 0 };
            }
            catch (Exception ex)
            {
                CloseConnection();
                return new Result<bool> { IsSuccess = false, Message = $"Executing DoMapDeleteAll Class Error: {ex.GetType().FullName}: {ex.Message}" };
            }
        }
        public Result<bool> DoMapDeleteAll<T>(IEnumerable<T> entityList, bool transaction = false) where T : class
        {
            return DoMapDeleteAll(ConnSchemaName, entityList, transaction);
        }

        public Result<bool> DoMapDeleteWithField<T>(string? schema, string fieldName, object fieldValue, bool transaction = false) where T : class
        {
            try
            {
                if (string.IsNullOrEmpty(fieldName) || fieldValue.In("", null)) return new Result<bool> { IsSuccess = false, Message = "Entity or Fields Null" };


                var dataName = typeof(T).GetProperties()
                                        .FirstOrDefault(x => x.GetCustomAttributes(typeof(DataNameAttribute), true).Any() && x.Name == fieldName)?.Name.ToString();

                if (dataName == null) return new Result<bool> { IsSuccess = true, Message = "Fields not found." };

                var sqlQuery = $"Delete from {schema ??= ConnSchemaName}.{typeof(T).Name} where {dataName}=@{dataName};";
                var exeResult = RunNonQuery(schema, sqlQuery, fieldValue?.CreateParameters(dataName.ToString() ?? "Id"), transaction);

                return !exeResult.IsSuccess
                       ? new Result<bool> { IsSuccess = false, Message = "Database DoMapDeleteWithField RunNonQuery error.\n" + exeResult.Message }
                       : new Result<bool> { IsSuccess = true, Data = exeResult.Data > 0 };
            }
            catch (Exception ex)
            {
                CloseConnection();
                return new Result<bool> { IsSuccess = false, Message = $"Executing DoMapDeleteWithField Class Error: {ex.GetType().FullName}: {ex.Message}" };
            }
        }
        public Result<bool> DoMapDeleteWithField<T>(string fieldName, object fieldValue, bool transaction = false) where T : class
        {
            return DoMapDeleteWithField<T>(ConnSchemaName, fieldName, fieldValue, transaction);
        }

        public Result<bool> DoMapDeleteCompositeTable<T>(string? schema, Dictionary<string, object> parameters, bool transaction = false) where T : class
        {
            try
            {
                if (parameters == null || parameters.Count == 0) return new Result<bool> { IsSuccess = false, Message = "Parameters not found." };

                var WhereClause = parameters.Select(x => $"{x.Key.Replace("@", "")}=@{x.Key.Replace("@", "")}").ToList();
                var sqlQuery = $"Delete from {schema ??= ConnSchemaName}.{typeof(T).Name} where {string.Join(" and ", WhereClause)};";
                var exeResult = RunNonQuery(schema, sqlQuery, parameters, transaction);

                return !exeResult.IsSuccess
                       ? new Result<bool> { IsSuccess = false, Message = "Database DoMapDeleteWithField RunNonQuery error.\n" + exeResult.Message }
                       : new Result<bool> { IsSuccess = true, Data = exeResult.Data > 0 };
            }
            catch (Exception ex)
            {
                CloseConnection();
                return new Result<bool> { IsSuccess = false, Message = $"Executing DoMapDeleteCompositeTable Class Error: {ex.GetType().FullName}: {ex.Message}" };
            }
        }
        public Result<bool> DoMapDeleteCompositeTable<T>(Dictionary<string, object> parameters, bool transaction) where T : class
        {
            return DoMapDeleteCompositeTable<T>(ConnSchemaName, parameters, transaction);
        }

        #endregion

        #endregion


        #region ASYNC Mapping Methods /// CRUD = RCUD :)) Read, Create, Update, Delete ///

        #region Read
        public async Task<Result<List<T>>> GetMapClassAsync<T>(string? queryString = null, Dictionary<string, object>? parameters = null, string? schema = null, CommandType commandType = CommandType.Text) where T : class
        {
            try
            {
                if (!await OpenConnectionAsync(schema)) return new Result<List<T>> { IsSuccess = false, Message = "The connection couldn't be opened or created." };

                queryString ??= $"Select * from {schema ??= ConnSchemaName}.{typeof(T).Name}";

                using var connection = MyConnection;
                using var command = ExeCommand(queryString, parameters, commandType);
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
        public async Task<Result<T>> GetMapClassByIdAsync<T>(object id, Dictionary<string, object>? parameters = null, CommandType commandType = CommandType.Text) where T : class
        {
            var entity = Activator.CreateInstance<T>();
            QueryString = $"Select * from {ConnSchemaName}.{typeof(T).Name} where {entity.GetKeyAttribute<T>()}={id}";

            var exeResult = await GetMapClassAsync<T>(QueryString, parameters, ConnSchemaName, commandType);

            return exeResult.IsSuccess
                ? new Result<T> { IsSuccess = true, Data = exeResult.Data?.FirstOrDefault() }
                : new Result<T> { IsSuccess = false, Message = "The data is incorrect or not found.\n" + exeResult.Message };
        }
        public async Task<Result<List<T>>> GetMapClassByWhereAsync<T>(string whereCond, Dictionary<string, object>? parameters = null, CommandType commandType = CommandType.Text) where T : class
        {
            QueryString = $"Select * from {ConnSchemaName}.{typeof(T).Name} {whereCond}";

            return await GetMapClassAsync<T>(QueryString, parameters, ConnSchemaName, commandType);
        }
        public async Task<Result<List<T>>> GetMapClassBySchemaAsync<T>(string schema, string? whereCond = null, Dictionary<string, object>? parameters = null, CommandType commandType = CommandType.Text) where T : class
        {
            QueryString = $"Select * from {(string.IsNullOrEmpty(schema) ? ConnSchemaName : schema)}.{typeof(T).Name} {whereCond}";

            return await GetMapClassAsync<T>(QueryString, parameters, (string.IsNullOrEmpty(schema) ? ConnSchemaName : schema), commandType);
        }
        #endregion
        #region Create
        private async Task<Result<int>> DoInsertAsync<T>(string? schema, T entity, bool getById = false, bool transaction = false) where T : class
        {
            try
            {
                if (entity == null) return new Result<int> { IsSuccess = false, Message = "Entity Null" };

                string check = CheckAttributeColumn<T>(entity, this);
                if (!string.IsNullOrEmpty(check)) return new Result<int> { IsSuccess = false, Message = check };

                var getColmAndParams = GetInsertColmAndParams<T>(entity);
                Dictionary<string, object> parameters = getColmAndParams?.Item2 ?? new();

                var identityColumn = entity.GetKeyAttribute<T>();
                var ReturnIdentity = DBServer.DBServerInfo?.DBModel switch
                {
                    DataBaseType.MySql => "; SELECT @@Identity;",
                    DataBaseType.Sql => "; SELECT SCOPE_IDENTITY();",
                    DataBaseType.Oracle => $" RETURNING {identityColumn} INTO :new_id;",
                    DataBaseType.PostgreSQL => "; SELECT LASTVAL();",
                    DataBaseType.None => "; SELECT @@Identity;",
                    null => "; SELECT @@Identity;",
                    _ => "; SELECT @@Identity;",
                };
                var sqlQuery = $"Insert Into {schema ??= ConnSchemaName}.{typeof(T).Name} {getColmAndParams?.Item1}";


                if (getById)
                {
                    var exeResult = await RunScalerAsync(schema, sqlQuery, parameters, transaction).ConfigureAwait(false);
                    return !exeResult.IsSuccess
                           ? new Result<int> { IsSuccess = false, Message = "Database DoInsertAsync RunScalerAsync error.\n" + exeResult.Message }
                           : new Result<int> { IsSuccess = true, Data = exeResult.Data?.MyToInt() ?? 0 };
                }
                else
                {
                    var affectedRows = await RunNonQueryAsync(schema, sqlQuery, parameters, transaction);
                    return !affectedRows.IsSuccess
                           ? new Result<int> { IsSuccess = false, Message = "Database DoInsertAsync RunNonQueryAsync error.\n" + affectedRows.Message }
                           : new Result<int> { IsSuccess = true, Data = affectedRows.Data.MyToInt() ?? 0 };
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
            return await DoInsertAsync<T>(schema ?? ConnSchemaName, entity, getById, transaction);
        }
        public async Task<Result<int>> DoMapInsertAsync<T>(T entity, bool getById = false, bool transaction = false) where T : class
        {
            return await DoInsertAsync<T>(ConnSchemaName, entity, getById, transaction);
        }

        public async Task<Result<bool>> DoMapMultiInsertAsync<T>(string? schema, IEnumerable<T> entityList) where T : class
        {
            try
            {
                if (entityList is null || !entityList.Any()) return new Result<bool> { IsSuccess = false, Message = "entityList Null" };

                foreach (T entity in entityList)
                {
                    string check = CheckAttributeColumn<T>(entity, this);
                    if (!string.IsNullOrEmpty(check)) return new Result<bool> { IsSuccess = false, Message = "There are problems in the list.\n\n" + check };
                }

                var getMultiInsertColmParams = GetInsertColmAndParamList<T>(entityList);

                var queryString = $"INSERT INTO {schema ??= ConnSchemaName}.{typeof(T).Name} {getMultiInsertColmParams?.Item1}";

                var exeResult = await RunNonQueryAsync(schema, queryString, getMultiInsertColmParams?.Item2, true);

                return !exeResult.IsSuccess
                       ? new Result<bool> { IsSuccess = false, Message = "Database DoMultiMapInsertAsync RunNonQueryAsync error.\n" + exeResult.Message }
                       : new Result<bool> { IsSuccess = true, Data = exeResult.Data > 0 };
            }
            catch (Exception ex)
            {
                CloseConnection();
                return new Result<bool> { IsSuccess = false, Message = $"Executing DoMultiMapInsertAsync Class Error: {ex.GetType().FullName}: {ex.Message}" };
            }
        }
        public async Task<Result<bool>> DoMapMultiInsertAsync<T>(IEnumerable<T> entityList) where T : class
        {
            return await DoMapMultiInsertAsync<T>(ConnSchemaName, entityList);
        }
        #endregion
        #region Update
        private async Task<Result<bool>> DoUpdateAsync<T>(string? schema, T entity, IEnumerable<string> fields, bool transaction = false) where T : class
        {
            try
            {
                if (entity == null || !fields.Any()) return new Result<bool> { IsSuccess = false, Message = "Entity or Fields Null" };

                string check = CheckAttributeColumn<T>(entity, this);
                if (!string.IsNullOrEmpty(check)) return new Result<bool> { IsSuccess = false, Message = check };

                var identityColumn = entity.GetKeyAttribute<T>();
                var _fields = string.Join(",", fields.Select(x => string.Format("{0}=@{0}", x.ToString())).ToList());
                var getUpdateColmParams = GetUpdateColmAndParams<T>(entity, fields);

                var sqlQuery = $"Update {schema ??= ConnSchemaName}.{typeof(T).Name} set {_fields} where {identityColumn}=@{identityColumn};";
                var exeResult = await RunNonQueryAsync(schema, sqlQuery, getUpdateColmParams?.Item2, transaction);

                return !exeResult.IsSuccess
                       ? new Result<bool> { IsSuccess = false, Message = "Database DoUpdateAsync RunNonQueryAsync error.\n" + exeResult.Message }
                       : new Result<bool> { IsSuccess = true, Data = exeResult.Data > 0 };
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

            return await DoUpdateAsync<T>(ConnSchemaName, currentT, fields, transaction);
        }
        #endregion
        #region Delete
        public async Task<Result<bool>> DoMapDeleteAsync<T>(string? schema, T entity, bool transaction = false) where T : class
        {
            try
            {
                if (entity == null) return new Result<bool> { IsSuccess = false, Message = "Entity Null" };


                var identityColumn = entity.GetKeyAttribute<T>();
                var identityValue = typeof(T).GetProperties()
                                         .Where(x => x.GetCustomAttributes(typeof(KeyAttribute), true).Any())
                                         .Select(s => s.GetValue(entity)).FirstOrDefault();

                if (identityValue == null || (int)identityValue == 0)
                    return new Result<bool> { IsSuccess = false, Message = $"{identityColumn}; Identity Colum not found." };


                var sqlQuery = $"Delete from {schema ??= ConnSchemaName}.{typeof(T).Name} where {identityColumn}=@{identityColumn};";
                var exeResult = await RunNonQueryAsync(schema, sqlQuery, identityValue?.CreateParameters(identityColumn.ToString() ?? "Id"), transaction);

                return !exeResult.IsSuccess
                       ? new Result<bool> { IsSuccess = false, Message = "Database DoMapDeleteAsync RunNonQueryAsync error.\n" + exeResult.Message }
                       : new Result<bool> { IsSuccess = true, Data = exeResult.Data > 0 };
            }
            catch (Exception ex)
            {
                CloseConnection();
                return new Result<bool> { IsSuccess = false, Message = $"Executing DoMapDeleteAsync Class Error: {ex.GetType().FullName}: {ex.Message}" };
            }
        }
        public async Task<Result<bool>> DoMapDeleteAsync<T>(T entity, bool transaction) where T : class
        {
            return await DoMapDeleteAsync<T>(ConnSchemaName, entity, transaction);
        }

        public async Task<Result<bool>> DoMapDeleteAsync<T>(Expression<Func<T, bool>> filter) where T : class
        {
            try
            {
                if (filter == null) return new Result<bool> { IsSuccess = false, Message = "Filter not found." };


                var WhereClause = filter.ConvertExpressionToQueryString();

                var sqlQuery = $"Delete from {ConnSchemaName}.{typeof(T).Name} where {WhereClause};";

                var exeResult = await RunNonQueryAsync(sqlQuery);


                return !exeResult.IsSuccess
                       ? new Result<bool> { IsSuccess = false, Message = "Database DoMapDeleteAsyncFilter RunNonQuery error.\n" + exeResult.Message }
                       : new Result<bool> { IsSuccess = true, Data = exeResult.Data > 0 };
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


                string identityColumn = entityList.FirstOrDefault()?.GetKeyAttribute<T>().ToString() ?? "";
                string paramsColm = string.Join(", ", entityList.Select((x, index) => $"@{index}{identityColumn}"));

                Dictionary<string, object> dictParams = entityList.SelectMany((item, index) => GetProperties(typeof(T), typeof(KeyAttribute), true)
                    .Select(property => new KeyValuePair<string, object>($"@{index + property.Name}", property.GetValue(item) ?? DBNull.Value)))
                    .ToDictionary(x => x.Key, x => x.Value);

                var sqlQuery = $"Delete from {schema ??= ConnSchemaName}.{typeof(T).Name} where {identityColumn} IN ({paramsColm});";
                var exeResult = await RunNonQueryAsync(schema, sqlQuery, dictParams, transaction);

                return !exeResult.IsSuccess
                    ? new Result<bool> { IsSuccess = false, Message = "Database DoMapDeleteAllAsync RunNonQueryAsync error.\n" + exeResult.Message }
                    : new Result<bool> { IsSuccess = true, Data = exeResult.Data > 0 };
            }
            catch (Exception ex)
            {
                CloseConnection();
                return new Result<bool> { IsSuccess = false, Message = $"Executing DoMapDeleteAllAsync Class Error: {ex.GetType().FullName}: {ex.Message}" };
            }
        }
        public async Task<Result<bool>> DoMapDeleteAllAsync<T>(IEnumerable<T> entityList, bool transaction = false) where T : class
        {
            return await DoMapDeleteAllAsync(ConnSchemaName, entityList, transaction);
        }

        public async Task<Result<bool>> DoMapDeleteWithFieldAsync<T>(string? schema, string fieldName, object fieldValue, bool transaction = false) where T : class
        {
            try
            {
                if (string.IsNullOrEmpty(fieldName) || fieldValue.In("", null)) return new Result<bool> { IsSuccess = false, Message = "fieldName or fieldValue Null" };


                var dataName = typeof(T).GetProperties()
                                        .FirstOrDefault(x => x.GetCustomAttributes(typeof(DataNameAttribute), true).Any() && x.Name == fieldName)?.Name.ToString();

                if (dataName == null) return new Result<bool> { IsSuccess = true, Message = "Fields not found." };

                var sqlQuery = $"Delete from {schema ??= ConnSchemaName}.{typeof(T).Name} where {dataName}=@{dataName};";
                var exeResult = await RunNonQueryAsync(schema, sqlQuery, fieldValue?.CreateParameters(dataName.ToString() ?? "Id"), transaction);

                return !exeResult.IsSuccess
                       ? new Result<bool> { IsSuccess = false, Message = "Database DoMapDeleteWithFieldAsync RunNonQueryAsync error.\n" + exeResult.Message }
                       : new Result<bool> { IsSuccess = true, Data = exeResult.Data > 0 };
            }
            catch (Exception ex)
            {
                CloseConnection();
                return new Result<bool> { IsSuccess = false, Message = $"Executing DoMapDeleteWithFieldAsync Class Error: {ex.GetType().FullName}: {ex.Message}" };
            }
        }
        public async Task<Result<bool>> DoMapDeleteWithFieldAsync<T>(string fieldName, object fieldValue, bool transaction = false) where T : class
        {
            return await DoMapDeleteWithFieldAsync<T>(ConnSchemaName, fieldName, fieldValue, transaction);
        }

        public async Task<Result<bool>> DoMapDeleteCompositeTableAsync<T>(string? schema, Dictionary<string, object> parameters, bool transaction = false) where T : class
        {
            try
            {
                if (parameters == null || parameters.Count == 0) return new Result<bool> { IsSuccess = false, Message = "Parameters Null" };

                var WhereClause = parameters.Select(x => $"{x.Key.Replace("@", "")}=@{x.Key.Replace("@", "")}").ToList();
                var sqlQuery = $"Delete from {schema ??= ConnSchemaName}.{typeof(T).Name} where {string.Join(" and ", WhereClause)};";
                var exeResult = await RunNonQueryAsync(schema, sqlQuery, parameters, transaction);

                return !exeResult.IsSuccess
                       ? new Result<bool> { IsSuccess = false, Message = "Database DoMapDeleteCompositeTableAsync RunNonQueryAsync error.\n" + exeResult.Message }
                       : new Result<bool> { IsSuccess = true, Data = exeResult.Data > 0 };
            }
            catch (Exception ex)
            {
                CloseConnection();
                return new Result<bool> { IsSuccess = false, Message = $"Executing DoMapDeleteCompositeTableAsync Class Error: {ex.GetType().FullName}: {ex.Message}" };
            }
        }
        public async Task<Result<bool>> DoMapDeleteCompositeTableAsync<T>(Dictionary<string, object> parameters, bool transaction = false) where T : class
        {
            return await DoMapDeleteCompositeTableAsync<T>(ConnSchemaName, parameters, transaction);
        }

        #endregion

        #endregion
    }
}