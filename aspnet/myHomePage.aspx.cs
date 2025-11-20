using System;
using System.Data;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data.SQLite;
using System.Configuration;
using System.Collections.Generic;

namespace aspnet
{
    public partial class MyHomepage : Page
    {
        private const string AppConfigKey = "ApplicationConfig";

        private string GetConnectionString()
        {
            return ConfigurationManager.ConnectionStrings["LibraryDBConnection"].ConnectionString;
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!User.Identity.IsAuthenticated)
            {
                Response.Redirect("~/Login.aspx");
                return;
            }

            if (!IsPostBack)
            {
                string userName = User.Identity.Name;

                if (IsUserAdmin(userName))
                {
                    Response.Redirect("~/admins/main.aspx");
                    return;
                }

                lblUserInfo.Text = $"歡迎您，{userName}！您目前的借閱狀態如下：";

                UpdateBorrowStatus(userName);
                BindLendRecords(userName);
            }
        }

        private int GetMaxBooksPerUser()
        {
            var config = Application[AppConfigKey] as Dictionary<string, string>;

            if (config != null && config.ContainsKey("MaxBooksPerUser") && int.TryParse(config["MaxBooksPerUser"], out int maxBooks))
            {
                return maxBooks;
            }

            // 預設值，應與 config.aspx.cs 中的預設值一致
            return 5;
        }

        private int GetCurrentBorrowedCount(string userName)
        {
            string connString = GetConnectionString();
            // 讀取 Users 表格中的 BorrowedBookCount 欄位
            string sql = "SELECT BorrowedBookCount FROM Users WHERE UserName = @UserName";

            using (SQLiteConnection conn = new SQLiteConnection(connString))
            using (SQLiteCommand cmd = new SQLiteCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@UserName", userName);
                conn.Open();
                object result = cmd.ExecuteScalar();
                // 檢查結果是否為 DBNull 或 null，並確認是否為非負整數
                return (result != null && result != DBNull.Value) ? Convert.ToInt32(result) : 0;
            }
        }

        private void UpdateBorrowStatus(string userName)
        {
            int maxBooks = GetMaxBooksPerUser();
            int currentCount = GetCurrentBorrowedCount(userName);
            int availableToBorrow = Math.Max(0, maxBooks - currentCount);

            string statusText = $"您目前借閱：<span style='color:#007bff;'>{currentCount}</span> 本 / 上限 <span style='color:#007bff;'>{maxBooks}</span> 本 (還可借 <span style='color:#28a745;'>{availableToBorrow}</span> 本)";

            if (currentCount >= maxBooks)
            {
                statusText += "<br /><span class='borrow-limit-message'>您已達到借書上限，請先歸還書籍。</span>";
            }

            lblBorrowStatus.Text = statusText;
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
                    UpdateBorrowStatus(userName);
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
            string sql = "SELECT isAdmin FROM Users WHERE UserName = @UserName";

            using (SQLiteConnection conn = new SQLiteConnection(connString))
            using (SQLiteCommand cmd = new SQLiteCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@UserName", userName);
                conn.Open();
                object result = cmd.ExecuteScalar();

                if (result != null && result != DBNull.Value)
                {
                    return Convert.ToInt32(result) == 1;
                }
                return false;
            }
        }
    }
}