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
    public partial class AdminPage : Page
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

                InitializeTableDropdown();
                BindAdminData(ddlTables.SelectedValue);
            }
            else
            {
                // *** 修正步驟 3：在 PostBack 階段重新生成動態控制項 ***
                // 檢查 Session 標記和新增表單的可見性，以確保 FindControl 能夠成功找到動態控制項。
                if (Session["IsInserting"] != null && pnlInsertForm.Visible)
                {
                    GenerateInsertForm(ddlTables.SelectedValue);
#if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[DEBUG-LOAD] PostBack: 重新生成 {ddlTables.SelectedValue} 的新增表單以確保 FindControl 成功。");
#endif
                }
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
                    if (result != null && result != DBNull.Value)
                    {
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
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"獲取 UserID 錯誤: {ex.Message}");
                    return null;
                }
            }
        }

        private void InitializeTableDropdown()
        {
            ddlTables.Items.Clear();
            ddlTables.Items.Add(new ListItem("使用者帳號 (Users)", "Users"));
            ddlTables.Items.Add(new ListItem("書籍主檔 (Books)", "Books"));
            ddlTables.Items.Add(new ListItem("借閱記錄 (LendRecords)", "LendRecords"));
            ddlTables.Items.Add(new ListItem("書籍類別 (Categories)", "Categories"));
            if (ddlTables.Items.FindByValue("Users") != null)
            {
                ddlTables.SelectedValue = "Users";
            }
        }

        private void BindAdminData(string tableName)
        {
            if (string.IsNullOrEmpty(tableName)) return;

            string connString = GetConnectionString();
            if (!new[] { "Users", "Books", "LendRecords", "Categories" }.Contains(tableName))
            {
                ShowMessage("無效的資料表名稱。", "error");
                return;
            }

            string selectQuery = $"SELECT * FROM {tableName}";

            try
            {
                using (SQLiteConnection conn = new SQLiteConnection(connString))
                using (SQLiteCommand cmd = new SQLiteCommand(selectQuery, conn))
                {
                    conn.Open();
                    SQLiteDataAdapter da = new SQLiteDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    SetDataKeyNames(tableName);

                    gvAdminData.DataSource = dt;
                    gvAdminData.DataBind();

                    ShowMessage($"已成功載入資料表：{tableName} (共 {dt.Rows.Count} 筆記錄)。", "success");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"資料綁定錯誤 ({tableName}): {ex.Message}");
                ShowMessage($"載入資料時發生錯誤 ({tableName})：{ex.Message}", "error");
            }
            // 每次綁定資料時，重設新增表單的可見性
            pnlInsertForm.Visible = false;
        }

        private void SetDataKeyNames(string tableName)
        {
            switch (tableName)
            {
                case "Users": gvAdminData.DataKeyNames = new string[] { "UserID" }; break;
                case "Books": gvAdminData.DataKeyNames = new string[] { "BookID" }; break;
                case "LendRecords": gvAdminData.DataKeyNames = new string[] { "LendRecordID" }; break;
                case "Categories": gvAdminData.DataKeyNames = new string[] { "CategoryID" }; break;
                default: gvAdminData.DataKeyNames = new string[] { "DummyKey" }; break;
            }
        }

        protected void ddlTables_SelectedIndexChanged(object sender, EventArgs e)
        {
            // 在切換表格時，重設新增狀態
            Session["IsInserting"] = null;
            pnlInsertForm.Visible = false;
            BindAdminData(ddlTables.SelectedValue);
        }

        protected void gvAdminData_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            gvAdminData.PageIndex = e.NewPageIndex;
            BindAdminData(ddlTables.SelectedValue);
        }

        protected void gvAdminData_RowEditing(object sender, GridViewEditEventArgs e)
        {
            gvAdminData.EditIndex = e.NewEditIndex;
            BindAdminData(ddlTables.SelectedValue);
        }

        protected void gvAdminData_RowCancelingEdit(object sender, GridViewCancelEditEventArgs e)
        {
            gvAdminData.EditIndex = -1;
            BindAdminData(ddlTables.SelectedValue);
        }

        protected void gvAdminData_RowDeleting(object sender, GridViewDeleteEventArgs e)
        {
            string tableName = ddlTables.SelectedValue;
            if (gvAdminData.DataKeyNames.Length == 0)
            {
                ShowMessage("無法刪除：未找到主鍵資訊。", "error");
                return;
            }
            string keyName = gvAdminData.DataKeyNames[0];
            object keyValue = gvAdminData.DataKeys[e.RowIndex].Value;

            if (keyValue == null)
            {
                ShowMessage("刪除失敗：主鍵值為空。", "error");
                return;
            }

            if (tableName.Equals("Users", StringComparison.OrdinalIgnoreCase) && keyName.Equals("UserID", StringComparison.OrdinalIgnoreCase))
            {
                object currentUserIdObj = GetCurrentUserID(User.Identity.Name);

                if (currentUserIdObj != null && keyValue.ToString() == currentUserIdObj.ToString())
                {
                    ShowMessage("安全警告：您不能在登入狀態下刪除自己的帳號！", "error");
                    gvAdminData.EditIndex = -1;
                    BindAdminData(tableName);
                    return;
                }
            }

            string connString = GetConnectionString();
            string deleteSql = $"DELETE FROM {tableName} WHERE {keyName} = @Key";

            using (SQLiteConnection conn = new SQLiteConnection(connString))
            using (SQLiteCommand cmd = new SQLiteCommand(deleteSql, conn))
            {
                cmd.Parameters.AddWithValue("@Key", keyValue);
                try
                {
                    conn.Open();
                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        ShowMessage($"成功刪除 {tableName} 表格中的一筆記錄 (ID: {keyValue})。", "success");
                    }
                    else
                    {
                        ShowMessage($"刪除失敗：沒有找到匹配的記錄。", "error");
                    }
                }
                catch (Exception ex)
                {
                    ShowMessage($"刪除錯誤：{ex.Message}", "error");
                }
            }

            gvAdminData.EditIndex = -1;
            BindAdminData(tableName);
        }

        protected void gvAdminData_RowUpdating(object sender, GridViewUpdateEventArgs e)
        {
            string tableName = ddlTables.SelectedValue;
            if (gvAdminData.DataKeyNames.Length == 0)
            {
                ShowMessage("無法更新：未找到主鍵資訊。", "error");
                return;
            }
            string keyName = gvAdminData.DataKeyNames[0];
            object keyValue = gvAdminData.DataKeys[e.RowIndex].Value;

            if (keyValue == null)
            {
                ShowMessage("更新失敗：主鍵值為空。", "error");
                return;
            }

            string connString = GetConnectionString();
            StringBuilder setClauses = new StringBuilder();
            List<SQLiteParameter> parameters = new List<SQLiteParameter>();

            try
            {
                DataTable currentData = GetTableSchema(tableName);

                if (currentData == null)
                {
                    ShowMessage("更新失敗：無法獲取資料表結構。", "error");
                    gvAdminData.EditIndex = -1;
                    return;
                }

                string[] columnNames = currentData.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToArray();

                for (int i = 0; i < gvAdminData.Columns.Count; i++)
                {
                    if (i == 0) continue;

                    DataControlFieldCell cell = gvAdminData.Rows[e.RowIndex].Cells[i] as DataControlFieldCell;
                    if (cell != null && cell.Controls.Count > 0)
                    {
                        TextBox txt = cell.Controls.OfType<TextBox>().FirstOrDefault();
                        if (txt != null)
                        {
                            if ((i - 1) < columnNames.Length)
                            {
                                string columnName = columnNames[i - 1];

                                if (columnName.Equals(keyName, StringComparison.OrdinalIgnoreCase)) continue;

                                string paramName = $"@{columnName}";
                                setClauses.Append($"{columnName} = {paramName}, ");
                                parameters.Add(new SQLiteParameter(paramName, txt.Text));
                            }
                        }
                    }
                }

                if (setClauses.Length == 0)
                {
                    ShowMessage("更新失敗：沒有可更新的欄位。", "error");
                    gvAdminData.EditIndex = -1;
                    BindAdminData(tableName);
                    return;
                }

                string updateSet = setClauses.ToString().TrimEnd(',', ' ');
                string updateSql = $"UPDATE {tableName} SET {updateSet} WHERE {keyName} = @Key";

                using (SQLiteConnection conn = new SQLiteConnection(connString))
                using (SQLiteCommand cmd = new SQLiteCommand(updateSql, conn))
                {
                    cmd.Parameters.AddWithValue("@Key", keyValue);
                    cmd.Parameters.AddRange(parameters.ToArray());

                    conn.Open();
                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        ShowMessage($"成功更新 {tableName} 表格中的一筆記錄 (ID: {keyValue})。", "success");
                    }
                    else
                    {
                        ShowMessage($"更新失敗：沒有找到匹配的記錄或數據未變更。", "error");
                    }
                }
            }
            catch (Exception ex)
            {
                ShowMessage($"更新錯誤：{ex.Message}", "error");
            }

            gvAdminData.EditIndex = -1;
            BindAdminData(tableName);
        }

        protected void gvAdminData_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            // 由於改為顯式新增按鈕和表單，此方法不再處理 Footer Row 邏輯
            if (e.Row.RowType == DataControlRowType.Footer)
            {
                e.Row.Visible = false;
            }
        }

        // =========================================================
        // 顯式新增功能 (New Insert Functionality)
        // =========================================================

        protected void btnShowInsert_Click(object sender, EventArgs e)
        {
            // 1. 隱藏 GridView 的編輯模式
            gvAdminData.EditIndex = -1;
            BindAdminData(ddlTables.SelectedValue);

            // 2. 顯示並動態生成新增表單
            pnlInsertForm.Visible = true;
            GenerateInsertForm(ddlTables.SelectedValue);
            ShowMessage("請在下方表單中輸入新紀錄數據。", "info");

            // *** 修正步驟 1：設置 Session 標記，告知 Page_Load 在 PostBack 時需要重建表單 ***
            Session["IsInserting"] = true;
        }

        protected void btnCancelInsert_Click(object sender, EventArgs e)
        {
            pnlInsertForm.Visible = false;
            ShowMessage($"已取消 {ddlTables.SelectedValue} 表格的新增操作。", "info");

            // *** 修正步驟 2：移除 Session 標記 ***
            Session["IsInserting"] = null;
        }

        private void GenerateInsertForm(string tableName)
        {
            // 清除現有的動態控制項
            phInsertFormControls.Controls.Clear();

            DataTable dtSchema = GetTableSchema(tableName);
            if (dtSchema == null) return;
            string primaryKeyName = GetPrimaryKeyName(tableName);

#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[DEBUG-FORM] 正在為表格 {tableName} 生成新增表單。主鍵: {primaryKeyName}");
#endif

            // 創建表格用於佈局
            Table formTable = new Table { CssClass = "insert-form-table" };

            // 顯示當前表格名稱
            TableHeaderRow headerRow = new TableHeaderRow();
            TableHeaderCell headerCell = new TableHeaderCell { Text = $"新增至表格：**{tableName}**", ColumnSpan = 2, CssClass = "insert-form-header" };
            headerRow.Cells.Add(headerCell);
            formTable.Rows.Add(headerRow);

            foreach (DataColumn column in dtSchema.Columns)
            {
                // 忽略主鍵欄位 (假設它們是 AUTOINCREMENT)
                if (column.ColumnName.Equals(primaryKeyName, StringComparison.OrdinalIgnoreCase))
                {
#if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[DEBUG-FORM] 欄位 {column.ColumnName} 為主鍵，已跳過。");
#endif
                    continue;
                }

                TableRow row = new TableRow();

                // 標籤欄位
                TableCell labelCell = new TableCell();
                Label lbl = new Label { Text = column.ColumnName + ":" };
                labelCell.Controls.Add(lbl);
                row.Cells.Add(labelCell);

                // 輸入欄位
                TableCell inputCell = new TableCell();
                TextBox txtInsert = new TextBox();
                // *** 關鍵 ID 命名 ***
                txtInsert.ID = "txtInsert_" + column.ColumnName;
                txtInsert.CssClass = "input-insert-form";
                txtInsert.Width = Unit.Percentage(90);

#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[DEBUG-FORM] 為欄位 {column.ColumnName} 創建 TextBox ID: {txtInsert.ID}");
#endif

                // 增加提示和類型設定
                if (column.ColumnName.Contains("Date"))
                {
                    txtInsert.ToolTip = "格式: YYYY-MM-DD (例如: 2024-01-01)";
                }
                else if (column.ColumnName.Equals("Password", StringComparison.OrdinalIgnoreCase))
                {
                    txtInsert.ToolTip = "請輸入明文密碼 (系統會自動處理)";
                    txtInsert.TextMode = TextBoxMode.Password;
                }
                else if (column.ColumnName.Equals("IsAdmin", StringComparison.OrdinalIgnoreCase))
                {
                    txtInsert.ToolTip = "0=普通用戶, 1=管理員";
                }
                else if (column.DataType == typeof(int) || column.DataType == typeof(long))
                {
                    txtInsert.ToolTip = "請輸入整數值";
                }

                inputCell.Controls.Add(txtInsert);
                row.Cells.Add(inputCell);

                formTable.Rows.Add(row);
            }

            phInsertFormControls.Controls.Add(formTable);
        }

        protected void btnInsertRecord_Click(object sender, EventArgs e)
        {
            string tableName = ddlTables.SelectedValue;
            string connString = GetConnectionString();

#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[DEBUG-INSERT] 開始新增紀錄到表格: {tableName}");
#endif

            DataTable dtSchema = GetTableSchema(tableName);
            if (dtSchema == null)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[DEBUG-INSERT] 錯誤: GetTableSchema 返回 null。");
#endif
                ShowMessage($"無法新增：無法獲取資料表 {tableName} 的結構。", "error");
                return;
            }

            string primaryKeyName = GetPrimaryKeyName(tableName);

            StringBuilder columnNames = new StringBuilder();
            StringBuilder parameterNames = new StringBuilder();
            List<SQLiteParameter> parameters = new List<SQLiteParameter>();

            // 從動態生成的控制項中獲取值
            foreach (DataColumn column in dtSchema.Columns)
            {
                if (column.ColumnName.Equals(primaryKeyName, StringComparison.OrdinalIgnoreCase)) continue;

                string expectedControlID = "txtInsert_" + column.ColumnName;

                // 根據 ID 找到對應的 TextBox
                TextBox txtInsert = (TextBox)phInsertFormControls.FindControl(expectedControlID);

                if (txtInsert != null)
                {
#if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[DEBUG-INSERT] 成功找到控制項 ID: {expectedControlID}");
#endif
                    string paramName = $"@{column.ColumnName}";

                    columnNames.Append($"{column.ColumnName}, ");
                    parameterNames.Append($"{paramName}, ");

                    string inputValue = txtInsert.Text.Trim();

#if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[DEBUG-INSERT] 收集值: 欄位 {column.ColumnName}, 值: '{inputValue}'");
#endif

                    // 如果是 Users 表格，且欄位是 Password，則進行簡單的 Hash
                    if (tableName.Equals("Users", StringComparison.OrdinalIgnoreCase) && column.ColumnName.Equals("Password", StringComparison.OrdinalIgnoreCase))
                    {
                        if (string.IsNullOrEmpty(inputValue))
                        {
                            ShowMessage("新增失敗：密碼欄位不能為空。", "error");
                            return;
                        }
                        inputValue = FormsAuthentication.HashPasswordForStoringInConfigFile(inputValue, "SHA1");
                    }

                    // *** 新增檢查：如果欄位值為空字串，且欄位允許 DBNull/Nullable，可以考慮插入 DBNull
                    // 由於 SQLite 對類型檢查寬鬆，且我們假設大部分欄位是必填，這裡保持直接插入空字串

                    parameters.Add(new SQLiteParameter(paramName, inputValue));
                }
#if DEBUG
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG-INSERT] 警告: 未找到控制項 ID: {expectedControlID}");
                }
