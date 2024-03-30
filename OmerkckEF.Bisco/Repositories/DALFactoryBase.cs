using OmerkckEF.Biscom.DBContext.DBSchemas;
using OmerkckEF.Biscom.Interfaces;
using static OmerkckEF.Biscom.ToolKit.Enums;

namespace OmerkckEF.Biscom.Repositories
{
    public class DALFactoryBase
    {
        public DALFactoryBase() { }

        public static IDALFactory GetDataBase(DataBaseType dbType) => dbType switch
        {
            DataBaseType.MySql => new MySqlDAL(),
            DataBaseType.Sql => new SqlDAL(),
            DataBaseType.Oracle => new OracleDAL(),
            DataBaseType.PostgreSQL => new PostgreSQLDAL(),
            _ => new MySqlDAL()
        };
    }
}
