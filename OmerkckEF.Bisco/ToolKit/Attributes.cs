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
            _ValueNames = new List<string>();
        }

        public DataNameAttribute(params string[] valueNames)
        {
            _ValueNames = valueNames.ToList();
        }

        public List<string> ValueNames
        {
            get { return _ValueNames; }
            set { _ValueNames = value; }
        }
        protected List<string> _ValueNames { get; set; }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class Table : Attribute
    {
        public Table()
        {
            TableType = TableType.PrimaryTable;
            TableName = "Tables";
            PrimaryKey = "id";
            SearchFields = new string[] { "id" };
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
