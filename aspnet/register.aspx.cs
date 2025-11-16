using System;
using System.Configuration;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data.SQLite;
using System.Text.RegularExpressions; // 用於 Email 驗證

// **【命名空間修正】**：使用使用者提供的命名空間 'aspnet'
namespace aspnet
{
    public partial class Register : Page
    {
        // 連接字串名稱
        private const string ConnectionStringName = "LibraryDBConnection";

        private string GetConnectionString()
        {
            return ConfigurationManager.ConnectionStrings[ConnectionStringName].ConnectionString;
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            // 清空訊息
            if (!IsPostBack)
            {
                lblMessage.Text = "";
                lblMessage.Visible = false;
            }
        }

        /// <summary>
        /// 顯示狀態訊息。
        /// </summary>
        private void ShowMessage(string message, string type)
        {
            lblMessage.Text = message;
            lblMessage.Visible = true;
            lblMessage.CssClass = "message-box";

            if (type == "error")
            {
                lblMessage.CssClass += " message-box-error";
            }
            else if (type == "success")
            {
                lblMessage.CssClass += " message-box-success";
            }
        }

        /// <summary>
        /// 驗證使用者名稱是否已被使用。
        /// </summary>
        private bool IsUsernameTaken(string username)
        {
            string connString = GetConnectionString();
            string sql = "SELECT COUNT(UserID) FROM Users WHERE Username = @Username";

            using (SQLiteConnection conn = new SQLiteConnection(connString))
            using (SQLiteCommand cmd = new SQLiteCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@Username", username);
                try
                {
                    conn.Open();
                    long count = (long)cmd.ExecuteScalar();
                    return count > 0;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"檢查使用者名稱錯誤: {ex.Message}");
                    // 出錯時假設未被佔用，並在 Register_Click 顯示錯誤
                    return false;
                }
            }
        }

        /// <summary>
        /// 驗證電子郵件格式是否正確。
        /// </summary>
        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;

            // 簡單的電子郵件正則表達式驗證
            string pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            return Regex.IsMatch(email, pattern, RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// 處理註冊按鈕點擊事件。
        /// </summary>
        protected void btnRegister_Click(object sender, EventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string email = txtEmail.Text.Trim();
            string password = txtPassword.Text;
            string confirmPassword = txtConfirmPassword.Text;

            // --- 1. 基本輸入驗證 ---
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ShowMessage("所有欄位都必須填寫。", "error");
                return;
            }

            if (password != confirmPassword)
            {
                ShowMessage("密碼與確認密碼不符。", "error");
                return;
            }

            if (!IsValidEmail(email))
            {
                ShowMessage("電子郵件格式無效。", "error");
                return;
            }

            // --- 2. 業務邏輯驗證 ---
            if (IsUsernameTaken(username))
            {
                ShowMessage($"使用者名稱 '{username}' 已被註冊，請選擇其他名稱。", "error");
                return;
            }

            // --- 3. 執行註冊 ---
            string connString = GetConnectionString();
            // UserID 會自動遞增，IsAdmin 預設為 0
            string sql = "INSERT INTO Users (Username, Password, Email, IsAdmin) VALUES (@Username, @Password, @Email, 0)";

            try
            {
                using (SQLiteConnection conn = new SQLiteConnection(connString))
                using (SQLiteCommand cmd = new SQLiteCommand(sql, conn))
                {
                    // **【重要】**：在實際生產環境中，請務必對密碼進行雜湊 (Hash) 處理！
                    cmd.Parameters.AddWithValue("@Username", username);
                    cmd.Parameters.AddWithValue("@Password", password);
                    cmd.Parameters.AddWithValue("@Email", email);

                    conn.Open();
                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        // 註冊成功，導向登入頁面
                        string script = "alert('註冊成功! 請使用您的帳號登入。'); window.location='Login.aspx';";
                        Page.ClientScript.RegisterStartupScript(this.GetType(), "RegisterSuccess", script, true);
                    }
                    else
                    {
                        ShowMessage("註冊失敗，請稍後再試。", "error");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"註冊資料庫錯誤: {ex.Message}");
                ShowMessage($"註冊過程中發生系統錯誤：{ex.Message}", "error");
            }
        }
    }
}