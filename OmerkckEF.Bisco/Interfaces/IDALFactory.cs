using OmerkckEF.Biscom.DBContext;
using System.Data;
using System.Data.Common;

namespace OmerkckEF.Biscom.Interfaces
{
    public abstract class IDALFactory
    {
        public abstract IDbConnection IDbConnection();
        public abstract IDbCommand IDbCommand();
        public abstract IDbDataParameter IDbParameter();
        public abstract IDbDataAdapter IDbAdapter();
        public abstract IDbTransaction IDbTransaction();

        public abstract DbConnectionStringBuilder IDbConnectionStringBuilder(DBServer dbServerInfo);
    }
}
