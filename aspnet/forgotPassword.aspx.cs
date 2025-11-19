using System;
using System.Configuration;
using System.Data.SQLite;
using System.Diagnostics;
using System.Web.UI;
using System.Web.UI.WebControls;
using static System.Runtime.CompilerServices.RuntimeHelpers;

public partial class ForgotPassword : System.Web.UI.Page
{
    // 在這裡加入 Panel 和新的 TextBox 控制項的定義，讓 CodeBehind 能夠存取它們
    // 注意: 如果您的專案設定為自動生成欄位 (例如使用 Designer 檔案)，請移除這些宣告
    // protected Panel pnlEmailInput;
    // protected Panel pnlVerification;
    // protected TextBox txtCode;
    // protected TextBox txtNewPassword;
    // protected TextBox txtConfirmPassword;


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

            // 3. 模擬發送電子郵件並在 Debug 視窗中顯示驗證碼
            SimulateSendEmail(email, verificationCode);

            // 4. 切換顯示的 Panel：隱藏 Email 輸入，顯示驗證碼輸入
            pnlEmailInput.Visible = false;
            pnlVerification.Visible = true;

            lblMessage.Text = $"重設密碼信件已發送至 {email}，請檢查信箱。";
            lblMessage.ForeColor = System.Drawing.Color.Green;
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
            lblMessage.Text = "密碼重設成功！請使用新密碼登入。";
            lblMessage.ForeColor = System.Drawing.Color.Blue;

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
        // 查詢 Users 表 (參考 schema.txt)
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

    private void SimulateSendEmail(string toEmail, string code)
    {
        // 🚨 僅在 Debug 視窗中顯示驗證碼
        Debug.WriteLine($"--- 重設密碼模擬郵件 ---");
        Debug.WriteLine($"收件人: {toEmail}");
        Debug.WriteLine($"驗證碼 (模擬): {code}");
        Debug.WriteLine($"-----------------------");
    }
}