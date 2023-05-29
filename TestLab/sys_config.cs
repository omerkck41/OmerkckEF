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
		[DataName,Unique]
		public string? value { get; set; }
		[DataName, Required(ErrorMessage = "Tarih Boş geçme!")]
		public DateTime? set_time { get; set; }
		[DataName, Required(ErrorMessage = "Kim Boş geçme!")]
		public string? set_by { get; set; }

		public sys_config? cfr { get; set; }
    }
}