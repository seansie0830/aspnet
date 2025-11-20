using System;
using System.Data;
using System.Configuration;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data.SQLite;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail; // 引入 System.Net.Mail 命名空間
using System.Diagnostics; // 引入 Debug.WriteLine

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
                                WHERE L.ReturnDate IS NULL";

            string filter = ddlOverdueDays.SelectedValue;
            string whereClause = string.Empty;

            if (filter == "Overdue")
            {
                whereClause = " AND L.DueDate < DATE('now')";
            }
            else if (filter == "DueIn7")
            {
                whereClause = " AND L.DueDate <= DATE('now', '+7 day') AND L.DueDate >= DATE('now')";
            }
            else if (filter == "DueToday")
            {
                whereClause = " AND L.DueDate = DATE('now')";
            }

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
                        lblStatus.Text = $"已逾期 {Math.Abs(diff.TotalDays)} 天";
                        e.Row.CssClass += " overdue-row";
                    }
                    else if (diff.TotalDays == 0)
                    {
                        lblStatus.Text = "今天到期";
                    }
                    else if (diff.TotalDays <= 7)
                    {
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
            int failureCount = 0;
            StringBuilder log = new StringBuilder();

            foreach (GridViewRow row in gvOverdueReminders.Rows)
            {
                if (row.RowType == DataControlRowType.DataRow)
                {
                    CheckBox chkSelect = (CheckBox)row.FindControl("chkSelect");
                    if (chkSelect != null && chkSelect.Checked)
                    {
                        int lendRecordID = Convert.ToInt32(gvOverdueReminders.DataKeys[row.RowIndex].Value);
                        string username = row.Cells[2].Text;
                        string email = row.Cells[3].Text;
                        string bookTitle = row.Cells[4].Text;
                        string dueDate = row.Cells[6].Text;

                        bool success = SendOverdueReminderEmail(username, email, bookTitle, dueDate);

                        if (success)
                        {
                            sentCount++;
                            log.AppendLine($"成功：[ID:{lendRecordID}] 寄送提醒給 {username} ({email})。");
                        }
                        else
                        {
                            failureCount++;
                            log.AppendLine($"失敗：[ID:{lendRecordID}] 無法寄送提醒給 {username}。");
                        }
                    }
                }
            }

            if (sentCount > 0)
            {
                string message = $"成功寄送 {sentCount} 筆提醒郵件。";
                if (failureCount > 0) message += $" (失敗 {failureCount} 筆)";
                ShowMessage(message, "success");
            }
            else
            {
                ShowMessage("沒有選取任何記錄，或寄送失敗。", "error");
            }

            BindOverdueRemindersData();
        }

        private bool SendOverdueReminderEmail(string username, string email, string bookTitle, string dueDate)
        {
            string smtpHost = ConfigurationManager.AppSettings["SmtpHost"];
            int smtpPort;
            if (!int.TryParse(ConfigurationManager.AppSettings["SmtpPort"], out smtpPort)) smtpPort = 587;
            string smtpUser = ConfigurationManager.AppSettings["SmtpUser"];
            string fromEmail = ConfigurationManager.AppSettings["FromEmail"];
            bool enableSsl;
            if (!bool.TryParse(ConfigurationManager.AppSettings["SmtpEnableSsl"], out enableSsl)) enableSsl = true;

            string smtpPassword = Environment.GetEnvironmentVariable("SMTP_APP_PASSWORD");

            if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(smtpUser) || string.IsNullOrEmpty(fromEmail) || string.IsNullOrEmpty(smtpPassword))
            {
                Debug.WriteLine($"郵件發送錯誤: 必要的 SMTP 設定遺失或為空。");
                Debug.WriteLine($" Host:{smtpHost}, User:{smtpUser}, From:{fromEmail}, Password:{!string.IsNullOrEmpty(smtpPassword)}");
                return false;
            }

            Debug.WriteLine($"--- 逾期提醒郵件發送 ---");
            Debug.WriteLine($"收件人: {email} ({username})");
            Debug.WriteLine($"書籍: {bookTitle}");
            Debug.WriteLine($"應還日: {dueDate}");
            Debug.WriteLine($"SMTP Host: {smtpHost}:{smtpPort}, SSL: {enableSsl}");
            Debug.WriteLine($"寄件人: {fromEmail}, 帳號: {smtpUser}");
            Debug.WriteLine($"-----------------------");

            try
            {
                using (MailMessage mail = new MailMessage())
                {
                    mail.From = new MailAddress(fromEmail, "圖書館管理系統 - 借閱提醒");
                    mail.To.Add(email);
                    mail.Subject = $"圖書借閱提醒：您借閱的《{bookTitle}》即將/已到期";
                    mail.Body = $@"
                        <p>親愛的 {username}，您好：</p>
                        <p>這封信是提醒您，您所借閱的圖書即將或已超過歸還日期：</p>
                        <ul>
                            <li><strong>書籍名稱</strong>：《{bookTitle}》</li>
                            <li><strong>應還日期</strong>：{dueDate}</li>
                        </ul>
                        <p>為避免影響您的借閱權益或產生逾期罰款，請您儘快至圖書館歸還。</p>
                        <p>此致，</p>
                        <p>圖書館管理系統</p>
                    ";
                    mail.IsBodyHtml = true;

                    using (SmtpClient smtp = new SmtpClient(smtpHost, smtpPort))
                    {
                        smtp.EnableSsl = enableSsl;
                        smtp.UseDefaultCredentials = false;
                        smtp.Credentials = new System.Net.NetworkCredential(smtpUser, smtpPassword);
                        smtp.DeliveryMethod = SmtpDeliveryMethod.Network;

                        smtp.Send(mail);
                    }
                }

                Debug.WriteLine($"郵件已成功發送至 {email}");
                return true;
            }
            catch (SmtpException ex)
            {
                Debug.WriteLine($"郵件發送失敗 (SMTP 錯誤): {ex.Message}");
                if (ex.InnerException != null)
                {
                    Debug.WriteLine($"內部錯誤: {ex.InnerException.Message}");
                }
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"郵件發送發生未知錯誤: {ex.Message}");
                return false;
            }
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