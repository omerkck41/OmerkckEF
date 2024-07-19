using OmerkckEF.Biscom.Interfaces;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;

namespace OmerkckEF.Biscom.DBContext.DBSchemas
{
    public class SQLiteDAL : IDALFactory
    {
        public SQLiteDAL() { }

        public override IDbConnection IDbConnection() => new SQLiteConnection();
        public override IDbCommand IDbCommand() => new SQLiteCommand();
        public override IDbDataParameter IDbParameter() => new SQLiteParameter();
        public override IDbDataAdapter IDbAdapter() => new SQLiteDataAdapter();
        public override IDbTransaction IDbTransaction() => new SQLiteConnection().BeginTransaction();


        public override DbConnectionStringBuilder IDbConnectionStringBuilder(DBServer dbServerInfo)
        {
            SQLiteConnectionStringBuilder builder = new()
            {
                DataSource = dbServerInfo?.DbIp ?? "OmerkckEFDB.db",
                Pooling = dbServerInfo?.DbPooling ?? true,
                Version = 3,
                JournalMode = SQLiteJournalModeEnum.Wal,
                SyncMode = SynchronizationModes.Normal,
                CacheSize = 2000,
                //Password = dbServerInfo?.DbPassword ?? "",
            };

            return builder;
        }
    }
}