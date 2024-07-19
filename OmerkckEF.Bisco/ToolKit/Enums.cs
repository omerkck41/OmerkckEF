namespace OmerkckEF.Biscom.ToolKit
{
    public class Enums
    {
        public enum DataBaseType
        {
            None = 0,
            MySql = 1,
            Sql = 2,
            Oracle = 3,
            PostgreSQL = 4,
            SQLite = 5
        }

        public enum TableType
        {
            PrimaryTable = 0,
            CompositeTable
        }

        public enum TableColumnAttribute
        {
            PrimaryKey = 0,
            NotNull = 1,
            Unique,
            Binary,
            Unsigned,
            ZeroFill,
            AutoIncrement,
            Generated
        }
    }
}
