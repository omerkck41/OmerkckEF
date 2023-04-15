using OmerkckEF.Biscom.Interfaces;
using System.Data;
using Oracle.ManagedDataAccess.Client;

namespace OmerkckEF.Biscom.DBContext.DBSchemas
{
    public class OracleDAL:IDALFactory
    {
        private string? _ConnectionString = "Data Source=myServerAddress:myPortNumber/myServiceName;User Id=myUsername;Password=myPassword;";
        public string ConnectionString
        {
            get
            {
                if (_ConnectionString == string.Empty || _ConnectionString == null)
                    throw new ArgumentException("Invalid database connection string.");

                return _ConnectionString;
            }
            set { _ConnectionString = value; }
        }

        public OracleDAL(string connectionString) { this.ConnectionString = connectionString; }

        public OracleDAL() { }

        public IDbConnection IDbConnection() => new OracleConnection();
        public IDbCommand IDbCommand() => new OracleCommand();
        public IDbDataParameter IDbParameter() => new OracleParameter();
        public IDbDataAdapter IDbAdapter() => new OracleDataAdapter();
        public IDbTransaction IDbTransaction() => new OracleConnection().BeginTransaction();
    }
}
