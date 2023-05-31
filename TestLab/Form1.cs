using OmerkckEF.Biscom.DBContext;
using OmerkckEF.Biscom.ToolKit;
using System.Data;
using System.Data.Common;

namespace TestLab
{
	public partial class Form1 : Form
	{
		readonly Bisco baglanhayata = new(
				new DbServer()
				{
					//DBIp = "127.0.0.1",
					//DBPort = 3306
					DbSchema = "sakila",
					DbUser = "root",
					DbPassword = "554654",

				});
		readonly Bisco bisco = new(
				new DbServer()
				{
					//DBIp = "127.0.0.1",
					//DBPort = 3306
					DbSchema = "sys",
					DbUser = "root",
					DbPassword = "1q2w3e4r",

				});
		readonly DbServer dbServer = new DbServer()
		{
			//DBIp = "127.0.0.1",
			//DBPort = 3306
			DbSchema = "sys",
			DbUser = "root",
			DbPassword = "1q2w3e4r",
		};

		readonly EntityDbHelper edh = new(new DbServer()
		{
			//DBIp = "127.0.0.1",
			//DBPort = 3306
			DbSchema = "sys",
			DbUser = "root",
			DbPassword = "1q2w3e4r",

		});


		public Form1()
		{
			InitializeComponent();


			List<sys_config> ll = new List<sys_config>();

			var current = new sys_config()
			{
				sys_id = 7,
				value = "64",
				variable = "statement_truncate_len",
			};
			var current1 = new sys_config()
			{
				sys_id = 8,
				value = "64",
				variable = "statement_truncate_len",
			};
			ll.Add(current);
			ll.Add(current1);



			var dat = edh.GetMappedClass<sys_config>("select * from sys_config").Data ?? new();
			MessageBox.Show(edh.bisco.MyConnection.State.ToString());
			foreach (sys_config item in dat)
			{
				MessageBox.Show(item.variable.ToString());

			}

			var alan = new List<int> { 1, 2, 3, 5 };
			string[] ids = { "omer", "okan", "statement_performance_analyzer.limit" };


			var data2 = edh.GetMapClassAsync<sys_config>(x => ids.Contains(x.variable));


			dgrid1.DataSource = data2.Result.Data;

		}

		async void gett()
		{
			var id = "5".CreateParameters("Id");

			var result = await edh.GetMapClassByIdAsync<sys_config>("@Id", id).ConfigureAwait(false);
			MessageBox.Show(result.Data?.value);
		}

		private void button1_Click(object sender, EventArgs e)
		{
			string[] ids = { "omer", "okan", "statement_performance_analyzer.limit" };


			var data2 = edh.GetMapClassAsync<sys_config>(x => ids.Contains(x.variable));


			dgrid1.DataSource = data2.Result.Data;
		}
	}
}