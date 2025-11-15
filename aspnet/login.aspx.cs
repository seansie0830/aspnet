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

    protected void btnLogin_Click(object sender, EventArgs e)
    {
        string username = txtUsername.Text;
        string password = txtPassword.Text;

        if (ValidateUser(username, password))
        {
#if DEBUG
            string redirectUrl = FormsAuthentication.GetRedirectUrl(username, false);

            // 2. 構建 JavaScript 腳本：先彈出 alert，然後再跳轉
            string script = $"alert('登入成功!'); window.location='{redirectUrl}';";

            // 3. 註冊腳本，讓它在頁面載入時執行
            // GetType() 是為了確保腳本名稱的唯一性
            Page.ClientScript.RegisterStartupScript(
                this.GetType(),
                "LoginSuccessAlert", // 腳本的 Key，確保唯一
                script,
                true // True 表示自動加上 <script> 標籤
            );
#endif
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