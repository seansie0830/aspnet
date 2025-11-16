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
    public partial class BookCategory : Page
    {
        private const string ConnectionStringName = "LibraryDBConnection";
        private string GetConnectionString()
        {
            // 由於原始碼未提供 ConfigurationManager，這裡假設它是可用的
            return ConfigurationManager.ConnectionStrings[ConnectionStringName].ConnectionString;
        }

        // ViewState 屬性用於儲存排序狀態和選中的書籍 ID
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
            get { return Session["BookCategory_SearchKeyword"] as string ?? string.Empty; }
            set { Session["BookCategory_SearchKeyword"] = value; }
        }

        private string SearchColumn
        {
            get { return Session["BookCategory_SearchColumn"] as string ?? "Title"; }
            set { Session["BookCategory_SearchColumn"] = value; }
        }

        // 儲存當前選中的書籍 ID (書籍導向模式)
        private int SelectedBookID
        {
            get { return (int)(ViewState["SelectedBookID"] ?? 0); }
            set { ViewState["SelectedBookID"] = value; }
        }

        // 儲存當前選中的類別 ID (類別導向模式)
        private int SelectedCategoryID
        {
            get { return (int)(ViewState["SelectedCategoryID"] ?? 0); }
            set { ViewState["SelectedCategoryID"] = value; }
        }

        // 儲存當前模式 (BookMode, CategoryMode)
        private string CurrentMode
        {
            get { return ViewState["CurrentMode"] as string ?? "BookMode"; }
            set { ViewState["CurrentMode"] = value; }
        }


        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                // 檢查登入和管理員權限 (沿用 books.aspx.cs 的邏輯)
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

                BindAllCategories(); // 綁定所有類別到 DropDownList (兩個模式都需要)

                // 預設進入書籍導向模式
                SetMode(CurrentMode);
            }
        }

        // 切換模式的按鈕事件
        protected void btnMode_Click(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            if (btn != null)
            {
                SetMode(btn.CommandArgument);
            }
        }

        // 設定並切換顯示模式
        private void SetMode(string mode)
        {
            CurrentMode = mode;
            pnlBookMode.Visible = mode == "BookMode";
            pnlCategoryMode.Visible = mode == "CategoryMode";

            // 更新按鈕樣式
            btnBookMode.CssClass = "mode-toggle-btn" + (mode == "BookMode" ? " active" : "");
            btnCategoryMode.CssClass = "mode-toggle-btn" + (mode == "CategoryMode" ? " active" : "");

            // 重新載入資料
            if (mode == "BookMode")
            {
                BindBooksForBookMode();
                pnlCategoryManagement.Visible = SelectedBookID > 0;
            }
            else // CategoryMode
            {
                BindCategoryBooks(SelectedCategoryID);
            }
        }

        // 綁定所有類別到兩個 DropDownList
        private void BindAllCategories()
        {
            string connString = GetConnectionString();
            string sql = "SELECT CategoryID, CategoryName FROM Categories ORDER BY CategoryName";

            try
            {
                using (SQLiteConnection conn = new SQLiteConnection(connString))
                using (SQLiteCommand cmd = new SQLiteCommand(sql, conn))
                {
                    conn.Open();
                    SQLiteDataAdapter da = new SQLiteDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    // 類別導向模式的 DropDownList
                    ddlSelectCategory.DataSource = dt;
                    ddlSelectCategory.DataTextField = "CategoryName";
                    ddlSelectCategory.DataValueField = "CategoryID";
                    ddlSelectCategory.DataBind();
                    ddlSelectCategory.Items.Insert(0, new ListItem("-- 選擇一個類別 --", "0"));

                    // 書籍導向模式新增類別的 DropDownList
                    ddlAvailableCategories.DataSource = dt;
                    ddlAvailableCategories.DataTextField = "CategoryName";
                    ddlAvailableCategories.DataValueField = "CategoryID";
                    ddlAvailableCategories.DataBind();
                    ddlAvailableCategories.Items.Insert(0, new ListItem("-- 選擇要新增的類別 --", "0"));

                    // 初始化選中的類別 ID
                    if (SelectedCategoryID == 0 && dt.Rows.Count > 0)
                    {
                        SelectedCategoryID = Convert.ToInt32(dt.Rows[0]["CategoryID"]);
                        ddlSelectCategory.SelectedValue = SelectedCategoryID.ToString();
                    }
                    else if (SelectedCategoryID > 0)
                    {
                        ddlSelectCategory.SelectedValue = SelectedCategoryID.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                ShowMessage($"載入類別時發生錯誤：{ex.Message}", "error");
            }
        }

        // ----------------------- 書籍導向模式 (BookMode) -----------------------

        // 綁定書籍資料 (左側 GridView)
        private void BindBooksForBookMode()
        {
            string connString = GetConnectionString();
            StringBuilder selectQuery = new StringBuilder("SELECT BookID, Title, Author, ISBN FROM Books");
            List<SQLiteParameter> parameters = new List<SQLiteParameter>();

            // 搜尋條件
            if (!string.IsNullOrEmpty(SearchKeyword))
            {
                selectQuery.Append($" WHERE {SearchColumn} LIKE @Keyword");
                parameters.Add(new SQLiteParameter("@Keyword", $"%{SearchKeyword}%"));
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

                    if (!string.IsNullOrEmpty(SearchKeyword))
                    {
                        ShowMessage($"書籍列表搜尋結果：欄位 '{SearchColumn}' 包含 '{SearchKeyword}' (共 {dt.Rows.Count} 筆記錄)。", "info");
                    }
                    else
                    {
                        ShowMessage($"已成功載入書籍列表 (共 {dt.Rows.Count} 筆記錄)。", "success");
                    }
                }
            }
            catch (Exception ex)
            {
                ShowMessage($"載入書籍列表時發生錯誤：{ex.Message}", "error");
            }

            // 確保右側面板的顯示狀態正確
            if (SelectedBookID > 0)
            {
                BindBookCategories(SelectedBookID);
            }
            else
            {
                pnlCategoryManagement.Visible = false;
            }
        }

        // 綁定選中書籍的類別 (右側 Repeater)
        private void BindBookCategories(int bookID)
        {
            string connString = GetConnectionString();
            // 查詢已關聯的類別名稱和顏色
            string sql = @"
                SELECT c.CategoryID, c.CategoryName, c.ColorHex 
                FROM Categories c
                INNER JOIN CategoryRecords cr ON c.CategoryID = cr.CategoryID
                WHERE cr.BookID = @BookID
                ORDER BY c.CategoryName";

            // 查詢書籍名稱
            string bookTitleSql = "SELECT Title FROM Books WHERE BookID = @BookID";

            try
            {
                using (SQLiteConnection conn = new SQLiteConnection(connString))
                {
                    conn.Open();

                    // 獲取書籍名稱
                    using (SQLiteCommand cmdTitle = new SQLiteCommand(bookTitleSql, conn))
                    {
                        cmdTitle.Parameters.AddWithValue("@BookID", bookID);
                        object result = cmdTitle.ExecuteScalar();
                        lblSelectedBookTitle.Text = result?.ToString() ?? "未知書籍";
                    }

                    // 獲取類別列表
                    using (SQLiteCommand cmd = new SQLiteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@BookID", bookID);
                        SQLiteDataAdapter da = new SQLiteDataAdapter(cmd);
                        DataTable dt = new DataTable();
                        da.Fill(dt);

                        rptBookCategories.DataSource = dt;
                        rptBookCategories.DataBind();

                        // 移除已關聯類別，更新 ddlAvailableCategories 
                        UpdateAvailableCategories(dt);
                    }
                }

                lblSelectedBookID.Text = bookID.ToString();
                pnlCategoryManagement.Visible = true;

            }
            catch (Exception ex)
            {
                ShowMessage($"載入書籍類別時發生錯誤：{ex.Message}", "error");
                pnlCategoryManagement.Visible = false;
            }
        }

        // 更新下拉選單，排除已關聯的類別
        private void UpdateAvailableCategories(DataTable currentCategories)
        {
            // 重新綁定所有類別
            BindAllCategories();

            // 從 ddlAvailableCategories 移除已關聯的類別
            foreach (DataRow row in currentCategories.Rows)
            {
                ListItem itemToRemove = ddlAvailableCategories.Items.FindByValue(row["CategoryID"].ToString());
                if (itemToRemove != null)
                {
                    ddlAvailableCategories.Items.Remove(itemToRemove);
                }
            }
            // 確保「選擇要新增的類別」選項存在且被選中
            if (ddlAvailableCategories.Items.FindByValue("0") == null)
            {
                ddlAvailableCategories.Items.Insert(0, new ListItem("-- 選擇要新增的類別 --", "0"));
            }
            ddlAvailableCategories.SelectedValue = "0";

            // 如果所有類別都已關聯，則禁用新增按鈕
            btnAddCategory.Enabled = ddlAvailableCategories.Items.Count > 1; // 1是選項 "-- 選擇要新增的類別 --"
            btnAddCategory.Text = btnAddCategory.Enabled ? "✚ 新增關聯" : "✅ 全部已關聯";
        }

        // GridView 換頁事件
        protected void gvBooks_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            gvBooks.PageIndex = e.NewPageIndex;
            BindBooksForBookMode();
        }

        // GridView 排序事件
        protected void gvBooks_Sorting(object sender, GridViewSortEventArgs e)
        {
            string newSortExpression = e.SortExpression;

            if (CurrentSortExpression == newSortExpression)
            {
                CurrentSortDirection = (CurrentSortDirection == SortDirection.Ascending) ? SortDirection.Descending : SortDirection.Ascending;
            }
            else
            {
                CurrentSortExpression = newSortExpression;
                CurrentSortDirection = SortDirection.Ascending;
            }

            gvBooks.PageIndex = 0;
            BindBooksForBookMode();
        }

        // GridView 行資料綁定 (用於高亮選中的行)
        protected void gvBooks_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                DataRowView drv = e.Row.DataItem as DataRowView;
                if (drv != null && drv["BookID"].ToString() == SelectedBookID.ToString())
                {
                    e.Row.CssClass += " selected-row";
                }

                // 加上排序箭頭 (沿用 books.aspx.cs 的邏輯)
                if (e.Row.RowType == DataControlRowType.Header)
                {
                    foreach (DataControlField column in gvBooks.Columns)
                    {
                        if (column is BoundField boundField && boundField.SortExpression == CurrentSortExpression)
                        {
                            string arrow = CurrentSortDirection == SortDirection.Ascending ? " ▲" : " ▼";
                            column.HeaderText += arrow;
                        }
                    }
                }
            }
        }

        // GridView 命令 (選擇書籍)
        protected void gvBooks_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName == "SelectBook")
            {
                if (int.TryParse(e.CommandArgument.ToString(), out int bookId))
                {
                    SelectedBookID = bookId;
                    BindBooksForBookMode(); // 重新綁定 GridView 以高亮顯示
                    BindBookCategories(bookId);
                    ShowMessage($"已選擇書籍 ID: {bookId}。請在右側管理其類別關聯。", "info");
                }
            }
        }

        // 處理搜尋按鈕點擊事件
        protected void btnSearch_Click(object sender, EventArgs e)
        {
            SearchKeyword = txtSearchKeyword.Text.Trim();
            SearchColumn = ddlSearchColumn.SelectedValue;
            gvBooks.PageIndex = 0;
            BindBooksForBookMode();

            // 搜尋後清空選中狀態，以避免混亂
            SelectedBookID = 0;
            pnlCategoryManagement.Visible = false;
        }

        // 處理清除搜尋按鈕點擊事件
        protected void btnClearSearch_Click(object sender, EventArgs e)
        {
            SearchKeyword = string.Empty;
            txtSearchKeyword.Text = string.Empty;
            ddlSearchColumn.SelectedValue = "Title";
            gvBooks.PageIndex = 0;
            BindBooksForBookMode();
        }

        // 新增書籍-類別關聯
        protected void btnAddCategory_Click(object sender, EventArgs e)
        {
            if (SelectedBookID <= 0)
            {
                ShowMessage("新增失敗：請先從列表中選擇一本書籍。", "error");
                return;
            }

            if (!int.TryParse(ddlAvailableCategories.SelectedValue, out int categoryID) || categoryID <= 0)
            {
                ShowMessage("新增失敗：請選擇一個有效的類別。", "error");
                return;
            }

            string connString = GetConnectionString();
            string insertSql = "INSERT INTO CategoryRecords (BookID, CategoryID) VALUES (@BookID, @CategoryID)";

            try
            {
                using (SQLiteConnection conn = new SQLiteConnection(connString))
                using (SQLiteCommand cmd = new SQLiteCommand(insertSql, conn))
                {
                    cmd.Parameters.AddWithValue("@BookID", SelectedBookID);
                    cmd.Parameters.AddWithValue("@CategoryID", categoryID);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                    ShowMessage($"成功為書籍 '{lblSelectedBookTitle.Text}' 新增類別關聯：{ddlAvailableCategories.SelectedItem.Text}。", "success");
                }
            }
            catch (SQLiteException ex)
            {
                if (ex.Message.Contains("UNIQUE constraint failed"))
                {
                    ShowMessage("新增失敗：此書籍與該類別的關聯已存在。", "error");
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

            // 重新綁定右側面板
            BindBookCategories(SelectedBookID);
        }

        // 刪除書籍-類別關聯 (右側 Repeater)
        protected void rptBookCategories_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (e.CommandName == "DeleteCategory")
            {
                if (SelectedBookID <= 0)
                {
                    ShowMessage("刪除失敗：選中的書籍 ID 無效。", "error");
                    return;
                }

                if (int.TryParse(e.CommandArgument.ToString(), out int categoryID))
                {
                    DeleteCategoryRecord(SelectedBookID, categoryID);
                }
            }
        }

        // 刪除書籍-類別關聯的資料庫操作
        private void DeleteCategoryRecord(int bookID, int categoryID)
        {
            string connString = GetConnectionString();
            string deleteSql = "DELETE FROM CategoryRecords WHERE BookID = @BookID AND CategoryID = @CategoryID";

            try
            {
                using (SQLiteConnection conn = new SQLiteConnection(connString))
                using (SQLiteCommand cmd = new SQLiteCommand(deleteSql, conn))
                {
                    cmd.Parameters.AddWithValue("@BookID", bookID);
                    cmd.Parameters.AddWithValue("@CategoryID", categoryID);

                    conn.Open();
                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        ShowMessage($"成功移除書籍 ID: {bookID} 與類別 ID: {categoryID} 的關聯。", "success");
                    }
                    else
                    {
                        ShowMessage("刪除失敗：沒有找到匹配的關聯記錄。", "error");
                    }
                }
            }
            catch (Exception ex)
            {
                ShowMessage($"刪除錯誤：{ex.Message}", "error");
            }

            // 重新綁定右側面板
            BindBookCategories(bookID);
        }


        // ----------------------- 類別導向模式 (CategoryMode) -----------------------

        // 類別下拉選單變更事件
        protected void ddlSelectCategory_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (int.TryParse(ddlSelectCategory.SelectedValue, out int categoryID) && categoryID > 0)
            {
                SelectedCategoryID = categoryID;
                BindCategoryBooks(categoryID);
            }
            else
            {
                SelectedCategoryID = 0;
                pnlCategoryBooks.Visible = false;
                ShowMessage("請選擇一個有效的類別。", "info");
            }
        }

        // 綁定類別下的書籍列表 (Repeater)
        private void BindCategoryBooks(int categoryID)
        {
            if (categoryID <= 0)
            {
                pnlCategoryBooks.Visible = false;
                ShowMessage("請先在上方選擇一個類別。", "info");
                return;
            }

            string connString = GetConnectionString();
            string sql = @"
                SELECT b.BookID, b.Title, b.Author, b.ISBN 
                FROM Books b
                INNER JOIN CategoryRecords cr ON b.BookID = cr.BookID
                WHERE cr.CategoryID = @CategoryID
                ORDER BY b.Title";

            // 查詢類別名稱
            string categoryNameSql = "SELECT CategoryName FROM Categories WHERE CategoryID = @CategoryID";


            try
            {
                using (SQLiteConnection conn = new SQLiteConnection(connString))
                {
                    conn.Open();

                    // 獲取類別名稱
                    using (SQLiteCommand cmdName = new SQLiteCommand(categoryNameSql, conn))
                    {
                        cmdName.Parameters.AddWithValue("@CategoryID", categoryID);
                        object result = cmdName.ExecuteScalar();
                        lblSelectedCategoryName.Text = result?.ToString() ?? "未知類別";
                    }

                    // 獲取書籍列表
                    using (SQLiteCommand cmd = new SQLiteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@CategoryID", categoryID);
                        SQLiteDataAdapter da = new SQLiteDataAdapter(cmd);
                        DataTable dt = new DataTable();
                        da.Fill(dt);

                        rptCategoryBooks.DataSource = dt;
                        rptCategoryBooks.DataBind();

                        lblSelectedCategoryID.Text = categoryID.ToString();
                        pnlCategoryBooks.Visible = true;
                        ShowMessage($"類別 '{lblSelectedCategoryName.Text}' 下共有 {dt.Rows.Count} 筆書籍記錄。", "success");
                    }
                }
            }
            catch (Exception ex)
            {
                ShowMessage($"載入類別書籍列表時發生錯誤：{ex.Message}", "error");
                pnlCategoryBooks.Visible = false;
            }
        }

        // 刪除類別下書籍的關聯 (類別導向 Repeater)
        protected void rptCategoryBooks_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (e.CommandName == "DeleteBookCategory")
            {
                if (SelectedCategoryID <= 0)
                {
                    ShowMessage("刪除失敗：選中的類別 ID 無效。", "error");
                    return;
                }

                if (int.TryParse(e.CommandArgument.ToString(), out int bookID))
                {
                    DeleteCategoryRecord(bookID, SelectedCategoryID); // 呼叫共用的刪除方法

                    // 重新綁定當前類別的書籍列表
                    BindCategoryBooks(SelectedCategoryID);
                }
            }
        }

        // ----------------------- 共用方法 -----------------------

        // 顯示訊息 (沿用 books.aspx.cs 的邏輯)
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

        // 檢查管理員權限 (沿用 books.aspx.cs 的邏輯)
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
    }
}