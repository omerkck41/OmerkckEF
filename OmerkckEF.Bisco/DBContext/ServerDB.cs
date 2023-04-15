using static OmerkckEF.Biscom.Enums;

namespace OmerkckEF.Biscom.DBContext
{
    public class ServerDB
    {
        [DataName]
        public int ServerDBId { get; set; }
        [DataName]
        public string DBName { get; set; } = string.Empty;
        [DataName]
        public string? DBIp { get; set; }
        [DataName]
        public int DBPort { get; set; } = 3306;
        [DataName]
        public int DBType { get; set; } = 1;
        [DataName]
        public string? DBUser { get; set; }
        [DataName]
        public string? DBPassword { get; set; }
        [DataName]
        public string? DBSchema { get; set; }
        [DataName]
        public bool DBPooling { get; set; } = true;
        [DataName]
        public int DBMaxpoolsize { get; set; } = 100;
        [DataName]
        public int DBConnLifetime { get; set; } = 300;
        [DataName]
        public int DBConnTimeout { get; set; } = 500;
        [DataName]
        public bool DBAllowuserinput { get; set; } = true;
        [DataName]
        public bool DBActivity { get; set; }

        public DataBaseType? DataBaseType => (DataBaseType)DBType;

        public ServerDB? serverDB { get; set; }
    }
}
