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
    public partial class books : Page
    {
        // 資料庫連線字串名稱
        private const string ConnectionStringName = "LibraryDBConnection";
        private string GetConnectionString()
        {
            // 由於原始碼未提供 ConfigurationManager，這裡假設它是可用的
            return ConfigurationManager.ConnectionStrings[ConnectionStringName].ConnectionString;
        }

        // 頁面屬性用於儲存排序狀態和搜尋條件
        private string CurrentSortExpression
        {
            get { return ViewState["SortExpression"] as string ?? "BookID"; }
            set { ViewState["SortExpression"] = value; }
        }

        private SortDirection CurrentSortDirection
        {
            get { return (SortDirection)(ViewState["SortDirection"] ?? SortDirection.Ascending); }
            set { ViewState["SortDirection"] = value; }
        }

        private string SearchKeyword
        {
            get { return Session["Books_SearchKeyword"] as string ?? string.Empty; }
            set { Session["Books_SearchKeyword"] = value; }
        }

        private string SearchColumn
        {
            get { return Session["Books_SearchColumn"] as string ?? "Title"; }
            set { Session["Books_SearchColumn"] = value; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                // 檢查登入和管理員權限 (沿用 AdminPage.aspx.cs 的邏輯)
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

                // 初始化分頁大小
                ddlPageSize.SelectedValue = gvBooks.PageSize.ToString();

                BindBooksData();
            }
        }

        // 沿用原始碼中檢查管理員權限的私有方法
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
                    // 實際應用中應記錄錯誤
                }
            }
            return false;
        }

        // 綁定 Books 表格資料，加入排序和搜尋功能
        private void BindBooksData()
        {
            string tableName = "Books";
            string connString = GetConnectionString();

            // 基礎 SQL
            StringBuilder selectQuery = new StringBuilder("SELECT * FROM Books");
            List<SQLiteParameter> parameters = new List<SQLiteParameter>();

            // 搜尋條件
            if (!string.IsNullOrEmpty(SearchKeyword))
            {
                selectQuery.Append($" WHERE {SearchColumn} LIKE @Keyword");
                parameters.Add(new SQLiteParameter("@Keyword", $"%{SearchKeyword}%"));
                ShowMessage($"搜尋結果：欄位 '{SearchColumn}' 包含 '{SearchKeyword}'。", "info");
            }

            // 排序
            string sortDirection = CurrentSortDirection == SortDirection.Ascending ? "ASC" : "DESC";
            selectQuery.Append($" ORDER BY {CurrentSortExpression} {sortDirection}");

            try
            {
                using (SQLiteConnection conn = new SQLiteConnection(connString))
                using (SQLiteCommand cmd = new SQLiteCommand(selectQuery.ToString(), conn))
                {
                    cmd.Parameters.AddRange(parameters.ToArray());
                    conn.Open();
                    SQLiteDataAdapter da = new SQLiteDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    gvBooks.DataSource = dt;
                    gvBooks.DataBind();

                    if (string.IsNullOrEmpty(SearchKeyword))
                    {
                        ShowMessage($"已成功載入資料表：書籍主檔 (Books) (共 {dt.Rows.Count} 筆記錄)。", "success");
                    }
                }
            }
            catch (Exception ex)
            {
                ShowMessage($"載入資料時發生錯誤 ({tableName})：{ex.Message}", "error");
            }
            pnlInsertForm.Visible = false;
        }

        // 處理 GridView 換頁事件
        protected void gvBooks_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            gvBooks.PageIndex = e.NewPageIndex;
            BindBooksData();
        }

        // 處理 GridView 排序事件
        protected void gvBooks_Sorting(object sender, GridViewSortEventArgs e)
        {
            string newSortExpression = e.SortExpression;

            if (CurrentSortExpression == newSortExpression)
            {
                // 如果是同一欄位，則切換排序方向
                CurrentSortDirection = (CurrentSortDirection == SortDirection.Ascending) ? SortDirection.Descending : SortDirection.Ascending;
            }
            else
            {
                // 如果是不同欄位，則預設為升序
                CurrentSortExpression = newSortExpression;
                CurrentSortDirection = SortDirection.Ascending;
            }

            gvBooks.PageIndex = 0; // 排序後回到第一頁
            BindBooksData();
        }

        // 處理分頁大小變更
        protected void ddlPageSize_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (int.TryParse(ddlPageSize.SelectedValue, out int newSize) && newSize > 0)
            {
                gvBooks.PageSize = newSize;
                gvBooks.PageIndex = 0;
                BindBooksData();
            }
        }

        // 處理搜尋按鈕點擊事件
        protected void btnSearch_Click(object sender, EventArgs e)
        {
            SearchKeyword = txtSearchKeyword.Text.Trim();
            SearchColumn = ddlSearchColumn.SelectedValue;
            gvBooks.PageIndex = 0; // 搜尋後回到第一頁
            BindBooksData();
        }

        // 處理清除搜尋按鈕點擊事件
        protected void btnClearSearch_Click(object sender, EventArgs e)
        {
            SearchKeyword = string.Empty;
            txtSearchKeyword.Text = string.Empty;
            ddlSearchColumn.SelectedValue = "Title"; // 重設為預設欄位
            gvBooks.PageIndex = 0;
            BindBooksData();
            // BindBooksData 會自動更新訊息
        }


        // 顯示新增表單
        protected void btnShowInsert_Click(object sender, EventArgs e)
        {
            gvBooks.EditIndex = -1;
            BindBooksData(); // 確保 GridView 退出編輯模式

            litInsertHeader.Text = "<h3 class='insert-form-header'>新增書籍記錄</h3>";
            pnlInsertForm.Visible = true;

            // 清空輸入欄位 (因為是用靜態控制項，手動清空)
            (phInsertFormControls.FindControl("txtInsert_Title") as TextBox).Text = string.Empty;
            (phInsertFormControls.FindControl("txtInsert_Author") as TextBox).Text = string.Empty;
            (phInsertFormControls.FindControl("txtInsert_ISBN") as TextBox).Text = string.Empty;
            (phInsertFormControls.FindControl("txtInsert_TotalCopies") as TextBox).Text = "1";
            (phInsertFormControls.FindControl("txtInsert_AvailableCopies") as TextBox).Text = "1";

            ShowMessage("請在下方表單中輸入新書籍記錄數據。", "info");
        }

        // 取消新增
        protected void btnCancelInsert_Click(object sender, EventArgs e)
        {
            pnlInsertForm.Visible = false;
            ShowMessage("已取消書籍新增操作。", "info");
        }

        // 確認新增並儲存
        protected void btnInsertRecord_Click(object sender, EventArgs e)
        {
            TextBox txtTitle = phInsertFormControls.FindControl("txtInsert_Title") as TextBox;
            TextBox txtAuthor = phInsertFormControls.FindControl("txtInsert_Author") as TextBox;
            TextBox txtISBN = phInsertFormControls.FindControl("txtInsert_ISBN") as TextBox;
            TextBox txtTotalCopies = phInsertFormControls.FindControl("txtInsert_TotalCopies") as TextBox;
            TextBox txtAvailableCopies = phInsertFormControls.FindControl("txtInsert_AvailableCopies") as TextBox;

            // 欄位必填檢查
            if (string.IsNullOrWhiteSpace(txtTitle.Text))
            {
                ShowMessage("新增失敗：書名 (Title) 欄位不能為空。", "error");
                return;
            }

            // 數值及邏輯檢查 (防呆裝置)
            if (!int.TryParse(txtTotalCopies.Text, out int totalCopies) || totalCopies < 1)
            {
                ShowMessage("新增失敗：總本數 (TotalCopies) 必須是至少為 1 的正整數。", "error");
                return;
            }

            if (!int.TryParse(txtAvailableCopies.Text, out int availableCopies) || availableCopies < 0)
            {
                ShowMessage("新增失敗：可借閱本數 (AvailableCopies) 必須是至少為 0 的整數。", "error");
                return;
            }

            if (availableCopies > totalCopies)
            {
                ShowMessage("新增失敗：可借閱本數不能大於總本數。", "error");
                return;
            }

            string connString = GetConnectionString();
            string insertSql = "INSERT INTO Books (Title, Author, ISBN, TotalCopies, AvailableCopies) VALUES (@Title, @Author, @ISBN, @TotalCopies, @AvailableCopies)";

            try
            {
                using (SQLiteConnection conn = new SQLiteConnection(connString))
                using (SQLiteCommand cmd = new SQLiteCommand(insertSql, conn))
                {
                    cmd.Parameters.AddWithValue("@Title", txtTitle.Text.Trim());
                    cmd.Parameters.AddWithValue("@Author", txtAuthor.Text.Trim());
                    cmd.Parameters.AddWithValue("@ISBN", txtISBN.Text.Trim());
                    cmd.Parameters.AddWithValue("@TotalCopies", totalCopies);
                    cmd.Parameters.AddWithValue("@AvailableCopies", availableCopies);

                    conn.Open();
                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        ShowMessage("成功新增一筆書籍記錄。", "success");
                    }
                    else
                    {
                        ShowMessage("新增失敗：數據未被插入。", "error");
                    }
                }
            }
            catch (SQLiteException ex)
            {
                if (ex.Message.Contains("UNIQUE constraint failed: Books.ISBN"))
                {
                    ShowMessage("新增失敗：ISBN 已存在。請確保 ISBN 是唯一的。", "error");
                }
                else
                {
                    ShowMessage($"新增資料庫錯誤：{ex.Message}", "error");
                }
            }
            catch (Exception ex)
            {
                ShowMessage($"新增錯誤：{ex.Message}", "error");
            }

            pnlInsertForm.Visible = false;
            BindBooksData();
        }

        // 進入編輯模式
        protected void gvBooks_RowEditing(object sender, GridViewEditEventArgs e)
        {
            gvBooks.EditIndex = e.NewEditIndex;
            pnlInsertForm.Visible = false;
            BindBooksData();
        }

        // 取消編輯模式
        protected void gvBooks_RowCancelingEdit(object sender, GridViewCancelEditEventArgs e)
        {
            gvBooks.EditIndex = -1;
            BindBooksData();
        }

        // 更新紀錄
        protected void gvBooks_RowUpdating(object sender, GridViewUpdateEventArgs e)
        {
            // 獲取主鍵值
            object bookID = gvBooks.DataKeys[e.RowIndex].Value;
            if (bookID == null)
            {
                ShowMessage("更新失敗：主鍵值為空。", "error");
                return;
            }

            // 獲取編輯欄位控制項
            TextBox txtTitle = gvBooks.Rows[e.RowIndex].Cells[1].Controls[0] as TextBox;
            TextBox txtAuthor = gvBooks.Rows[e.RowIndex].Cells[2].Controls[0] as TextBox;
            TextBox txtISBN = gvBooks.Rows[e.RowIndex].Cells[3].Controls[0] as TextBox;
            TextBox txtTotalCopies = gvBooks.Rows[e.RowIndex].Cells[4].Controls[0] as TextBox;
            TextBox txtAvailableCopies = gvBooks.Rows[e.RowIndex].Cells[5].Controls[0] as TextBox;

            string title = txtTitle.Text.Trim();
            string author = txtAuthor.Text.Trim();
            string isbn = txtISBN.Text.Trim();

            // 欄位必填檢查
            if (string.IsNullOrWhiteSpace(title))
            {
                ShowMessage("更新失敗：書名 (Title) 欄位不能為空。", "error");
                gvBooks.EditIndex = -1;
                BindBooksData();
                return;
            }

            // 數值及邏輯檢查 (防呆裝置)
            if (!int.TryParse(txtTotalCopies.Text, out int totalCopies) || totalCopies < 1)
            {
                ShowMessage("更新失敗：總本數 (TotalCopies) 必須是至少為 1 的正整數。", "error");
                gvBooks.EditIndex = -1;
                BindBooksData();
                return;
            }

            if (!int.TryParse(txtAvailableCopies.Text, out int availableCopies) || availableCopies < 0)
            {
                ShowMessage("更新失敗：可借閱本數 (AvailableCopies) 必須是至少為 0 的整數。", "error");
                gvBooks.EditIndex = -1;
                BindBooksData();
                return;
            }

            if (availableCopies > totalCopies)
            {
                ShowMessage("更新失敗：可借閱本數不能大於總本數。", "error");
                gvBooks.EditIndex = -1;
                BindBooksData();
                return;
            }

            // 組建更新 SQL 語句
            string connString = GetConnectionString();
            string updateSql = "UPDATE Books SET Title = @Title, Author = @Author, ISBN = @ISBN, TotalCopies = @TotalCopies, AvailableCopies = @AvailableCopies WHERE BookID = @BookID";

            try
            {
                using (SQLiteConnection conn = new SQLiteConnection(connString))
                using (SQLiteCommand cmd = new SQLiteCommand(updateSql, conn))
                {
                    cmd.Parameters.AddWithValue("@Title", title);
                    cmd.Parameters.AddWithValue("@Author", author);
                    cmd.Parameters.AddWithValue("@ISBN", isbn);
                    cmd.Parameters.AddWithValue("@TotalCopies", totalCopies);
                    cmd.Parameters.AddWithValue("@AvailableCopies", availableCopies);
                    cmd.Parameters.AddWithValue("@BookID", bookID);

                    conn.Open();
                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        ShowMessage($"成功更新書籍記錄 (ID: {bookID})。", "success");
                    }
                    else
                    {
                        ShowMessage("更新失敗：沒有找到匹配的記錄或數據未變更。", "error");
                    }
                }
            }
            catch (SQLiteException ex)
            {
                if (ex.Message.Contains("UNIQUE constraint failed: Books.ISBN"))
                {
                    ShowMessage("更新失敗：ISBN 已存在。請確保 ISBN 是唯一的。", "error");
                }
                else
                {
                    ShowMessage($"更新資料庫錯誤：{ex.Message}", "error");
                }
            }
            catch (Exception ex)
            {
                ShowMessage($"更新錯誤：{ex.Message}", "error");
            }

            gvBooks.EditIndex = -1;
            BindBooksData();
        }

        // 刪除紀錄
        protected void gvBooks_RowDeleting(object sender, GridViewDeleteEventArgs e)
        {
            object bookID = gvBooks.DataKeys[e.RowIndex].Value;

            if (bookID == null)
            {
                ShowMessage("刪除失敗：主鍵值為空。", "error");
                return;
            }

            string connString = GetConnectionString();
            string deleteSql = "DELETE FROM Books WHERE BookID = @BookID";

            try
            {
                using (SQLiteConnection conn = new SQLiteConnection(connString))
                using (SQLiteCommand cmd = new SQLiteCommand(deleteSql, conn))
                {
                    cmd.Parameters.AddWithValue("@BookID", bookID);

                    conn.Open();
                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        ShowMessage($"成功刪除書籍記錄 (ID: {bookID})。", "success");
                    }
                    else
                    {
                        ShowMessage("刪除失敗：沒有找到匹配的記錄。", "error");
                    }
                }
            }
            catch (SQLiteException ex)
            {
                if (ex.Message.Contains("FOREIGN KEY constraint failed"))
                {
                    ShowMessage("刪除失敗：此書籍有相關的借閱記錄或類別關聯，請先清除相關記錄。", "error");
                }
                else
                {
                    ShowMessage($"刪除資料庫錯誤：{ex.Message}", "error");
                }
            }
            catch (Exception ex)
            {
                ShowMessage($"刪除錯誤：{ex.Message}", "error");
            }

            gvBooks.EditIndex = -1;
            BindBooksData();
        }

        // 資料綁定時，在標題加上排序箭頭
        protected void gvBooks_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.Header)
            {
                foreach (DataControlField column in gvBooks.Columns)
                {
                    if (column is BoundField boundField)
                    {
                        if (boundField.SortExpression == CurrentSortExpression)
                        {
                            // 加上排序箭頭
                            string arrow = CurrentSortDirection == SortDirection.Ascending ? " ▲" : " ▼";
                            column.HeaderText += arrow;
                        }
                    }
                }
            }
        }

        // 沿用原始碼中顯示訊息的私有方法
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