using MySqlConnector;
using OmerkckEF.Biscom.Interfaces;
using System.Data;
using System.Data.Common;

namespace OmerkckEF.Biscom.DBContext.DBSchemas
{
    public class MySqlDAL : IDALFactory
    {
        public MySqlDAL() { }

        public override IDbConnection IDbConnection() => new MySqlConnection();
        public override IDbCommand IDbCommand() => new MySqlCommand();
        public override IDbDataParameter IDbParameter() => new MySqlParameter();
        public override IDbDataAdapter IDbAdapter() => new MySqlDataAdapter();
        public override IDbTransaction IDbTransaction() => new MySqlConnection().BeginTransaction();


        public override DbConnectionStringBuilder IDbConnectionStringBuilder(DBServer dbServerInfo)
        {
            MySqlConnectionStringBuilder builder = new()
            {
                Server = dbServerInfo?.DbIp ?? "127.0.0.1",
                Port = (uint)(dbServerInfo?.DbPort ?? 3336),
                Database = dbServerInfo?.DbSchema ?? "",
                UserID = dbServerInfo?.DbUser ?? "root",
                Password = dbServerInfo?.DbPassword ?? "root123",
                Pooling = dbServerInfo?.DbPooling ?? true,
                MaximumPoolSize = (uint)(dbServerInfo?.DbMaxpoolsize ?? 100),
                ConnectionLifeTime = (uint)(dbServerInfo?.DbConnLifetime ?? 300),
                ConnectionTimeout = (uint)(dbServerInfo?.DbConnTimeout ?? 500),
                AllowUserVariables = dbServerInfo?.DbAllowuserinput ?? true,
                SslMode = (MySqlSslMode)Enum.Parse(typeof(MySqlSslMode), dbServerInfo!.DbSslMode)
            };

            if (Enum.TryParse(dbServerInfo?.DbSslMode, true, out MySqlSslMode sslMode))
                builder.SslMode = sslMode;
            else
                builder.SslMode = MySqlSslMode.Disabled;


            return builder;
        }
    }
}