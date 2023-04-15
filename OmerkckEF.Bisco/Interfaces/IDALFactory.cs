using MongoDB.Driver.Core.Configuration;
using System.Data;

namespace OmerkckEF.Biscom.Interfaces
{
    public interface IDALFactory
    {
        public string ConnectionString { get; set; }

        IDbConnection IDbConnection();
        IDbCommand IDbCommand();
        IDbDataParameter IDbParameter();
        IDbDataAdapter IDbAdapter();
        IDbTransaction IDbTransaction();
    }
}
