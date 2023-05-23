using MySqlX.XDevAPI;
using OmerkckEF.Biscom.Interfaces;
using OmerkckEF.Biscom.Repositories;
using System.Data;
using System.Data.Common;
using System.Runtime.CompilerServices;
using ZstdSharp;
using static OmerkckEF.Biscom.Enums;

namespace OmerkckEF.Biscom.DBContext
{
	public partial class Bisco : IDisposable
    {
        #region Properties
        private IDALFactory DALFactory { get; set; }
        public DbConnection? MyConnection { get; set; }
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
        public bool OpenConnection(string? schemaName)
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
		public async Task<bool> OpenConnectionAsync(string schemaName)
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
        public DbCommand ExeCommand(string queryString, Dictionary<string, object>? parameters = null, CommandType commandType = CommandType.Text)
        {
            try
            {
                DbCommand dbCommand = (DbCommand)this.DALFactory.IDbCommand();
                dbCommand.Connection = this.MyConnection;
                dbCommand.CommandText = queryString;
                dbCommand.CommandType = commandType;

                if (parameters != null)
                {
                    foreach (KeyValuePair<string, object> param in parameters)
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


        public int RunNonQuery(string queryString, Dictionary<string, object>? parameters = null, bool transaction = false, CommandType commandType = CommandType.Text)
        {
            return RunNonQuery(this.connSchemaName, queryString, parameters, transaction, commandType);
        }
        public int RunNonQuery(string schema, string queryString, Dictionary<string, object>? parameters = null, bool transaction = false, CommandType commandType = CommandType.Text)
        {
            try
            {
                this.connSchemaName = string.IsNullOrEmpty(schema) ? this.connSchemaName : schema;
                if (!OpenConnection(this.connSchemaName)) return -1;
                using (MyConnection)

                    if (transaction)
                    {
                        using var _transaction = this.MyConnection?.BeginTransaction();
                        using var command = ExeCommand(queryString, parameters, commandType);

                        command.Transaction = _transaction;
                        int result = command.ExecuteNonQuery();
                        _transaction?.Commit();

                        return result;
                    }
                    else
                    {
                        using var command = ExeCommand(queryString, parameters, commandType);
                        return command.ExecuteNonQuery();
                    }
            }
            catch (DbException ex)
            {
                throw new Exception("Executing NonQuery Error: " + ex.Message);
            }
        }

        public object? RunScaler(string queryString, Dictionary<string, object>? parameters = null, bool transaction = false, CommandType commandType = CommandType.Text)
        {
            return RunScaler(this.connSchemaName, queryString, parameters, transaction, commandType);
        }
        public object? RunScaler(string schema, string queryString, Dictionary<string, object>? parameters = null, bool transaction = false, CommandType commandType = CommandType.Text)
        {
            try
            {
				this.connSchemaName = string.IsNullOrEmpty(schema) ? this.connSchemaName : schema;
				if (!OpenConnection(this.connSchemaName)) return null;
				using var connection = this.MyConnection;

				if (transaction)
                {
                    using var _transaction = this.MyConnection?.BeginTransaction();
                    using var command = ExeCommand(queryString, parameters, commandType);

                    command.Transaction = _transaction;
                    object? result = command.ExecuteScalar() ?? null;
                    _transaction?.Commit();

                    return result;
                }
                else
                {
                    using var command = ExeCommand(queryString, parameters, commandType);
                    return command?.ExecuteScalar() ?? null;
                }
            }
            catch (DbException ex)
            {
                throw new Exception("Executing ExecuteScalar Error: " + ex.Message);
            }
        }

        public DbDataReader? RunDataReader(string queryString, Dictionary<string, object>? parameters = null, CommandType commandType = CommandType.Text)
        {
            return RunDataReader(this.connSchemaName, queryString, parameters, commandType);
        }
        public DbDataReader? RunDataReader(string schema, string queryString, Dictionary<string, object>? parameters = null, CommandType commandType = CommandType.Text)
        {
            try
            {
				this.connSchemaName = string.IsNullOrEmpty(schema) ? this.connSchemaName : schema;
				if (!OpenConnection(this.connSchemaName)) return null;
				
                using var connection = this.MyConnection;
				using var command = ExeCommand(queryString, parameters, commandType);
                var reader = command.ExecuteReader(CommandBehavior.CloseConnection);

                return reader;
            }
            catch (DbException ex)
            {
                throw new Exception("Executing RunDataReader Error: " + ex.Message);
            }
        }

        public DataTable? RunDataTable(string queryString, Dictionary<string, object>? parameters = null, CommandType commandType = CommandType.Text)
        {
            return RunDataTable(this.connSchemaName, queryString, parameters, commandType);
        }
        public DataTable? RunDataTable(string schema, string queryString, Dictionary<string, object>? parameters = null, CommandType commandType = CommandType.Text)
        {
            try
            {
				this.connSchemaName = string.IsNullOrEmpty(schema) ? this.connSchemaName : schema;
				if (!OpenConnection(this.connSchemaName)) return null;
				using var connection = this.MyConnection;

				using var command = ExeCommand(queryString, parameters, commandType);
                using var dataReader = command.ExecuteReader(CommandBehavior.CloseConnection);

                var dataTable = new DataTable();
                if (dataReader != null && !dataReader.IsClosed)
                    dataTable.Load(dataReader);

                return dataTable;
            }
            catch (DbException ex)
            {
                throw new Exception("Executing RunDataTable Error: " + ex.Message);
            }
        }
        #endregion

        #region ASYNC Regular Operations (Adapter, Query etc.)
        public async Task<int> RunNonQueryAsync(string queryString, Dictionary<string, object>? parameters = null, bool transaction = false, CommandType commandType = CommandType.Text)
        {
            return await RunNonQueryAsync(this.connSchemaName, queryString, parameters, transaction, commandType);
        }
        public async Task<int> RunNonQueryAsync(string schema, string queryString, Dictionary<string, object>? parameters = null, bool transaction = false, CommandType commandType = CommandType.Text)
        {
            try
            {
				this.connSchemaName = string.IsNullOrEmpty(schema) ? this.connSchemaName : schema;
				if (!await OpenConnectionAsync(this.connSchemaName)) return -1;
                using (this.MyConnection)
                {
                    if (transaction)
                    {
                        using var _transaction = this.MyConnection?.BeginTransaction();
                        using var command = ExeCommand(queryString, parameters, commandType);

                        command.Transaction = _transaction;
                        int result = Convert.ToInt32(await command.ExecuteNonQueryAsync());
                        _transaction?.Commit();

                        return result;
                    }
                    else
                    {
                        using var command = ExeCommand(queryString, parameters, commandType);
                        return Convert.ToInt32(await command.ExecuteNonQueryAsync());
                    }
                }
            }
            catch (DbException ex)
            {
                throw new Exception("Executing NonQuery Error: " + ex.Message);
            }
        }

		public async Task<object?> RunScalerAsync(string queryString, Dictionary<string, object>? parameters = null, bool transaction = false, CommandType commandType = CommandType.Text)
		{
			return await RunScalerAsync(this.connSchemaName, queryString, parameters, transaction, commandType);
		}
		public async Task<object?> RunScalerAsync(string schema, string queryString, Dictionary<string, object>? parameters = null, bool transaction = false, CommandType commandType = CommandType.Text)
		{
            try
            {
				this.connSchemaName = string.IsNullOrEmpty(schema) ? this.connSchemaName : schema;
				if (!await OpenConnectionAsync(this.connSchemaName)) return null;
                using (this.MyConnection)
                {
                    if (transaction)
                    {
                        using var _transaction = this.MyConnection?.BeginTransaction();
                        using var command = ExeCommand(queryString, parameters, commandType);

                        command.Transaction = _transaction;
                        object? result = await command.ExecuteScalarAsync() ?? null;
                        _transaction?.Commit();

                        return result;
                    }
                    else
                    {
                        using var command = ExeCommand(queryString, parameters, commandType);
                        return await command.ExecuteScalarAsync() ?? null;
                    }
                }
            }
            catch (DbException ex)
            {
                throw new Exception("Executing ExecuteScalar Error: " + ex.Message);
            }
		}

		public async Task<DataTable> RunSelectDataAsync(string queryString, Dictionary<string, object>? parameters = null, bool transaction = false, CommandType commandType = CommandType.Text)
        {
            return await RunSelectDataAsync(this.connSchemaName, queryString, parameters, transaction, commandType);
        }
        public async Task<DataTable> RunSelectDataAsync(string schema, string queryString, Dictionary<string, object>? parameters = null, bool transaction = false, CommandType commandType = CommandType.Text)
        {
            try
            {
				this.connSchemaName = string.IsNullOrEmpty(schema) ? this.connSchemaName : schema;
				if (!await OpenConnectionAsync(this.connSchemaName)) return new DataTable();
                using (this.MyConnection)
                {

                    DataTable? dataTable = new();
                    using var command = ExeCommand(queryString, parameters, commandType);
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