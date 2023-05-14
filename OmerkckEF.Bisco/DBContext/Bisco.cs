using OmerkckEF.Biscom.Interfaces;
using OmerkckEF.Biscom.Repositories;
using static OmerkckEF.Biscom.Enums;
using static OmerkckEF.Biscom.Tools;
using System.Data;
using System.Data.Common;
using System.Reflection;
using System.Linq.Expressions;
using System.Transactions;

namespace OmerkckEF.Biscom.DBContext
{
	public sealed partial class Bisco : IDisposable
    {
        #region Properties
        private IDALFactory DALFactory { get; set; }
        private DbConnection? MyConnection { get; set; }
        public DbServer? DbServer { get; set; }
        private string? QueryString { get; set; }
        public string? Bisco_ErrorInfo { get; set; }
        #endregion

        public Bisco(DbServer DbServer)
        {
            this.DbServer = DbServer;
            var SelectDBConn = this.DbServer?.DataBaseType ?? DataBaseType.MySql;
            this.DALFactory = DALFactoryBase.GetDataBase((DataBaseType)SelectDBConn);

            connSchemaName = this.DbServer?.DbSchema ?? "";
        }

        #region ConnectionString

        public string ConnectionString
        {
            get
            {
                string DBString = "";
                DBString += " server = " + con_ServerIP + "; ";
                DBString += " port=" + con_ServerPort + "; ";
                DBString += " database = " + ConnSchemaName + "; ";
                DBString += " Uid=" + con_ServerUser + "; ";
                DBString += " Pwd = " + con_ServerPassword + "; ";
                DBString += " pooling = " + con_ServerPooling + "; ";
                DBString += " Max Pool Size = " + con_MaxPoolSize + "; ";
                DBString += " connection lifetime = " + con_ConnectionLifeTime + "; ";
                DBString += " connection timeout = " + con_ConnectionTimeOut + "; ";
                DBString += " Allow User Variables = " + con_AllowUserInput + "; ";
                DBString += " SslMode=none; ";

                return DBString;
            }
        }

        private string con_ServerIP => DbServer?.DbIp ?? "127.0.0.1";
        private int con_ServerPort => DbServer?.DbPort ?? 3306;

        private string connSchemaName;
        public string ConnSchemaName
        {
            get => connSchemaName;
            set => connSchemaName = value;
        }

        private string con_ServerUser => DbServer?.DbUser ?? "root";
        private string con_ServerPassword => DbServer?.DbPassword ?? "root123";
        private bool con_ServerPooling => DbServer?.DbPooling ?? true;
        private int con_MaxPoolSize => DbServer?.DbMaxpoolsize ?? 100;
        private int con_ConnectionLifeTime => DbServer?.DbConnLifetime ?? 300;
        private int con_ConnectionTimeOut => DbServer?.DbConnTimeout ?? 500;
        private bool con_AllowUserInput => DbServer?.DbAllowuserinput ?? true;
        public string ServerIP => con_ServerIP;

        #endregion
        #region Connection
        private bool OpenConnection(string? schemaName)
        {
            try
            {
                bool IsNewConnection = false;
                if (schemaName != null && schemaName != string.Empty && ConnSchemaName != schemaName)
                {
                    IsNewConnection = true;
                    ConnSchemaName = schemaName;
                }

                if (this.MyConnection == null || IsNewConnection)
                {
                    this.MyConnection = (DbConnection)this.DALFactory.IDbConnection();
                    this.MyConnection.ConnectionString = this.ConnectionString;
                }

                if (this.MyConnection.State != ConnectionState.Open)
                    this.MyConnection.Open();

                return true;
            }
            catch (DbException ex)
            {
                Bisco_ErrorInfo = ex.ErrorCode switch
                {
                    0 => "Server bağlantı hatası. Sistem yöneticisi ile görüşün.",
                    1042 => "Server bulunamadı. DNS adresi yanlış olabilir.",
                    1045 => "Server bağlantısı için gerekli Kullanıcı adı veya Şifre yanlış. Sistem yöneticisi ile görüşün.",
                    _ => "Connection Error : " + ex.Message,
                };
                return false;
            }
        }
        private async Task<bool> OpenConnectionAsync(string schemaName)
        {
            try
            {
                bool IsNewConnection = false;
                if (schemaName != null && schemaName != string.Empty && ConnSchemaName != schemaName)
                {
                    IsNewConnection = true;
                    ConnSchemaName = schemaName;
                }

                if (this.MyConnection == null || IsNewConnection)
                {
                    this.MyConnection = (DbConnection)this.DALFactory.IDbConnection();
                    this.MyConnection.ConnectionString = this.ConnectionString;
                }

                if (this.MyConnection.State != ConnectionState.Open)
                    await this.MyConnection.OpenAsync();

                Bisco_ErrorInfo = string.Empty;

				return true;
            }
            catch (DbException ex)
            {
                Bisco_ErrorInfo = ex.ErrorCode switch
                {
                    0 => "Server bağlantı hatası. Sistem yöneticisi ile görüşün.",
                    1042 => "Server bulunamadı. DNS adresi yanlış olabilir.",
                    1045 => "Server bağlantısı için gerekli Kullanıcı adı veya Şifre yanlış. Sistem yöneticisi ile görüşün.",
                    _ => "Connection Error : " + ex.Message,
                };
                return false;
            }
        }
        #endregion

