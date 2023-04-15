using OmerkckEF.Biscom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestLab
{
    public class actor
    {
        [DataName]
        public int actor_id { get; set; }
        [DataName]
        public string first_name { get; set; }
        [DataName]
        public string last_name { get; set; }
        [DataName]
        public DateTime last_update { get; set; }

    }
}
