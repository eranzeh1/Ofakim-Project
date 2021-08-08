using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace OfakimProject
{
    public class Repository
    {
        readonly string conStringOfakim = ConfigurationManager.ConnectionStrings["conStringOfakim"].ConnectionString;

        internal bool SaveData(string cur, double curValue)
        {
            using (SqlConnection con = new SqlConnection(conStringOfakim))
            {
                try
                {
                    SqlCommand command = new SqlCommand($"INSERT INTO Currencies (cur, curValue, updateDate) VALUES({cur},{curValue},{DateTime.Now})");
                    command.CommandType = CommandType.Text;
                    command.Connection = con;
                    con.Open();

                    if (command.ExecuteNonQuery() > 0)
                        return true;
                    return false;

                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        //Return the last inserted value of each Currency
        internal DataTable GetLastData()
        {
            DataTable dt = new DataTable();
            using (SqlConnection con = new SqlConnection(conStringOfakim))
            {
                try
                {
                    SqlCommand command = new SqlCommand(@"select * from Currencies c1 where c1.updateDate in
                                                          (select top 1 c2.updateDate from Currencies c2 where c1.updateDate = c2.updateDate
                                                           order by c2.updateDate desc)");
                    command.CommandType = CommandType.Text;
                    command.Connection = con;
                    con.Open();

                    SqlDataAdapter adapter = new SqlDataAdapter(command);
                    adapter.Fill(dt);
                    return dt;
                }
                catch (Exception)
                {
                    return dt;
                }
            }
        }
    }
}