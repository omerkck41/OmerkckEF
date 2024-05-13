using OmerkckEF.Biscom.Interfaces;
using OmerkckEF.Biscom.Repositories;
using OmerkckEF.Biscom.ToolKit;
using System.Data;
using System.Data.Common;
using static OmerkckEF.Biscom.ToolKit.Enums;

namespace OmerkckEF.Biscom.DBContext
{
    public class Bisco : IDisposable
    {
        #region Properties
        protected IDALFactory DALFactory { get; set; }
        protected DbConnectionStringBuilder ConnectionStringBuilder { get; set; }
        protected DbConnection? MyConnection { get; set; }
        private DBServer DBServerInfo { get; set; }
        public string? DBSchemaName => DBServerInfo?.DbSchema;
        #endregion

        public Bisco(DBServer dbServerInfo)
        {
            DBServerInfo = dbServerInfo;
            var SelectDBConn = DBServerInfo.DBModel ?? DataBaseType.MySql;
            DALFactory = DALFactoryBase.GetDataBase((DataBaseType)SelectDBConn);

            ConnectionStringBuilder = DALFactory.IDbConnectionStringBuilder(dbServerInfo);
        }

        #region Connection
        protected bool OpenConnection(string? schemaName = "")
        {
            try
            {
                bool IsNewConnection = false;
                if (!string.IsNullOrEmpty(schemaName) && DBServerInfo.DbSchema != schemaName)
                {
                    IsNewConnection = true;
                    DBServerInfo.DbSchema = schemaName;
                }

                if (MyConnection == null || IsNewConnection)
                {
                    MyConnection = (DbConnection)DALFactory.IDbConnection();
                    MyConnection.ConnectionString = ConnectionStringBuilder.ConnectionString;
                }

                if (MyConnection.State != ConnectionState.Open)
                    MyConnection.Open();

                return true;
            }
            catch
            {
                //throw new Exception(ex.ErrorCode switch
                //{
                //    0 => "Server bağlantı hatası. Sistem yöneticisi ile görüşün.",
                //    1042 => "Server bulunamadı. DNS adresi yanlış olabilir.",
                //    1045 => "Server bağlantısı için gerekli Kullanıcı adı veya Şifre yanlış. Sistem yöneticisi ile görüşün.",
                //    _ => "Connection Error : " + ex.Message,
                //});
                CloseConnection();
                return false;
            }
        }
        protected async Task<bool> OpenConnectionAsync(string? schemaName = "")
        {
            try
            {
                bool IsNewConnection = false;
                if (!string.IsNullOrEmpty(schemaName) && DBServerInfo.DbSchema != schemaName)
                {
                    IsNewConnection = true;
                    DBServerInfo.DbSchema = schemaName;
                }

                if (MyConnection == null || IsNewConnection)
                {
                    MyConnection = (DbConnection)DALFactory.IDbConnection();
                    MyConnection.ConnectionString = ConnectionStringBuilder.ConnectionString;
                }

                if (MyConnection.State != ConnectionState.Open)
                    await MyConnection.OpenAsync();

                return true;
            }
            catch
            {
                CloseConnectionAsync();
                return false;
            }
        }

        protected void CloseConnection()
        {
            try
            {
                if (MyConnection != null && MyConnection.State != ConnectionState.Closed)
                    MyConnection.Close();
            }
            catch (Exception ex)
            {
                throw new Exception("While Close Connection, Error : " + ex.Message.ToString());
            }
        }
        protected void CloseConnectionAsync()
        {
            try
            {
                if (MyConnection != null && MyConnection.State != ConnectionState.Closed)
                    MyConnection.CloseAsync();
            }
            catch (Exception ex)
            {
                throw new Exception("While Close Connection, Error : " + ex.Message.ToString());
            }
        }
        #endregion

