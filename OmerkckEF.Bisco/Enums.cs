namespace OmerkckEF.Biscom
{
    public class Enums
    {
        public enum DataBaseType
        { 
            None = 0,
            MySql = 1,
            Sql = 2,
            Oracle = 3,
            PostgreSQL = 4
        }

        public enum TableType
        {
            PrimaryTable = 0,
            CompositeTable
        }
    }
}
