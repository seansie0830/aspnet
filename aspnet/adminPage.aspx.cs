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
                if (Session["IsInserting"] != null && pnlInsertForm.Visible)
                {
                    GenerateInsertForm(ddlTables.SelectedValue);
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
            ddlTables.Items.Add(new ListItem("書籍類別關聯 (CategoryRecords)", "CategoryRecords"));
            if (ddlTables.Items.FindByValue("Users") != null)
            {
                ddlTables.SelectedValue = "Users";
            }
        }

        private void BindAdminData(string tableName)
        {
            if (string.IsNullOrEmpty(tableName)) return;

            string connString = GetConnectionString();
            if (!new[] { "Users", "Books", "LendRecords", "Categories", "CategoryRecords" }.Contains(tableName))
            {
                ShowMessage("無效的資料表名稱。", "error");
                return;
            }

            string selectQuery = string.Empty;
            if (tableName == "CategoryRecords")
            {
                selectQuery = @"SELECT 
                                b.BookID, 
                                b.Title AS BookTitle,
                                GROUP_CONCAT(c.CategoryName, ', ') AS CategoriesList
                                FROM Books b
                                LEFT JOIN CategoryRecords bcr ON b.BookID = bcr.BookID
                                LEFT JOIN Categories c ON bcr.CategoryID = c.CategoryID
                                GROUP BY b.BookID, b.Title
                                ORDER BY b.BookID";
            }
            else
            {
                selectQuery = $"SELECT * FROM {tableName}";
            }

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

                    if (tableName == "Categories")
                    {
                        gvAdminData.Columns.Clear();
                        BoundField idField = new BoundField { DataField = "CategoryID", HeaderText = "CategoryID", ReadOnly = true };
                        gvAdminData.Columns.Add(idField);

                        BoundField nameField = new BoundField { DataField = "CategoryName", HeaderText = "CategoryName" };
                        gvAdminData.Columns.Add(nameField);

                        TemplateField colorField = new TemplateField { HeaderText = "colorHex" };
                        colorField.ItemTemplate = new colorHexItemTemplate();
                        colorField.EditItemTemplate = new colorHexEditItemTemplate();
                        gvAdminData.Columns.Add(colorField);

                        CommandField editField = new CommandField { ShowEditButton = true, EditText = "編輯", UpdateText = "更新", CancelText = "取消" };
                        gvAdminData.Columns.Add(editField);

                        CommandField deleteField = new CommandField { ShowDeleteButton = true, DeleteText = "刪除" };
                        gvAdminData.Columns.Add(deleteField);

                        gvAdminData.AutoGenerateColumns = false;
                    }
                    else if (tableName == "CategoryRecords")
                    {
                        gvAdminData.Columns.Clear();
                        gvAdminData.AutoGenerateColumns = true;
                    }
                    else
                    {
                        gvAdminData.AutoGenerateColumns = true;
                        gvAdminData.Columns.Clear();
                    }


                    gvAdminData.DataSource = dt;
                    gvAdminData.DataBind();

                    ShowMessage($"已成功載入資料表：{ddlTables.SelectedItem.Text} (共 {dt.Rows.Count} 筆記錄)。", "success");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"資料綁定錯誤 ({tableName}): {ex.Message}");
                ShowMessage($"載入資料時發生錯誤 ({tableName})：{ex.Message}", "error");
            }
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
                case "CategoryRecords": gvAdminData.DataKeyNames = new string[] { "BookID" }; break;
                default: gvAdminData.DataKeyNames = new string[] { "DummyKey" }; break;
            }
        }

        protected void ddlTables_SelectedIndexChanged(object sender, EventArgs e)
        {
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
            if (ddlTables.SelectedValue == "CategoryRecords")
            {
                e.Cancel = true;
                ShowMessage("CategoryRecords 應透過新增功能進行調整，不開放直接編輯 GridView。", "info");
                return;
            }

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

            string deleteSql = string.Empty;
            if (tableName == "CategoryRecords")
            {
                deleteSql = "DELETE FROM CategoryRecords WHERE BookID = @Key";
                keyName = "BookID";
            }
            else
            {
                deleteSql = $"DELETE FROM {tableName} WHERE {keyName} = @Key";
            }

            string connString = GetConnectionString();

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
            if (tableName == "CategoryRecords")
            {
                ShowMessage("CategoryRecords 應透過新增功能進行調整。", "error");
                gvAdminData.EditIndex = -1;
                BindAdminData(tableName);
                return;
            }

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

                if (tableName == "Categories")
                {
                    string categoryName = (gvAdminData.Rows[e.RowIndex].Cells[1].Controls[0] as TextBox)?.Text.Trim();
                    string colorHex = (gvAdminData.Rows[e.RowIndex].Cells[2].FindControl("txtcolorHexEdit") as TextBox)?.Text.Trim();

                    if (!string.IsNullOrEmpty(categoryName))
                    {
                        setClauses.Append("CategoryName = @CategoryName, ");
                        parameters.Add(new SQLiteParameter("@CategoryName", categoryName));
                    }

                    if (!string.IsNullOrEmpty(colorHex))
                    {
                        setClauses.Append("colorHex = @colorHex, ");
                        parameters.Add(new SQLiteParameter("@colorHex", colorHex));
                    }
                }
                else
                {
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
            if (e.Row.RowType == DataControlRowType.DataRow && ddlTables.SelectedValue == "Categories")
            {
                DataRowView drv = e.Row.DataItem as DataRowView;
                if (drv != null)
                {
                    if (e.Row.RowState == DataControlRowState.Normal || e.Row.RowState == DataControlRowState.Alternate)
                    {
                        string colorHex = drv["colorHex"].ToString();
                        TableCell colorCell = e.Row.Cells[2];
                        colorCell.Controls.Clear();
                        if (!string.IsNullOrEmpty(colorHex))
                        {
                            Label colorLabel = new Label { Text = colorHex };
                            colorLabel.Style.Add("background-color", colorHex);
                            colorLabel.Style.Add("color", IsColorDark(colorHex) ? "white" : "black");
                            colorLabel.Style.Add("padding", "2px 5px");
                            colorLabel.Style.Add("border-radius", "3px");
                            colorLabel.Style.Add("display", "inline-block");
                            colorCell.Controls.Add(colorLabel);
                        }
                    }
                }
            }
            if (e.Row.RowType == DataControlRowType.Footer)
            {
                e.Row.Visible = false;
            }
        }

        private bool IsColorDark(string hex)
        {
            if (string.IsNullOrEmpty(hex) || !hex.StartsWith("#") || hex.Length < 4) return false;
            try
            {
                string rHex = hex.Length == 4 ? hex.Substring(1, 1) + hex.Substring(1, 1) : hex.Substring(1, 2);
                string gHex = hex.Length == 4 ? hex.Substring(2, 1) + hex.Substring(2, 1) : hex.Substring(3, 2);
                string bHex = hex.Length == 4 ? hex.Substring(3, 1) + hex.Substring(3, 1) : hex.Substring(5, 2);

                int r = int.Parse(rHex, System.Globalization.NumberStyles.HexNumber);
                int g = int.Parse(gHex, System.Globalization.NumberStyles.HexNumber);
                int b = int.Parse(bHex, System.Globalization.NumberStyles.HexNumber);

                double brightness = (r * 299 + g * 587 + b * 114) / 1000;
                return brightness < 128;
            }
            catch
            {
                return false;
            }
        }

        protected void btnShowInsert_Click(object sender, EventArgs e)
        {
            gvAdminData.EditIndex = -1;
            BindAdminData(ddlTables.SelectedValue);

            pnlInsertForm.Visible = true;
            GenerateInsertForm(ddlTables.SelectedValue);
            ShowMessage("請在下方表單中輸入新紀錄數據。", "info");

            Session["IsInserting"] = true;
        }

        protected void btnCancelInsert_Click(object sender, EventArgs e)
        {
            pnlInsertForm.Visible = false;
            ShowMessage($"已取消 {ddlTables.SelectedValue} 表格的新增操作。", "info");

            Session["IsInserting"] = null;
        }

        private void GenerateInsertForm(string tableName)
        {
            phInsertFormControls.Controls.Clear();

            DataTable dtSchema = GetTableSchema(tableName);
            if (dtSchema == null) return;
            string primaryKeyName = GetPrimaryKeyName(tableName);

            Table formTable = new Table { CssClass = "insert-form-table" };

            TableHeaderRow headerRow = new TableHeaderRow();
            TableHeaderCell headerCell = new TableHeaderCell { Text = $"新增至表格：**{ddlTables.SelectedItem.Text}**", ColumnSpan = 2, CssClass = "insert-form-header" };
            headerRow.Cells.Add(headerCell);
            formTable.Rows.Add(headerRow);

            if (tableName == "CategoryRecords")
            {
                AddBookCategoryRecordFormControls(formTable);
            }
            else
            {
                foreach (DataColumn column in dtSchema.Columns)
                {
                    if (column.ColumnName.Equals(primaryKeyName, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    TableRow row = new TableRow();
                    TableCell labelCell = new TableCell();
                    Label lbl = new Label { Text = column.ColumnName + ":" };
                    labelCell.Controls.Add(lbl);
                    row.Cells.Add(labelCell);

                    TableCell inputCell = new TableCell();

                    if (tableName == "Categories" && column.ColumnName.Equals("colorHex", StringComparison.OrdinalIgnoreCase))
                    {
                        TextBox txtColor = new TextBox();
                        txtColor.ID = "txtInsert_" + column.ColumnName;
                        txtColor.CssClass = "input-insert-form color-picker-input";
                        txtColor.Width = Unit.Percentage(90);
                        txtColor.TextMode = TextBoxMode.Color;
                        txtColor.Text = "#cccccc";
                        inputCell.Controls.Add(txtColor);
                    }
                    else
                    {
                        TextBox txtInsert = new TextBox();
                        txtInsert.ID = "txtInsert_" + column.ColumnName;
                        txtInsert.CssClass = "input-insert-form";
                        txtInsert.Width = Unit.Percentage(90);

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
                    }

                    row.Cells.Add(inputCell);
                    formTable.Rows.Add(row);
                }
            }

            phInsertFormControls.Controls.Add(formTable);
        }

        private void AddBookCategoryRecordFormControls(Table formTable)
        {
            TableRow bookRow = new TableRow();
            bookRow.Cells.Add(new TableCell { Text = "BookID/Title:" });
            DropDownList ddlBook = new DropDownList { ID = "ddlInsert_BookID", CssClass = "input-insert-form" };
            BindBooksDropdown(ddlBook);
            bookRow.Cells.Add(new TableCell { Controls = { ddlBook } });
            formTable.Rows.Add(bookRow);

            TableRow categoryRow = new TableRow();
            categoryRow.Cells.Add(new TableCell { Text = "Categories (多選):" });

            Panel categoryPanel = new Panel();
            categoryPanel.CssClass = "category-selector-container";

            ListBox lbCategory = new ListBox
            {
                ID = "lbInsert_CategoryID",
                SelectionMode = ListSelectionMode.Multiple,
                Rows = 5,
                CssClass = "input-insert-form category-multiselect"
            };
            BindCategoriesListBox(lbCategory);

            categoryPanel.Controls.Add(lbCategory);

            if (lbCategory.Items.Count == 0)
            {
                categoryPanel.Controls.Add(new LiteralControl("<span style='color: red; font-weight: bold;'>目前沒有類別！請先到 Categories 表格新增類別。</span>"));
                btnInsertRecord.Enabled = false;
            }
            else
            {
                btnInsertRecord.Enabled = true;
            }

            categoryRow.Cells.Add(new TableCell { Controls = { categoryPanel } });
            formTable.Rows.Add(categoryRow);
        }

        private void BindBooksDropdown(DropDownList ddl)
        {
            string connString = GetConnectionString();
            string sql = "SELECT BookID, Title FROM Books ORDER BY Title";
            ddl.Items.Clear();
            ddl.Items.Add(new ListItem("-- 選擇書籍 --", ""));

            using (SQLiteConnection conn = new SQLiteConnection(connString))
            using (SQLiteCommand cmd = new SQLiteCommand(sql, conn))
            {
                try
                {
                    conn.Open();
                    using (SQLiteDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            ddl.Items.Add(new ListItem(dr["Title"].ToString(), dr["BookID"].ToString()));
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"書籍載入錯誤: {ex.Message}");
                }
            }
        }

        private void BindCategoriesListBox(ListBox lb)
        {
            string connString = GetConnectionString();
            string sql = "SELECT CategoryID, CategoryName FROM Categories ORDER BY CategoryName";
            lb.Items.Clear();

            using (SQLiteConnection conn = new SQLiteConnection(connString))
            using (SQLiteCommand cmd = new SQLiteCommand(sql, conn))
            {
                try
                {
                    conn.Open();
                    using (SQLiteDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            lb.Items.Add(new ListItem(dr["CategoryName"].ToString(), dr["CategoryID"].ToString()));
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"類別載入錯誤: {ex.Message}");
                }
            }
        }

        protected void btnInsertRecord_Click(object sender, EventArgs e)
        {
            string tableName = ddlTables.SelectedValue;
            string connString = GetConnectionString();

            if (tableName == "CategoryRecords")
            {
                InsertBookCategoryRecord(connString);
                return;
            }

            DataTable dtSchema = GetTableSchema(tableName);
            if (dtSchema == null)
            {
                ShowMessage($"無法新增：無法獲取資料表 {tableName} 的結構。", "error");
                return;
            }

            string primaryKeyName = GetPrimaryKeyName(tableName);

            StringBuilder columnNames = new StringBuilder();
            StringBuilder parameterNames = new StringBuilder();
            List<SQLiteParameter> parameters = new List<SQLiteParameter>();

            foreach (DataColumn column in dtSchema.Columns)
            {
                if (column.ColumnName.Equals(primaryKeyName, StringComparison.OrdinalIgnoreCase)) continue;

                string expectedControlID = "txtInsert_" + column.ColumnName;

                TextBox txtInsert = (TextBox)phInsertFormControls.FindControl(expectedControlID);

                if (txtInsert != null)
                {
                    string paramName = $"@{column.ColumnName}";

                    columnNames.Append($"{column.ColumnName}, ");
                    parameterNames.Append($"{paramName}, ");

                    string inputValue = txtInsert.Text.Trim();

                    if (tableName.Equals("Users", StringComparison.OrdinalIgnoreCase) && column.ColumnName.Equals("Password", StringComparison.OrdinalIgnoreCase))
                    {
                        if (string.IsNullOrEmpty(inputValue))
                        {
                            ShowMessage("新增失敗：密碼欄位不能為空。", "error");
                            return;
                        }
                        inputValue = FormsAuthentication.HashPasswordForStoringInConfigFile(inputValue, "SHA1");
                    }

                    if (tableName.Equals("Categories", StringComparison.OrdinalIgnoreCase) && column.ColumnName.Equals("colorHex", StringComparison.OrdinalIgnoreCase))
                    {
                        if (string.IsNullOrEmpty(inputValue))
                        {
                            inputValue = "#000000";
                        }
                    }

                    parameters.Add(new SQLiteParameter(paramName, inputValue));
                }
            }

            if (columnNames.Length == 0)
            {
                ShowMessage("新增失敗：請輸入至少一個有效的值。", "error");
                return;
            }

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
                ShowMessage($"新增資料庫錯誤：{ex.Message}", "error");
            }
            catch (Exception ex)
            {
                ShowMessage($"新增錯誤：{ex.Message}", "error");
            }

            pnlInsertForm.Visible = false;
            Session["IsInserting"] = null;
            BindAdminData(tableName);
        }

        private void InsertBookCategoryRecord(string connString)
        {
            DropDownList ddlBook = phInsertFormControls.FindControl("ddlInsert_BookID") as DropDownList;
            ListBox lbCategory = phInsertFormControls.FindControl("lbInsert_CategoryID") as ListBox;

            if (ddlBook == null || lbCategory == null)
            {
                ShowMessage("新增失敗：找不到必要的控制項。", "error");
                return;
            }

            string bookID = ddlBook.SelectedValue;
            if (string.IsNullOrEmpty(bookID))
            {
                ShowMessage("新增失敗：請選擇一本圖書。", "error");
                return;
            }

            List<string> selectedCategoryIDs = lbCategory.Items.Cast<ListItem>()
                                                      .Where(li => li.Selected)
                                                      .Select(li => li.Value)
                                                      .ToList();

            try
            {
                using (SQLiteConnection conn = new SQLiteConnection(connString))
                {
                    conn.Open();

                    string deleteSql = "DELETE FROM CategoryRecords WHERE BookID = @BookID";
                    using (SQLiteCommand deleteCmd = new SQLiteCommand(deleteSql, conn))
                    {
                        deleteCmd.Parameters.AddWithValue("@BookID", bookID);
                        deleteCmd.ExecuteNonQuery();
                    }

                    if (selectedCategoryIDs.Any())
                    {
                        string insertSql = "INSERT INTO CategoryRecords (BookID, CategoryID) VALUES (@BookID, @CategoryID)";
                        using (SQLiteCommand insertCmd = new SQLiteCommand(insertSql, conn))
                        {
                            insertCmd.Parameters.AddWithValue("@BookID", bookID);

                            foreach (string categoryID in selectedCategoryIDs)
                            {
                                insertCmd.Parameters.AddWithValue("@CategoryID", categoryID);
                                insertCmd.ExecuteNonQuery();
                            }
                        }
                        ShowMessage($"成功更新書籍 ID {bookID} 的類別關聯 (共 {selectedCategoryIDs.Count} 個類別)。", "success");
                    }
                    else
                    {
                        ShowMessage($"成功清除書籍 ID {bookID} 的所有類別關聯。", "success");
                    }
                }
            }
            catch (SQLiteException ex)
            {
                ShowMessage($"新增/更新關聯資料庫錯誤：{ex.Message}", "error");
            }
            catch (Exception ex)
            {
                ShowMessage($"新增/更新關聯錯誤：{ex.Message}", "error");
            }

            pnlInsertForm.Visible = false;
            Session["IsInserting"] = null;
            BindAdminData(ddlTables.SelectedValue);
        }

        private DataTable GetTableSchema(string tableName)
        {
            if (tableName == "CategoryRecords")
            {
                DataTable dt = new DataTable();
                dt.Columns.Add("BookID");
                dt.Columns.Add("BookTitle");
                dt.Columns.Add("CategoriesList");
                return dt;
            }

            string connString = GetConnectionString();
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

        private string GetPrimaryKeyName(string tableName)
        {
            switch (tableName)
            {
                case "Users": return "UserID";
                case "Books": return "BookID";
                case "LendRecords": return "LendRecordID";
                case "Categories": return "CategoryID";
                case "CategoryRecords": return "BookID";
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

        private class colorHexItemTemplate : ITemplate
        {
            public void InstantiateIn(Control container)
            {
                Label lbl = new Label();
                lbl.DataBinding += (s, e) =>
                {
                    Label senderLabel = (Label)s;
                    GridViewRow row = (GridViewRow)senderLabel.NamingContainer;
                    string colorHex = DataBinder.Eval(row.DataItem, "ColorHex")?.ToString();
                    senderLabel.Text = colorHex;
                };
                container.Controls.Add(lbl);
            }
        }

        private class colorHexEditItemTemplate : ITemplate
        {
            public void InstantiateIn(Control container)
            {
                TextBox txt = new TextBox { ID = "txtcolorHexEdit", TextMode = TextBoxMode.Color, Width = new Unit(80, UnitType.Pixel) };
                txt.DataBinding += (s, e) =>
                {
                    TextBox senderTextBox = (TextBox)s;
                    GridViewRow row = (GridViewRow)senderTextBox.NamingContainer;
                    senderTextBox.Text = DataBinder.Eval(row.DataItem, "colorHex")?.ToString();
                };
                container.Controls.Add(txt);
            }
        }
    }
}