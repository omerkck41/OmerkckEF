using OmerkckEF.Biscom.Interfaces;
using System.Data;
using System.Data.SqlClient;

namespace OmerkckEF.Biscom.DBContext.DBSchemas
{
    public class SqlDAL : IDALFactory
    {
        private string? _ConnectionString = "Data Source=xxxx;Initial Catalog=xxxx;User ID=xxxx;Password=xxxx";
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

        public SqlDAL(string connectionString) { this.ConnectionString = connectionString; }
        public SqlDAL() { }

        public IDbConnection IDbConnection() => new SqlConnection();
        public IDbCommand IDbCommand() => new SqlCommand();
        public IDbDataParameter IDbParameter() => new SqlParameter();
        public IDbDataAdapter IDbAdapter() => new SqlDataAdapter();
        public IDbTransaction IDbTransaction() => new SqlConnection().BeginTransaction();
    }
}
