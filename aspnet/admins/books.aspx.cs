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

        // 頁面屬性用於儲存 GridView 狀態（現在主要從 GridView 屬性或 URL 參數取得）

        // CurrentPageIndex: 直接使用 GridView 的PageIndex屬性
        private int CurrentPageIndex
        {
            get { return gvBooks.PageIndex; }
            set { gvBooks.PageIndex = value; }
        }

        // CurrentSortExpression: 優先從 ViewState/URL 取得，並在設定時更新 ViewState
        private string CurrentSortExpression
        {
            get { return ViewState["SortExpression"] as string ?? "BookID"; }
            set { ViewState["SortExpression"] = value; }
        }

        // CurrentSortDirection: 優先從 ViewState/URL 取得，並在設定時更新 ViewState
        private SortDirection CurrentSortDirection
        {
            get { return (SortDirection)(ViewState["SortDirection"] ?? SortDirection.Ascending); }
            set { ViewState["SortDirection"] = value; }
        }

        // SearchKeyword & SearchColumn: 保持 Session 儲存以供跨頁面使用，但 Page_Load 時優先從 URL 讀取
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
                // 1. 頁面載入時，優先從 URL 參數讀取狀態
                LoadStateFromUrl();

                // 檢查搜尋欄位是否匹配狀態，並設置文本框
                ddlSearchColumn.SelectedValue = SearchColumn;
                txtSearch.Text = SearchKeyword;

                // 初次綁定資料
                BindBooksData();
            }
            // 每次載入都確保 GridView 的分頁索引與 CurrentPageIndex 同步 (已透過 Getter/Setter 處理)
        }

        /// <summary>
        /// 從 URL 參數讀取分頁、排序和搜尋狀態，並設置到對應的屬性或控件。
        /// </summary>
        private void LoadStateFromUrl()
        {
            // 讀取分頁索引
            if (int.TryParse(Request.QueryString["page"], out int pageIndex) && pageIndex >= 0)
            {
                // 注意：GridView 的 PageIndex 從 0 開始，URL 參數通常是 1-based (頁碼)
                // 但這裡為了簡化，讓 URL 參數與 GridView 的 0-based index 一致，或直接使用 GridView 的 PageIndex
                // 由於 PageIndex 在 BindBooksData 前設置，這裡直接設置 gvBooks.PageIndex
                gvBooks.PageIndex = pageIndex;
            }
            else
            {
                gvBooks.PageIndex = 0; // 預設第一頁
            }

            // 讀取排序表達式
            if (Request.QueryString["sort"] is string sortExpression && !string.IsNullOrEmpty(sortExpression))
            {
                CurrentSortExpression = sortExpression;
            }

            // 讀取排序方向
            if (Request.QueryString["dir"] is string sortDirection)
            {
                if (sortDirection.Equals("DESC", StringComparison.OrdinalIgnoreCase))
                {
                    CurrentSortDirection = SortDirection.Descending;
                }
                else
                {
                    CurrentSortDirection = SortDirection.Ascending;
                }
            }

            // 讀取搜尋欄位
            if (Request.QueryString["col"] is string searchColumn && !string.IsNullOrEmpty(searchColumn))
            {
                SearchColumn = searchColumn;
            }

            // 讀取搜尋關鍵字
            if (Request.QueryString["q"] is string searchKeyword)
            {
                // 注意：URL 參數通常是 URL 編碼的，但 ASP.NET 會自動解碼 QueryString
                SearchKeyword = searchKeyword;
            }
        }

        /// <summary>
        /// 根據目前的狀態（分頁、排序、搜尋）產生新的 URL，並重定向。
        /// </summary>
        private void RedirectWithState()
        {
            var urlParams = new List<string>();

            // 1. 頁面索引 (page)
            if (gvBooks.PageIndex > 0)
            {
                urlParams.Add($"page={gvBooks.PageIndex}");
            }

            // 2. 排序表達式 (sort)
            if (!CurrentSortExpression.Equals("BookID", StringComparison.OrdinalIgnoreCase))
            {
                urlParams.Add($"sort={CurrentSortExpression}");
            }

            // 3. 排序方向 (dir)
            if (CurrentSortDirection == SortDirection.Descending)
            {
                urlParams.Add("dir=DESC");
            }

            // 4. 搜尋欄位 (col) - 只有在非預設值時才加入
            if (!SearchColumn.Equals("Title", StringComparison.OrdinalIgnoreCase))
            {
                urlParams.Add($"col={SearchColumn}");
            }

            // 5. 搜尋關鍵字 (q) - 只有在非空時才加入
            if (!string.IsNullOrWhiteSpace(SearchKeyword))
            {
                // 使用 HttpUtility.UrlEncode 確保關鍵字中的特殊字符正確編碼
                urlParams.Add($"q={Server.UrlEncode(SearchKeyword)}");
            }

            // 組合新的 URL
            string newUrl = Request.Url.AbsolutePath;
            if (urlParams.Any())
            {
                newUrl += "?" + string.Join("&", urlParams);
            }

            // 重定向
            Response.Redirect(newUrl, false);
            Context.ApplicationInstance.CompleteRequest();
        }

        // 綁定資料方法 (主要邏輯不變，但排序/搜尋條件來自 Class Properties)
        private void BindBooksData()
        {
            DataTable dt = new DataTable();
            string connectionString = GetConnectionString();

            // 建立基本的 SQL 查詢字串
            StringBuilder sql = new StringBuilder("SELECT BookID, Title, Author, ISBN, TotalCopies, AvailableCopies FROM Books");

            // 處理搜尋條件
            if (!string.IsNullOrWhiteSpace(SearchKeyword))
            {
                // 使用 LIKE 進行模糊搜尋
                sql.Append($" WHERE {SearchColumn} LIKE @SearchKeyword");
            }

            // 處理排序
            sql.Append($" ORDER BY {CurrentSortExpression} {(CurrentSortDirection == SortDirection.Ascending ? "ASC" : "DESC")}");

            try
            {
                using (SQLiteConnection conn = new SQLiteConnection(connectionString))
                {
                    using (SQLiteCommand cmd = new SQLiteCommand(sql.ToString(), conn))
                    {
                        if (!string.IsNullOrWhiteSpace(SearchKeyword))
                        {
                            // 綁定參數以防止 SQL 注入
                            cmd.Parameters.AddWithValue("@SearchKeyword", $"%{SearchKeyword}%");
                        }

                        conn.Open();
                        SQLiteDataReader reader = cmd.ExecuteReader();
                        dt.Load(reader);
                    }
                }

                // 綁定資料到 GridView
                gvBooks.DataSource = dt;

                // GridView 的分頁索引已在 LoadStateFromUrl 或事件處理器中設置
                // gvBooks.PageIndex = CurrentPageIndex; // 已通過 Class Property 處理

                gvBooks.DataBind();

                // 這裡我們不呼叫 RedirectWithState()，因為 BindBooksData 是讀取操作，
                // 改變狀態的操作（如分頁、排序）會自己呼叫 RedirectWithState。
            }
            catch (Exception ex)
            {
                ShowMessage($"載入書籍資料失敗: {ex.Message}", "error");
            }
        }

        // GridView 事件處理器

        // 分頁事件處理：更新頁碼並重定向
        protected void gvBooks_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            // 更新 GridView 的 PageIndex
            gvBooks.PageIndex = e.NewPageIndex;

            // 重定向以更新 URL 狀態
            RedirectWithState();
            // 注意：RedirectWithState() 會結束請求並重新載入頁面，因此不需要再呼叫 BindBooksData()
        }

        // 排序事件處理：更新排序表達式和方向，並重定向
        protected void gvBooks_Sorting(object sender, GridViewSortEventArgs e)
        {
            string sortExpression = e.SortExpression;

            if (sortExpression == CurrentSortExpression)
            {
                // 相同欄位，切換排序方向
                CurrentSortDirection = (CurrentSortDirection == SortDirection.Ascending) ? SortDirection.Descending : SortDirection.Ascending;
            }
            else
            {
                // 變更欄位，重設為升序
                CurrentSortExpression = sortExpression;
                CurrentSortDirection = SortDirection.Ascending;
            }

            // 排序時將頁碼設回第一頁 (PageIndex = 0)
            gvBooks.PageIndex = 0;

            // 重定向以更新 URL 狀態
            RedirectWithState();
            // 注意：RedirectWithState() 會結束請求並重新載入頁面
        }

        // 搜尋按鈕點擊：更新搜尋條件並重定向
        protected void btnSearch_Click(object sender, EventArgs e)
        {
            // 更新搜尋狀態
            SearchKeyword = txtSearch.Text.Trim();
            SearchColumn = ddlSearchColumn.SelectedValue;

            // 搜尋時將頁碼設回第一頁 (PageIndex = 0)
            gvBooks.PageIndex = 0;

            // 重定向以更新 URL 狀態
            RedirectWithState();
            // 注意：RedirectWithState() 會結束請求並重新載入頁面
        }

        // 清除搜尋按鈕點擊：清除搜尋條件並重定向
        protected void btnClearSearch_Click(object sender, EventArgs e)
        {
            // 清除搜尋狀態
            SearchKeyword = string.Empty;
            SearchColumn = "Title"; // 預設值

            // 清除 GridView 編輯狀態和頁碼
            gvBooks.EditIndex = -1;
            gvBooks.PageIndex = 0;

            // 重定向以清除 URL 上的搜尋參數
            RedirectWithState();
            // 注意：RedirectWithState() 會結束請求並重新載入頁面
        }

        // 編輯、取消編輯、更新、刪除等操作不影響 URL 狀態，因此保持原樣，並在操作完成後呼叫 BindBooksData()。

        protected void gvBooks_RowEditing(object sender, GridViewEditEventArgs e)
        {
            gvBooks.EditIndex = e.NewEditIndex;
            BindBooksData(); // 保持分頁、排序、搜尋狀態
        }

        protected void gvBooks_RowCancelingEdit(object sender, GridViewCancelEditEventArgs e)
        {
            gvBooks.EditIndex = -1;
            BindBooksData(); // 保持分頁、排序、搜尋狀態
        }

        protected void gvBooks_RowUpdating(object sender, GridViewUpdateEventArgs e)
        {
            // ... (更新書籍資料的邏輯，保持不變) ...
            GridViewRow row = gvBooks.Rows[e.RowIndex];
            int bookId = Convert.ToInt32(gvBooks.DataKeys[e.RowIndex].Value);

            // 獲取編輯後的資料
            string title = ((TextBox)row.FindControl("txtTitle")).Text.Trim();
            string author = ((TextBox)row.FindControl("txtAuthor")).Text.Trim();
            string isbn = ((TextBox)row.FindControl("txtISBN")).Text.Trim();
            int totalCopies, availableCopies;

            if (!int.TryParse(((TextBox)row.FindControl("txtTotalCopies")).Text.Trim(), out totalCopies) ||
                !int.TryParse(((TextBox)row.FindControl("txtAvailableCopies")).Text.Trim(), out availableCopies))
            {
                ShowMessage("總本數和可借數必須是有效的整數。", "error");
                return;
            }

            // 檢查資料的有效性
            if (string.IsNullOrWhiteSpace(title) || totalCopies <= 0 || availableCopies < 0)
            {
                ShowMessage("書名不能為空，總本數必須大於 0，可借數不能為負。", "error");
                return;
            }

            if (availableCopies > totalCopies)
            {
                ShowMessage("可借數不能大於總本數。", "error");
                return;
            }

            string connectionString = GetConnectionString();
            string sql = "UPDATE Books SET Title = @Title, Author = @Author, ISBN = @ISBN, TotalCopies = @TotalCopies, AvailableCopies = @AvailableCopies WHERE BookID = @BookID";

            try
            {
                using (SQLiteConnection conn = new SQLiteConnection(connectionString))
                {
                    using (SQLiteCommand cmd = new SQLiteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Title", title);
                        cmd.Parameters.AddWithValue("@Author", author);
                        cmd.Parameters.AddWithValue("@ISBN", isbn);
                        cmd.Parameters.AddWithValue("@TotalCopies", totalCopies);
                        cmd.Parameters.AddWithValue("@AvailableCopies", availableCopies);
                        cmd.Parameters.AddWithValue("@BookID", bookId);

                        conn.Open();
                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            ShowMessage($"書籍 ID {bookId} 已成功更新。", "success");
                        }
                        else
                        {
                            ShowMessage($"更新書籍 ID {bookId} 失敗。", "error");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ShowMessage($"更新書籍資料時發生錯誤: {ex.Message}", "error");
            }

            gvBooks.EditIndex = -1;
            BindBooksData(); // 重新綁定資料以顯示更新結果
        }

        protected void gvBooks_RowDeleting(object sender, GridViewDeleteEventArgs e)
        {
            // ... (刪除書籍資料的邏輯，保持不變) ...
            int bookId = Convert.ToInt32(gvBooks.DataKeys[e.RowIndex].Value);
            string connectionString = GetConnectionString();
            string sql = "DELETE FROM Books WHERE BookID = @BookID";

            try
            {
                using (SQLiteConnection conn = new SQLiteConnection(connectionString))
                {
                    using (SQLiteCommand cmd = new SQLiteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@BookID", bookId);
                        conn.Open();
                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            ShowMessage($"書籍 ID {bookId} 已成功刪除。", "success");
                        }
                        else
                        {
                            ShowMessage($"刪除書籍 ID {bookId} 失敗。", "error");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ShowMessage($"刪除書籍資料時發生錯誤: {ex.Message}", "error");
            }

            gvBooks.EditIndex = -1;
            BindBooksData(); // 重新綁定資料以顯示結果
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
                pnlMessage.CssClass += " message-box-info"; // 假設預設是 info
            }

            // 讓訊息持續顯示
            // ScriptManager.RegisterStartupScript(this, GetType(), "HideMessage", "setTimeout(function(){ document.getElementById('" + pnlMessage.ClientID + "').style.display='none'; }, 5000);", true);
        }
    }
}