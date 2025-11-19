using System;
using System.Data;
using System.Configuration;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data.SQLite;
using System.Text;
using System.Collections.Generic;
using System.Linq;

namespace aspnet
{
    public partial class mailQueue : Page
    {
        private const string ConnectionStringName = "LibraryDBConnection";

        private string GetConnectionString()
        {
            return ConfigurationManager.ConnectionStrings[ConnectionStringName].ConnectionString;
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                if (!User.Identity.IsAuthenticated)
                {
                    Response.Redirect("~/Login.aspx");
                    return;
                }
                if (!IsUserAdmin(User.Identity.Name))
                {
                    ShowMessage("存取遭拒：您不具備管理員權限。", "error");
                    Response.Redirect("~/MyHomepage.aspx?AccessDenied=True");
                    return;
                }
                BindOverdueRemindersData();
            }
        }

        private bool IsUserAdmin(string username)
        {
            if (string.IsNullOrEmpty(username)) return false;
            string connString = GetConnectionString();
            string sql = "SELECT IsAdmin FROM Users WHERE Username = @Username";
            using (SQLiteConnection conn = new SQLiteConnection(connString))
            using (SQLiteCommand cmd = new SQLiteCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@Username", username);
                try
                {
                    conn.Open();
                    object result = cmd.ExecuteScalar();
                    return result != null && result != DBNull.Value && Convert.ToInt64(result) == 1;
                }
                catch
                {
                    return false;
                }
            }
        }

        private void BindOverdueRemindersData()
        {
            string connString = GetConnectionString();
            string selectQuery = @"SELECT 
                                    L.LendRecordID, 
                                    B.Title AS BookTitle,
                                    U.Username,
                                    U.Email AS UserEmail,
                                    L.BorrowDate,
                                    L.DueDate
                                FROM LendRecords L
                                JOIN Books B ON L.BookID = B.BookID
                                JOIN Users U ON L.UserID = U.UserID
                                WHERE L.ReturnDate IS NULL"; // 僅限未歸還的記錄

            string filter = ddlOverdueDays.SelectedValue;
            string whereClause = string.Empty;

            if (filter == "Overdue")
            {
                // 已逾期：ReturnDate IS NULL AND DueDate < 今天
                whereClause = " AND L.DueDate < DATE('now')";
            }
            else if (filter == "DueIn7")
            {
                // 7 天內到期：ReturnDate IS NULL AND DueDate <= 今天 + 7天 AND DueDate >= 今天
                whereClause = " AND L.DueDate <= DATE('now', '+7 day') AND L.DueDate >= DATE('now')";
            }
            else if (filter == "DueToday")
            {
                // 今天到期：ReturnDate IS NULL AND DueDate = 今天
                whereClause = " AND L.DueDate = DATE('now')";
            }
            // AllInHand: 不增加 whereClause (預設就是 L.ReturnDate IS NULL)

            string orderByClause = " ORDER BY L.DueDate ASC, L.LendRecordID ASC";

            try
            {
                using (SQLiteConnection conn = new SQLiteConnection(connString))
                using (SQLiteCommand cmd = new SQLiteCommand(selectQuery + whereClause + orderByClause, conn))
                {
                    conn.Open();
                    SQLiteDataAdapter da = new SQLiteDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    gvOverdueReminders.DataSource = dt;
                    gvOverdueReminders.DataBind();

                    ShowMessage($"已載入 {filter} 條件下的未歸還記錄 (共 {dt.Rows.Count} 筆)。", "info");
                }
            }
            catch (Exception ex)
            {
                ShowMessage($"載入提醒資料時發生錯誤：{ex.Message}", "error");
            }
        }

        protected void ddlOverdueDays_SelectedIndexChanged(object sender, EventArgs e)
        {
            gvOverdueReminders.PageIndex = 0;
            BindOverdueRemindersData();
        }

