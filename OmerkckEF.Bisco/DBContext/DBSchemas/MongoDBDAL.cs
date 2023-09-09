using MySql.Data.MySqlClient;
using OmerkckEF.Biscom.Interfaces;
using System.Data;
using System.Data.Common;

namespace OmerkckEF.Biscom.DBContext.DBSchemas
{
    public class MongoDBDAL : IDALFactory
    {
        public MongoDBDAL() { }

        public override IDbConnection IDbConnection() => new MySqlConnection();
        public override IDbCommand IDbCommand() => new MySqlCommand();
        public override IDbDataParameter IDbParameter() => new MySqlParameter();
        public override IDbDataAdapter IDbAdapter() => new MySqlDataAdapter();
        public override IDbTransaction IDbTransaction() => new MySqlConnection().BeginTransaction();


        public override DbConnectionStringBuilder IDbConnectionStringBuilder(DBServer dbServerInfo)
        {
            throw new NotImplementedException();
        }
    }
}