using Amazon.Runtime.Internal.Transform;
using Microsoft.VisualBasic.ApplicationServices;
using MySqlX.XDevAPI;
using OmerkckEF.Biscom;
using OmerkckEF.Biscom.DBContext;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Data.SqlClient;
using System.Linq.Expressions;
using System.Reflection;
using System.Windows.Forms;
using static Mysqlx.Expect.Open.Types.Condition.Types;

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

		readonly EntityDbHelper ee = new(new DbServer()
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

			for (int i = 0; i < 50000; i++)
			{
				//string sss = bisco.MyConnection?.State.ToString() ?? string.Empty;
				//MessageBox.Show(sss);

				bisco.RunNonQuery("insert into sys.sys_config (variable,value) values ('omer','kck')");

				//string sss1 = bisco.MyConnection?.State.ToString() ?? string.Empty;
				//MessageBox.Show(sss1);
			}

			
		}

		async void gett()
		{
			var id = "5".CreateParameters("Id");

			var result = await ee.GetMapClassByIdAsync<sys_config>("@Id",id).ConfigureAwait(false);
			MessageBox.Show(result?.value);
		}

	}
}