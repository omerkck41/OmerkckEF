using OmerkckEF.Biscom.Interfaces;
using Oracle.ManagedDataAccess.Client;
using System.Data;
using System.Data.Common;

namespace OmerkckEF.Biscom.DBContext.DBSchemas
{
	public class OracleDAL:IDALFactory
    {
        public OracleDAL() { }

        public override IDbConnection IDbConnection() => new OracleConnection();
        public override IDbCommand IDbCommand() => new OracleCommand();
        public override IDbDataParameter IDbParameter() => new OracleParameter();
        public override IDbDataAdapter IDbAdapter() => new OracleDataAdapter();
        public override IDbTransaction IDbTransaction() => new OracleConnection().BeginTransaction();


        public override DbConnectionStringBuilder IDbConnectionStringBuilder(DBServer dbServerInfo)
        {
            OracleConnectionStringBuilder builder = new OracleConnectionStringBuilder
            {
                DataSource = $"(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST={dbServerInfo?.DbIp})(PORT={dbServerInfo?.DbPort ?? 1521}))))",
                UserID = dbServerInfo?.DbUser ?? "myUser", // Veritabanı kullanıcı adı
                Password = dbServerInfo?.DbPassword ?? "myPassword", // Veritabanı şifresi
                Pooling = dbServerInfo?.DbPooling ?? true,
                ConnectionTimeout = dbServerInfo?.DbConnTimeout ?? 15,
                ConnectionLifeTime = dbServerInfo?.DbConnLifetime ?? 1200,
            };

            // SSL yapılandırması burada belirtilmez; bu, Oracle veritabanı ve sertifikalarınızın yapısına bağlıdır

            return builder;
        }
    }
}
