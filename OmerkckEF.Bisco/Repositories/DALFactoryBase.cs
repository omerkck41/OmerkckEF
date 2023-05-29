using OmerkckEF.Biscom.DBContext.DBSchemas;
using OmerkckEF.Biscom.Interfaces;
using static OmerkckEF.Biscom.ToolKit.Enums;

namespace OmerkckEF.Biscom.Repositories
{
    public class DALFactoryBase
    {
        public DALFactoryBase() { }

        #region Different use of Switch
            public static DataBaseType dbType { get; set; }
            public static IDALFactory GetDB
            {
                get => dbType switch
                {
                    DataBaseType.MySql => new MySqlDAL(),
                    DataBaseType.Sql => new SqlDAL(),
                    DataBaseType.Oracle => new OracleDAL(),
                    DataBaseType.PostgreSQL => new PostgreSQLDAL(),
                    _ => new MySqlDAL()
                };
            }
        #endregion
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
