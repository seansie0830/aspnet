using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data.SQLite;
using System.Configuration;
using System.Web.Security;


public partial class Login : System.Web.UI.Page // 確保類別名稱與您的頁面名稱一致
{
    protected TextBox txtUsername;
    protected TextBox txtPassword;
    protected Label lblMessage;
    private const string ConnectionStringName = "LibraryDBConnection";
    private string GetConnectionString()
    {
        // 確保從 ConfigurationManager 讀取連接字串
        return ConfigurationManager.ConnectionStrings[ConnectionStringName].ConnectionString;
    }
    private bool IsUserAdmin(string username)
    {
        if (string.IsNullOrEmpty(username)) return false;

        string connString = GetConnectionString();
        // 查詢 IsAdmin 欄位
        string sql = "SELECT IsAdmin FROM Users WHERE Username = @Username";

        using (SQLiteConnection conn = new SQLiteConnection(connString))
        using (SQLiteCommand cmd = new SQLiteCommand(sql, conn))
        {
            cmd.Parameters.AddWithValue("@Username", username);
            try
            {
                conn.Open();
                object result = cmd.ExecuteScalar();
                if (result != null && result != DBNull.Value)
                {
                    // SQLite 的 INTEGER 欄位會被讀取為 long
                    return Convert.ToInt64(result) == 1;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"管理員檢查錯誤: {ex.Message}");
            }
        }
        return false;
    }
    protected void btnLogin_Click(object sender, EventArgs e)
    {
        string username = txtUsername.Text;
        string password = txtPassword.Text;

        if (ValidateUser(username, password))
        {
            FormsAuthentication.SetAuthCookie(username, false);

            bool isAdmin = IsUserAdmin(username);

            // 2. 【修正】根據身份設定導向網址
            string redirectUrl;
            string welcomeMessage;

            if (isAdmin)
            {
                // 【重點】: 如果是管理員，導向 AdminPage.aspx
                redirectUrl = ResolveUrl("~/AdminPage.aspx");
                welcomeMessage = "登入成功! 歡迎來到管理員專區";
            }
            else
            {
                // 如果是普通使用者，導向 MyHomepage.aspx
                redirectUrl = ResolveUrl("~/MyHomepage.aspx");
                welcomeMessage = "登入成功! 歡迎來到個人頁面";
            }

            // 3. 構建 JavaScript 腳本：先彈出 alert，然後再跳轉
            string script = $"alert('{welcomeMessage}'); window.location='{redirectUrl}';";

            // 4. 註冊腳本，讓它在頁面載入時執行 (移除 #if DEBUG 條件)
            Page.ClientScript.RegisterStartupScript(
                this.GetType(),
                "LoginSuccessAlert", // 腳本的 Key，確保唯一
                script,
                true // True 表示自動加上 <script> 標籤
            );

        }
        else
        {
            lblMessage.Text = "登入失敗：帳號或密碼錯誤。";
        }
    }

    private bool ValidateUser(string username, string password)
    {
        // 從 Web.config 讀取連接字串
        string connString = ConfigurationManager.ConnectionStrings["LibraryDBConnection"].ConnectionString;
        string sql = "SELECT UserID FROM Users WHERE Username = @Username AND Password = @Password";

        using (SQLiteConnection conn = new SQLiteConnection(connString))
        {
            using (SQLiteCommand cmd = new SQLiteCommand(sql, conn))
            {
                // **【重要】** 參數化查詢
                cmd.Parameters.AddWithValue("@Username", username);
                cmd.Parameters.AddWithValue("@Password", password);

                conn.Open();

                object result = cmd.ExecuteScalar();

                return result != null; // 如果找到 UserID，則返回 true
            }
        }
    }
}