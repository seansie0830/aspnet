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

// **【命名空間修正】**：使用使用者提供的命名空間 'aspnet'
namespace aspnet
{
    // 確保 Inherits="AdminPage" 與這裡的 class 名稱一致
    public partial class AdminPage : Page
    {
        // 連接字串名稱，用於 ConfigurationManager
        private const string ConnectionStringName = "LibraryDBConnection";

        // 獲取連接字串
        private string GetConnectionString()
        {
            return ConfigurationManager.ConnectionStrings[ConnectionStringName].ConnectionString;
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            // 確保 gvAdminData 和 ddlTables 已經被 designer.cs 宣告
            // 否則這裡就會報 CS0103 錯誤

            if (!IsPostBack)
            {
                // --- 1. 權限檢查 ---
                if (!User.Identity.IsAuthenticated)
                {
                    Response.Redirect("~/Login.aspx");
                    return;
                }

                if (!IsUserAdmin(User.Identity.Name))
                {
                    // 如果不是管理員，導向回個人首頁
                    ShowMessage("存取遭拒：您不具備管理員權限。", "error");
                    // 為了安全，直接導向
                    Response.Redirect("~/MyHomepage.aspx?AccessDenied=True");
                    return;
                }

                // --- 2. 初始化介面 ---
                InitializeTableDropdown();
                // 預設載入 Users 表格
                BindAdminData(ddlTables.SelectedValue);
            }
        }

