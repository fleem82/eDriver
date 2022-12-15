using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace eDriver_Control
{
    public partial class FrmItemCode : Form
    {
        FrmMain m_pMain;

        public FrmItemCode(FrmMain dlg)
        {
            m_pMain = dlg;

            InitializeComponent();
        }

        private void button_UploadCSV_Click(object sender, EventArgs e)
        {

        }

        private void button_DownloadCSV_Click(object sender, EventArgs e)
        {

        }

        private void button_Apply_Click(object sender, EventArgs e)
        {
            if (textBox_ItemCode.Text.Length < 8)
            {
                MessageBox.Show("ItemCode가 올바르지 않습니다.", "ItemCode 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            int iM3, iM4M5;

            if (int.TryParse(textBox_M3.Text,out iM3) == false)
            {
                MessageBox.Show("M3 볼트 갯수가 올바르지 않습니다.", "M3 볼트 갯수 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (int.TryParse(textBox_M4M5.Text, out iM4M5) == false)
            {
                MessageBox.Show("M4/M5 볼트 갯수가 올바르지 않습니다.", "M4/M5 볼트 갯수 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            using (SqlConnection conn = m_pMain.connectDB_TEAMDB())
            {
                if (conn == null)
                {
                    MessageBox.Show("DB에 접속할 수 없습니다.", "DB 접속 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    this.Close();
                }

                bool bIsExist = false;

                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT COUNT(ITEMCODE) AS CNT FROM tbl_ITEMCODE_BOLTCNT WHERE ITEMCODE = @item";
                    cmd.Parameters.AddWithValue("@item", textBox_ItemCode.Text);

                    try
                    {
                        var vv = cmd.ExecuteScalar();
                        if (vv != null)
                        {
                            if (int.Parse(vv.ToString()) > 0)
                                bIsExist = true;
                        }
                    }
                    catch(Exception ex) { m_pMain.writeLog(ex.ToString()); }
                }

                using (SqlCommand cmd = conn.CreateCommand())
                {
                    if (bIsExist == true)
                        cmd.CommandText = "UPDATE tbl_ITEMCODE_BOLTCNT SET BOLT_M3 = @m3, BOLT_M4M5 = @m4m5, MISC = @misc WHERE ITEMCODE = @item";
                    else
                        cmd.CommandText = "INSERT INTO tbl_ITEMCODE_BOLTCNT VALUES (@item, @m3, @m4m5, @misc)";

                    cmd.Parameters.AddWithValue("@item", textBox_ItemCode.Text);
                    cmd.Parameters.AddWithValue("@m3", iM3); 
                    cmd.Parameters.AddWithValue("@m4m5", iM4M5);

                    int iLength = -1;
                    if (textBox_MISC.Text.Length > 200)
                        iLength = 200;
                    else
                        iLength = textBox_MISC.Text.Length;

                    cmd.Parameters.AddWithValue("@misc", textBox_MISC.Text.Substring(0, iLength));

                    try
                    {
                        cmd.ExecuteNonQuery();   
                    }
                    catch (Exception ex) { m_pMain.writeLog(ex.ToString()); }
                }
            }

            reloadDGV();
        }

        private void FrmItemCode_Load(object sender, EventArgs e)
        {
            using (SqlConnection conn = m_pMain.connectDB_TEAMDB())
            {
                if (conn == null)
                {
                    MessageBox.Show("DB에 접속할 수 없습니다.", "DB 접속 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    this.Close();
                }

                DataTable dt = new DataTable();

                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM tbl_ITEMCODE_BOLTCNT";

                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        try
                        {
                            da.Fill(dt);
                        }
                        catch (Exception ex)
                        {
                            m_pMain.writeLog(ex.ToString());
                        }
                    }
                }

                dataGridView_ItemCode.DataSource = dt;
            }
            for (int i = 0; i < dataGridView_ItemCode.Columns.Count; i++) dataGridView_ItemCode.Columns[i].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
        }

        private void reloadDGV()
        {
            using (SqlConnection conn = m_pMain.connectDB_TEAMDB())
            {
                if (conn == null)
                {
                    return;
                }

                DataTable dt = new DataTable();

                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM tbl_ITEMCODE_BOLTCNT";

                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        try
                        {
                            da.Fill(dt);
                        }
                        catch (Exception ex)
                        {
                            m_pMain.writeLog(ex.ToString());
                        }
                    }
                }

                dataGridView_ItemCode.DataSource = dt;
            }

            for (int i = 0; i < dataGridView_ItemCode.Columns.Count; i++) dataGridView_ItemCode.Columns[i].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
        }

        private void dataGridView_ItemCode_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (dataGridView_ItemCode.RowCount < 1)
                return;

            if (e.RowIndex < 0)
                return;

            textBox_ItemCode.Text = dataGridView_ItemCode.Rows[e.RowIndex].Cells[0].Value.ToString();
            textBox_M3.Text = dataGridView_ItemCode.Rows[e.RowIndex].Cells[1].Value.ToString();
            textBox_M4M5.Text = dataGridView_ItemCode.Rows[e.RowIndex].Cells[2].Value.ToString();

            if (dataGridView_ItemCode.Rows[e.RowIndex].Cells[3].Value != null)
                textBox_MISC.Text = dataGridView_ItemCode.Rows[e.RowIndex].Cells[3].Value.ToString();
            else
                textBox_MISC.Text = string.Empty;
        }

        private void button_Delete_Click(object sender, EventArgs e)
        {

        }
    }
}