        #region Regular Operations (Adapter, Query etc.)
        public DbCommand ExeCommand(string QueryString, Dictionary<string, object>? Parameters = null, CommandType CommandType = CommandType.Text)
        {
            try
            {
                DbCommand dbCommand = (DbCommand)this.DALFactory.IDbCommand();
                dbCommand.Connection = this.MyConnection;
                dbCommand.CommandText = QueryString;
                dbCommand.CommandType = CommandType;

                if (Parameters != null)
                {
                    foreach(KeyValuePair<string, object> param in Parameters)
                    {
                        DbParameter DbParam = dbCommand.CreateParameter();
                        DbParam.ParameterName = param.Key;
                        DbParam.Value = param.Value ?? DBNull.Value;
                        dbCommand.Parameters.Add(DbParam);
                    }
                }
                return dbCommand;
            }
            catch (DbException ex)
            {
                throw new Exception(ex.Message);
            }
        }


        public int RunNonQuery(string QueryString, Dictionary<string, object>? Parameters = null, bool Transaction = false, CommandType CommandType = CommandType.Text)
        {
            return RunNonQuery(ConnSchemaName, QueryString, Parameters, Transaction, CommandType);
        }
        public int RunNonQuery(string Schema, string QueryString, Dictionary<string, object>? Parameters = null, bool Transaction = false, CommandType CommandType = CommandType.Text)
        {
            try
            {
                using var connection = this.MyConnection;
                if (!OpenConnection(Schema))
                    return -1;

                if (Transaction)
                {
                    using var _transaction = this.MyConnection?.BeginTransaction();
                    using var command = ExeCommand(QueryString, Parameters, CommandType);

                    command.Transaction = _transaction;
                    int result = command.ExecuteNonQuery();
                    _transaction?.Commit();

                    return result;
                }
                else
                {
                    using var command = ExeCommand(QueryString, Parameters, CommandType);
                    return command.ExecuteNonQuery();
                }
            }
            catch (DbException ex)
            {
                throw new Exception("Executing NonQuery Error: " + ex.Message);
            }
        }

        public object? RunScaler(string QueryString, Dictionary<string, object>? Parameters = null, bool Transaction = false, CommandType CommandType = CommandType.Text)
        {
            return RunScaler(ConnSchemaName, QueryString, Parameters, Transaction, CommandType);
        }
        public object? RunScaler(string Schema, string QueryString, Dictionary<string, object>? Parameters = null, bool Transaction = false, CommandType CommandType = CommandType.Text)
        {
            try
            {
                using (this.MyConnection)
                {
                    if (!OpenConnection(Schema))
                        return null;

                    if (Transaction)
                    {
						using var _transaction = this.MyConnection?.BeginTransaction();
						using var command = ExeCommand(QueryString, Parameters, CommandType);

						command.Transaction = _transaction;
						object? result = command.ExecuteScalar() ?? null;
						_transaction?.Commit();

						return result;
					}
                    else
                    {
                        using var command = ExeCommand(QueryString, Parameters, CommandType);
                        return command?.ExecuteScalar() ?? null;
                    }
                }
            }
            catch (DbException ex)
            {
                throw new Exception("Executing ExecuteScalar Error: " + ex.Message);
            }
        }