#endif
            }

#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[DEBUG-INSERT] 欄位總數 (排除主鍵): {dtSchema.Columns.Count - 1}，已收集的欄位數量: {parameters.Count}");
#endif

            if (columnNames.Length == 0)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[DEBUG-INSERT] 失敗: columnNames 為空。沒有收集到任何有效的輸入值。");
#endif
                ShowMessage("新增失敗：請輸入至少一個有效的值。", "error");
                return;
            }

            string cols = columnNames.ToString().TrimEnd(',', ' ');
            string vals = parameterNames.ToString().TrimEnd(',', ' ');
            string insertSql = $"INSERT INTO {tableName} ({cols}) VALUES ({vals})";

#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[DEBUG-INSERT] 最終 SQL: {insertSql}");
            System.Diagnostics.Debug.WriteLine($"[DEBUG-INSERT] 參數列表: {string.Join(", ", parameters.Select(p => $"{p.ParameterName}='{p.Value}'"))}");
#endif

            try
            {
                using (SQLiteConnection conn = new SQLiteConnection(connString))
                using (SQLiteCommand cmd = new SQLiteCommand(insertSql, conn))
                {
                    cmd.Parameters.AddRange(parameters.ToArray());

                    conn.Open();
                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        ShowMessage($"成功新增一筆記錄到 {tableName} 表格中。", "success");
                    }
                    else
                    {
                        ShowMessage($"新增失敗：數據未被插入。", "error");
                    }
                }
            }
            catch (SQLiteException ex)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[DEBUG-ERROR] SQLiteException: {ex.Message}");
