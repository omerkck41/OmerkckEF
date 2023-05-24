using OmerkckEF.Biscom.Interfaces;
using OmerkckEF.Biscom.Repositories;
using System.Data;
using System.Data.Common;
using static OmerkckEF.Biscom.Enums;

namespace OmerkckEF.Biscom.DBContext
{
	public partial class Bisco : IDisposable
    {
        #region Properties
        private IDALFactory DALFactory { get; set; }
        public DbConnection? MyConnection { get; set; }
        public DbServer? DbServer { get; set; }
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

		private void CloseConnection()
		{
			try
			{
				if (this.MyConnection?.State != ConnectionState.Closed)
				{
					this.MyConnection?.Close();
				}
			}
			catch (Exception ex)
			{
				Bisco_ErrorInfo = "While Close Connection, Error : " + ex.Message.ToString();
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


        public Result<int> RunNonQuery(string queryString, Dictionary<string, object>? parameters = null, bool transaction = false, CommandType commandType = CommandType.Text)
        {
            return RunNonQuery(this.connSchemaName, queryString, parameters, transaction, commandType);
        }
        public Result<int> RunNonQuery(string schema, string queryString, Dictionary<string, object>? parameters = null, bool transaction = false, CommandType commandType = CommandType.Text)
        {
            try
            {
                this.connSchemaName = string.IsNullOrEmpty(schema) ? this.connSchemaName : schema;
                if (!OpenConnection(this.connSchemaName)) return new Result<int> { IsSuccess = false, Message = "The connection couldn't be opened or created." };

                int exeResult = 0;
                using (MyConnection)

                    if (transaction)
                    {
                        using var _transaction = this.MyConnection?.BeginTransaction();
                        try
                        {
							using var command = ExeCommand(queryString, parameters, commandType);

							command.Transaction = _transaction;
							exeResult = command.ExecuteNonQuery();
							_transaction?.Commit();
						}
                        catch
                        {
                            _transaction?.Rollback();
							return new Result<int> { IsSuccess = false, Message = "Error: Rollback finished." };
						}                        
                    }
                    else
                    {
                        using var command = ExeCommand(queryString, parameters, commandType);
                        exeResult = command.ExecuteNonQuery();
                    }

                return new Result<int> { IsSuccess = exeResult > 0, Message = exeResult <= 0 ? "Error : ExecuteNonQueryAsync Process" : "", Data = exeResult };
            }
            catch (Exception ex)
            {
				CloseConnection();
				return new Result<int> { IsSuccess = false, Message = "Executing NonQuery Error: " + ex.Message };
            }
		}

        public Result<object?> RunScaler(string queryString, Dictionary<string, object>? parameters = null, bool transaction = false, CommandType commandType = CommandType.Text)
        {
            return RunScaler(this.connSchemaName, queryString, parameters, transaction, commandType);
        }
        public Result<object?> RunScaler(string schema, string queryString, Dictionary<string, object>? parameters = null, bool transaction = false, CommandType commandType = CommandType.Text)
        {
            try
            {
				this.connSchemaName = string.IsNullOrEmpty(schema) ? this.connSchemaName : schema;
				if (!OpenConnection(this.connSchemaName)) return new Result<object?> { IsSuccess = false, Message = "The connection couldn't be opened or created." };
				using var connection = this.MyConnection;

                object? exeResult = null;
				if (transaction)
                {
					using var _transaction = this.MyConnection?.BeginTransaction();
					try
                    {						
						using var command = ExeCommand(queryString, parameters, commandType);

						command.Transaction = _transaction;
						exeResult = command.ExecuteScalar() ?? null;
						_transaction?.Commit();
					}
                    catch
                    {
                        _transaction?.Rollback();
						return new Result<object?> { IsSuccess = false, Message = "Error: Rollback finished." };
					}                    
                }
                else
                {
                    using var command = ExeCommand(queryString, parameters, commandType);
					exeResult = command?.ExecuteScalar() ?? null;
                }
				return new Result<object?> { IsSuccess = true, Data = exeResult };
			}
            catch (Exception ex)
            {
				CloseConnection();
				return new Result<object?> { IsSuccess = false, Message = "Executing ExecuteScalar Error: " + ex.Message };
			}
        }

        public Result<DbDataReader?> RunDataReader(string queryString, Dictionary<string, object>? parameters = null, CommandType commandType = CommandType.Text)
        {
            return RunDataReader(this.connSchemaName, queryString, parameters, commandType);
        }
        public Result<DbDataReader?> RunDataReader(string schema, string queryString, Dictionary<string, object>? parameters = null, CommandType commandType = CommandType.Text)
        {
            try
            {
				this.connSchemaName = string.IsNullOrEmpty(schema) ? this.connSchemaName : schema;
				if (!OpenConnection(this.connSchemaName)) return new Result<DbDataReader?> { IsSuccess = false, Message = "The connection couldn't be opened or created." };

				using var connection = this.MyConnection;
				using var command = ExeCommand(queryString, parameters, commandType);
                var reader = command.ExecuteReader(CommandBehavior.CloseConnection);

				return new Result<DbDataReader?> { IsSuccess = true, Data = reader };
			}
            catch (Exception ex)
            {
				CloseConnection();
				return new Result<DbDataReader?> { IsSuccess = false, Message = "Executing RunDataReader Error: " + ex.Message };
			}
        }

        public Result<DataTable?> RunDataTable(string queryString, Dictionary<string, object>? parameters = null, CommandType commandType = CommandType.Text)
        {
            return RunDataTable(this.connSchemaName, queryString, parameters, commandType);
        }
        public Result<DataTable?> RunDataTable(string schema, string queryString, Dictionary<string, object>? parameters = null, CommandType commandType = CommandType.Text)
        {
            try
            {
				this.connSchemaName = string.IsNullOrEmpty(schema) ? this.connSchemaName : schema;
				if (!OpenConnection(this.connSchemaName)) return new Result<DataTable?> { IsSuccess = false, Message = "The connection couldn't be opened or created." };
				using var connection = this.MyConnection;

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
            return await RunNonQueryAsync(this.connSchemaName, queryString, parameters, transaction, commandType);
        }
        public async Task<Result<int>> RunNonQueryAsync(string schema, string queryString, Dictionary<string, object>? parameters = null, bool transaction = false, CommandType commandType = CommandType.Text)
        {
            try
            {
				this.connSchemaName = string.IsNullOrEmpty(schema) ? this.connSchemaName : schema;
				if (!await OpenConnectionAsync(this.connSchemaName)) return new Result<int> { IsSuccess = false, Message = "The connection couldn't be opened or created." };
                
                int exeResult = 0;
				using (this.MyConnection)
                {
                    if (transaction)
                    {
                        using var _transaction = this.MyConnection?.BeginTransaction();
                        try
                        {
							using var command = ExeCommand(queryString, parameters, commandType);

							command.Transaction = _transaction;
							exeResult = Convert.ToInt32(await command.ExecuteNonQueryAsync());
							_transaction?.Commit();
						}
                        catch
                        {
                            _transaction?.RollbackAsync();
							return new Result<int> { IsSuccess = false, Message = "Error: Rollback finished." };
						}
                    }
                    else
                    {
                        using var command = ExeCommand(queryString, parameters, commandType);
						exeResult = Convert.ToInt32(await command.ExecuteNonQueryAsync());
                    }
                }
				return new Result<int> { IsSuccess = exeResult > 0, Message= exeResult <= 0 ? "Error : ExecuteNonQueryAsync Process" : "", Data = exeResult };
			}
            catch (Exception ex)
            {
				CloseConnection();
				return new Result<int> { IsSuccess = false, Message = "Executing NonQueryAsync Error: " + ex.Message };
			}
        }

		public async Task<Result<object?>> RunScalerAsync(string queryString, Dictionary<string, object>? parameters = null, bool transaction = false, CommandType commandType = CommandType.Text)
		{
			return await RunScalerAsync(this.connSchemaName, queryString, parameters, transaction, commandType);
		}
		public async Task<Result<object?>> RunScalerAsync(string schema, string queryString, Dictionary<string, object>? parameters = null, bool transaction = false, CommandType commandType = CommandType.Text)
		{
            try
            {
				this.connSchemaName = string.IsNullOrEmpty(schema) ? this.connSchemaName : schema;
				if (!await OpenConnectionAsync(this.connSchemaName)) return new Result<object?> { IsSuccess = false, Message = "The connection couldn't be opened or created." };

                object? exeResult = null;
				using (this.MyConnection)
                {
                    if (transaction)
                    {
                        using var _transaction = this.MyConnection?.BeginTransaction();
                        try
                        {
							using var command = ExeCommand(queryString, parameters, commandType);

							command.Transaction = _transaction;
							exeResult = await command.ExecuteScalarAsync() ?? null;
							_transaction?.Commit();
						}
                        catch
                        {
                            _transaction?.RollbackAsync();
							return new Result<object?> { IsSuccess = false, Message = "Error: Rollback finished." };
						}
                    }
                    else
                    {
                        using var command = ExeCommand(queryString, parameters, commandType);
						exeResult = await command.ExecuteScalarAsync() ?? null;
                    }
                }
                return new Result<object?> { IsSuccess = true, Data = exeResult };
			}
            catch (Exception ex)
            {
				CloseConnection();
				return new Result<object?> { IsSuccess = false, Message = "Executing ExecuteScalarAsync Error: " + ex.Message };
			}
        }

		public async Task<Result<DataTable>> RunSelectDataAsync(string queryString, Dictionary<string, object>? parameters = null, bool transaction = false, CommandType commandType = CommandType.Text)
        {
            return await RunSelectDataAsync(this.connSchemaName, queryString, parameters, transaction, commandType);
        }
        public async Task<Result<DataTable>> RunSelectDataAsync(string schema, string queryString, Dictionary<string, object>? parameters = null, bool transaction = false, CommandType commandType = CommandType.Text)
        {
            try
            {
                this.connSchemaName = string.IsNullOrEmpty(schema) ? this.connSchemaName : schema;
                if (!await OpenConnectionAsync(this.connSchemaName)) return new Result<DataTable> { IsSuccess = false, Message = "The connection couldn't be opened or created." };

                DataTable? dataTable = new();
                using (this.MyConnection)
                {
                    using var command = ExeCommand(queryString, parameters, commandType);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        dataTable.Load(reader);
                    }
                }
                return new Result<DataTable> { IsSuccess = true, Data = dataTable };
            }
            catch (Exception ex)
            {
				CloseConnection();
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