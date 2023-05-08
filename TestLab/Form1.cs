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

            var sys = new sys_config()
            {
                //set_by = DateTime.Now.ToString(),
                //set_time = DateTime.Now,
                sys_id = 0,
                value = "311",
                variable = "diagnostics.allow_i_s_tables"
            };

            MessageBox.Show(Tools.CheckAttributeColumn<sys_config>(sys, bisco));

            //var acc = new actor() { actor_id = 1, first_name = "Omer", last_name = "Kucuk", last_update = DateTime.Now };

            //Dictionary<string, object> prm = Tools.GetDbPrameters<actor>(acc);



            //MessageBox.Show(Tools.GetColumnNames<actor>(false) + "\n" + Tools.GetParameterNames<actor>(false) + "\n" + Tools.GetUpdateSetClause<actor>());

            //var keys = GetClassProperties(typeof(actor), typeof(DataNameAttribute), false).Select(x => x.Name);

            ////Insert
            //var insert_pairs = keys.Select(key => $"@{key}");
            //string insQuery = $"({string.Join(", ", keys)}) values ({string.Join(", ", insert_pairs)}";
            ////Update
            //var update_pairs = keys.Select(key => $"{key}=@{key}");
            //string upQuery = string.Join(", ", update_pairs);

            //MessageBox.Show(upQuery + "\n\n" + insQuery);


            //foreach (var property in GetClassProperties(typeof(actor), typeof(DataNameAttribute)))
            //{
            //    MessageBox.Show(property.Name);
            //}

            //return;


            //var ac = bisco.GetMappedClass<sys_config>();
            //if (ac == null) { return; }
            //foreach (sys_config item in ac)
            //{
            //    MessageBox.Show(item.variable);
            //}


            ////async select

            //DataTable? result = baglanhayata.RunSelectDataAsync("sakila", "select first_name from actor").Result;
            //if (result == null) { return; }
            //foreach (DataRow adr in result.Rows) { MessageBox.Show(adr["first_name"].ToString()); }


            ////async insert
            //int r = Convert.ToInt32(baglanhayata.RunNonQueryAsync("insert into actor (first_name,last_name) values ('ÖMER','ESRA')").Result);

            //MessageBox.Show(r.ToString());

            //return;

            //int insert = baglanhayata.RunNonQuery("insert into actor (first_name,last_name) values ('ÖMER','ESRA')");

            //Dictionary<string, object> studentGrades = new()
            //{
            //    { "@first_name", "omerkck" },
            //    { "@last_name", "System" }
            //};

            //baglanhayata.RunNonQuery("sakila", "insert into actor (first_name,last_name) values (@first_name,@last_name)", studentGrades, true);

            //MessageBox.Show("Test");

            //using var reader = baglanhayata.RunDataReader("sakila", "select * from actor limit 3");
            //DataTable tt = new();
            //tt.Load(reader);
            //foreach (DataRow dr in tt.Rows)
            //    MessageBox.Show(dr["first_name"].ToString());



            ////string? str = baglanhayata.RunScaler("sakila", "select first_name from actor where actor_id=1").ToString();
            //DataTable? dt = baglanhayata.RunDataTable("sakila", "select * from actor limit 3");
            //foreach (DataRow dr in dt.Rows)
            //    MessageBox.Show(dr["first_name"].ToString());
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            using var reader = baglanhayata.RunDataReader("sakila", "select * from actor limit 3");
            DataTable tt = new();
            if (reader != null)
                tt.Load(reader);
            foreach (DataRow dr in tt.Rows)
                MessageBox.Show(dr["first_name"].ToString());



            //DataTable? dt = baglanhayata.RunDataTable("sakila", "select * from actor limit 3");
            //foreach (DataRow dr in dt.Rows)
            //    MessageBox.Show(dr["first_name"].ToString());
        }


        public static IEnumerable<PropertyInfo> GetClassProperties(Type ClassType, Type AttirbuteType, bool IsKeyAttirbute = true)
        {
            if (ClassType == null) return new List<PropertyInfo>();
            if (AttirbuteType == null) return ClassType.GetProperties();

            return ClassType.GetProperties().Where(x => IsKeyAttirbute ? x.GetCustomAttributes(AttirbuteType, true).Any()
                                                                       : x.GetCustomAttributes(AttirbuteType, true).Any() && !x.GetCustomAttributes(typeof(KeyAttribute), true).Any());
        }


	}
}