#endif
                ShowMessage($"新增資料庫錯誤：{ex.Message}", "error");
            }
            catch (Exception ex)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[DEBUG-ERROR] General Exception: {ex.Message}");
#endif
                ShowMessage($"新增錯誤：{ex.Message}", "error");
            }

            // 新增完成後隱藏表單並重新綁定數據
            pnlInsertForm.Visible = false;
            // *** 修正步驟 4：新增成功後，移除 Session 標記 ***
            Session["IsInserting"] = null;
            BindAdminData(tableName);
        }

        // =========================================================
        // 通用輔助方法
        // =========================================================

        private DataTable GetTableSchema(string tableName)
        {
            string connString = GetConnectionString();
            string selectQuery = $"SELECT * FROM {tableName} LIMIT 0";

#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[DEBUG-SCHEMA] 嘗試獲取表格結構: {tableName}");
#endif

            try
            {
                using (SQLiteConnection conn = new SQLiteConnection(connString))
                using (SQLiteCommand cmd = new SQLiteCommand(selectQuery, conn))
                {
                    conn.Open();
                    SQLiteDataAdapter da = new SQLiteDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
#if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[DEBUG-SCHEMA] 成功獲取 {tableName} 結構，包含 {dt.Columns.Count} 個欄位。");
#endif
                    return dt;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"獲取資料表結構錯誤 ({tableName}): {ex.Message}");
                return null;
            }
        }

        private string GetPrimaryKeyName(string tableName)
        {
            switch (tableName)
            {
                case "Users": return "UserID";
                case "Books": return "BookID";
                case "LendRecords": return "LendRecordID";
                case "Categories": return "CategoryID";
                default: return "DummyKey";
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
    }
}