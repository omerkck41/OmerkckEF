using Amazon.Runtime.Internal.Transform;
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
		public Form1()
		{
			InitializeComponent();



			List<sys_config> ll = new List<sys_config>();

			var current = new sys_config()
			{
				sys_id = 6,
				value = "64",
				variable = "statement_truncate_len",
			};


			MessageBox.Show(bisco.DoMapUpdate<sys_config>(current).ToString());



		}

	}
}