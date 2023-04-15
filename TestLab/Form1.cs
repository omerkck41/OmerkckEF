using MongoDB.Bson;
using OmerkckEF.Biscom.DBContext;
using System.Data;
using JsonConvert = Newtonsoft.Json.JsonConvert;

namespace TestLab
{
    public partial class Form1 : Form
    {
        Bisco baglanhayata = new Bisco
            (
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


            //async select

            DataTable? result = baglanhayata.RunSelectDataAsync("sakila", "select first_name from actor").Result;
            foreach (DataRow adr in result.Rows) { MessageBox.Show(adr["first_name"].ToString()); }


            //async insert
            int r = Convert.ToInt32(baglanhayata.RunNonQueryAsync("insert into actor (first_name,last_name) values ('ÖMER','ESRA')").Result);
            
            MessageBox.Show(r.ToString());

            return;

            baglanhayata.RunNonQuery("insert into actor (first_name,last_name) values ('ÖMER','ESRA')");

            Dictionary<string, object> studentGrades = new Dictionary<string, object>();
            studentGrades.Add("@first_name", "omerkck");
            studentGrades.Add("@last_name", "System");

            baglanhayata.RunNonQuery("sakila", "insert into actor (first_name,last_name) values (@first_name,@last_name)", studentGrades, true);

            MessageBox.Show("Test");

            using var reader = baglanhayata.RunDataReader("sakila", "select * from actor limit 3");
            DataTable tt = new DataTable();
            tt.Load(reader);
            foreach (DataRow dr in tt.Rows)
                MessageBox.Show(dr["first_name"].ToString());



            //string? str = baglanhayata.RunScaler("sakila", "select first_name from actor where actor_id=1").ToString();
            DataTable? dt = baglanhayata.RunDataTable("sakila", "select * from actor limit 3");
            foreach (DataRow dr in dt.Rows)
                MessageBox.Show(dr["first_name"].ToString());
        }

        private void button1_Click(object sender, EventArgs e)
        {
            using var reader = baglanhayata.RunDataReader("sakila", "select * from actor limit 3");
            DataTable tt = new DataTable();
            tt.Load(reader);
            foreach (DataRow dr in tt.Rows)
                MessageBox.Show(dr["first_name"].ToString());



            //DataTable? dt = baglanhayata.RunDataTable("sakila", "select * from actor limit 3");
            //foreach (DataRow dr in dt.Rows)
            //    MessageBox.Show(dr["first_name"].ToString());
        }
    }
}