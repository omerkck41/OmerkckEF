using System.ComponentModel.DataAnnotations;
using static OmerkckEF.Biscom.ToolKit.Enums;

namespace OmerkckEF.Biscom.ToolKit
{
    public class Attributes { }

    [AttributeUsage(AttributeTargets.Property)]
    public class ClassNameAttribute : Attribute
    {

    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
    public class UniqueAttribute : ValidationAttribute
    {

    }

    [AttributeUsage(AttributeTargets.Property)]
    public class DataNameAttribute : Attribute
    {
        public DataNameAttribute()
        {
            ValueName = [];
        }

        public DataNameAttribute(params string[] valueNames)
        {
            ValueName = [.. valueNames];
        }

        public List<string> ValueNames
        {
            get { return ValueName; }
            set { ValueName = value; }
        }
        protected List<string> ValueName { get; set; }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class Table : Attribute
    {
        public Table()
        {
            TableType = TableType.PrimaryTable;
            TableName = "Tables";
            PrimaryKey = "id";
            SearchFields = ["id"];
        }

        public string? TableName { get; set; }
        public string? PrimaryKey { get; set; }
        public TableType TableType { get; set; }
        public string[]? CompositeKeys { get; set; }
        public string[]? SearchFields { get; set; }
        public string? IsActiveColm { get; set; }
        public int? Uni { get; set; }
    }
}