        /// <summary>
        /// 檢查當前登入使用者是否為管理員。
        /// </summary>
        private bool IsUserAdmin(string username)
        {
            if (string.IsNullOrEmpty(username)) return false;

            string connString = GetConnectionString();
            // 確保查詢只針對 Username 欄位
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
                    // 記錄錯誤，但為了頁面功能保持運行，不拋出
                    System.Diagnostics.Debug.WriteLine($"管理員檢查錯誤: {ex.Message}");
                }
            }
            return false;
        }

        /// <summary>
        /// 根據使用者名稱 (Username) 獲取其 UserID。
        /// </summary>
        private object GetCurrentUserID(string username)
        {
            if (string.IsNullOrEmpty(username)) return null;

            string connString = GetConnectionString();
            // 查詢 UserID 欄位
            string sql = "SELECT UserID FROM Users WHERE Username = @Username";

            using (SQLiteConnection conn = new SQLiteConnection(connString))
            using (SQLiteCommand cmd = new SQLiteCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@Username", username);
                try
                {
                    conn.Open();
                    // 執行並返回 UserID
                    return cmd.ExecuteScalar();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"獲取 UserID 錯誤: {ex.Message}");
                    return null;
                }
            }
        }

        /// <summary>
        /// 初始化資料表下拉選單。
        /// </summary>
        private void InitializeTableDropdown()
        {
            ddlTables.Items.Clear();
            // Item.Value 應該只包含實際的表格名稱
            ddlTables.Items.Add(new ListItem("使用者帳號 (Users)", "Users"));
            ddlTables.Items.Add(new ListItem("書籍主檔 (Books)", "Books"));
            ddlTables.Items.Add(new ListItem("借閱記錄 (LendRecords)", "LendRecords"));
            ddlTables.Items.Add(new ListItem("書籍類別 (Categories)", "Categories"));
            // 預設選中 Users
            if (ddlTables.Items.FindByValue("Users") != null)
            {
                ddlTables.SelectedValue = "Users";
            }
        }

        /// <summary>
        /// 根據選擇的資料表名稱，動態綁定數據到 GridView。
        /// </summary>
        private void BindAdminData(string tableName)
        {
            if (string.IsNullOrEmpty(tableName)) return;

            string connString = GetConnectionString();
            // 確保只允許操作預期的表格
            if (!new[] { "Users", "Books", "LendRecords", "Categories" }.Contains(tableName))
            {
                ShowMessage("無效的資料表名稱。", "error");
                return;
            }

            // 【重要修正】: 即使表格為空，Footer Row 也需要結構，
            //             因此需要確保 DataTable dt 即使沒有數據，也有欄位結構。
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

                    // 【修正】: 必須在 DataBind 之前設定 DataKeyNames，否則 GridView 讀取數據時會使用錯誤的 Key
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
        }

        /// <summary>
        /// 動態設置 GridView 的 DataKeyNames 以支援 CRUD 操作。
        /// </summary>
        private void SetDataKeyNames(string tableName)
        {
            // 設置主鍵名稱 (根據提供的 Schema 資訊)
            switch (tableName)
            {
                case "Users": gvAdminData.DataKeyNames = new string[] { "UserID" }; break;
                case "Books": gvAdminData.DataKeyNames = new string[] { "BookID" }; break;
                case "LendRecords": gvAdminData.DataKeyNames = new string[] { "LendRecordID" }; break;
                case "Categories": gvAdminData.DataKeyNames = new string[] { "CategoryID" }; break;
                default: gvAdminData.DataKeyNames = new string[] { "DummyKey" }; break;
            }
        }

        /// <summary>
        /// 處理資料表選擇變更事件。
        /// </summary>
        protected void ddlTables_SelectedIndexChanged(object sender, EventArgs e)
        {
            BindAdminData(ddlTables.SelectedValue);
        }

        /// <summary>
        /// 處理分頁事件。
        /// </summary>
        protected void gvAdminData_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            gvAdminData.PageIndex = e.NewPageIndex;
            BindAdminData(ddlTables.SelectedValue);
        }

        /// <summary>
        /// 啟用行編輯模式。
        /// </summary>
        protected void gvAdminData_RowEditing(object sender, GridViewEditEventArgs e)
        {
            gvAdminData.EditIndex = e.NewEditIndex;
            BindAdminData(ddlTables.SelectedValue);
        }

        /// <summary>
        /// 取消編輯模式。
        /// </summary>
        protected void gvAdminData_RowCancelingEdit(object sender, GridViewCancelEditEventArgs e)
        {
            gvAdminData.EditIndex = -1;
            BindAdminData(ddlTables.SelectedValue);
        }

        /// <summary>
        /// 執行刪除操作。
        /// </summary>
        protected void gvAdminData_RowDeleting(object sender, GridViewDeleteEventArgs e)
        {
            string tableName = ddlTables.SelectedValue;
            if (gvAdminData.DataKeyNames.Length == 0)
            {
                ShowMessage("無法刪除：未找到主鍵資訊。", "error");
                return;
            }
            string keyName = gvAdminData.DataKeyNames[0];
            object keyValue = gvAdminData.DataKeys[e.RowIndex].Value; // 這是要刪除的記錄的主鍵值

            if (keyValue == null)
            {
                ShowMessage("刪除失敗：主鍵值為空。", "error");
                return;
            }

            // --- 【安全修正】防止管理員刪除自己的帳號 ---
            if (tableName.Equals("Users", StringComparison.OrdinalIgnoreCase) && keyName.Equals("UserID", StringComparison.OrdinalIgnoreCase))
            {
                // 1. 獲取當前登入使用者的 UserID (主鍵值)
                object currentUserIdObj = GetCurrentUserID(User.Identity.Name);

                // 2. 比較要刪除的 ID 和當前使用者的 ID。
                //    使用 ToString() 進行可靠比較，因為底層類型可能為 long/int/string。
                if (currentUserIdObj != null && keyValue.ToString() == currentUserIdObj.ToString())
                {
                    ShowMessage("安全警告：您不能在登入狀態下刪除自己的帳號！", "error");
                    // 阻止刪除操作
                    gvAdminData.EditIndex = -1;
                    BindAdminData(tableName);
                    return;
                }
            }
            // --- 【安全修正】結束 ---


            string connString = GetConnectionString();
            string deleteSql = $"DELETE FROM {tableName} WHERE {keyName} = @Key";

            using (SQLiteConnection conn = new SQLiteConnection(connString))
            using (SQLiteCommand cmd = new SQLiteCommand(deleteSql, conn))
            {
                // 由於主鍵可能有多種型別 (INTEGER/TEXT)，這裡使用 AddWithValue 讓 SQLite 驅動程式自行處理型別轉換
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

        /// <summary>
        /// 執行更新操作。
        /// </summary>
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
                // 這裡的 currentData 可能為 null，因此需要重新綁定
                DataTable currentData = (DataTable)gvAdminData.DataSource;
                // 注意：這裡在 RowUpdating 事件中，gvAdminData.DataSource 可能為空，
                // 最安全的方法是從數據庫重新讀取數據的結構。

                if (currentData == null || currentData.Rows.Count == 0)
                {
                    // 重新獲取數據結構
                    string selectQuery = $"SELECT * FROM {tableName} LIMIT 1"; // 只取一行以獲取結構
                    using (SQLiteConnection conn = new SQLiteConnection(connString))
                    using (SQLiteCommand cmd = new SQLiteCommand(selectQuery, conn))
                    {
                        conn.Open();
                        SQLiteDataAdapter da = new SQLiteDataAdapter(cmd);
                        currentData = new DataTable();
                        da.Fill(currentData);
                    }
                    if (currentData == null)
                    {
                        ShowMessage("更新失敗：無法重新獲取資料表結構。", "error");
                        gvAdminData.EditIndex = -1;
                        return;
                    }
                }

                // 獲取所有欄位名稱 (Column Names)
                string[] columnNames = currentData.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToArray();


                // 遍歷 GridView 行中的單元格
                // GridView 的 Columns 包含 CommandField (索引 0)
                for (int i = 0; i < gvAdminData.Columns.Count; i++)
                {
                    // CommandField (操作欄) 是 Columns[0]，因此我們從 Columns[1] 開始查找對應的資料欄位
                    if (i == 0) continue;

                    DataControlFieldCell cell = gvAdminData.Rows[e.RowIndex].Cells[i] as DataControlFieldCell;
                    if (cell != null && cell.Controls.Count > 0)
                    {
                        // 嘗試從 TextBox 中獲取值 (編輯模式下是 TextBox)
                        TextBox txt = cell.Controls.OfType<TextBox>().FirstOrDefault();
                        if (txt != null)
                        {
                            // 確保索引 i-1 對應到 columnNames 陣列 (跳過 CommandField)
                            if ((i - 1) < columnNames.Length)
                            {
                                string columnName = columnNames[i - 1];

                                // 忽略主鍵欄位本身，因為它是 WHERE 條件
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

                // 移除尾隨的 ", "
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

        /// <summary>
        /// 在 GridView Footer Row 中動態插入輸入框和「新增」按鈕。
        /// </summary>
        protected void gvAdminData_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.Footer)
            {
                string tableName = ddlTables.SelectedValue;

                // 1. 獲取表格結構以便知道要跳過哪個欄位 (主鍵)
                DataTable dtSchema = GetTableSchema(tableName);
                if (dtSchema == null) return;
                string primaryKeyName = GetPrimaryKeyName(tableName);

                // 2. 遍歷 Footer Row 中的單元格
                for (int i = 0; i < e.Row.Cells.Count; i++)
                {
                    TableCell cell = e.Row.Cells[i];

                    // CommandField 總是在索引 0
                    if (i == 0)
                    {
                        // 在 CommandField 的位置放置「新增」按鈕
                        Button btnInsert = new Button();
                        btnInsert.Text = "新增";
                        btnInsert.CommandName = "InsertNew";
                        btnInsert.CssClass = "btn-action btn-insert";
                        cell.Controls.Add(btnInsert);
                        cell.Style.Add("text-align", "center");
                    }
                    else
                    {
                        // 嘗試從 GridView.Columns 獲取欄位名稱。
                        // 由於 AutoGenerateColumns，我們必須使用 DataTable Schema 進行匹配
                        if ((i - 1) < dtSchema.Columns.Count)
                        {
                            string columnName = dtSchema.Columns[i - 1].ColumnName;

                            // 忽略主鍵欄位和自動生成的欄位 (例如：UserID, BookID)
                            if (columnName.Equals(primaryKeyName, StringComparison.OrdinalIgnoreCase))
                            {
                                cell.Text = "AUTO ID"; // 標示為自動生成
                                cell.Style.Add("color", "#6c757d");
                            }
                            else
                            {
                                // 為非主鍵欄位添加 TextBox
                                TextBox txtInsert = new TextBox();
                                txtInsert.ID = "txtInsert_" + columnName;
                                txtInsert.CssClass = "input-insert"; // 可選的 CSS 類別

                                // 為特定的欄位提供提示文字 (例如密碼, 日期)
                                if (columnName.Contains("Date"))
                                {
                                    txtInsert.ToolTip = "格式: YYYY-MM-DD (例如: 2024-01-01)";
                                }
                                else if (columnName.Equals("Password", StringComparison.OrdinalIgnoreCase))
                                {
                                    txtInsert.ToolTip = "請輸入密碼";
                                    txtInsert.TextMode = TextBoxMode.Password;
                                }
                                else if (columnName.Equals("IsAdmin", StringComparison.OrdinalIgnoreCase))
                                {
                                    txtInsert.ToolTip = "0=普通用戶, 1=管理員";
                                }

                                cell.Controls.Add(txtInsert);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 處理 GridView 的命令（包括新增紀錄）。
        /// </summary>
        protected void gvAdminData_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName == "InsertNew")
            {
                InsertNewRecord();
            }
        }

        /// <summary>
        /// 執行新增數據到資料庫的操作。
        /// </summary>
        private void InsertNewRecord()
        {
            string tableName = ddlTables.SelectedValue;
            string connString = GetConnectionString();
            DataTable dtSchema = GetTableSchema(tableName);
            if (dtSchema == null)
            {
                ShowMessage($"無法新增：無法獲取資料表 {tableName} 的結構。", "error");
                return;
            }

            string primaryKeyName = GetPrimaryKeyName(tableName);

            // 由於 AutoGenerateColumns 且我們手動創建了 Footer Row，我們需要手動獲取 TextBox 的值
            GridViewRow footerRow = gvAdminData.FooterRow;
            if (footerRow == null) return;

            StringBuilder columnNames = new StringBuilder();
            StringBuilder parameterNames = new StringBuilder();
            List<SQLiteParameter> parameters = new List<SQLiteParameter>();

            int columnIndex = 0;
            // GridView Columns 包含 CommandField
            foreach (DataControlField column in gvAdminData.Columns)
            {
                // 跳過 CommandField (索引 0)
                if (columnIndex == 0)
                {
                    columnIndex++;
                    continue;
                }

                // 使用 DataTable 的結構來確定欄位名稱
                if ((columnIndex - 1) < dtSchema.Columns.Count)
                {
                    string columnName = dtSchema.Columns[columnIndex - 1].ColumnName;

                    // 忽略主鍵欄位，SQLite 會自動處理 AUTOINCREMENT
                    if (columnName.Equals(primaryKeyName, StringComparison.OrdinalIgnoreCase))
                    {
                        columnIndex++;
                        continue;
                    }

                    // 從 Footer Row 找到對應的 TextBox
                    TextBox txtInsert = (TextBox)footerRow.FindControl("txtInsert_" + columnName);

                    if (txtInsert != null)
                    {
                        string paramName = $"@{columnName}";

                        columnNames.Append($"{columnName}, ");
                        parameterNames.Append($"{paramName}, ");
                        parameters.Add(new SQLiteParameter(paramName, txtInsert.Text.Trim()));
                    }
                }
                columnIndex++;
            }

            if (columnNames.Length == 0)
            {
                ShowMessage("新增失敗：請輸入至少一個有效的值。", "error");
                return;
            }

            // 移除尾隨的 ", "
            string cols = columnNames.ToString().TrimEnd(',', ' ');
            string vals = parameterNames.ToString().TrimEnd(',', ' ');
            string insertSql = $"INSERT INTO {tableName} ({cols}) VALUES ({vals})";

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
                // 處理 UNIQUE 約束錯誤等
                ShowMessage($"新增資料庫錯誤：{ex.Message}", "error");
            }
            catch (Exception ex)
            {
                ShowMessage($"新增錯誤：{ex.Message}", "error");
            }

            // 新增完成後重新綁定數據
            BindAdminData(tableName);
        }

        /// <summary>
        /// 獲取指定資料表的結構 (欄位名稱)。
        /// </summary>
        private DataTable GetTableSchema(string tableName)
        {
            string connString = GetConnectionString();
            // 使用 LIMIT 0 來獲取結構而不獲取數據
            string selectQuery = $"SELECT * FROM {tableName} LIMIT 0";

            try
            {
                using (SQLiteConnection conn = new SQLiteConnection(connString))
                using (SQLiteCommand cmd = new SQLiteCommand(selectQuery, conn))
                {
                    conn.Open();
                    SQLiteDataAdapter da = new SQLiteDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    return dt;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"獲取資料表結構錯誤 ({tableName}): {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 根據表格名稱獲取主鍵名稱。
        /// </summary>
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

        /// <summary>
        /// 顯示狀態訊息。
        /// </summary>
        private void ShowMessage(string message, string type)
        {
            lblMessage.Text = message;
            pnlMessage.Visible = true;

            // --- 根據傳統 ASP.NET CSS 類別更新 ---
            // 使用 CSS 類別而非 inline Tailwind 類別
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
                // 預設為警告/訊息
                pnlMessage.CssClass += " message-box-info";
            }
        }
    }
}