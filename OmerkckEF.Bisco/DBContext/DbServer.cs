using OmerkckEF.Biscom.ToolKit;
using static OmerkckEF.Biscom.ToolKit.Enums;

namespace OmerkckEF.Biscom.DBContext
{
    public class DBServer
    {
        [DataName]
        public int DbServerId { get; set; }
        [DataName]
        public string? DbIp { get; set; }
        [DataName]
        public int DbPort { get; set; } = 3306;
        [DataName]
        public int DbType { get; set; } = 1;
        [DataName]
        public string? DbUser { get; set; }
        [DataName]
        public string? DbPassword { get; set; }
        [DataName]
        public string? DbSchema { get; set; }
        [DataName]
        public bool DbPooling { get; set; } = true;
        [DataName]
        public int DbMaxpoolsize { get; set; } = 100;
        [DataName]
        public int DbConnLifetime { get; set; } = 300;
        [DataName]
        public int DbConnTimeout { get; set; } = 500;
        [DataName]
        public bool DbAllowuserinput { get; set; } = true;
        [DataName]
        public bool DbActivity { get; set; }

        public DataBaseType? DBModel => (DataBaseType)DbType;

        public static DBServer? DBServerInfo { get; set; }

        public DBServer() => DBServerInfo = this;
    }
}