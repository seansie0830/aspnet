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
            if (!User.Identity.IsAuthenticated)
            {
                Response.Redirect("~/Login.aspx");
                return; // 確保在重定向後停止執行
            }

            if (!IsPostBack)
            {
                string userName = User.Identity.Name;

                // **STEP 1: 檢查是否為管理員並跳轉**
                if (IsUserAdmin(userName))
                {
                    // 如果 isAdmin 是 1，則跳轉到 /adminPage
                    Response.Redirect("~/admins/main.aspx");
                    return; // 確保在重定向後停止執行
                }
                // **STEP 1 結束**

                lblUserInfo.Text = $"歡迎您，{userName}！您目前的借閱狀態如下：";

                BindLendRecords(userName);
            }
        }

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

        private void BindLendRecords(string userName)
        {
            int userId = GetUserIDByUserName(userName);
            if (userId <= 0)
            {
                gvLendRecords.DataSource = null;
                gvLendRecords.DataBind();
                lblUserInfo.Text += "<br /><span style='color:red;'>錯誤：無法載入使用者資訊。</span>";
                return;
            }

            DataTable dt = new DataTable();
            string connString = GetConnectionString();

            string sql = @"
                SELECT 
                    T1.LendRecordID, 
                    T1.BookID,
                    T1.BorrowDate, 
                    T1.DueDate, 
                    T2.Title, 
                    T2.Author, 
                    T2.ISBN 
                FROM LendRecords T1 
                JOIN Books T2 ON T1.BookID = T2.BookID
                WHERE T1.UserID = @UserID AND T1.ReturnDate IS NULL
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

        protected void gvLendRecords_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                DataRowView rowView = (DataRowView)e.Row.DataItem;

                if (rowView["DueDate"] != DBNull.Value)
                {
                    DateTime dueDate = Convert.ToDateTime(rowView["DueDate"]);
                    Label lblStatus = (Label)e.Row.FindControl("lblStatus");

                    if (lblStatus != null)
                    {
                        if (dueDate < DateTime.Today)
                        {
                            lblStatus.Text = "已逾期！";
                            lblStatus.CssClass = "overdue";
                            e.Row.BackColor = System.Drawing.Color.LightPink;
                        }
                        else
                        {
                            TimeSpan remaining = dueDate - DateTime.Today;
                            if (remaining.TotalDays <= 3)
                            {
                                lblStatus.Text = $"即將到期 ({remaining.TotalDays} 天)";
                                lblStatus.CssClass = "overdue";
                            }
                        }
                    }
                }
            }
        }

        protected void gvLendRecords_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName == "ReturnBook")
            {
                lblReturnMessage.Text = "";
                int rowIndex = Convert.ToInt32(e.CommandArgument);
                int lendRecordID = Convert.ToInt32(gvLendRecords.DataKeys[rowIndex]["LendRecordID"]);
                int bookID = Convert.ToInt32(gvLendRecords.DataKeys[rowIndex]["BookID"]);

                if (PerformReturnBook(lendRecordID, bookID))
                {
                    lblReturnMessage.Text = "書籍歸還成功！";
                    string userName = User.Identity.Name;
                    BindLendRecords(userName);
                }
                else
                {
                    lblReturnMessage.Text = "<span style='color:red;'>書籍歸還失敗，請聯繫管理員。</span>";
                }
            }
        }

        private bool PerformReturnBook(int lendRecordID, int bookID)
        {
            string connString = GetConnectionString();

            string updateLendSql = "UPDATE LendRecords SET ReturnDate = @ReturnDate WHERE LendRecordID = @LendRecordID AND ReturnDate IS NULL";
            string updateBookSql = "UPDATE Books SET AvailableCopies = AvailableCopies + 1 WHERE BookID = @BookID";

            using (SQLiteConnection conn = new SQLiteConnection(connString))
            {
                conn.Open();
                using (SQLiteTransaction transaction = conn.BeginTransaction())
                {
                    try
                    {
                        using (SQLiteCommand cmdLend = new SQLiteCommand(updateLendSql, conn, transaction))
                        {
                            cmdLend.Parameters.AddWithValue("@ReturnDate", DateTime.Today.ToString("yyyy-MM-dd"));
                            cmdLend.Parameters.AddWithValue("@LendRecordID", lendRecordID);
                            if (cmdLend.ExecuteNonQuery() == 0)
                            {
                                transaction.Rollback();
                                return false;
                            }
                        }

                        using (SQLiteCommand cmdBook = new SQLiteCommand(updateBookSql, conn, transaction))
                        {
                            cmdBook.Parameters.AddWithValue("@BookID", bookID);
                            if (cmdBook.ExecuteNonQuery() == 0)
                            {
                                transaction.Rollback();
                                return false;
                            }
                        }

                        transaction.Commit();
                        return true;
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        return false;
                    }
                }
            }
        }
        private bool IsUserAdmin(string userName)
        {
            string connString = GetConnectionString();
            // 查詢 isAdmin 欄位
            string sql = "SELECT isAdmin FROM Users WHERE UserName = @UserName";

            using (SQLiteConnection conn = new SQLiteConnection(connString))
            using (SQLiteCommand cmd = new SQLiteCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@UserName", userName);
                conn.Open();
                object result = cmd.ExecuteScalar();

                // 檢查結果是否為 DBNull 或 null，並確認是否為 1
                if (result != null && result != DBNull.Value)
                {
                    // 假設 isAdmin 儲存為 INTEGER，1 表示是管理員
                    return Convert.ToInt32(result) == 1;
                }
                return false;
            }
        }
    }


}