using OmerkckEF.Biscom.Interfaces;
using System.Data;
using Npgsql;

namespace OmerkckEF.Biscom.DBContext.DBSchemas
{
    public class PostgreSQLDAL:IDALFactory
    {
        private string? _ConnectionString = "Server=myServerAddress; Port=myPortNumber; Database=myDataBase; User Id=myUsername; Password=myPassword;";
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

        public PostgreSQLDAL(string connectionString) { this.ConnectionString = connectionString; }
        public PostgreSQLDAL() { }

        public IDbConnection IDbConnection() => new NpgsqlConnection();
        public IDbCommand IDbCommand() => new NpgsqlCommand();
        public IDbDataParameter IDbParameter() => new NpgsqlParameter();
        public IDbDataAdapter IDbAdapter() => new NpgsqlDataAdapter();
        public IDbTransaction IDbTransaction() => new NpgsqlConnection().BeginTransaction();
    }
}
