using OmerkckEF.Biscom.ToolKit;
using System.ComponentModel.DataAnnotations;

namespace TestLab
{
	public class actor
    {
        [Key]
        [DataName]
        public int actor_id { get; set; }
        [DataName]
        public string first_name { get; set; }
        [DataName]
        public string last_name { get; set; }
        [DataName]
        public DateTime? last_update { get; set; }

    }
}
