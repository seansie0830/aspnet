using System;
using System.Configuration;
using System.Data.SQLite;
using System.Diagnostics;
using System.Net.Mail; // 引入 System.Net.Mail 命名空間
using System.Web.UI;
using System.Web.UI.WebControls;
using static System.Runtime.CompilerServices.RuntimeHelpers;

public partial class ForgotPassword : System.Web.UI.Page
{
    private const string ConnectionStringName = "LibraryDBConnection";
    private const string SessionKeyEmail = "ResetEmail";
    private const string SessionKeyCode = "ResetCode";

    private string GetConnectionString()
    {
        return ConfigurationManager.ConnectionStrings[ConnectionStringName].ConnectionString;
    }

    protected void btnReset_Click(object sender, EventArgs e)
    {
        string email = txtEmail.Text.Trim();

        if (string.IsNullOrEmpty(email))
        {
            lblMessage.Text = "請輸入您的註冊信箱。";
            lblMessage.ForeColor = System.Drawing.Color.Red;
            return;
        }

        // 1. 檢查資料庫中是否存在該 Email
        string username = GetUsernameByEmail(email);

        if (!string.IsNullOrEmpty(username))
        {
            // 2. 如果 Email 存在，生成並暫存驗證碼
            string verificationCode = GenerateVerificationCode();

            // 將 Email 和驗證碼存入 Session
            Session[SessionKeyEmail] = email;
            Session[SessionKeyCode] = verificationCode;

            // 3. **實作發送電子郵件**
            bool isEmailSent = SendEmail(email, verificationCode);

            // 4. 根據寄信結果決定下一步
            if (isEmailSent)
            {
                // 切換顯示的 Panel：隱藏 Email 輸入，顯示驗證碼輸入
                pnlEmailInput.Visible = false;
                pnlVerification.Visible = true;

                lblMessage.Text = $"重設密碼信件已發送至 {email}，請檢查信箱。";
                lblMessage.ForeColor = System.Drawing.Color.Green;
            }
            else
            {
                // 如果寄信失敗，顯示錯誤訊息，並保留在當前頁面
                lblMessage.Text = "系統發生錯誤，郵件發送失敗，請檢查 SMTP 設定或網路連線。";
                lblMessage.ForeColor = System.Drawing.Color.Red;
                pnlEmailInput.Visible = true; // 讓使用者可以重新嘗試輸入信箱
                pnlVerification.Visible = false;
            }
        }
        else
        {
            // 為了安全，給予模糊的回應
            lblMessage.Text = "如果該信箱存在，重設密碼連結將會寄出。";
            lblMessage.ForeColor = System.Drawing.Color.Black;
        }
    }

    protected void btnConfirm_Click(object sender, EventArgs e)
    {
        // 1. 檢查 Session 中是否有暫存資料
        string expectedEmail = Session[SessionKeyEmail] as string;
        string expectedCode = Session[SessionKeyCode] as string;

        if (string.IsNullOrEmpty(expectedEmail) || string.IsNullOrEmpty(expectedCode))
        {
            lblMessage.Text = "驗證資訊已過期或無效，請重新輸入信箱。";
            lblMessage.ForeColor = System.Drawing.Color.Red;
            pnlEmailInput.Visible = true;
            pnlVerification.Visible = false;
            return;
        }

        string enteredCode = txtCode.Text.Trim();
        string newPassword = txtNewPassword.Text;
        string confirmPassword = txtConfirmPassword.Text;

        // 2. 驗證碼比對
        if (enteredCode != expectedCode)
        {
            lblMessage.Text = "驗證碼錯誤，請重新輸入。";
            lblMessage.ForeColor = System.Drawing.Color.Red;
            return;
        }

        // 3. 密碼比對
        if (newPassword != confirmPassword)
        {
            lblMessage.Text = "新密碼與確認密碼不一致。";
            lblMessage.ForeColor = System.Drawing.Color.Red;
            return;
        }

        // 4. 更新資料庫中的密碼
        if (UpdateUserPassword(expectedEmail, newPassword))
        {
            // 清除 Session 資料
            Session.Remove(SessionKeyEmail);
            Session.Remove(SessionKeyCode);

            // 導向登入頁面
            string script = "alert('密碼重設成功！請使用新密碼登入。'); window.location='login.aspx';";
            Page.ClientScript.RegisterStartupScript(this.GetType(), "PasswordResetSuccess", script, true);
        }
        else
        {
            lblMessage.Text = "密碼更新失敗，請稍後再試。";
            lblMessage.ForeColor = System.Drawing.Color.Red;
        }
    }

