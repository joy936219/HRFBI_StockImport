using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.OleDb;
using System.Data.SqlClient;
namespace HRF_StockImport
{
    public partial class Form1 : Form
    {
        DataTable sheetdata = new DataTable();
        DataTable nodata = new DataTable();
        DataRow no_dr;
        public Form1()
        {
            InitializeComponent();
            string sql = @"select CompID,CompName from Company where StopStatus = 'false'";
            DataTable data = Sql.GetData(sql);
            cbCom.DataSource = data;
            cbCom.DisplayMember = "CompName";
            cbCom.ValueMember = "CompID";

            nodata.Columns.Add("貨品編號");
            nodata.Columns.Add("中文品名");
            nodata.Columns.Add("期末成本");
            nodata.Columns.Add("期末數量");

            
        }

        private void tableLayoutPanel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void btnSelectFile_Click(object sender, EventArgs e)
        {

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                txtFilePath.Text = openFileDialog1.SafeFileName;
                DataTable sheetnamedata = new DataTable();
                //DataTable sheetdata = new DataTable();
                string connstr = "Provider=Microsoft.Ace.OleDb.12.0;" + "data source=" + openFileDialog1.FileName + ";Extended Properties='Excel 12.0; HDR=YES; IMEX=1'";
                string comstr = "select * from [{0}]";
                string sheetname;
                OleDbConnection conn = new OleDbConnection(connstr);
                conn.Open();
                sheetnamedata = conn.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null);
                sheetname = sheetnamedata.Rows[0]["TABLE_NAME"].ToString();
                OleDbDataAdapter da = new OleDbDataAdapter(string.Format(comstr, sheetname), conn);
                da.Fill(sheetdata);
                //dataGridView1.DataSource = sheetdata;
                //ImportData(sheetdata);

            }
        }

        private void btnDataImport_Click(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(txtFilePath.Text))
            {
                MessageBox.Show("請先選擇匯入的檔案");
                return;
            }
            else
            {
                DialogResult result = MessageBox.Show("匯入後將更新庫存數量，是否要匯入?","訊息視窗",MessageBoxButtons.OKCancel,MessageBoxIcon.Question);
                if(result == DialogResult.OK)
                {
                    ImportData(sheetdata);
                }
                
            }

        }
        private void ImportData(DataTable data)
        {
            string insert_sql = @"insert into Receipt (RecID,[RecDate],[RecType],[CAGE],[Currency],[EmpID],[SubTotal],[Tax],[TaxType],[Comment],[CloseStatus],[StopStatus],[Rate],[WareID],[Freight],[Customs],[Tariff])
                                  values ({0})";
            string insert_sqldeatil = @"Insert into ReceiptDetail ([RecID] ,[RecSeq] ,[NSN],[CAGE],[PartNo] ,[Price] ,[Quantity]) values ('{0}','{1}','{2}','{3}','{4}','{5}','{6}')";
            string colums = "";
            int import_index = 0;
            int index = 0;
            int seq = 1;
            double price = 0;
            int quantity = 0;
            string selectCode_sql = "select PartNo,NSN,CAGE from Code where PartNo ='{0}' and CAGE='{1}'";
            DataTable selectCode_data = new DataTable();
            string id = "";
            string CAGE = "";
            string PartNo = "";
            id = Sql.AutoNum();
            for (int i = 0; i < 17; i++)
            {
                if (i == 16)
                {
                    colums += "'{" + i + "}'";
                }
                else
                {
                    colums += "'{" + i + "}'" + ",";
                }
            }
            insert_sql = string.Format(insert_sql, colums);
            //讀取資料後,將DataTable的欄位名稱變更
            for (int i = 0; i < data.Rows.Count; i++)
            {
                for (int j = 0; j < data.Columns.Count; j++)
                {
                    switch (data.Rows[i][j].ToString())
                    {
                        case "貨品編號":
                            data.Columns[j].ColumnName = "貨品編號";                
                            break;
                        case "中文品名":
                            data.Columns[j].ColumnName = "中文品名";
                            break;
                        case "期末數量":
                            data.Columns[j].ColumnName = "期末數量";
                            break;
                        case "期末總成本":
                            data.Columns[j].ColumnName = "期末總成本";
                            break;
                        case "期末成本":
                            data.Columns[j].ColumnName = "期末成本";
                            break;
                    }
                }
            }

            foreach (DataRow dr in data.Rows)
            {
                if (dr["貨品編號"].ToString() == "貨品編號")
                {
                    import_index = 1;
                    continue;
                }
                if (import_index == 1)
                {
                    if (index == 0)
                    {
                        Sql.ExecQuery(string.Format(insert_sql,
                        id,
                        "2017/01/01",
                        1,
                        99999,
                        "NTD",
                        99999,
                        0,
                        0,
                        2,
                        "原庫存資料匯入",
                        false,
                        false,
                        1,
                        Sql.GetCompWareID(cbCom.SelectedValue.ToString()),
                        0,
                        0,
                        0
                        ));
                        index++;
                    }

                    if (dr["貨品編號"].ToString() != "合計")
                    {
                        //貨品編號為廠商代號+配件號,透過-切割取出廠商代號及配件號
                        int cage_index = dr["貨品編號"].ToString().IndexOf("-");
                        if (cage_index > 0)
                        {
                            CAGE = dr["貨品編號"].ToString().Substring(0, cage_index);
                            PartNo = dr["貨品編號"].ToString().Substring(cage_index + 1, dr["貨品編號"].ToString().Length - cage_index - 1);
                            //MessageBox.Show(CAGE + " " + PartNo);
                            selectCode_data = Sql.GetData(string.Format(selectCode_sql, PartNo, CAGE));
                            if (selectCode_data.Rows.Count > 0)
                            {
                                if (dr["期末成本"].ToString() == "")
                                {
                                    price = 0;
                                }
                                else
                                {
                                    price = Convert.ToDouble(dr["期末成本"]);
                                }
                                if (dr["期末數量"].ToString() == "")
                                {
                                    quantity = 0;
                                }
                                else
                                {
                                    quantity = Convert.ToInt32(dr["期末數量"]);
                                }
                                if (dr["期末成本"].ToString() != "" && dr["期末數量"].ToString() != "")
                                {
                                    Sql.ExecQuery(string.Format(insert_sqldeatil,
                               id,
                                seq,
                               selectCode_data.Rows[0]["NSN"].ToString(),
                               selectCode_data.Rows[0]["CAGE"].ToString(),
                               selectCode_data.Rows[0]["PartNo"].ToString(),
                               Convert.ToSingle(dr["期末成本"]),
                               dr["期末數量"].ToString()
                               ));
                                    Sql.UpdateStock(selectCode_data.Rows[0]["PartNo"].ToString(), selectCode_data.Rows[0]["CAGE"].ToString(), selectCode_data.Rows[0]["NSN"].ToString(), Convert.ToInt32(dr["期末數量"]), cbCom.SelectedValue.ToString());
                                }



                            }
                            else
                            {
                                no_dr = nodata.NewRow();
                                no_dr["貨品編號"] = dr["貨品編號"];
                                nodata.Rows.Add(no_dr);
                            }

                        }
                        else
                        {
                            no_dr = nodata.NewRow();
                            no_dr["貨品編號"] = dr["貨品編號"];
                            no_dr["中文品名"] = dr["中文品名"];
                            no_dr["期末成本"] = dr["期末成本"];
                            no_dr["期末數量"] = dr["期末數量"];
                            nodata.Rows.Add(no_dr);
                        }


                    }
                    seq++;
                }
            }
            if(nodata.Rows.Count == 0)
            {
                MessageBox.Show("匯入完成");
            }
            else
            {
                MessageBox.Show("匯入完成，但有找不到的料號，請詳查");
            }
            
            dataGridView1.DataSource = nodata;
            dataGridView1.Columns[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            dataGridView1.Columns[1].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            dataGridView1.Columns[2].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            dataGridView1.Columns[3].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
        }
    }
}
