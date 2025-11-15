using System;
using System.Data;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data.SQLite;
using System.Configuration;

namespace aspnet
{
    public partial class MyHomepage : Page
    {
        private string GetConnectionString()
        {
            return ConfigurationManager.ConnectionStrings["LibraryDBConnection"].ConnectionString;
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            // 檢查使用者是否已登入，未登入則導向登入頁
            if (!User.Identity.IsAuthenticated)
            {
                // 假設登入頁名為 Login.aspx
                Response.Redirect("~/Login.aspx");
            }

            if (!IsPostBack)
            {
                string userName = User.Identity.Name;
                lblUserInfo.Text = $"歡迎您，{userName}！您目前的借閱狀態如下：";

                BindLendRecords(userName);
            }
        }

        // 輔助方法：獲取 UserID
        private int GetUserIDByUserName(string userName)
        {
            string connString = GetConnectionString();
            string sql = "SELECT UserID FROM Users WHERE UserName = @UserName";

            using (SQLiteConnection conn = new SQLiteConnection(connString))
            using (SQLiteCommand cmd = new SQLiteCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@UserName", userName);
                conn.Open();
                object result = cmd.ExecuteScalar();
                return (result != null && result != DBNull.Value) ? Convert.ToInt32(result) : -1;
            }
        }

        // 核心方法：綁定未歸還的借閱記錄
        private void BindLendRecords(string userName)
        {
            int userId = GetUserIDByUserName(userName);
            if (userId <= 0)
            {
                // 如果找不到 UserID，則清空表格並顯示錯誤
                gvLendRecords.DataSource = null;
                gvLendRecords.DataBind();
                lblUserInfo.Text += "<br /><span style='color:red;'>錯誤：無法載入使用者資訊。</span>";
                return;
            }

            DataTable dt = new DataTable();
            string connString = GetConnectionString();

            // 查詢 SQL：聯合 Books 和 LendRecords 表格
            string sql = @"
                SELECT 
                    T1.BorrowDate, 
                    T1.DueDate, 
                    T2.Title, 
                    T2.Author, 
                    T2.ISBN 
                FROM LendRecords T1 
                JOIN Books T2 ON T1.BookID = T2.BookID
                WHERE T1.UserID = @UserID AND T1.ReturnDate IS NULL -- *** 關鍵變更：檢查 ReturnDate 是否為 NULL ***
                ORDER BY T1.DueDate ASC";

            using (SQLiteConnection conn = new SQLiteConnection(connString))
            using (SQLiteCommand cmd = new SQLiteCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@UserID", userId);
                conn.Open();
                SQLiteDataAdapter da = new SQLiteDataAdapter(cmd);
                da.Fill(dt);
            }

            gvLendRecords.DataSource = dt;
            gvLendRecords.DataBind();
        }

        // GridView 行資料綁定事件：用於檢查是否逾期
        protected void gvLendRecords_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                DataRowView rowView = (DataRowView)e.Row.DataItem;

                // 確保 DueDate 欄位存在且可以轉換為 DateTime
                if (rowView["DueDate"] != DBNull.Value)
                {
                    DateTime dueDate = Convert.ToDateTime(rowView["DueDate"]);
                    Label lblStatus = (Label)e.Row.FindControl("lblStatus");

                    if (lblStatus != null)
                    {
                        // 檢查是否逾期
                        if (dueDate < DateTime.Today)
                        {
                            lblStatus.Text = "已逾期！";
                            lblStatus.CssClass = "overdue"; // 應用紅色樣式
                            e.Row.BackColor = System.Drawing.Color.LightPink; // 整行設為淡紅色
                        }
                        else
                        {
                            // 距離到期日少於 3 天，顯示警告
                            TimeSpan remaining = dueDate - DateTime.Today;
                            if (remaining.TotalDays <= 3)
                            {
                                lblStatus.Text = $"即將到期 ({remaining.TotalDays} 天)";
                                lblStatus.CssClass = "overdue"; // 應用紅色樣式
                            }
                        }
                    }
                }
            }
        }
    }
}