        #region Regular Operations (Adapter, Query etc.)
        public DbCommand ExeCommand(string queryString, Dictionary<string, object>? parameters = null, CommandType commandType = CommandType.Text)
        {
            try
            {
                var dbCommand = (DbCommand)DALFactory.IDbCommand();
                dbCommand.Connection = MyConnection;
                dbCommand.CommandText = queryString;
                dbCommand.CommandType = commandType;

                if (parameters != null)
                {
                    foreach (KeyValuePair<string, object> param in parameters)
                    {
                        var DbParam = dbCommand.CreateParameter();
                        DbParam.ParameterName = param.Key;
                        DbParam.Value = param.Value ?? DBNull.Value;

                        if (param.Value != null && !string.IsNullOrEmpty(param.Value.ToString()))
                            DbParam.Value = param.Value;
                        else
                            DbParam.Value = DBNull.Value;

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


        public Result<int> RunNonQuery(string queryString, Dictionary<string, object>? parameters = null, bool transaction = false, CommandType commandType = CommandType.Text)
        {
            return RunNonQuery(null, queryString, parameters, transaction, commandType);
        }
        public Result<int> RunNonQuery(string? schema, string queryString, Dictionary<string, object>? parameters = null, bool transaction = false, CommandType commandType = CommandType.Text)
        {
            try
            {
                if (!OpenConnection(schema)) return new Result<int> { IsSuccess = false, Message = "The connection couldn't be opened or created." };

                int exeResult = -1;
                if (transaction)
                {
                    using var _transaction = MyConnection?.BeginTransaction();
                    try
                    {
                        using var command = ExeCommand(queryString, parameters, commandType);

                        command.Transaction = _transaction;
                        exeResult = command.ExecuteNonQuery();
                        _transaction?.Commit();
                    }
                    catch (Exception ex)
                    {
                        _transaction?.Rollback();
                        return new Result<int> { IsSuccess = false, Message = "Error: Rollback finished.\n" + ex.Message };
                    }
                }
                else
                {
                    using var command = ExeCommand(queryString, parameters, commandType);
                    exeResult = command.ExecuteNonQuery();
                }
                return new Result<int> { IsSuccess = exeResult >= 0, Message = exeResult < 0 ? "Error : ExecuteNonQueryAsync Process" : "", Data = exeResult };
            }
            catch (Exception ex)
            {
                CloseConnection();
                return new Result<int> { IsSuccess = false, Data = -1, Message = "Executing NonQuery Error: " + ex.Message };
            }
        }

        public Result<object> RunScaler(string queryString, Dictionary<string, object>? parameters = null, CommandType commandType = CommandType.Text)
        {
            return RunScaler(null, queryString, parameters, commandType);
        }
        public Result<object> RunScaler(string? schema, string queryString, Dictionary<string, object>? parameters = null, CommandType commandType = CommandType.Text)
        {
            try
            {
                if (!OpenConnection(schema)) return new Result<object> { IsSuccess = false, Message = "The connection couldn't be opened or created." };

                using var command = ExeCommand(queryString, parameters, commandType);
                var exeResult = command.ExecuteScalar();

                return new Result<object> { IsSuccess = true, Data = exeResult };
            }
            catch (Exception ex)
            {
                CloseConnection();
                return new Result<object> { IsSuccess = false, Message = "Executing ExecuteScalar Error: " + ex.Message };
            }
        }

        public Result<DataTable?> RunDataTable(string queryString, Dictionary<string, object>? parameters = null, CommandType commandType = CommandType.Text)
        {
            return RunDataTable(null, queryString, parameters, commandType);
        }
        public Result<DataTable?> RunDataTable(string? schema, string queryString, Dictionary<string, object>? parameters = null, CommandType commandType = CommandType.Text)
        {
            try
            {
                if (!OpenConnection(schema)) return new Result<DataTable?> { IsSuccess = false, Message = "The connection couldn't be opened or created." };

                using var command = ExeCommand(queryString, parameters, commandType);
                using var dataReader = command.ExecuteReader(CommandBehavior.CloseConnection);

                var dataTable = new DataTable();
                if (dataReader != null && !dataReader.IsClosed)
                    dataTable.Load(dataReader);

                return new Result<DataTable?> { IsSuccess = true, Data = dataTable };
            }
            catch (Exception ex)
            {
                CloseConnection();
                return new Result<DataTable?> { IsSuccess = false, Message = "Executing RunDataTable Error: " + ex.Message };
            }
        }
        #endregion

        #region ASYNC Regular Operations (Adapter, Query etc.)
        public async Task<Result<int>> RunNonQueryAsync(string queryString, Dictionary<string, object>? parameters = null, bool transaction = false, CommandType commandType = CommandType.Text)
        {
            return await RunNonQueryAsync(null, queryString, parameters, transaction, commandType);
        }
        public async Task<Result<int>> RunNonQueryAsync(string? schema, string queryString, Dictionary<string, object>? parameters = null, bool transaction = false, CommandType commandType = CommandType.Text)
        {
            try
            {
                if (!await OpenConnectionAsync(schema)) return new Result<int> { IsSuccess = false, Message = "The connection couldn't be opened or created." };

                int exeResult = 0;
                if (transaction)
                {
                    using var _transaction = MyConnection?.BeginTransaction();
                    try
                    {
                        using var command = ExeCommand(queryString, parameters, commandType);

                        command.Transaction = _transaction;
                        exeResult = Convert.ToInt32(await command.ExecuteNonQueryAsync());
                        _transaction?.Commit();
                    }
                    catch (Exception ex)
                    {
                        _transaction?.RollbackAsync();
                        return new Result<int> { IsSuccess = false, Message = "Error: Rollback finished.\n" + ex.Message };
                    }
                }
                else
                {
                    using var command = ExeCommand(queryString, parameters, commandType);
                    exeResult = Convert.ToInt32(await command.ExecuteNonQueryAsync());
                }
                return new Result<int> { IsSuccess = exeResult > 0, Message = exeResult <= 0 ? "Error : ExecuteNonQueryAsync Process" : "", Data = exeResult };
            }
            catch (Exception ex)
            {
                CloseConnectionAsync();
                return new Result<int> { IsSuccess = false, Message = "Executing NonQueryAsync Error: " + ex.Message };
            }
        }

        public async Task<Result<object>> RunScalerAsync(string queryString, Dictionary<string, object>? parameters = null, CommandType commandType = CommandType.Text)
        {
            return await RunScalerAsync(null, queryString, parameters, commandType);
        }
        public async Task<Result<object>> RunScalerAsync(string? schema, string queryString, Dictionary<string, object>? parameters = null, CommandType commandType = CommandType.Text)
        {
            try
            {
                if (!await OpenConnectionAsync(schema)) return new Result<object> { IsSuccess = false, Message = "The connection couldn't be opened or created." };

                using var command = ExeCommand(queryString, parameters, commandType);
                var exeResult = await command.ExecuteScalarAsync() ?? null;

                return new Result<object> { IsSuccess = true, Data = exeResult };
            }
            catch (Exception ex)
            {
                CloseConnectionAsync();
                return new Result<object> { IsSuccess = false, Message = "Executing ExecuteScalarAsync Error: " + ex.Message };
            }
        }

        public async Task<Result<DataTable>> RunDataTableAsync(string queryString, Dictionary<string, object>? parameters = null, CommandType commandType = CommandType.Text)
        {
            return await RunDataTableAsync(null, queryString, parameters, commandType);
        }
        public async Task<Result<DataTable>> RunDataTableAsync(string? schema, string queryString, Dictionary<string, object>? parameters = null, CommandType commandType = CommandType.Text)
        {
            try
            {
                if (!await OpenConnectionAsync(schema)) return new Result<DataTable> { IsSuccess = false, Message = "The connection couldn't be opened or created." };

                DataTable? dataTable = new();

                using var command = ExeCommand(queryString, parameters, commandType);
                using var reader = await command.ExecuteReaderAsync(CommandBehavior.CloseConnection);
                dataTable.Load(reader);

                return new Result<DataTable> { IsSuccess = true, Data = dataTable };
            }
            catch (Exception ex)
            {
                CloseConnectionAsync();
                return new Result<DataTable> { IsSuccess = false, Message = "Executing RunDataTableAsync Error: " + ex.Message };
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
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // Managed resources are released here.
                    MyConnection?.Dispose();
                    DBServerInfo = DBServer.DBServerInfo ?? new DBServer();
                }

                // Unmanaged resources are released here.
                disposed = true;
            }
        }
        ~Bisco() { Dispose(false); }
        #endregion
    }
}