    private bool UpdateUserPassword(string email, string newPassword)
    {
        string connString = GetConnectionString();
        // 更新 Users 表中的 Password 欄位
        string sql = "UPDATE Users SET Password = @NewPassword WHERE Email = @Email";

        using (SQLiteConnection conn = new SQLiteConnection(connString))
        using (SQLiteCommand cmd = new SQLiteCommand(sql, conn))
        {
            // 參數化查詢
            cmd.Parameters.AddWithValue("@NewPassword", newPassword);
            cmd.Parameters.AddWithValue("@Email", email);

            try
            {
                conn.Open();
                int rowsAffected = cmd.ExecuteNonQuery();
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"更新密碼錯誤: {ex.Message}");
                return false;
            }
        }
    }

    private string GetUsernameByEmail(string email)
    {
        string connString = GetConnectionString();
        // 查詢 Users 表
        string sql = "SELECT Username FROM Users WHERE Email = @Email";

        using (SQLiteConnection conn = new SQLiteConnection(connString))
        using (SQLiteCommand cmd = new SQLiteCommand(sql, conn))
        {
            // 參數化查詢
            cmd.Parameters.AddWithValue("@Email", email);

            try
            {
                conn.Open();
                object result = cmd.ExecuteScalar();
                if (result != null && result != DBNull.Value)
                {
                    return result.ToString();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"查詢電子郵件錯誤: {ex.Message}");
            }
        }
        return null;
    }

    private string GenerateVerificationCode()
    {
        // 產生一個簡單的 6 位數驗證碼
        Random random = new Random();
        return random.Next(100000, 999999).ToString();
    }

    private bool SendEmail(string toEmail, string code)
    {
        // 從 web.config 的 appSettings 中讀取 SMTP 設定
        string smtpHost = ConfigurationManager.AppSettings["SmtpHost"];
        int smtpPort;
        if (!int.TryParse(ConfigurationManager.AppSettings["SmtpPort"], out smtpPort)) smtpPort = 587;
        string smtpUser = ConfigurationManager.AppSettings["SmtpUser"];
        string fromEmail = ConfigurationManager.AppSettings["FromEmail"];
        bool enableSsl;
        if (!bool.TryParse(ConfigurationManager.AppSettings["SmtpEnableSsl"], out enableSsl)) enableSsl = true;

        // ⚠️ 關鍵：從環境變數中讀取密碼
        string smtpPassword = Environment.GetEnvironmentVariable("SMTP_APP_PASSWORD");

        // 檢查必要參數是否遺失 (修正上次的 ArgumentNullException 錯誤)
        if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(smtpUser) || string.IsNullOrEmpty(fromEmail) || string.IsNullOrEmpty(smtpPassword))
        {
            Debug.WriteLine($"郵件發送錯誤: 必要的 SMTP 設定遺失或為空。");
            Debug.WriteLine($" Host:{smtpHost}, User:{smtpUser}, From:{fromEmail}, Password:{!string.IsNullOrEmpty(smtpPassword)}");
            return false;
        }

        // Debug 輸出驗證碼和 SMTP 資訊 (不包含密碼)
        Debug.WriteLine($"--- 重設密碼發送郵件 ---");
        Debug.WriteLine($"收件人: {toEmail}");
        Debug.WriteLine($"驗證碼: {code}");
        Debug.WriteLine($"SMTP Host: {smtpHost}:{smtpPort}, SSL: {enableSsl}");
        Debug.WriteLine($"寄件人: {fromEmail}, 帳號: {smtpUser}");
        Debug.WriteLine($"-----------------------");

        try
        {
            using (MailMessage mail = new MailMessage())
            {
                mail.From = new MailAddress(fromEmail, "圖書館管理系統");
                mail.To.Add(toEmail);
                mail.Subject = "您的密碼重設驗證碼";
                mail.Body = $"您的密碼重設驗證碼是: <strong>{code}</strong>。請勿將此碼告知他人。";
                mail.IsBodyHtml = true;

                using (SmtpClient smtp = new SmtpClient(smtpHost, smtpPort))
                {
                    smtp.EnableSsl = enableSsl;
                    smtp.UseDefaultCredentials = false;
                    // 使用從環境變數讀取的密碼
                    smtp.Credentials = new System.Net.NetworkCredential(smtpUser, smtpPassword);
                    smtp.DeliveryMethod = SmtpDeliveryMethod.Network;

                    // 寄送郵件
                    smtp.Send(mail);
                }
            }

            Debug.WriteLine($"郵件已成功發送至 {toEmail}");
            return true;
        }
        catch (SmtpException ex)
        {
            // 如果 SMTP 連線或驗證失敗 (例如：主機、帳號、密碼錯誤)
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
}