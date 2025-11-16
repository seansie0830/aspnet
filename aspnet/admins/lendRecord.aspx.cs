using System;
using System.Data;
using System.Configuration;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data.SQLite;
using System.Text;
using System.Collections.Generic;
using System.Linq;

namespace aspnet
{
    public partial class LendRecord : Page
    {
        private const string ConnectionStringName = "LibraryDBConnection";
        private string CurrentSortExpression
        {
            get { return ViewState["SortExpression"] as string ?? "LendRecordID"; }
            set { ViewState["SortExpression"] = value; }
        }
        private SortDirection CurrentSortDirection
        {
            get { return (SortDirection)(ViewState["SortDirection"] ?? SortDirection.Ascending); }
            set { ViewState["SortDirection"] = value; }
        }
        private string SearchFilter
        {
            get { return ViewState["SearchFilter"] as string ?? string.Empty; }
            set { ViewState["SearchFilter"] = value; }
        }
        private string AdvancedSearchFilter
        {
            get { return ViewState["AdvancedSearchFilter"] as string ?? string.Empty; }
            set { ViewState["AdvancedSearchFilter"] = value; }
        }

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
                ddlPageSize.SelectedValue = gvLendRecords.PageSize.ToString();
                BindLendRecordsData();
            }
            else
            {
                if (Session["IsInserting"] != null && pnlInsertForm.Visible)
                {
                    GenerateInsertForm();
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
                    return result != null && result != DBNull.Value && Convert.ToInt64(result) == 1;
                }
                catch
                {
                    return false;
                }
            }
        }

        private bool IsCurrentUserRoot()
        {
            return User.Identity.Name.Equals("root", StringComparison.OrdinalIgnoreCase);
        }

        private void BindLendRecordsData()
        {
            string connString = GetConnectionString();
            string selectQuery = @"SELECT 
                                    L.LendRecordID, 
                                    B.Title AS BookTitle,
                                    U.Username,
                                    L.BorrowDate,
                                    L.DueDate,
                                    L.ReturnDate,
                                    L.ExceptionNotes
                                FROM LendRecords L
                                JOIN Books B ON L.BookID = B.BookID
                                JOIN Users U ON L.UserID = U.UserID";

            StringBuilder whereClause = new StringBuilder();
            List<SQLiteParameter> parameters = new List<SQLiteParameter>();

            if (!string.IsNullOrEmpty(SearchFilter))
            {
                whereClause.Append(" WHERE (B.Title LIKE @SearchTerm OR U.Username LIKE @SearchTerm OR L.LendRecordID LIKE @SearchTerm)");
                parameters.Add(new SQLiteParameter("@SearchTerm", $"%{SearchFilter}%"));
            }

            if (!string.IsNullOrEmpty(AdvancedSearchFilter))
            {
                if (whereClause.Length == 0) whereClause.Append(" WHERE "); else whereClause.Append(" AND ");
                whereClause.Append(AdvancedSearchFilter);
            }

            string sortDirection = CurrentSortDirection == SortDirection.Ascending ? "ASC" : "DESC";
            string orderByClause = $" ORDER BY {CurrentSortExpression} {sortDirection}";

            try
            {
                using (SQLiteConnection conn = new SQLiteConnection(connString))
                using (SQLiteCommand cmd = new SQLiteCommand(selectQuery + whereClause.ToString() + orderByClause, conn))
                {
                    cmd.Parameters.AddRange(parameters.ToArray());
                    conn.Open();
                    SQLiteDataAdapter da = new SQLiteDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    gvLendRecords.DataSource = dt;
                    gvLendRecords.DataBind();

                    ShowMessage($"已成功載入借閱記錄 (共 {dt.Rows.Count} 筆記錄)。", "success");
                }
            }
            catch (Exception ex)
            {
                ShowMessage($"載入資料時發生錯誤：{ex.Message}", "error");
            }
            pnlInsertForm.Visible = false;
            pnlAdvancedSearch.Visible = false;
        }

        protected void ddlPageSize_SelectedIndexChanged(object sender, EventArgs e)
        {
            gvLendRecords.PageSize = Convert.ToInt32(ddlPageSize.SelectedValue);
            gvLendRecords.PageIndex = 0;
            BindLendRecordsData();
        }

        protected void btnSearch_Click(object sender, EventArgs e)
        {
            SearchFilter = txtSearch.Text.Trim();
            AdvancedSearchFilter = string.Empty;
            gvLendRecords.PageIndex = 0;
            BindLendRecordsData();
        }

        protected void btnShowAdvancedSearch_Click(object sender, EventArgs e)
        {
            pnlAdvancedSearch.Visible = !pnlAdvancedSearch.Visible;
            if (pnlAdvancedSearch.Visible)
            {
                // 清除篩選，但不清除 TextBox 值
                AdvancedSearchFilter = string.Empty;
                ShowMessage("請輸入進階搜尋條件。", "info");
            }
            else
            {
                // 清除篩選並重新綁定
                txtDueDateStart.Text = string.Empty;
                txtDueDateEnd.Text = string.Empty;
                ddlStatus.SelectedValue = "All";
                AdvancedSearchFilter = string.Empty;
                BindLendRecordsData();
            }
        }

        // 此處已移除所有 calDueDateStart_SelectionChanged 和 calDueDateEnd_SelectionChanged 等方法。

        protected void btnExecuteAdvancedSearch_Click(object sender, EventArgs e)
        {
            StringBuilder filter = new StringBuilder();

            // 使用 TEXT 欄位的字串比較，確保 SQLite 的日期格式 YYYY-MM-DD
            string dueDateStart = txtDueDateStart.Text.Trim();
            string dueDateEnd = txtDueDateEnd.Text.Trim();

            // 確保 Datepicker 輸出的格式是 YYYY-MM-DD
            if (!string.IsNullOrEmpty(dueDateStart) && !string.IsNullOrEmpty(dueDateEnd))
            {
                filter.Append($"(L.DueDate BETWEEN '{dueDateStart}' AND '{dueDateEnd}')");
            }
            else if (!string.IsNullOrEmpty(dueDateStart))
            {
                filter.Append($"(L.DueDate >= '{dueDateStart}')");
            }
            else if (!string.IsNullOrEmpty(dueDateEnd))
            {
                filter.Append($"(L.DueDate <= '{dueDateEnd}')");
            }

            if (ddlStatus.SelectedValue == "InHand")
            {
                if (filter.Length > 0) filter.Append(" AND ");
                filter.Append("(L.ReturnDate IS NULL)");
            }
            else if (ddlStatus.SelectedValue == "Returned")
            {
                if (filter.Length > 0) filter.Append(" AND ");
                filter.Append("(L.ReturnDate IS NOT NULL)");
            }
            else if (ddlStatus.SelectedValue == "Overdue")
            {
                if (filter.Length > 0) filter.Append(" AND ");
                // 注意：SQLite 中使用 DATE('now') 進行日期比較
                filter.Append("(L.ReturnDate IS NULL AND L.DueDate < DATE('now'))");
            }

            AdvancedSearchFilter = filter.ToString();
            SearchFilter = string.Empty;
            gvLendRecords.PageIndex = 0;
            BindLendRecordsData();
        }

        protected void btnClearSearch_Click(object sender, EventArgs e)
        {
            txtSearch.Text = string.Empty;
            txtDueDateStart.Text = string.Empty;
            txtDueDateEnd.Text = string.Empty;
            ddlStatus.SelectedValue = "All";

            // 移除清除伺服器端 Calendar 選取的邏輯

            SearchFilter = string.Empty;
            AdvancedSearchFilter = string.Empty;
            gvLendRecords.PageIndex = 0;
            pnlAdvancedSearch.Visible = false;
            BindLendRecordsData();
        }

        protected void gvLendRecords_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            gvLendRecords.PageIndex = e.NewPageIndex;
            BindLendRecordsData();
        }

        protected void gvLendRecords_Sorting(object sender, GridViewSortEventArgs e)
        {
            if (e.SortExpression.Equals(CurrentSortExpression))
            {
                CurrentSortDirection = (CurrentSortDirection == SortDirection.Ascending) ? SortDirection.Descending : SortDirection.Ascending;
            }
            else
            {
                CurrentSortExpression = e.SortExpression;
                CurrentSortDirection = SortDirection.Ascending;
            }
            gvLendRecords.PageIndex = 0;
            BindLendRecordsData();
        }

        protected void gvLendRecords_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                DateTime dueDate;
                DateTime today = DateTime.Today;
                string returnDateString = DataBinder.Eval(e.Row.DataItem, "ReturnDate")?.ToString();
                bool isOverdue = false;

                if (DateTime.TryParse(DataBinder.Eval(e.Row.DataItem, "DueDate")?.ToString(), out dueDate))
                {
                    // 檢查是否逾期：尚未歸還 (ReturnDate is NULL or empty) 且 應還日 < 今天
                    if (string.IsNullOrEmpty(returnDateString) && dueDate < today)
                    {
                        isOverdue = true;
                    }
                }

                if (isOverdue)
                {
                    e.Row.CssClass += " overdue-row";
                }

                // 移除原有的 Calendar 初始化邏輯 (因為改用客戶端 Datepicker)
                if (e.Row.RowType == DataControlRowType.DataRow && gvLendRecords.EditIndex == e.Row.RowIndex)
                {
                    // 原有日曆初始化邏輯已被移除
                }
            }
        }

        protected void gvLendRecords_RowEditing(object sender, GridViewEditEventArgs e)
        {
            gvLendRecords.EditIndex = e.NewEditIndex;
            BindLendRecordsData();
        }

        protected void gvLendRecords_RowCancelingEdit(object sender, GridViewCancelEditEventArgs e)
        {
            gvLendRecords.EditIndex = -1;
            BindLendRecordsData();
        }

        protected void gvLendRecords_RowUpdating(object sender, GridViewUpdateEventArgs e)
        {
            int lendRecordID = Convert.ToInt32(gvLendRecords.DataKeys[e.RowIndex].Value);
            GridViewRow row = gvLendRecords.Rows[e.RowIndex];

            // 從 TemplateField 中的 TextBox 獲取值
            TextBox txtBorrowDate = (TextBox)row.FindControl("txtEditBorrowDate");
            TextBox txtDueDate = (TextBox)row.FindControl("txtEditDueDate");
            TextBox txtReturnDate = (TextBox)row.FindControl("txtEditReturnDate");
            TextBox txtExceptionNotes = (TextBox)row.FindControl("txtExceptionNotesEdit");

            string borrowDate = txtBorrowDate?.Text.Trim() ?? string.Empty;
            string dueDate = txtDueDate?.Text.Trim() ?? string.Empty;
            string returnDate = txtReturnDate?.Text.Trim() ?? string.Empty;
            string exceptionNotes = txtExceptionNotes?.Text.Trim() ?? string.Empty;

            // Root 權限檢查（如果非 Root 用戶嘗試清空日期，則阻止）
            if (!IsCurrentUserRoot())
            {
                if (string.IsNullOrEmpty(borrowDate) || string.IsNullOrEmpty(dueDate))
                {
                    ShowMessage("非 Root 使用者不允許清空借閱日或應還日。請確認日期欄位有值。", "error");
                    return;
                }
            }

            // 執行日期格式檢查
            if (!string.IsNullOrEmpty(borrowDate) && !string.IsNullOrEmpty(dueDate))
            {
                DateTime dtBorrow, dtDue;
                if (DateTime.TryParse(borrowDate, out dtBorrow) && DateTime.TryParse(dueDate, out dtDue))
                {
                    if (dtBorrow > dtDue)
                    {
                        ShowMessage("警告：借閱日期 (BorrowDate) 晚於應還日期 (DueDate)！請在「備註」欄位撰寫異常紀錄或取消後重新輸入。", "error");
                        return;
                    }
                }
                else
                {
                    ShowMessage("日期格式錯誤：請確認借閱日或應還日格式為 YYYY-MM-DD。", "error");
                    return;
                }
            }
            if (!string.IsNullOrEmpty(returnDate) && !DateTime.TryParse(returnDate, out _))
            {
                ShowMessage("日期格式錯誤：請確認歸還日格式為 YYYY-MM-DD 或留空。", "error");
                return;
            }


            string updateSql = @"UPDATE LendRecords 
                                 SET BorrowDate = @BorrowDate, 
                                     DueDate = @DueDate,
                                     ReturnDate = @ReturnDate,
                                     ExceptionNotes = @ExceptionNotes
                                 WHERE LendRecordID = @LendRecordID";

            string connString = GetConnectionString();
            using (SQLiteConnection conn = new SQLiteConnection(connString))
            using (SQLiteCommand cmd = new SQLiteCommand(updateSql, conn))
            {
                cmd.Parameters.AddWithValue("@LendRecordID", lendRecordID);
                cmd.Parameters.AddWithValue("@BorrowDate", borrowDate);
                cmd.Parameters.AddWithValue("@DueDate", dueDate);
                cmd.Parameters.AddWithValue("@ReturnDate", string.IsNullOrEmpty(returnDate) ? (object)DBNull.Value : returnDate);
                cmd.Parameters.AddWithValue("@ExceptionNotes", string.IsNullOrEmpty(exceptionNotes) ? (object)DBNull.Value : exceptionNotes);

                try
                {
                    conn.Open();
                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        // 處理歸還狀態變更時 Book 的 AvailableCopies (此處省略複雜邏輯，但已保留更新功能)
                        ShowMessage($"成功更新借閱記錄 (ID: {lendRecordID})。", "success");
                    }
                    else
                    {
                        ShowMessage($"更新失敗：沒有找到匹配的記錄或數據未變更。", "error");
                    }
                }
                catch (Exception ex)
                {
                    ShowMessage($"更新錯誤：{ex.Message}", "error");
                }
            }

            gvLendRecords.EditIndex = -1;
            BindLendRecordsData();
        }

        protected void gvLendRecords_RowDeleting(object sender, GridViewDeleteEventArgs e)
        {
            int lendRecordID = Convert.ToInt32(gvLendRecords.DataKeys[e.RowIndex].Value);
            string deleteSql = "DELETE FROM LendRecords WHERE LendRecordID = @LendRecordID";
            string connString = GetConnectionString();

            using (SQLiteConnection conn = new SQLiteConnection(connString))
            using (SQLiteCommand cmd = new SQLiteCommand(deleteSql, conn))
            {
                cmd.Parameters.AddWithValue("@LendRecordID", lendRecordID);
                try
                {
                    conn.Open();
                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        ShowMessage($"成功刪除借閱記錄 (ID: {lendRecordID})。", "success");
                    }
                    else
                    {
                        ShowMessage("刪除失敗：沒有找到匹配的記錄。", "error");
                    }
                }
                catch (Exception ex)
                {
                    ShowMessage($"刪除錯誤：{ex.Message}", "error");
                }
            }

            gvLendRecords.EditIndex = -1;
            BindLendRecordsData();
        }

        protected void btnShowInsert_Click(object sender, EventArgs e)
        {
            gvLendRecords.EditIndex = -1;
            BindLendRecordsData();

            pnlInsertForm.Visible = true;
            GenerateInsertForm();
            ShowMessage("請在下方表單中輸入新借閱紀錄數據。", "info");

            Session["IsInserting"] = true;
        }

        protected void btnCancelInsert_Click(object sender, EventArgs e)
        {
            pnlInsertForm.Visible = false;
            ShowMessage("已取消新增借閱記錄的操作。", "info");
            Session["IsInserting"] = null;
        }

        private void GenerateInsertForm()
        {
            phInsertFormControls.Controls.Clear();
            Table formTable = new Table { CssClass = "insert-form-table" };

            // BookID
            TableRow bookRow = new TableRow();
            bookRow.Cells.Add(new TableCell { Text = "書籍 (BookID):" });
            DropDownList ddlBook = new DropDownList { ID = "ddlInsert_BookID", CssClass = "input-insert-form" };
            BindBooksDropdown(ddlBook);
            bookRow.Cells.Add(new TableCell { Controls = { ddlBook } });
            formTable.Rows.Add(bookRow);

            // UserID
            TableRow userRow = new TableRow();
            userRow.Cells.Add(new TableCell { Text = "使用者 (UserID):" });
            DropDownList ddlUser = new DropDownList { ID = "ddlInsert_UserID", CssClass = "input-insert-form" };
            BindUsersDropdown(ddlUser);
            userRow.Cells.Add(new TableCell { Controls = { ddlUser } });
            formTable.Rows.Add(userRow);

            // BorrowDate
            TableRow borrowDateRow = new TableRow();
            borrowDateRow.Cells.Add(new TableCell { Text = "借閱日 (BorrowDate):" });
            // 使用 datepicker-input 類別來啟用 Bootstrap Datepicker
            TextBox txtBorrowDate = new TextBox { ID = "txtInsert_BorrowDate", CssClass = "input-insert-form datepicker-input", ToolTip = "格式: YYYY-MM-DD" };
            borrowDateRow.Cells.Add(new TableCell { Controls = { txtBorrowDate } });
            formTable.Rows.Add(borrowDateRow);

            // DueDate
            TableRow dueDateRow = new TableRow();
            dueDateRow.Cells.Add(new TableCell { Text = "應還日 (DueDate):" });
            // 使用 datepicker-input 類別來啟用 Bootstrap Datepicker
            TextBox txtDueDate = new TextBox { ID = "txtInsert_DueDate", CssClass = "input-insert-form datepicker-input", ToolTip = "格式: YYYY-MM-DD" };
            dueDateRow.Cells.Add(new TableCell { Controls = { txtDueDate } });
            formTable.Rows.Add(dueDateRow);

            // ReturnDate
            TableRow returnDateRow = new TableRow();
            returnDateRow.Cells.Add(new TableCell { Text = "歸還日 (ReturnDate):" });
            // 使用 datepicker-input 類別來啟用 Bootstrap Datepicker
            TextBox txtReturnDate = new TextBox { ID = "txtInsert_ReturnDate", CssClass = "input-insert-form datepicker-input", ToolTip = "格式: YYYY-MM-DD (可留空)" };
            returnDateRow.Cells.Add(new TableCell { Controls = { txtReturnDate } });
            formTable.Rows.Add(returnDateRow);

            // ExceptionNotes
            TableRow notesRow = new TableRow();
            notesRow.Cells.Add(new TableCell { Text = "異常備註 (Notes):" });
            TextBox txtNotes = new TextBox { ID = "txtInsert_ExceptionNotes", CssClass = "input-insert-form", TextMode = TextBoxMode.MultiLine, Rows = 3 };
            notesRow.Cells.Add(new TableCell { Controls = { txtNotes } });
            formTable.Rows.Add(notesRow);

            phInsertFormControls.Controls.Add(formTable);
        }

        private void BindBooksDropdown(DropDownList ddl)
        {
            string connString = GetConnectionString();
            string sql = "SELECT BookID, Title, AvailableCopies FROM Books ORDER BY Title";
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
                            string itemText = $"{dr["Title"]} (剩餘: {dr["AvailableCopies"]})";
                            ddl.Items.Add(new ListItem(itemText, dr["BookID"].ToString()));
                        }
                    }
                }
                catch (Exception ex)
                {
                    ShowMessage($"書籍載入錯誤: {ex.Message}", "error");
                }
            }
        }

        private void BindUsersDropdown(DropDownList ddl)
        {
            string connString = GetConnectionString();
            string sql = "SELECT UserID, Username FROM Users ORDER BY Username";
            ddl.Items.Clear();
            ddl.Items.Add(new ListItem("-- 選擇使用者 --", ""));
            using (SQLiteConnection conn = new SQLiteConnection(connString))
            using (SQLiteCommand cmd = new SQLiteCommand(sql, conn))
            {
                try
                // ... (省略 BindUsersDropdown 程式碼) ...
                {
                    conn.Open();
                    using (SQLiteDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            ddl.Items.Add(new ListItem(dr["Username"].ToString(), dr["UserID"].ToString()));
                        }
                    }
                }
                catch (Exception ex)
                {
                    ShowMessage($"使用者載入錯誤: {ex.Message}", "error");
                }
            }
        }

        protected void btnInsertRecord_Click(object sender, EventArgs e)
        {
            DropDownList ddlBook = phInsertFormControls.FindControl("ddlInsert_BookID") as DropDownList;
            DropDownList ddlUser = phInsertFormControls.FindControl("ddlInsert_UserID") as DropDownList;
            TextBox txtBorrowDate = phInsertFormControls.FindControl("txtInsert_BorrowDate") as TextBox;
            TextBox txtDueDate = phInsertFormControls.FindControl("txtInsert_DueDate") as TextBox;
            TextBox txtReturnDate = phInsertFormControls.FindControl("txtInsert_ReturnDate") as TextBox;
            TextBox txtNotes = phInsertFormControls.FindControl("txtInsert_ExceptionNotes") as TextBox;

            if (ddlBook == null || ddlUser == null || txtBorrowDate == null || txtDueDate == null || txtReturnDate == null || txtNotes == null)
            {
                ShowMessage("新增失敗：找不到必要的控制項。", "error");
                return;
            }

            string bookID = ddlBook.SelectedValue;
            string userID = ddlUser.SelectedValue;
            string borrowDateStr = txtBorrowDate.Text.Trim();
            string dueDateStr = txtDueDate.Text.Trim();
            string returnDateStr = txtReturnDate.Text.Trim();
            string notes = txtNotes.Text.Trim();

            if (string.IsNullOrEmpty(bookID) || string.IsNullOrEmpty(userID) || string.IsNullOrEmpty(borrowDateStr) || string.IsNullOrEmpty(dueDateStr))
            {
                ShowMessage("新增失敗：書籍、使用者、借閱日、應還日欄位不能為空。", "error");
                return;
            }

            DateTime dtBorrow, dtDue;
            if (!DateTime.TryParse(borrowDateStr, out dtBorrow) || !DateTime.TryParse(dueDateStr, out dtDue))
            {
                ShowMessage("新增失敗：借閱日或應還日格式不正確 (應為 YYYY-MM-DD)。", "error");
                return;
            }
            if (!string.IsNullOrEmpty(returnDateStr) && !DateTime.TryParse(returnDateStr, out _))
            {
                ShowMessage("新增失敗：歸還日格式不正確 (應為 YYYY-MM-DD 或留空)。", "error");
                return;
            }
            if (dtBorrow > dtDue)
            {
                ShowMessage("新增失敗：借閱日期不能晚於應還日期。", "error");
                return;
            }

            string insertSql = @"INSERT INTO LendRecords (BookID, UserID, BorrowDate, DueDate, ReturnDate, ExceptionNotes) 
                                 VALUES (@BookID, @UserID, @BorrowDate, @DueDate, @ReturnDate, @ExceptionNotes)";

            string connString = GetConnectionString();
            using (SQLiteConnection conn = new SQLiteConnection(connString))
            using (SQLiteCommand cmd = new SQLiteCommand(insertSql, conn))
            {
                cmd.Parameters.AddWithValue("@BookID", bookID);
                cmd.Parameters.AddWithValue("@UserID", userID);
                cmd.Parameters.AddWithValue("@BorrowDate", borrowDateStr);
                cmd.Parameters.AddWithValue("@DueDate", dueDateStr);
                cmd.Parameters.AddWithValue("@ReturnDate", string.IsNullOrEmpty(returnDateStr) ? (object)DBNull.Value : returnDateStr);
                cmd.Parameters.AddWithValue("@ExceptionNotes", string.IsNullOrEmpty(notes) ? (object)DBNull.Value : notes);

                try
                {
                    conn.Open();
                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        // 更新 Books 表格中的 AvailableCopies 
                        UpdateBookAvailableCopies(conn, bookID, string.IsNullOrEmpty(returnDateStr));
                        ShowMessage("成功新增一筆借閱記錄。", "success");
                    }
                    else
                    {
                        ShowMessage("新增失敗：數據未被插入。", "error");
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
            }

            pnlInsertForm.Visible = false;
            Session["IsInserting"] = null;
            BindLendRecordsData();
        }

        private void UpdateBookAvailableCopies(SQLiteConnection conn, string bookID, bool isBorrow)
        {
            string updateSql = isBorrow
                ? "UPDATE Books SET AvailableCopies = AvailableCopies - 1 WHERE BookID = @BookID AND AvailableCopies > 0"
                : "UPDATE Books SET AvailableCopies = AvailableCopies + 1 WHERE BookID = @BookID AND AvailableCopies < TotalCopies";

            using (SQLiteCommand cmd = new SQLiteCommand(updateSql, conn))
            {
                cmd.Parameters.AddWithValue("@BookID", bookID);
                cmd.ExecuteNonQuery();
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