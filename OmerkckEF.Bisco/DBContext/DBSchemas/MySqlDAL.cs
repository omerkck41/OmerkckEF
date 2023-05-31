using MySql.Data.MySqlClient;
using OmerkckEF.Biscom.Interfaces;
using System.Data;

namespace OmerkckEF.Biscom.DBContext.DBSchemas
{
	public class MySqlDAL : IDALFactory
    {
        private string? _ConnectionString = "server=127.0.0.1;user=root;database=gmt_db;port=3306;password=1q2w3e4r;";
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

        public MySqlDAL(string connectionString) { ConnectionString = connectionString; }
        public MySqlDAL() { }

        public IDbConnection IDbConnection() => new MySqlConnection();
        public IDbCommand IDbCommand()=> new MySqlCommand();
        public IDbDataParameter IDbParameter() => new MySqlParameter();
        public IDbDataAdapter IDbAdapter() => new MySqlDataAdapter();
        public IDbTransaction IDbTransaction() => new MySqlConnection().BeginTransaction();
	}
}
