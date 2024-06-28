using OmerkckEF.Biscom.ToolKit;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Text;
using static OmerkckEF.Biscom.ToolKit.Enums;
using static OmerkckEF.Biscom.ToolKit.Tools;

namespace OmerkckEF.Biscom.DBContext
{
    public class EntityContext(DBServer dbServerInfo) : Bisco(dbServerInfo)
    {
        private readonly DBServer dbServerInfo = dbServerInfo;

        private string? QueryString { get; set; }


        #region Mapping Methods /// CRUD = RCUD :)) Read, Create, Update, Delete ///

        #region Read		
        public Result<List<T>> GetMapClass<T>(string? queryString = null, Dictionary<string, object>? parameters = null, string? schema = null, CommandType commandType = CommandType.Text) where T : class
        {
            try
            {
                if (!OpenConnection(schema)) return new Result<List<T>> { IsSuccess = false, Message = "The connection couldn't be opened or created." };

                queryString ??= $"Select * from {schema ??= DBSchemaName}.{typeof(T).Name}";

                using var command = ExeCommand(queryString, parameters, commandType);
                using var reader = command.ExecuteReader(CommandBehavior.CloseConnection);
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
            if (filter != null)
                QueryString = $"Select * from {DBSchemaName}.{typeof(T).Name} where {filter.ConvertExpressionToQueryString()}";
            else
                QueryString = $"Select * from {DBSchemaName}.{typeof(T).Name};";

            return GetMapClass<T>(QueryString);
        }
        public Result<T> GetMapClassById<T>(object id) where T : class
        {
            var entity = Activator.CreateInstance<T>();
            QueryString = $"Select * from {DBSchemaName}.{typeof(T).Name} where {entity.GetKeyAttribute<T>()}={id}";

            var exeResult = GetMapClass<T>(QueryString);

            return exeResult.IsSuccess
                ? new Result<T> { IsSuccess = true, Data = exeResult.Data?.FirstOrDefault() }
                : new Result<T> { IsSuccess = false, Message = "The data is incorrect or not found.\n" + exeResult.Message };
        }
        public Result<List<T>> GetMapClassByWhere<T>(string whereCond, Dictionary<string, object>? parameters = null, CommandType commandType = CommandType.Text) where T : class
        {
            QueryString = $"Select * from {DBSchemaName}.{typeof(T).Name} {whereCond}";

            return GetMapClass<T>(QueryString, parameters, DBSchemaName, commandType);
        }
        public Result<List<T>> GetMapClassBySchema<T>(string schema, string? whereCond = null, Dictionary<string, object>? parameters = null, CommandType commandType = CommandType.Text) where T : class
        {
            QueryString = $"Select * from {(string.IsNullOrEmpty(schema) ? DBSchemaName : schema)}.{typeof(T).Name} {whereCond}";

            return GetMapClass<T>(QueryString, parameters, (string.IsNullOrEmpty(schema) ? DBSchemaName : schema), commandType);
        }
        #endregion

        #region Create
        private Result<int> DoInsert<T>(string? schema, T entity, bool getById = false, bool transaction = false) where T : class
        {
            try
            {
                if (entity == null) return new Result<int> { IsSuccess = false, Message = "Entity Null" };

                string check = CheckAttributeColumn<T>(entity, this, schema);
                if (!string.IsNullOrEmpty(check)) return new Result<int> { IsSuccess = false, Message = check };

                var getColmAndParams = GetInsertColmAndParams<T>(entity);
                Dictionary<string, object> parameters = getColmAndParams?.Item2 ?? [];

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
                var sqlQuery = $"Insert Into {schema ??= DBSchemaName}.{typeof(T).Name} {getColmAndParams?.Item1}" + ReturnIdentity;


                if (getById)
                {
                    var exeResult = RunScaler(schema, sqlQuery, parameters);
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
            return DoInsert<T>(schema ?? DBSchemaName, entity, getById, transaction);
        }
        public Result<int> DoMapInsert<T>(T entity, bool getById = false, bool transaction = false) where T : class
        {
            return DoInsert<T>(DBSchemaName, entity, getById, transaction);
        }

        public Result<bool> DoMapMultiInsert<T>(string? schema, IEnumerable<T> entityList) where T : class
        {
            try
            {
                if (entityList is null || !entityList.Any()) return new Result<bool> { IsSuccess = false, Message = "entityList Null or Count = 0" };

                foreach (T entity in entityList)
                {
                    string check = CheckAttributeColumn<T>(entity, this, schema);
                    if (!string.IsNullOrEmpty(check)) return new Result<bool> { IsSuccess = false, Message = "There are problems in the list.\n\n" + check };
                }

                var getMultiInsertColmParams = GetInsertColmAndParamList<T>(entityList);

                var queryString = $"INSERT INTO {schema ??= DBSchemaName}.{typeof(T).Name} {getMultiInsertColmParams?.Item1}";

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
            return DoMapMultiInsert<T>(DBSchemaName, entityList);
        }
        #endregion

        #region Update
        private Result<bool> DoUpdate<T>(string? schema, T entity, IEnumerable<string> fields, bool transaction = false) where T : class
        {
            try
            {
                if (entity == null || !fields.Any()) return new Result<bool> { IsSuccess = false, Message = "Entity or Fields Null" };

                string check = CheckAttributeColumn<T>(entity, this, schema);
                if (!string.IsNullOrEmpty(check)) return new Result<bool> { IsSuccess = false, Message = check };


                var identityColumn = entity.GetKeyAttribute<T>();
                var _fields = string.Join(",", fields.Select(x => string.Format("{0}=@{0}", x.ToString())).ToList());
                var getUpdateColmParams = GetUpdateColmAndParams<T>(entity, fields);

                var sqlQuery = $"Update {schema ??= DBSchemaName}.{typeof(T).Name} set {_fields} where {identityColumn}=@{identityColumn};";
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
                                         .Where(x => x.GetCustomAttributes(typeof(KeyAttribute), true).Length > 0)
                                         .Select(s => $"where {s.Name}={s.GetValue(currentT)}").FirstOrDefault();

            if (identityValue == null) return new Result<bool> { IsSuccess = false, Message = "Identity Value Null" };

            var entity = GetMapClassByWhere<T>(identityValue).Data?.FirstOrDefault();

            if (entity == null) return new Result<bool> { IsSuccess = false, Message = "Entity Null" };

            List<string> fields = GetChangedFields<T>(currentT, entity);

            if (fields.Count <= 0) return new Result<bool> { IsSuccess = false, Message = "Fields Null" };

            return DoUpdate<T>(schema, currentT, fields, transaction);
        }
        public Result<bool> DoMapUpdate<T>(T currentT, bool transaction = false) where T : class
        {
            return DoMapUpdate<T>(DBSchemaName, currentT, transaction);
        }
        public Result<bool> DoMapUpdateCompositeTable<T>(string? schema, T currentT, params object[] fieldValue) where T : class
        {
            //If there is no KeyAttribute, it is a Composite table.
            string identityColm = (string)currentT.GetKeyAttribute<T>();
            string? whereClause = string.Empty;
            schema ??= DBSchemaName;

            if (identityColm == null)
            {
                object[] keys = GetProperties(typeof(T), typeof(DataNameAttribute), false)
                                .Where(x => x.GetValue(currentT) != null)
                                .Select(x => (object)x.Name)
                                .ToArray();

                if (keys.Length <= 0 || fieldValue.Length <= 0)
                    return new Result<bool> { IsSuccess = false, Message = "Fields or Columns Null" };

                whereClause = $"where {keys[0]}={fieldValue[0]} and {keys[1]}={fieldValue[1]}";
            }

            var setField = typeof(T).GetProperties()
                                         .Where(x => x.GetCustomAttributes(typeof(DataNameAttribute), true).Length > 0)
                                         .Select(s => $"{s.Name}={s.GetValue(currentT)}");

            var sqlQuery = $"Update {schema}.{typeof(T).Name} set {string.Join(" , ", setField)} {whereClause};";
            var exeResult = RunNonQuery(schema, sqlQuery);


            return !exeResult.IsSuccess
                   ? new Result<bool> { IsSuccess = false, Message = "Database DoMapUpdateCompositeTable RunNonQuery error.\n" + exeResult.Message }
                   : new Result<bool> { IsSuccess = true, Data = exeResult.Data > 0 };
        }
        public Result<bool> DoMapUpdateCompositeTable<T>(T currentT, params object[] fieldValue) where T : class
        {
            return DoMapUpdateCompositeTable<T>(null, currentT, fieldValue);
        }
        public Result<bool> DoUpdateQuery<T>(Dictionary<string, object> prms, Expression<Func<T, bool>> filter)
        {
            try
            {
                if (prms == null) return new Result<bool> { IsSuccess = false, Message = "Parameters Null" };

                var setClause = string.Join(" , ", prms.Select(x => $"{x.Key.Replace("@", "")}=@{x.Key.Replace("@", "")}").ToList());

                var sqlQuery = $"Update {DBSchemaName}.{typeof(T).Name} set {setClause} where {filter.ConvertExpressionToQueryString()}";
                var exeResult = RunNonQuery(sqlQuery, prms);

                return !exeResult.IsSuccess
                       ? new Result<bool> { IsSuccess = false, Message = "Database DoUpdateQuery RunNonQuery error.\n" + exeResult.Message }
                       : new Result<bool> { IsSuccess = true, Data = exeResult.Data > 0 };
            }
            catch (Exception ex)
            {
                CloseConnection();
                return new Result<bool> { IsSuccess = false, Message = $"Executing DoUpdateQuery Class Error: {ex.GetType().FullName}: {ex.Message}" };
            }
        }
        #endregion

        #region Delete
        public Result<bool> DoMapDelete<T>(string? schema, T entity, bool transaction = false) where T : class
        {
            try
            {
                if (entity == null) return new Result<bool> { IsSuccess = false, Message = "Entity Null" };


                var identityColumn = entity.GetKeyAttribute<T>();
                //var identityValue = typeof(T).GetProperties()
                //                             .Where(x => x.GetCustomAttributes(typeof(KeyAttribute), true).Any())
                //                             .Select(s => s.GetValue(entity)).FirstOrDefault();
                var identityValue = entity.GetEntityValue<T, KeyAttribute>();

                if (identityValue == null || (int)identityValue == 0)
                    return new Result<bool> { IsSuccess = false, Message = $"{identityColumn}; Identity Colum not found." };


                var sqlQuery = $"Delete from {schema ??= DBSchemaName}.{typeof(T).Name} where {identityColumn}=@{identityColumn};";
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
            return DoMapDelete<T>(DBSchemaName, entity, transaction);
        }

        public Result<bool> DoMapDelete<T>(Expression<Func<T, bool>> filter) where T : class
        {
            try
            {
                if (filter == null) return new Result<bool> { IsSuccess = false, Message = "Filter not found." };


                var WhereClause = filter.ConvertExpressionToQueryString();

                var sqlQuery = $"Delete from {DBSchemaName}.{typeof(T).Name} where {WhereClause};";

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

                var sqlQuery = $"Delete from {schema ??= DBSchemaName}.{typeof(T).Name} where {identityColumn} IN ({paramsColm});";
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
            return DoMapDeleteAll(DBSchemaName, entityList, transaction);
        }

        public Result<bool> DoMapDeleteWithField<T>(string? schema, string fieldName, object fieldValue, bool transaction = false) where T : class
        {
            try
            {
                if (string.IsNullOrEmpty(fieldName) || fieldValue.In("", null)) return new Result<bool> { IsSuccess = false, Message = "Entity or Fields Null" };


                var dataName = typeof(T).GetProperties()
                                        .FirstOrDefault(x => x.GetCustomAttributes(typeof(DataNameAttribute), true).Length > 0 && x.Name == fieldName)?.Name.ToString();

                if (dataName == null) return new Result<bool> { IsSuccess = true, Message = "Fields not found." };

                var sqlQuery = $"Delete from {schema ??= DBSchemaName}.{typeof(T).Name} where {dataName}=@{dataName};";
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
            return DoMapDeleteWithField<T>(DBSchemaName, fieldName, fieldValue, transaction);
        }

        public Result<bool> DoMapDeleteCompositeTable<T>(string? schema, Dictionary<string, object> parameters, bool transaction = false) where T : class
        {
            try
            {
                if (parameters == null || parameters.Count == 0) return new Result<bool> { IsSuccess = false, Message = "Parameters not found." };

                var WhereClause = parameters.Select(x => $"{x.Key.Replace("@", "")}=@{x.Key.Replace("@", "")}").ToList();
                var sqlQuery = $"Delete from {schema ??= DBSchemaName}.{typeof(T).Name} where {string.Join(" and ", WhereClause)};";
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
            return DoMapDeleteCompositeTable<T>(DBSchemaName, parameters, transaction);
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

                queryString ??= $"Select * from {schema ??= DBSchemaName}.{typeof(T).Name}";

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
            QueryString = $"Select * from {DBSchemaName}.{typeof(T).Name} where {filter.ConvertExpressionToQueryString()}";

            return await GetMapClassAsync<T>(QueryString);
        }
        public async Task<Result<T>> GetMapClassByIdAsync<T>(object id) where T : class
        {
            var entity = Activator.CreateInstance<T>();
            QueryString = $"Select * from {DBSchemaName}.{typeof(T).Name} where {entity.GetKeyAttribute<T>()}={id}";

            var exeResult = await GetMapClassAsync<T>(QueryString);

            return exeResult.IsSuccess
                ? new Result<T> { IsSuccess = true, Data = exeResult.Data?.FirstOrDefault() }
                : new Result<T> { IsSuccess = false, Message = "The data is incorrect or not found.\n" + exeResult.Message };
        }
        public async Task<Result<List<T>>> GetMapClassByWhereAsync<T>(string whereCond, Dictionary<string, object>? parameters = null, CommandType commandType = CommandType.Text) where T : class
        {
            QueryString = $"Select * from {DBSchemaName}.{typeof(T).Name} {whereCond}";

            return await GetMapClassAsync<T>(QueryString, parameters, DBSchemaName, commandType);
        }
        public async Task<Result<List<T>>> GetMapClassBySchemaAsync<T>(string schema, string? whereCond = null, Dictionary<string, object>? parameters = null, CommandType commandType = CommandType.Text) where T : class
        {
            QueryString = $"Select * from {(string.IsNullOrEmpty(schema) ? DBSchemaName : schema)}.{typeof(T).Name} {whereCond}";

            return await GetMapClassAsync<T>(QueryString, parameters, (string.IsNullOrEmpty(schema) ? DBSchemaName : schema), commandType);
        }
        #endregion

        #region Create
        private async Task<Result<int>> DoInsertAsync<T>(string? schema, T entity, bool getById = false, bool transaction = false) where T : class
        {
            try
            {
                if (entity == null) return new Result<int> { IsSuccess = false, Message = "Entity Null" };

                string check = CheckAttributeColumn<T>(entity, this, schema);
                if (!string.IsNullOrEmpty(check)) return new Result<int> { IsSuccess = false, Message = check };

                var getColmAndParams = GetInsertColmAndParams<T>(entity);
                Dictionary<string, object> parameters = getColmAndParams?.Item2 ?? [];

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
                var sqlQuery = $"Insert Into {schema ??= DBSchemaName}.{typeof(T).Name} {getColmAndParams?.Item1}" + ReturnIdentity;


                if (getById)
                {
                    var exeResult = await RunScalerAsync(schema, sqlQuery, parameters).ConfigureAwait(false);
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
            return await DoInsertAsync<T>(schema ?? DBSchemaName, entity, getById, transaction);
        }
        public async Task<Result<int>> DoMapInsertAsync<T>(T entity, bool getById = false, bool transaction = false) where T : class
        {
            return await DoInsertAsync<T>(DBSchemaName, entity, getById, transaction);
        }

        public async Task<Result<bool>> DoMapMultiInsertAsync<T>(string? schema, IEnumerable<T> entityList) where T : class
        {
            try
            {
                if (entityList is null || !entityList.Any()) return new Result<bool> { IsSuccess = false, Message = "entityList Null" };

                foreach (T entity in entityList)
                {
                    string check = CheckAttributeColumn<T>(entity, this, schema);
                    if (!string.IsNullOrEmpty(check)) return new Result<bool> { IsSuccess = false, Message = "There are problems in the list.\n\n" + check };
                }

                var getMultiInsertColmParams = GetInsertColmAndParamList<T>(entityList);

                var queryString = $"INSERT INTO {schema ??= DBSchemaName}.{typeof(T).Name} {getMultiInsertColmParams?.Item1}";

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
            return await DoMapMultiInsertAsync<T>(DBSchemaName, entityList);
        }
        #endregion

        #region Update
        private async Task<Result<bool>> DoUpdateAsync<T>(string? schema, T entity, IEnumerable<string> fields, bool transaction = false) where T : class
        {
            try
            {
                if (entity == null || !fields.Any()) return new Result<bool> { IsSuccess = false, Message = "Entity or Fields Null" };

                string check = CheckAttributeColumn<T>(entity, this, schema);
                if (!string.IsNullOrEmpty(check)) return new Result<bool> { IsSuccess = false, Message = check };

                var identityColumn = entity.GetKeyAttribute<T>();
                var _fields = string.Join(",", fields.Select(x => string.Format("{0}=@{0}", x.ToString())).ToList());
                var getUpdateColmParams = GetUpdateColmAndParams<T>(entity, fields);

                var sqlQuery = $"Update {schema ??= DBSchemaName}.{typeof(T).Name} set {_fields} where {identityColumn}=@{identityColumn};";
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
                                         .Where(x => x.GetCustomAttributes(typeof(KeyAttribute), true).Length > 0)
                                         .Select(s => $"where {s.Name}={s.GetValue(currentT)}").FirstOrDefault();


            var entity = await GetMapClassByWhereAsync<T>(identityValue ?? "");

            if (entity.IsSuccess == false) return new Result<bool> { IsSuccess = false, Message = "Entity Null" };

            List<string> fields = GetChangedFields<T>(currentT, entity.Data?.FirstOrDefault()!);

            if (fields.Count <= 0) return new Result<bool> { IsSuccess = false, Message = "Fields Null" };

            return await DoUpdateAsync<T>(schema, currentT, fields, transaction);

        }
        public async Task<Result<bool>> DoMapUpdateAsync<T>(T currentT, bool transaction = false) where T : class
        {
            var identityValue = typeof(T).GetProperties()
                                         .Where(x => x.GetCustomAttributes(typeof(KeyAttribute), true).Length > 0)
                                         .Select(s => $"where {s.Name}={s.GetValue(currentT)}").FirstOrDefault();

            var entity = await GetMapClassByWhereAsync<T>(identityValue ?? "");

            if (entity.IsSuccess == false) return new Result<bool> { IsSuccess = false, Message = "Entity Null" };

            List<string> fields = GetChangedFields<T>(currentT, entity.Data?.FirstOrDefault()!);

            if (fields.Count <= 0) return new Result<bool> { IsSuccess = false, Message = "Fields Null" };

            return await DoUpdateAsync<T>(DBSchemaName, currentT, fields, transaction);
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
                                         .Where(x => x.GetCustomAttributes(typeof(KeyAttribute), true).Length > 0)
                                         .Select(s => s.GetValue(entity)).FirstOrDefault();

                if (identityValue == null || (int)identityValue == 0)
                    return new Result<bool> { IsSuccess = false, Message = $"{identityColumn}; Identity Colum not found." };


                var sqlQuery = $"Delete from {schema ??= DBSchemaName}.{typeof(T).Name} where {identityColumn}=@{identityColumn};";
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
            return await DoMapDeleteAsync<T>(DBSchemaName, entity, transaction);
        }

        public async Task<Result<bool>> DoMapDeleteAsync<T>(Expression<Func<T, bool>> filter) where T : class
        {
            try
            {
                if (filter == null) return new Result<bool> { IsSuccess = false, Message = "Filter not found." };


                var WhereClause = filter.ConvertExpressionToQueryString();

                var sqlQuery = $"Delete from {DBSchemaName}.{typeof(T).Name} where {WhereClause};";

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

                var sqlQuery = $"Delete from {schema ??= DBSchemaName}.{typeof(T).Name} where {identityColumn} IN ({paramsColm});";
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
            return await DoMapDeleteAllAsync(DBSchemaName, entityList, transaction);
        }

        public async Task<Result<bool>> DoMapDeleteWithFieldAsync<T>(string? schema, string fieldName, object fieldValue, bool transaction = false) where T : class
        {
            try
            {
                if (string.IsNullOrEmpty(fieldName) || fieldValue.In("", null)) return new Result<bool> { IsSuccess = false, Message = "fieldName or fieldValue Null" };


                var dataName = typeof(T).GetProperties()
                                        .FirstOrDefault(x => x.GetCustomAttributes(typeof(DataNameAttribute), true).Length > 0 && x.Name == fieldName)?.Name.ToString();

                if (dataName == null) return new Result<bool> { IsSuccess = true, Message = "Fields not found." };

                var sqlQuery = $"Delete from {schema ??= DBSchemaName}.{typeof(T).Name} where {dataName}=@{dataName};";
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
            return await DoMapDeleteWithFieldAsync<T>(DBSchemaName, fieldName, fieldValue, transaction);
        }

        public async Task<Result<bool>> DoMapDeleteCompositeTableAsync<T>(string? schema, Dictionary<string, object> parameters, bool transaction = false) where T : class
        {
            try
            {
                if (parameters == null || parameters.Count == 0) return new Result<bool> { IsSuccess = false, Message = "Parameters Null" };

                var WhereClause = parameters.Select(x => $"{x.Key.Replace("@", "")}=@{x.Key.Replace("@", "")}").ToList();
                var sqlQuery = $"Delete from {schema ??= DBSchemaName}.{typeof(T).Name} where {string.Join(" and ", WhereClause)};";
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
            return await DoMapDeleteCompositeTableAsync<T>(DBSchemaName, parameters, transaction);
        }

        #endregion

        #endregion


        #region CUD Table
        /// <summary>
        /// Creates a new database if it doesn't already exist on the specified database server.
        /// If no database name is provided, it defaults to the value set in <see cref="DBSchemaName"/>.
        /// </summary>
        /// <param name="databaseName">Optional. The name of the database to create. If not provided, the default schema name is used.</param>
        /// <returns>
        /// A <see cref="Result{T}"/> indicating whether the operation was successful (<see cref="Result{T}.IsSuccess"/>).
        /// If successful, the result contains <c>true</c>; otherwise, it contains an error message.
        /// </returns>
        /// <example>
        /// <code>
        /// // Example usage:
        /// var result = CreateDatabase("NewDatabase");
        /// </code>
        /// </example>
        public Result<bool> CreateDatabase(string? databaseName = null)
        {
            databaseName ??= DBSchemaName;
            string query = "CREATE DATABASE IF NOT EXISTS " + databaseName;

            dbServerInfo.DbSchema = null;

            ConnectionStringBuilder = DALFactory.IDbConnectionStringBuilder(dbServerInfo);


            //Create Database in MySql
            var exResult = RunNonQuery(databaseName, query);
            return !exResult.IsSuccess
                   ? new Result<bool> { IsSuccess = false, Message = "Database CreateTable RunNonQuery error.\n" + exResult.Message }
                   : new Result<bool> { IsSuccess = true, Data = exResult.Data >= 0 };
        }

        /// <summary>
        /// Creates a new table in the specified schema (defaults to the current database schema).
        /// </summary>
        /// <typeparam name="T">The type of the table.</typeparam>
        /// <param name="schema">The schema of the table (optional). Defaults to the current database schema.</param>
        /// <returns>A result indicating whether the creation was successful and providing information about the operation.</returns>
        /// <example>
        /// var result = CreateTable<YourTableType>("your_schema_name");
        /// </example>
        public Result<bool> CreateTable<T>(string? schema = null) where T : class
        {
            try
            {
                StringBuilder script = new();
                StringBuilder strUniq = new();

                string tableName = schema ??= DBSchemaName + "." + typeof(T).Name.ToLower();
                script.AppendLine($"CREATE TABLE IF NOT EXISTS {tableName} (");

                //We assigned unique by finding KeyAttribute and Primary Key
                var keyName = typeof(T).GetProperties()
                                       .Where(x => x.GetCustomAttributes(typeof(KeyAttribute), true).Length > 0)
                                       .Select(p => p.Name).FirstOrDefault()!;

                //strUniq.AppendLine($"\tPRIMARY KEY (`{(keyName ?? tableName + "Id")}`),");
                //strUniq.AppendLine($"\tUNIQUE INDEX `{keyName}_UNIQUE` (`{keyName}` ASC) VISIBLE,");

                GetProperties(typeof(T), typeof(DataNameAttribute), true).ToList().ForEach(property =>
                {
                    string columnType = GetMySQLDataType(property);
                    string constraints = Tools.GetConstraints(property);

                    script.AppendLine($"\t{property.Name} {columnType} {constraints},");

                    //if (constraints.Contains("UNIQUE"))
                    //    strUniq.AppendLine($"\tUNIQUE INDEX `{property.Name}_UNIQUE` (`{property.Name}` ASC) VISIBLE,");
                });

                //strUniq.Remove(strUniq.Length - 3, 2);
                //script.AppendLine("\n" + strUniq.ToString());

                script.AppendLine($"\tPRIMARY KEY (`{(keyName ?? tableName + "Id")}`)");
                script.AppendLine(");");

                //Create Table in MySql
                var exResult = RunNonQuery(schema, script.ToString());
                return !exResult.IsSuccess
                       ? new Result<bool> { IsSuccess = false, Message = "Database CreateTable RunNonQuery error.\n" + exResult.Message }
                       : new Result<bool> { IsSuccess = true, Data = exResult.Data >= 0 };
            }
            catch (Exception ex)
            {
                CloseConnection();
                return new Result<bool> { IsSuccess = false, Message = $"Executing CreateTable Class Error: {ex.GetType().FullName}: {ex.Message}" };
            }
        }
        /// <summary>
        /// Drops a table in the specified schema (defaults to the current database schema).
        /// </summary>
        /// <typeparam name="T">The type of the table.</typeparam>
        /// <param name="schema">The schema of the table (optional). Defaults to the current database schema.</param>
        /// <returns>A result indicating whether the deletion was successful and providing information about the operation.</returns>
        /// <example>
        /// var result = DropTable<YourTableType>("your_schema_name");
        /// </example>
        public Result<bool> DropTable<T>(string? schema = null) where T : class
        {
            try
            {
                string tableName = schema ??= DBSchemaName + "." + typeof(T).Name.ToLower();

                //Drop Table in MySql
                var exResult = RunNonQuery(schema, $"DROP TABLE IF EXISTS {tableName};");
                return !exResult.IsSuccess
                       ? new Result<bool> { IsSuccess = false, Message = "Database DropTable RunNonQuery error.\n" + exResult.Message }
                       : new Result<bool> { IsSuccess = true, Data = exResult.Data >= 0 };
            }
            catch (Exception ex)
            {
                CloseConnection();
                return new Result<bool> { IsSuccess = false, Message = $"Executing DropTable Class Error: {ex.GetType().FullName}: {ex.Message}" };
            }
        }
        /// <summary>
        /// Updates a table in the specified schema (defaults to the current database schema).
        /// </summary>
        /// <typeparam name="T">The type of the table.</typeparam>
        /// <param name="schema">The schema of the table (optional). Defaults to the current database schema.</param>
        /// <returns>A result indicating whether the update was successful and providing information about the operation.</returns>
        /// <example>
        /// var result = UpdateTable<YourTableType>("your_schema_name");
        /// </example>
        public Result<bool> UpdateTable<T>(string? schema = null) where T : class
        {
            try
            {
                StringBuilder script = new();
                string tableName = schema ??= DBSchemaName + "." + typeof(T).Name.ToLower();
                script.AppendLine($"ALTER TABLE {tableName}");

                List<string> existingColumns = [];

                if (!OpenConnection(schema)) return new Result<bool> { IsSuccess = false, Message = "The connection couldn't be opened or created." };

                using var command = ExeCommand($"DESCRIBE {tableName};");
                var reader = command.ExecuteReader(CommandBehavior.CloseConnection);
                using (reader)
                {
                    while (reader.Read())
                    {
                        existingColumns.Add(reader.GetString(0)); // Assuming column names are in the first column
                    }
                }

                var properties = typeof(T).GetProperties().Where(prop => Attribute.IsDefined(prop, typeof(DataNameAttribute)));

                foreach (var property in properties)
                {
                    string columnName = property.Name;
                    if (!existingColumns.Contains(columnName))
                    {
                        // Add the new column
                        string columnType = GetMySQLDataType(property);
                        string constraints = GetConstraints(property);
                        script.AppendLine($"ADD COLUMN {columnName} {columnType} {constraints},");
                    }
                }

                // Remove the last comma from the script
                script.Remove(script.Length - 3, 2);
                script.AppendLine(";");

                //Update Table in MySql
                var exResult = RunNonQuery(schema, script.ToString());
                return !exResult.IsSuccess
                       ? new Result<bool> { IsSuccess = false, Message = "Database UpdateTable RunNonQuery error.\n" + exResult.Message }
                       : new Result<bool> { IsSuccess = true, Data = exResult.Data >= 0 };
            }
            catch (Exception ex)
            {
                CloseConnection();
                return new Result<bool> { IsSuccess = false, Message = $"Executing UpdateTable Class Error: {ex.GetType().FullName}: {ex.Message}" };
            }
        }
        /// <summary>
        /// Removes a column from a table in the specified schema (defaults to the current database schema), or removes all columns.
        /// </summary>
        /// <typeparam name="T">The type of the table.</typeparam>
        /// <param name="columnName">The name of the column to remove (optional). If left empty, removes all columns.</param>
        /// <param name="schema">The schema of the table (optional). Defaults to the current database schema.</param>
        /// <returns>A result indicating whether the removal was successful and providing information about the operation.</returns>
        /// <example>
        /// var result = RemoveTableColumn<YourTableType>("column_name", "your_schema_name");
        /// </example>
        public Result<bool> RemoveTableColumn<T>(string? columnName = null, string? schema = null) where T : class
        {
            try
            {
                StringBuilder script = new();
                string tableName = schema ??= DBSchemaName + "." + typeof(T).Name.ToLower();
                script.AppendLine($"ALTER TABLE {tableName}");

                if (string.IsNullOrEmpty(columnName))
                {
                    List<string> existingColumns = [];

                    if (!OpenConnection(schema)) return new Result<bool> { IsSuccess = false, Message = "The connection couldn't be opened or created." };

                    using var command = ExeCommand($"DESCRIBE {tableName};");
                    var reader = command.ExecuteReader(CommandBehavior.CloseConnection);
                    using (reader)
                    {
                        while (reader.Read())
                        {
                            existingColumns.Add(reader.GetString(0)); // Assuming column names are in the first column
                        }
                    }

                    var properties = typeof(T).GetProperties().Where(prop => Attribute.IsDefined(prop, typeof(DataNameAttribute)));

                    foreach (var existingColumn in existingColumns)
                    {
                        if (!properties.Any(prop => string.Equals(prop.Name, existingColumn, StringComparison.OrdinalIgnoreCase)))
                        {
                            // Remove the column not present in class
                            script.AppendLine($"DROP COLUMN {existingColumn},");
                        }
                    }

                    // Remove the last comma from the script
                    script.Remove(script.Length - 3, 2);
                    script.AppendLine(";");
                }
                else
                {
                    script.AppendLine($"DROP COLUMN {columnName};");
                }

                //Remove Table column in MySql
                var exResult = RunNonQuery(schema, script.ToString());
                return !exResult.IsSuccess
                       ? new Result<bool> { IsSuccess = false, Message = "Database RemoveTableColumn RunNonQuery error.\n" + exResult.Message }
                       : new Result<bool> { IsSuccess = true, Data = exResult.Data >= 0 };
            }
            catch (Exception ex)
            {
                CloseConnection();
                return new Result<bool> { IsSuccess = false, Message = $"Executing RemoveTableColumn Class Error: {ex.GetType().FullName}: {ex.Message}" };
            }
        }
        /// <summary>
        /// Adds an attribute to a column in the specified table schema (defaults to the current database schema).
        /// </summary>
        /// <typeparam name="T">The type of the table.</typeparam>
        /// <param name="attribute">The attribute to add to the column.</param>
        /// <param name="propertyName">The name of the property representing the column.</param>
        /// <param name="schema">The schema of the table (optional). Defaults to the current database schema.</param>
        /// <returns>A result indicating whether the addition was successful and providing information about the operation.</returns>
        /// <example>
        /// var result = AddAttributeToTableColumn<YourTableType>(TableColumnAttribute.PrimaryKey, "column_name", "your_schema_name");
        /// </example>
        public Result<bool> AddAttributeToTableColumn<T>(TableColumnAttribute attribute, string propertyName, string? schema = null) where T : class
        {
            try
            {
                string attributeName = string.Empty;
                if ((int)attribute == 0)
                    attributeName = "Primary Key";
                else if ((int)attribute == 1)
                    attributeName = "Not Null";
                else if ((int)attribute == 5)
                    attributeName = "Zero Fill";
                else if ((int)attribute == 6)
                    attributeName = "Auto Increment";

                StringBuilder script = new();
                string tableName = schema ??= DBSchemaName + "." + typeof(T).Name.ToLower();
                script.AppendLine($"ALTER TABLE {tableName}");

                bool columnFound = false;

                if (!OpenConnection(schema)) return new Result<bool> { IsSuccess = false, Message = "The connection couldn't be opened or created." };

                using var command = ExeCommand($"DESCRIBE {tableName};");
                var reader = command.ExecuteReader(CommandBehavior.CloseConnection);
                using (reader)
                {
                    while (reader.Read())
                    {
                        string columnName = reader.GetString(0); // Assuming column names are in the first column
                        if (columnName.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
                        {
                            columnFound = true;
                            // Add the new attribute to the existing column
                            script.AppendLine($"MODIFY COLUMN {propertyName} {reader.GetString(1)} {attributeName},");
                            break;
                        }
                    }
                }

                if (!columnFound)
                {
                    return new Result<bool> { IsSuccess = false, Message = $"Column '{propertyName}' does not exist in table '{tableName}'." };
                }

                script.Remove(script.Length - 3, 2);
                script.AppendLine(";");

                //Remove Table column in MySql
                var exResult = RunNonQuery(schema, script.ToString());
                return !exResult.IsSuccess
                       ? new Result<bool> { IsSuccess = false, Message = "Database AddAttributeToTableColumn RunNonQuery error.\n" + exResult.Message }
                       : new Result<bool> { IsSuccess = true, Data = exResult.Data >= 0 };
            }
            catch (Exception ex)
            {
                CloseConnection();
                return new Result<bool> { IsSuccess = false, Message = $"Executing AddAttributeToTableColumn Class Error: {ex.GetType().FullName}: {ex.Message}" };
            }
        }
        #endregion
    }
}