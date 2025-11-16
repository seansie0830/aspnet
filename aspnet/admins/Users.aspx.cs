using System;
using System.Data;
using System.Configuration;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data.SQLite;
using System.Web.Security;
using System.Text;
using System.Collections.Generic;
using System.Linq;

namespace aspnet
{
    public partial class Users : Page
    {
        private const string ConnectionStringName = "LibraryDBConnection";
        private string GetConnectionString()
        {
            return ConfigurationManager.ConnectionStrings[ConnectionStringName].ConnectionString;
        }

        public string SortExpression
        {
            get { return ViewState["SortExpression"] as string ?? "UserID"; }
            set { ViewState["SortExpression"] = value; }
        }

        public SortDirection SortDirection
        {
            get { return (SortDirection)(ViewState["SortDirection"] ?? SortDirection.Ascending); }
            set { ViewState["SortDirection"] = value; }
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

                if (!IsUserAdminOrStaff(User.Identity.Name))
                {
                    ShowMessage("存取遭拒：您不具備管理員或工作人員權限。", "error");
                    Response.Redirect("~/MyHomepage.aspx?AccessDenied=True");
                    return;
                }

                ddlPageSize.SelectedValue = gvUsers.PageSize.ToString();
                BindUsersData();
            }
        }

        private bool IsUserAdminOrStaff(string username)
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
                    if (result != null && result != DBNull.Value)
                    {
                        long isAdmin = Convert.ToInt64(result);
                        return isAdmin == 1 || isAdmin == 2;
                    }
                }
                catch (Exception)
                {
                }
            }
            return false;
        }

        private int GetUserAdminLevel(string username)
        {
            if (string.IsNullOrEmpty(username)) return -1;
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
                    if (result != null && result != DBNull.Value)
                    {
                        return Convert.ToInt32(result);
                    }
                }
                catch (Exception)
                {
                }
            }
            return -1;
        }

        private object GetCurrentUserID(string username)
        {
            if (string.IsNullOrEmpty(username)) return null;
            string connString = GetConnectionString();
            string sql = "SELECT UserID FROM Users WHERE Username = @Username";
            using (SQLiteConnection conn = new SQLiteConnection(connString))
            using (SQLiteCommand cmd = new SQLiteCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@Username", username);
                try
                {
                    conn.Open();
                    return cmd.ExecuteScalar();
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }

        private void BindUsersData()
        {
            string connString = GetConnectionString();
            string selectQuery = "SELECT UserID, Username, Password, Email, IsAdmin FROM Users";

            StringBuilder whereClause = new StringBuilder();
            List<SQLiteParameter> parameters = new List<SQLiteParameter>();

            // 處理一般搜尋
            string searchKeyword = txtSearch.Text.Trim();
            if (!string.IsNullOrEmpty(searchKeyword) && !pnlAdvancedSearch.Visible)
            {
                whereClause.Append(" WHERE Username LIKE @Search OR Email LIKE @Search ");
                parameters.Add(new SQLiteParameter("@Search", $"%{searchKeyword}%"));
            }

            // 處理進階搜尋
            if (pnlAdvancedSearch.Visible)
            {
                string advUsername = txtAdvUsername.Text.Trim();
                string advEmail = txtAdvEmail.Text.Trim();
                string advIsAdmin = ddlAdvIsAdmin.SelectedValue;

                if (!string.IsNullOrEmpty(advUsername))
                {
                    whereClause.Append(whereClause.Length == 0 ? " WHERE " : " AND ");
                    whereClause.Append(" Username LIKE @AdvUsername ");
                    parameters.Add(new SQLiteParameter("@AdvUsername", $"%{advUsername}%"));
                }
                if (!string.IsNullOrEmpty(advEmail))
                {
                    whereClause.Append(whereClause.Length == 0 ? " WHERE " : " AND ");
                    whereClause.Append(" Email LIKE @AdvEmail ");
                    parameters.Add(new SQLiteParameter("@AdvEmail", $"%{advEmail}%"));
                }
                if (!string.IsNullOrEmpty(advIsAdmin))
                {
                    whereClause.Append(whereClause.Length == 0 ? " WHERE " : " AND ");
                    whereClause.Append(" IsAdmin = @AdvIsAdmin ");
                    parameters.Add(new SQLiteParameter("@AdvIsAdmin", advIsAdmin));
                }
            }

            selectQuery += whereClause.ToString();

            string sortOrder = SortExpression;
            if (SortDirection == SortDirection.Descending)
            {
                sortOrder += " DESC";
            }

            selectQuery += $" ORDER BY {sortOrder}";

            try
            {
                using (SQLiteConnection conn = new SQLiteConnection(connString))
                using (SQLiteCommand cmd = new SQLiteCommand(selectQuery, conn))
                {
                    cmd.Parameters.AddRange(parameters.ToArray());
                    conn.Open();
                    SQLiteDataAdapter da = new SQLiteDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    gvUsers.DataSource = dt;
                    gvUsers.DataBind();

                    ShowMessage($"已成功載入使用者帳號 (共 {dt.Rows.Count} 筆記錄)。", "success");
                }
            }
            catch (Exception ex)
            {
                ShowMessage($"載入資料時發生錯誤：{ex.Message}", "error");
            }
            pnlInsertForm.Visible = false;
        }

        protected void gvUsers_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            gvUsers.PageIndex = e.NewPageIndex;
            BindUsersData();
        }

        protected void gvUsers_Sorting(object sender, GridViewSortEventArgs e)
        {
            if (e.SortExpression.Equals(SortExpression))
            {
                SortDirection = (SortDirection == SortDirection.Ascending) ? SortDirection.Descending : SortDirection.Ascending;
            }
            else
            {
                SortExpression = e.SortExpression;
                SortDirection = SortDirection.Ascending;
            }
            gvUsers.PageIndex = 0;
            BindUsersData();
        }

        protected void ddlPageSize_SelectedIndexChanged(object sender, EventArgs e)
        {
            gvUsers.PageSize = int.Parse(ddlPageSize.SelectedValue);
            gvUsers.PageIndex = 0;
            BindUsersData();
        }

        protected void gvUsers_RowEditing(object sender, GridViewEditEventArgs e)
        {
            gvUsers.EditIndex = e.NewEditIndex;
            BindUsersData();
        }

        protected void gvUsers_RowCancelingEdit(object sender, GridViewCancelEditEventArgs e)
        {
            gvUsers.EditIndex = -1;
            BindUsersData();
        }

        protected void gvUsers_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                int currentAdminLevel = GetUserAdminLevel(User.Identity.Name);
                DataRowView drv = e.Row.DataItem as DataRowView;

                // 限制 IsAdmin=2 (工作人員) 不能修改 IsAdmin=1 (管理員) 的帳號
                if (currentAdminLevel == 2)
                {
                    if (drv != null && Convert.ToInt32(drv["IsAdmin"]) == 1)
                    {
                        LinkButton editButton = e.Row.Cells[e.Row.Cells.Count - 2].Controls.OfType<LinkButton>().FirstOrDefault(b => b.CommandName == "Edit");
                        LinkButton deleteButton = e.Row.Cells[e.Row.Cells.Count - 1].Controls.OfType<LinkButton>().FirstOrDefault(b => b.CommandName == "Delete");
                        if (editButton != null) editButton.Visible = false;
                        if (deleteButton != null) deleteButton.Visible = false;
                    }
                }

                // Root 帳號的防呆邏輯
                if (drv != null && drv["Username"].ToString().Equals("root", StringComparison.OrdinalIgnoreCase))
                {
                    // 禁止修改 root 帳號的權限為非管理員
                    if (e.Row.RowState == DataControlRowState.Edit)
                    {
                        DropDownList ddl = e.Row.FindControl("ddlIsAdminEdit") as DropDownList;
                        if (ddl != null)
                        {
                            ListItem staffItem = ddl.Items.FindByValue("2");
                            ListItem normalItem = ddl.Items.FindByValue("0");
                            if (staffItem != null) ddl.Items.Remove(staffItem);
                            if (normalItem != null) ddl.Items.Remove(normalItem);
                        }
                    }
                }
            }
        }

        protected void gvUsers_RowUpdating(object sender, GridViewUpdateEventArgs e)
        {
            int currentAdminLevel = GetUserAdminLevel(User.Identity.Name);
            int rowUserID = Convert.ToInt32(gvUsers.DataKeys[e.RowIndex].Value);
            GridViewRow row = gvUsers.Rows[e.RowIndex];

            TextBox txtUsernameEdit = row.FindControl("txtUsernameEdit") as TextBox;
            TextBox txtPasswordEdit = row.FindControl("txtPasswordEdit") as TextBox;
            TextBox txtEmailEdit = row.FindControl("txtEmailEdit") as TextBox;
            DropDownList ddlIsAdminEdit = row.FindControl("ddlIsAdminEdit") as DropDownList;

            string newUsername = txtUsernameEdit?.Text.Trim();
            string newPassword = txtPasswordEdit?.Text;
            string newEmail = txtEmailEdit?.Text.Trim();
            int newIsAdmin = Convert.ToInt32(ddlIsAdminEdit?.SelectedValue);

            // 獲取原始資料以進行比較
            DataRowView originalData = ((DataRowView)((GridView)sender).Rows[e.RowIndex].DataItem);
            string originalUsername = originalData["Username"].ToString();
            int originalIsAdmin = Convert.ToInt32(originalData["IsAdmin"]);

            // 工作人員 (IsAdmin=2) 限制: 禁止修改管理員 (IsAdmin=1) 的帳號
            if (currentAdminLevel == 2 && originalIsAdmin == 1)
            {
                ShowMessage("權限不足：工作人員禁止修改管理員帳號。", "error");
                gvUsers.EditIndex = -1;
                BindUsersData();
                return;
            }

            // root 帱號防呆: root 權限必須保持為 1
            if (originalUsername.Equals("root", StringComparison.OrdinalIgnoreCase) && newIsAdmin != 1)
            {
                ShowMessage("防呆警告：root 帳號的權限必須保持為 1 (管理員)。", "error");
                gvUsers.EditIndex = -1;
                BindUsersData();
                return;
            }

            // root 帳號數量防呆: 禁止將 root 以外的唯一管理員帳號的權限改掉
            if (originalIsAdmin == 1 && newIsAdmin != 1)
            {
                if (IsSingleRoot(rowUserID))
                {
                    ShowMessage("防呆警告：您不能將唯一的管理員帳號的權限移除。", "error");
                    gvUsers.EditIndex = -1;
                    BindUsersData();
                    return;
                }
            }

            // 工作人員 (IsAdmin=2) 限制: 禁止授予別人 IsAdmin=1 (管理員) 權限
            if (currentAdminLevel == 2 && newIsAdmin == 1 && originalIsAdmin != 1)
            {
                ShowMessage("權限不足：工作人員禁止授予他人管理員權限。", "error");
                gvUsers.EditIndex = -1;
                BindUsersData();
                return;
            }

            string connString = GetConnectionString();
            StringBuilder setClauses = new StringBuilder();
            List<SQLiteParameter> parameters = new List<SQLiteParameter>();

            try
            {
                // 檢查 Username 唯一性
                if (!originalUsername.Equals(newUsername, StringComparison.OrdinalIgnoreCase))
                {
                    if (IsUsernameExist(newUsername, rowUserID))
                    {
                        ShowMessage("更新失敗：帳號名稱已存在。", "error");
                        gvUsers.EditIndex = -1;
                        BindUsersData();
                        return;
                    }
                }
                setClauses.Append("Username = @Username, ");
                parameters.Add(new SQLiteParameter("@Username", newUsername));

                // 密碼更新 (如果輸入了新密碼)
                if (!string.IsNullOrEmpty(newPassword))
                {
                    string hashedPassword = FormsAuthentication.HashPasswordForStoringInConfigFile(newPassword, "SHA1");
                    setClauses.Append("Password = @Password, ");
                    parameters.Add(new SQLiteParameter("@Password", hashedPassword));
                }

                setClauses.Append("Email = @Email, ");
                parameters.Add(new SQLiteParameter("@Email", newEmail));

                setClauses.Append("IsAdmin = @IsAdmin, ");
                parameters.Add(new SQLiteParameter("@IsAdmin", newIsAdmin));

                string updateSet = setClauses.ToString().TrimEnd(',', ' ');
                string updateSql = $"UPDATE Users SET {updateSet} WHERE UserID = @Key";

                using (SQLiteConnection conn = new SQLiteConnection(connString))
                using (SQLiteCommand cmd = new SQLiteCommand(updateSql, conn))
                {
                    cmd.Parameters.AddWithValue("@Key", rowUserID);
                    cmd.Parameters.AddRange(parameters.ToArray());

                    conn.Open();
                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        ShowMessage($"成功更新使用者帳號 ID {rowUserID}。", "success");
                    }
                    else
                    {
                        ShowMessage("更新失敗：沒有找到匹配的記錄或數據未變更。", "error");
                    }
                }
            }
            catch (Exception ex)
            {
                ShowMessage($"更新錯誤：{ex.Message}", "error");
            }

            gvUsers.EditIndex = -1;
            BindUsersData();
        }

        protected void gvUsers_RowDeleting(object sender, GridViewDeleteEventArgs e)
        {
            int rowUserID = Convert.ToInt32(gvUsers.DataKeys[e.RowIndex].Value);
            object currentUserIdObj = GetCurrentUserID(User.Identity.Name);
            int currentAdminLevel = GetUserAdminLevel(User.Identity.Name);

            // 獲取被刪除帳號的資訊
            string originalUsername = ((DataRowView)((GridView)sender).Rows[e.RowIndex].DataItem)["Username"].ToString();
            int originalIsAdmin = Convert.ToInt32(((DataRowView)((GridView)sender).Rows[e.RowIndex].DataItem)["IsAdmin"]);

            // 防呆 1: 禁止刪除自己
            if (currentUserIdObj != null && rowUserID.ToString() == currentUserIdObj.ToString())
            {
                ShowMessage("安全警告：您不能在登入狀態下刪除自己的帳號！", "error");
                BindUsersData();
                return;
            }

            // 防呆 2: 禁止刪除 root 帳號
            if (originalUsername.Equals("root", StringComparison.OrdinalIgnoreCase))
            {
                ShowMessage("防呆警告：root 帳號禁止刪除。", "error");
                BindUsersData();
                return;
            }

            // 防呆 3: 工作人員 (IsAdmin=2) 禁止刪除管理員 (IsAdmin=1)
            if (currentAdminLevel == 2 && originalIsAdmin == 1)
            {
                ShowMessage("權限不足：工作人員禁止刪除管理員帳號。", "error");
                BindUsersData();
                return;
            }

            // 防呆 4: 禁止刪除唯一的管理員帳號 (root除外)
            if (originalIsAdmin == 1 && IsSingleRoot(rowUserID))
            {
                ShowMessage("防呆警告：您不能刪除唯一的管理員帳號 (root除外)。", "error");
                BindUsersData();
                return;
            }

            string deleteSql = "DELETE FROM Users WHERE UserID = @Key";
            string connString = GetConnectionString();

            try
            {
                using (SQLiteConnection conn = new SQLiteConnection(connString))
                using (SQLiteCommand cmd = new SQLiteCommand(deleteSql, conn))
                {
                    cmd.Parameters.AddWithValue("@Key", rowUserID);
                    conn.Open();
                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        ShowMessage($"成功刪除使用者帳號 ID {rowUserID}。", "success");
                    }
                    else
                    {
                        ShowMessage("刪除失敗：沒有找到匹配的記錄。", "error");
                    }
                }
            }
            catch (Exception ex)
            {
                ShowMessage($"刪除錯誤：{ex.Message}", "error");
            }

            BindUsersData();
        }

        protected void btnSearch_Click(object sender, EventArgs e)
        {
            pnlAdvancedSearch.Visible = false;
            gvUsers.PageIndex = 0;
            BindUsersData();
        }

        protected void btnToggleAdvancedSearch_Click(object sender, EventArgs e)
        {
            pnlAdvancedSearch.Visible = !pnlAdvancedSearch.Visible;
            if (pnlAdvancedSearch.Visible)
            {
                txtSearch.Text = string.Empty; // 關閉一般搜尋
            }
            else
            {
                // 清除進階搜尋條件
                txtAdvUsername.Text = string.Empty;
                txtAdvEmail.Text = string.Empty;
                ddlAdvIsAdmin.SelectedValue = "";
            }
            gvUsers.PageIndex = 0;
            BindUsersData();
        }

        protected void btnPerformAdvancedSearch_Click(object sender, EventArgs e)
        {
            txtSearch.Text = string.Empty;
            gvUsers.PageIndex = 0;
            BindUsersData();
        }

        protected void btnClearSearch_Click(object sender, EventArgs e)
        {
            txtSearch.Text = string.Empty;
            pnlAdvancedSearch.Visible = false;
            txtAdvUsername.Text = string.Empty;
            txtAdvEmail.Text = string.Empty;
            ddlAdvIsAdmin.SelectedValue = "";
            gvUsers.PageIndex = 0;
            BindUsersData();
        }

        protected void btnShowInsert_Click(object sender, EventArgs e)
        {
            gvUsers.EditIndex = -1;
            BindUsersData();
            pnlInsertForm.Visible = true;
            ShowMessage("請在下方表單中輸入新帳號資訊。", "info");
        }

        protected void btnCancelInsert_Click(object sender, EventArgs e)
        {
            pnlInsertForm.Visible = false;
            ShowMessage("已取消新增操作。", "info");
        }

        protected void btnInsertRecord_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid)
            {
                ShowMessage("新增失敗：請檢查所有必填欄位和輸入格式。", "error");
                return;
            }

            TextBox txtUsername = pnlInsertForm.FindControl("txtInsert_Username") as TextBox;
            TextBox txtPassword = pnlInsertForm.FindControl("txtInsert_Password") as TextBox;
            TextBox txtEmail = pnlInsertForm.FindControl("txtInsert_Email") as TextBox;
            DropDownList ddlIsAdmin = pnlInsertForm.FindControl("ddlInsert_IsAdmin") as DropDownList;

            string newUsername = txtUsername.Text.Trim();
            string newPassword = txtPassword.Text;
            string newEmail = txtEmail.Text.Trim();
            int newIsAdmin = Convert.ToInt32(ddlIsAdmin.SelectedValue);

            int currentAdminLevel = GetUserAdminLevel(User.Identity.Name);

            // 防呆 1: 檢查 Username 唯一性
            if (IsUsernameExist(newUsername, null))
            {
                ShowMessage("新增失敗：帳號名稱已存在。", "error");
                return;
            }

            // 防呆 2: 工作人員 (IsAdmin=2) 限制: 禁止授予別人 IsAdmin=1 (管理員) 權限
            if (currentAdminLevel == 2 && newIsAdmin == 1)
            {
                ShowMessage("權限不足：工作人員禁止授予他人管理員權限。", "error");
                return;
            }

            // 防呆 3: root 帳號只能有一個
            if (newUsername.Equals("root", StringComparison.OrdinalIgnoreCase))
            {
                if (IsRootExist())
                {
                    ShowMessage("新增失敗：root 帳號只能有一個。", "error");
                    return;
                }
                newIsAdmin = 1; // 確保 root 的權限是 1
            }

            string connString = GetConnectionString();
            string insertSql = "INSERT INTO Users (Username, Password, Email, IsAdmin) VALUES (@Username, @Password, @Email, @IsAdmin)";

            try
            {
                string hashedPassword = FormsAuthentication.HashPasswordForStoringInConfigFile(newPassword, "SHA1");

                using (SQLiteConnection conn = new SQLiteConnection(connString))
                using (SQLiteCommand cmd = new SQLiteCommand(insertSql, conn))
                {
                    cmd.Parameters.AddWithValue("@Username", newUsername);
                    cmd.Parameters.AddWithValue("@Password", hashedPassword);
                    cmd.Parameters.AddWithValue("@Email", newEmail);
                    cmd.Parameters.AddWithValue("@IsAdmin", newIsAdmin);

                    conn.Open();
                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        ShowMessage("成功建立新使用者帳號。", "success");
                        // 清空表單
                        txtUsername.Text = string.Empty;
                        txtPassword.Text = string.Empty;
                        txtEmail.Text = string.Empty;
                        ddlIsAdmin.SelectedValue = "0";
                    }
                    else
                    {
                        ShowMessage("新增失敗：數據未被插入。", "error");
                    }
                }
            }
            catch (Exception ex)
            {
                ShowMessage($"新增錯誤：{ex.Message}", "error");
            }

            pnlInsertForm.Visible = false;
            BindUsersData();
        }

        private bool IsUsernameExist(string username, int? excludeUserID)
        {
            string connString = GetConnectionString();
            string sql = "SELECT COUNT(*) FROM Users WHERE Username = @Username";
            if (excludeUserID.HasValue)
            {
                sql += " AND UserID != @UserID";
            }

            using (SQLiteConnection conn = new SQLiteConnection(connString))
            using (SQLiteCommand cmd = new SQLiteCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@Username", username);
                if (excludeUserID.HasValue)
                {
                    cmd.Parameters.AddWithValue("@UserID", excludeUserID.Value);
                }
                try
                {
                    conn.Open();
                    return Convert.ToInt64(cmd.ExecuteScalar()) > 0;
                }
                catch (Exception)
                {
                    return true;
                }
            }
        }

        private bool IsRootExist()
        {
            string connString = GetConnectionString();
            string sql = "SELECT COUNT(*) FROM Users WHERE Username = 'root'";
            using (SQLiteConnection conn = new SQLiteConnection(connString))
            using (SQLiteCommand cmd = new SQLiteCommand(sql, conn))
            {
                try
                {
                    conn.Open();
                    return Convert.ToInt64(cmd.ExecuteScalar()) > 0;
                }
                catch (Exception)
                {
                    return true;
                }
            }
        }

        private bool IsSingleRoot(int excludeUserID)
        {
            string connString = GetConnectionString();
            string sql = "SELECT COUNT(*) FROM Users WHERE IsAdmin = 1 AND UserID != @UserID";
            using (SQLiteConnection conn = new SQLiteConnection(connString))
            using (SQLiteCommand cmd = new SQLiteCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@UserID", excludeUserID);
                try
                {
                    conn.Open();
                    // 如果只剩下 root 這個管理員，則返回 1
                    // 檢查排除後的管理員數量是否 <= 1 (<=1 是指只剩下 root 帳號)
                    return Convert.ToInt64(cmd.ExecuteScalar()) == 0;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        public string GetAdminStatusText(object isAdminObj)
        {
            if (isAdminObj == null || isAdminObj == DBNull.Value) return "未知";

            int isAdmin = Convert.ToInt32(isAdminObj);
            switch (isAdmin)
            {
                case 0: return "普通用戶";
                case 1: return "<span style='color: red; font-weight: bold;'>管理員</span>";
                case 2: return "<span style='color: green; font-weight: bold;'>工作人員</span>";
                default: return "其他";
            }
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

        private (string Username, int IsAdmin) GetUserById(int userId)
        {
            string connString = GetConnectionString();
            string sql = "SELECT Username, IsAdmin FROM Users WHERE UserID = @UserID";
            using (var conn = new SQLiteConnection(connString))
            using (var cmd = new SQLiteCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@UserID", userId);
                conn.Open();
                using (var rdr = cmd.ExecuteReader())
                {
                    if (rdr.Read())
                    {
                        return (rdr["Username"].ToString(), Convert.ToInt32(rdr["IsAdmin"]));
                    }
                }
            }
            return (null, -1);
        }
    }
}