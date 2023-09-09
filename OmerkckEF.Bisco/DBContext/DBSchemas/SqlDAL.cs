using Npgsql;
using OmerkckEF.Biscom.Interfaces;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;

namespace OmerkckEF.Biscom.DBContext.DBSchemas
{
    public class SqlDAL : IDALFactory
    {
        public SqlDAL() { }

        public override IDbConnection IDbConnection() => new SqlConnection();
        public override IDbCommand IDbCommand() => new SqlCommand();
        public override IDbDataParameter IDbParameter() => new SqlParameter();
        public override IDbDataAdapter IDbAdapter() => new SqlDataAdapter();
        public override IDbTransaction IDbTransaction() => new SqlConnection().BeginTransaction();


        public override DbConnectionStringBuilder IDbConnectionStringBuilder(DBServer dbServerInfo)
        {
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder
            {
                DataSource = dbServerInfo?.DbIp ?? "127.0.0.1",
                InitialCatalog = dbServerInfo?.DbSchema ?? "",
                UserID = dbServerInfo?.DbUser ?? "sa",
                Password = dbServerInfo?.DbPassword ?? "password123",
                Pooling = dbServerInfo?.DbPooling ?? true,
                MaxPoolSize = dbServerInfo?.DbMaxpoolsize ?? 100,
                ConnectTimeout = dbServerInfo?.DbConnTimeout ?? 15,
                // Diğer bağlantı seçeneklerini buraya ekleyebilirsiniz
            };

            // SslMode enum'u ile karşılık gelen bir değeri varsa, SSL seçeneklerini ayarlayın
            if (Enum.TryParse(dbServerInfo?.DbSslMode, out SslMode sslMode))
            {
                // SSL seçeneklerini ayarlayın (Örnek: TrustServerCertificate = true)
                switch (sslMode)
                {
                    case SslMode.Disable:
                        builder.TrustServerCertificate = false;
                        builder.Encrypt = false;
                        break;
                    case SslMode.Prefer:
                        builder.Encrypt = true;
                        builder.TrustServerCertificate = false; // veya true, tercihinize bağlı
                        break;
                    case SslMode.Require:
                        builder.Encrypt = true;
                        builder.TrustServerCertificate = false; // veya true, güvence durumuna bağlı
                        break;
                    case SslMode.VerifyCA:
                        builder.Encrypt = true;
                        builder.TrustServerCertificate = false; // veya true, CA doğrulama tercihinize bağlı
                        break;
                    case SslMode.VerifyFull:
                        builder.Encrypt = true;
                        builder.TrustServerCertificate = false; // veya true, tam sertifika doğrulama tercihinize bağlı
                                                                // Diğer ek ayarları burada da yapabilirsiniz
                        break;
                    default:
                        builder.TrustServerCertificate = false;
                        builder.Encrypt = false; // default durumda Disable ayarlarını kullanın
                        break;
                }
            }

            return builder;
        }
    }
}
