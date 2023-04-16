using OmerkckEF.Biscom.Interfaces;
using OmerkckEF.Biscom.Repositories;
using static OmerkckEF.Biscom.Enums;
using System.Data;
using System.Data.Common;
using System.Reflection;

namespace OmerkckEF.Biscom.DBContext
{
    public sealed partial class Bisco : IDisposable
    {
        #region Properties
        private IDALFactory DALFactory { get; set; }
        public ServerDB? serverDB { get; set; }
        private DbConnection? MyConnection { get; set; }
        public string? Bisco_ErrorInfo { get; set; }
        #endregion

        public Bisco(ServerDB SerderDb)
        {
            this.serverDB = SerderDb;
            var SelectDBConn = serverDB?.DataBaseType ?? DataBaseType.MySql;
            this.DALFactory = DALFactoryBase.GetDataBase((DataBaseType)SelectDBConn);

            connSchemaName = this.serverDB?.DBSchema ?? "";
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

        private string con_ServerIP => serverDB?.DBIp ?? "127.0.0.1";
        private int con_ServerPort => serverDB?.DBPort ?? 3306;

        private string connSchemaName;
        public string ConnSchemaName
        {
            get { return connSchemaName; }
            set { connSchemaName = value; }
        }

        private string con_ServerUser => serverDB?.DBUser ?? "root";
        private string con_ServerPassword => serverDB?.DBPassword ?? "root123";
        private bool con_ServerPooling => serverDB?.DBPooling ?? true;
        private int con_MaxPoolSize => serverDB?.DBMaxpoolsize ?? 100;
        private int con_ConnectionLifeTime => serverDB?.DBConnLifetime ?? 300;
        private int con_ConnectionTimeOut => serverDB?.DBConnTimeout ?? 500;
        private bool con_AllowUserInput => serverDB?.DBAllowuserinput ?? true;
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
                        DbParam.Value = param.Value;
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

        public object RunScaler(string QueryString, Dictionary<string, object>? Parameters = null, CommandType CommandType = CommandType.Text)
        {
            return RunScaler(ConnSchemaName, QueryString, Parameters, CommandType);
        }
        public object RunScaler(string Schema, string QueryString, Dictionary<string, object>? Parameters = null, CommandType CommandType = CommandType.Text)
        {
            try
            {
                using (this.MyConnection)
                {
                    if (!OpenConnection(Schema))
                        return false;

                    using var command = ExeCommand(QueryString, Parameters, CommandType);
                    return command?.ExecuteScalar() ?? false;
                }
            }
            catch (DbException ex)
            {
                throw new Exception("Executing NonScaler Error: " + ex.Message);
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

        #region Mapping Methods

        public List<T>? GetMappedClass<T>(string? WhereCond = null) where T : class, new()
        {
            return GetMappedClass<T>(connSchemaName, WhereCond, null, CommandType.Text);
        }
        public List<T>? GetMappedClass<T>(string? Schema, string? WhereCond = null, Dictionary<string, object>? Parameters = null, CommandType CommandType = CommandType.Text) where T : class, new()
        {
            try
            {
                using var connection = this.MyConnection;
                if (!OpenConnection(Schema))
                    return null;

                string QueryString = $"Select * from {Schema}.{typeof(T).Name} {WhereCond}";
                using var command = ExeCommand(QueryString, Parameters, CommandType);
                using var reader = command.ExecuteReader();
                var entities = new List<T>();

                while (reader.Read())
                {
                    var entity = Activator.CreateInstance<T>();

                    foreach (var property in GetClassProperties(typeof(T), typeof(DataNameAttribute)).ToArray())
                    {
                        if (!reader.HasColumn(property.Name)) continue;

                        if (!reader.IsDBNull(reader.GetOrdinal(property.Name)))
                            Tools.ParsePrimitive(property, entity, reader[property.Name]);
                    }

                    entities.Add(entity);
                }

                return entities;
            }
            catch (DbException ex)
            {
                throw new Exception("Executing TClass RunDataReader Error: " + ex.Message);
            }
        }
        public List<T>? GetMappedClassByQuery<T>(string QueryString, Dictionary<string, object>? Parameters = null, CommandType CommandType = CommandType.Text) where T : class, new()
        {
            try
            {
                using var connection = this.MyConnection;
                if (!OpenConnection(connSchemaName))
                    return null;

                using var command = ExeCommand(QueryString, Parameters, CommandType);
                using var reader = command.ExecuteReader();
                var entities = new List<T>();

                while (reader.Read())
                {
                    var entity = Activator.CreateInstance<T>();

                    foreach (var property in GetClassProperties(typeof(T), typeof(DataNameAttribute)).ToArray())
                    {
                        if (!reader.HasColumn(property.Name)) continue;

                        if (!reader.IsDBNull(reader.GetOrdinal(property.Name)))
                            Tools.ParsePrimitive(property, entity, reader[property.Name]);
                    }

                    entities.Add(entity);
                }

                return entities;
            }
            catch (DbException ex)
            {
                throw new Exception("Executing TClass RunDataReader Error: " + ex.Message);
            }
        }


        public static IEnumerable<PropertyInfo> GetClassProperties(Type ClassType, Type AttirbuteType)
        {
            if (ClassType == null) return new List<PropertyInfo>();

            if (AttirbuteType == null) return ClassType.GetProperties();

            return ClassType.GetProperties().Where(x => x.GetCustomAttributes(AttirbuteType, true).Any());
        }
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