        public DbDataReader? RunDataReader(string QueryString, Dictionary<string, object>? Parameters = null, CommandType CommandType = CommandType.Text)
        {
            return RunDataReader(ConnSchemaName, QueryString, Parameters, CommandType);
        }
        public DbDataReader? RunDataReader(string Schema, string QueryString, Dictionary<string, object>? Parameters = null, CommandType CommandType = CommandType.Text)
        {
            try
            {
                using (this.MyConnection)
                {
                    if (!OpenConnection(Schema))
                        return null;
                    
                    using var command = ExeCommand(QueryString, Parameters, CommandType);
                    var reader = command.ExecuteReader(CommandBehavior.CloseConnection);

                    return reader;
                }
            }
            catch (DbException ex)
            {
                throw new Exception("Executing RunDataReader Error: " + ex.Message);
            }
        }

        public DataTable? RunDataTable(string QueryString, Dictionary<string, object>? Parameters = null, CommandType CommandType = CommandType.Text)
        {
            return RunDataTable(ConnSchemaName, QueryString, Parameters, CommandType);
        }
        public DataTable? RunDataTable(string Schema, string QueryString, Dictionary<string, object>? Parameters = null, CommandType CommandType = CommandType.Text)
        {
            try
            {
                using (this.MyConnection)
                {
                    if (!OpenConnection(Schema))
                        return null;

                    //using var dataReader = RunDataReader(Schema, QueryString, Parameters, CommandType);
                    using var command = ExeCommand(QueryString, Parameters, CommandType);
                    using var dataReader = command.ExecuteReader(CommandBehavior.CloseConnection);

                    var dataTable = new DataTable();
                    if(dataReader != null && !dataReader.IsClosed)
                        dataTable.Load(dataReader);

                    return dataTable;
                }
            }
            catch (DbException ex)
            {
                throw new Exception("Executing RunDataTable Error: " + ex.Message);
            }
        }
        #endregion

        #region ASYNC Regular Operations (Adapter, Query etc.)
        public async Task<int> RunNonQueryAsync(string QueryString, Dictionary<string, object>? Parameters = null, bool Transaction = false, CommandType CommandType = CommandType.Text)
        {
            return await RunNonQueryAsync(ConnSchemaName, QueryString, Parameters, Transaction, CommandType);
        }
        public async Task<int> RunNonQueryAsync(string Schema, string QueryString, Dictionary<string, object>? Parameters = null, bool Transaction = false, CommandType CommandType = CommandType.Text)
        {
            try
            {
                using (this.MyConnection)
                {
                    if (!await OpenConnectionAsync(Schema))
                        return -1;

                    if (Transaction)
                    {
                        using var _transaction = this.MyConnection?.BeginTransaction();
                        using var command = ExeCommand(QueryString, Parameters, CommandType);

                        command.Transaction = _transaction;
                        int result = Convert.ToInt32(await command.ExecuteNonQueryAsync());
                        _transaction?.Commit();

                        return result;
                    }
                    else
                    {
                        using var command = ExeCommand(QueryString, Parameters, CommandType);
                        return Convert.ToInt32(await command.ExecuteNonQueryAsync());
                    }
                }
            }
            catch (DbException ex)
            {
                throw new Exception("Executing NonQuery Error: " + ex.Message);
            }
        }

        public async Task<DataTable> RunSelectDataAsync(string QueryString, Dictionary<string, object>? Parameters = null, bool Transaction = false, CommandType CommandType = CommandType.Text)
        {
            return await RunSelectDataAsync(ConnSchemaName,QueryString, Parameters, Transaction, CommandType);
        }
        public async Task<DataTable> RunSelectDataAsync(string Schema, string QueryString, Dictionary<string, object>? Parameters = null, bool Transaction = false, CommandType CommandType = CommandType.Text)
        {
            try
            {
                using (this.MyConnection)
                {
                    if (!await OpenConnectionAsync(Schema))
                        return new DataTable();


                    DataTable? dataTable = new();
                    using var command = ExeCommand(QueryString, Parameters, CommandType);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        dataTable.Load(reader);
                    }

                    return dataTable;
                }
            }
            catch (DbException ex)
            {
                throw new Exception("Executing RunSelectDataAsync Error: " + ex.Message);
            }
        }
		#endregion

