using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Windows.Forms;
using System.Data;
namespace HRF_StockImport
{
    class Sql
    {
        private static string connection = @"Data Source=120.119.80.10;Initial Catalog=HRFBI;Persist Security Info=True;User ID=HRF;Password=075551343";
        public static string msg = "";
        public static DataTable GetData(string sqlcmd)
        {
            DataTable dt = new DataTable();
            SqlConnection conn = new SqlConnection(connection);
            conn.Open();
            SqlDataAdapter da = new SqlDataAdapter(sqlcmd, conn);
            da.Fill(dt);
            conn.Close();

            return dt;
        }
        public static void ExecQuery(string sqlcmd)
        {
            SqlConnection conn = new SqlConnection(connection);
            conn.Open();
            SqlCommand com = new SqlCommand(sqlcmd, conn);
            com.ExecuteNonQuery();
            conn.Close();
        }
        public static void UpdateStock(string PartNo, string CAGE, string NSN, int Quantity, string Com)
        {
            string select_sql = @"select stock,cs.WareID
                                        from CodeStock as cs 
                                        join 
                                        (select  WareID,com.CompID
                                        from Company as com join Warehouse as wh
                                        on com.CompID = wh.CompID
                                        where  DisableStatus ='false' and IsStock = 'true' and IsComWare='true' ) as ComData
                                        on cs.WareID = ComData.WareID
                                        where NSN='{0}' and PartNo ='{1}' and CAGE ='{2}' and CompID='{3}'";
            DataTable data = Sql.GetData(string.Format(select_sql, NSN, PartNo, CAGE, Com));
            if (data.Rows.Count > 0)
            {
                int num = Convert.ToInt32(data.Rows[0][0]);
                num = num + Quantity;
                string update_sql = "update [CodeStock] set [Stock] ='{0}' where NSN='{1}' and PartNo ='{2}' and CAGE ='{3}' and WareID='{4}'";
                Sql.ExecQuery(string.Format(update_sql, Quantity, NSN, PartNo, CAGE, data.Rows[0]["WareID"].ToString()));
            }
        }
        public static string GetCompWareID(string comp)
        {
            string sql = @"select WareID
                            from Company as c 
                            join Warehouse as w
                            on c.CompID = w.CompID
                            where w.IsComWare = 'true' and w.IsStock = 'true' and c.CompID = '{0}'";
            DataTable dt = GetData(string.Format(sql, comp));
            if (dt.Rows.Count > 0)
            {
                return dt.Rows[0][0].ToString();
            }
            return "";

        }
        public static string AutoNum()
        {
            string sql = "Select max(right(RecID,2)) from Receipt where RecID like 'IZZ%'";
            DataTable dt = GetData(sql);
            int num = 0;
            string id = "IZZ"+DateTime.Now.ToString("yyyyMMdd");
            if(dt.Rows[0][0] == DBNull.Value)
            {
                num = 1;
            }
            else
            {
                num = Convert.ToInt32(dt.Rows[0][0]) + 1;
            }
            id = id + string.Format("{0:00}", num);

            return id;
        }
    }
}
