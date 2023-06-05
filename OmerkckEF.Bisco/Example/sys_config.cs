using OmerkckEF.Biscom.ToolKit;
using System.ComponentModel.DataAnnotations;

namespace TestLab
{
	public class sys_config
	{


        [Key, DataName]
		public int sys_id { get; set; }
		[DataName, Required(ErrorMessage ="Değer Boş geçme!"), Unique]
		public string? variable { get; set; }
		[DataName,Unique,Required]
		public string? value { get; set; }
		[DataName]
		public DateTime? set_time { get; set; }
		[DataName]
		public string? set_by { get; set; }

		public sys_config? cfr { get; set; }
    }
}