using OmerkckEF.Biscom.DBContext;
using OmerkckEF.Biscom.ToolKit;
using System.Data;
using System.Data.Common;

namespace TestLab
{
	public partial class Form1 : Form
	{
		public Form1()
		{
			InitializeComponent();


			List<sys_config> ll = new List<sys_config>();

			var current = new sys_config()
			{
				sys_id=39348,
				value = "645",
				variable = "statement_truncate_len-2",
			};
			var current1 = new sys_config()
			{
				sys_id = 39349,
				value = "kck",
				variable = "omer-11",
			};
			ll.Add(current);
			ll.Add(current1);

			//dgrid1.DataSource = Function.EntityContext.GetMapClass<sys_config>().Data;


						



		}


		private void button1_Click(object sender, EventArgs e)
		{
		}
	}
}