        protected void gvOverdueReminders_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            gvOverdueReminders.PageIndex = e.NewPageIndex;
            BindOverdueRemindersData();
        }

        protected void gvOverdueReminders_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                DateTime dueDate;
                DateTime today = DateTime.Today;
                Label lblStatus = (Label)e.Row.FindControl("lblStatus");

                if (DateTime.TryParse(DataBinder.Eval(e.Row.DataItem, "DueDate")?.ToString(), out dueDate))
                {
                    TimeSpan diff = dueDate - today;

                    if (diff.TotalDays < 0)
                    {
                        // 已逾期
                        lblStatus.Text = $"已逾期 {Math.Abs(diff.TotalDays)} 天";
                        e.Row.CssClass += " overdue-row";
                    }
                    else if (diff.TotalDays == 0)
                    {
                        // 今天到期
                        lblStatus.Text = "今天到期";
                        // 可以添加一個專門的樣式，但此處沿用基礎樣式
                    }
                    else if (diff.TotalDays <= 7)
                    {
                        // 7 天內到期
                        lblStatus.Text = $"剩餘 {diff.TotalDays} 天到期";
                    }
                    else
                    {
                        lblStatus.Text = "正常借閱中";
                    }
                }
                else
                {
                    lblStatus.Text = "日期錯誤";
                }
            }
        }

        protected void btnSendOverdueReminders_Click(object sender, EventArgs e)
        {
            int sentCount = 0;
            StringBuilder log = new StringBuilder();

            foreach (GridViewRow row in gvOverdueReminders.Rows)
            {
                if (row.RowType == DataControlRowType.DataRow)
                {
                    CheckBox chkSelect = (CheckBox)row.FindControl("chkSelect");
                    if (chkSelect != null && chkSelect.Checked)
                    {
                        int lendRecordID = Convert.ToInt32(gvOverdueReminders.DataKeys[row.RowIndex].Value);
                        string username = row.Cells[2].Text; // 假設 Username 在第 3 欄
                        string email = row.Cells[3].Text;    // 假設 Email 在第 4 欄
                        string bookTitle = row.Cells[4].Text; // 假設 BookTitle 在第 5 欄
                        string dueDate = row.Cells[6].Text; // 假設 DueDate 在第 7 欄

                        // TODO: 實際的郵件發送邏輯，目前僅為模擬
                        bool success = SimulateSendEmail(username, email, bookTitle, dueDate);

                        if (success)
                        {
                            sentCount++;
                            log.AppendLine($"成功：[ID:{lendRecordID}] 寄送提醒給 {username} ({email})。");
                        }
                        else
                        {
                            log.AppendLine($"失敗：[ID:{lendRecordID}] 無法寄送提醒給 {username}。");
                        }
                    }
                }
            }

            if (sentCount > 0)
            {
                ShowMessage($"成功寄送 {sentCount} 筆提醒郵件。", "success");
            }
            else
            {
                ShowMessage("沒有選取任何記錄，或寄送失敗。", "error");
            }

            // 重新綁定以清空選取
            BindOverdueRemindersData();
        }

        private bool SimulateSendEmail(string username, string email, string bookTitle, string dueDate)
        {
            // 在實際應用中，此處應呼叫 SmtpClient 或其他郵件服務
            // 檢查 email 格式是否有效，以及是否為測試環境
            if (string.IsNullOrEmpty(email) || !email.Contains("@"))
            {
                return false;
            }

            // 模擬成功
            return true;
        }

        protected void btnRefreshReminders_Click(object sender, EventArgs e)
        {
            BindOverdueRemindersData();
        }

        private void ShowMessage(string message, string type)
        {
            lblMessage.Text = message;
            pnlMessage.Visible = true;
            pnlMessage.CssClass = "message-box";

            if (type == "error")
            {
                pnlMessage.CssClass += " message-box-error";
            }
            else if (type == "success")
            {
                pnlMessage.CssClass += " message-box-success";
            }
            else
            {
                pnlMessage.CssClass += " message-box-info";
            }
        }
    }
}