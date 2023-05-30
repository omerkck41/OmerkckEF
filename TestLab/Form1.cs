using OmerkckEF.Biscom.DBContext;
using OmerkckEF.Biscom.ToolKit;

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


			var data1 = edh.GetMapClassAsync<sys_config>(x=> x.sys_id == 6);
			var data2 = edh.GetMappedClassById<sys_config>(6);


			dgrid.DataSource = data1.Result.Data;

			var del = edh.DoMapDelete<sys_config>(x => x.variable == "omer" && x.value == "kck");
			var delasync = edh.DoMapDeleteAsync<sys_config>(x => x.variable == "okan" && x.value == "krdmn");
		}

		async void gett()
		{
			var id = "5".CreateParameters("Id");

			var result = await edh.GetMapClassByIdAsync<sys_config>("@Id",id).ConfigureAwait(false);
			MessageBox.Show(result.Data?.value);
		}

	}
}