		#region Mapping Methods /// CRUD = RCUD :)) Read, Create, Update, Delete ///

		#region Read
		public List<T>? GetMappedClass<T>(string? QueryString = null, Dictionary<string, object>? Parameters = null, CommandType CommandType = CommandType.Text) where T : class, new()
		{
			try
			{
				using var connection = this.MyConnection;
				if (!OpenConnection(this.connSchemaName))
					return null;

				QueryString ??= $"Select * from {this.connSchemaName}.{typeof(T).Name}";

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
		public List<T>? GetMappedClassByWhere<T>(string WhereCond, Dictionary<string, object>? Parameters = null, CommandType CommandType = CommandType.Text) where T : class, new()
        {
			this.QueryString = $"Select * from {this.connSchemaName}.{typeof(T).Name} {WhereCond}";

			return GetMappedClass<T>(QueryString, Parameters, CommandType);
        }
        public List<T>? GetMappedClassBySchema<T>(string Schema, string? WhereCond = null, Dictionary<string, object>? Parameters = null, CommandType CommandType = CommandType.Text) where T : class, new()
        {
			this.QueryString = $"Select * from {Schema}.{typeof(T).Name} {WhereCond}";
            this.connSchemaName = Schema;
            
			return GetMappedClass<T>(QueryString, Parameters, CommandType);
        }
        #endregion
        #region Create
        private int DoInsert<T>(T entity, bool getById = false, bool transaction = false) where T : class
		{
            try
            {
                if (entity == null) return -1;

                using var connection = this.MyConnection;
                if (!OpenConnection(this.connSchemaName))
                    return -1;


                var getColmAndParams = GetInsertColumnAndParameter<T>(entity);
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
                var sqlQuery = $"Insert Into {this.connSchemaName}.{typeof(T).Name} {getColmAndParams?.Item1}";


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
        public int DoMapInsert<T>(T entity, bool getById = false, bool transaction = false) where T : class
        {
            return DoInsert<T>(entity, getById, transaction);
		}
        public bool DoMultiMapInsert<T>(IEnumerable<T> entityList) where T : class
        {
            try
            {
                if (entityList is null || !entityList.Any()) return false;

                using var connection = this.MyConnection;

                if (!OpenConnection(this.connSchemaName))
                    return false;

                var getMultiInsertParamColm = GetInsertColumnAndParameterList<T>(entityList);

                var queryString = $"INSERT INTO {this.connSchemaName}.{typeof(T).Name} {getMultiInsertParamColm?.Item1}";

                return RunNonQuery(queryString, getMultiInsertParamColm?.Item2, true) > 0;
            }
            catch (Exception ex)
            {
                return false;
                throw new Exception("Executing DoMultiMapInsert Error: " + ex.Message);
            }
        }
        #endregion
        #region Update
        public bool DoUpdate<T>(T entity, bool transaction = false) where T : class
        {
            try
            {
                if (entity == null) return false;

                using var connection = this.MyConnection;
                if (!OpenConnection(this.connSchemaName))
                    return false;


                var identityColumn = entity.GetKeyAttribute<T>();
                Dictionary<string, object> parameters = GetDbParameters<T>(entity);


                var sqlQuery = $"Update {this.connSchemaName}.{typeof(T).Name} set {GetUpdateSetClause<T>()} where {identityColumn}=@{identityColumn}";

                var result = RunNonQuery(sqlQuery, GetDbParameters<T>(entity), transaction);

                return result > 0;
            }
            catch
            {
                return false;
            }
        }
        #endregion
        #region Delete

        #endregion




        
        #endregion











        #region IDisposable Members
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private bool disposed = false;
        private void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // Managed resources are released here.
                    this.MyConnection?.Dispose();
                }

                // Unmanaged resources are released here.
                disposed = true;
            }
        }
        ~Bisco() { Dispose(false); }
        #endregion
    }
}