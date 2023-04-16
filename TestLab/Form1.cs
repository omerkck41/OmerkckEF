using MongoDB.Bson;
using OmerkckEF.Biscom.DBContext;
using System.Data;
using JsonConvert = Newtonsoft.Json.JsonConvert;

namespace TestLab
{
    public partial class Form1 : Form
    {
        readonly Bisco baglanhayata = new (
                new ServerDB()
                {
                    //DBIp = "127.0.0.1",
                    //DBPort = 3306
                    DBSchema = "sakila",
                    DBUser = "root",
                    DBPassword = "554654",

                }
            );
        public Form1()
        {
            InitializeComponent();
            

            var ac = baglanhayata.GetMappedClassByQuery<actor>("select first_name from actor");
            if(ac == null ) { return; }
            foreach (actor item in ac)
            {
                MessageBox.Show(item.first_name);
            }



            //async select

            DataTable? result = baglanhayata.RunSelectDataAsync("sakila", "select first_name from actor").Result;
            if(result == null ) { return; }
            foreach (DataRow adr in result.Rows) { MessageBox.Show(adr["first_name"].ToString()); }


            //async insert
            int r = Convert.ToInt32(baglanhayata.RunNonQueryAsync("insert into actor (first_name,last_name) values ('�MER','ESRA')").Result);
            
            MessageBox.Show(r.ToString());

            return;

            int insert = baglanhayata.RunNonQuery("insert into actor (first_name,last_name) values ('�MER','ESRA')");

            Dictionary<string, object> studentGrades = new()
            {
                { "@first_name", "omerkck" },
                { "@last_name", "System" }
            };

            baglanhayata.RunNonQuery("sakila", "insert into actor (first_name,last_name) values (@first_name,@last_name)", studentGrades, true);

            MessageBox.Show("Test");

            using var reader = baglanhayata.RunDataReader("sakila", "select * from actor limit 3");
            DataTable tt = new();
            tt.Load(reader);
            foreach (DataRow dr in tt.Rows)
                MessageBox.Show(dr["first_name"].ToString());



            //string? str = baglanhayata.RunScaler("sakila", "select first_name from actor where actor_id=1").ToString();
            DataTable? dt = baglanhayata.RunDataTable("sakila", "select * from actor limit 3");
            foreach (DataRow dr in dt.Rows)
                MessageBox.Show(dr["first_name"].ToString());
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
    }
}