using Npgsql;
using OmerkckEF.Biscom.Interfaces;
using System.Data;
using System.Data.Common;

namespace OmerkckEF.Biscom.DBContext.DBSchemas
{
    public class PostgreSQLDAL:IDALFactory
    {
        public PostgreSQLDAL() { }

        public override IDbConnection IDbConnection() => new NpgsqlConnection();
        public override IDbCommand IDbCommand() => new NpgsqlCommand();
        public override IDbDataParameter IDbParameter() => new NpgsqlParameter();
        public override IDbDataAdapter IDbAdapter() => new NpgsqlDataAdapter();
        public override IDbTransaction IDbTransaction() => new NpgsqlConnection().BeginTransaction();

        public override DbConnectionStringBuilder IDbConnectionStringBuilder(DBServer dbServerInfo)
        {
            NpgsqlConnectionStringBuilder builder = new NpgsqlConnectionStringBuilder
            {
                Host = dbServerInfo?.DbIp ?? "localhost", // Veritabanı sunucusunun IP adresi
                Port = dbServerInfo?.DbPort ?? 5432, // PostgreSQL bağlantı noktası
                Database = dbServerInfo?.DbSchema ?? "myDatabase", // Veritabanı adı
                Username = dbServerInfo?.DbUser ?? "myUser", // Veritabanı kullanıcı adı
                Password = dbServerInfo?.DbPassword ?? "myPassword", // Veritabanı şifresi
                Pooling = dbServerInfo?.DbPooling ?? true,
                MaxPoolSize = dbServerInfo?.DbMaxpoolsize ?? 100,
                Timeout = dbServerInfo?.DbConnTimeout ?? 15, // Bağlantı zaman aşımı (saniye cinsinden)
                
            };

            if (Enum.TryParse(dbServerInfo?.DbSslMode, true, out SslMode sslMode))
                builder.SslMode = sslMode;
            else
                builder.SslMode = SslMode.Disable;

            return builder;
        }
    }
}