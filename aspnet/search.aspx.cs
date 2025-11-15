using System;
using System.Data;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data.SQLite;
using System.Configuration;
using System.Text; // 用於 StringBuilder
using System.Web.Security; // 用於 User.Identity.IsAuthenticated

public partial class Search : Page
{
    // ====== ViewState 屬性用於追蹤排序狀態 ======
    private string SortExpression
    {
        get { return ViewState["SortExpression"] as string ?? "BookID"; } // 預設按 BookID 排序
        set { ViewState["SortExpression"] = value; }
    }

    private string SortDirection
    {
        get { return ViewState["SortDirection"] as string ?? "ASC"; } // 預設升序
        set { ViewState["SortDirection"] = value; }
    }

    // ====== 資料庫連接與頁面載入 ======

    private string GetConnectionString()
    {
        return ConfigurationManager.ConnectionStrings["LibraryDBConnection"].ConnectionString;
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            // 頁面首次載入時，執行初始查詢
            BindBookData();
        }
    }

    // ====== 處理 GridView 事件 ======

    // 1. 處理分頁
    protected void gvBooks_PageIndexChanging(object sender, GridViewPageEventArgs e)
    {
        gvBooks.PageIndex = e.NewPageIndex;
        BindBookData();
    }

    // 2. 處理排序
    protected void gvBooks_Sorting(object sender, GridViewSortEventArgs e)
    {
        // 如果點擊的是同一個欄位，則切換排序方向
        if (SortExpression == e.SortExpression)
        {
            SortDirection = (SortDirection == "ASC" ? "DESC" : "ASC");
        }
        else // 如果點擊的是新欄位，則設定新欄位並預設為升序
        {
            SortExpression = e.SortExpression;
            SortDirection = "ASC";
        }

        gvBooks.PageIndex = 0;
        BindBookData();
    }

    // 3. 處理搜尋按鈕點擊事件 (用於觸發新的查詢)
    protected void btnSearch_Click(object sender, EventArgs e)
    {
        gvBooks.PageIndex = 0;
        BindBookData();
    }
    protected void btnQuickSearch_Click(object sender, EventArgs e)
    {
        // 清空進階查詢欄位，確保不影響快速查詢結果
        txtSearchTitle.Text = string.Empty;
        txtSearchAuthor.Text = string.Empty;
        txtSearchISBN.Text = string.Empty;
        // 保持 chkAvailableOnly 狀態，或者可以選擇重置它

        gvBooks.PageIndex = 0;
        BindBookData();
    }


    // 4. 控制「借閱」按鈕的顯示 (只有登入者才顯示)
    protected void gvBooks_RowDataBound(object sender, GridViewRowEventArgs e)
    {
        if (e.Row.RowType == DataControlRowType.DataRow)
        {
            Button btnBorrow = (Button)e.Row.FindControl("btnBorrow");

            if (btnBorrow != null)
            {
                // 如果使用者未登入，則隱藏借閱按鈕
                if (!User.Identity.IsAuthenticated)
                {
                    btnBorrow.Visible = false;
                }
                else
                {
                    // (未來借閱邏輯會用到：檢查庫存，如果為 0，則禁用按鈕)
                    DataRowView rowView = (DataRowView)e.Row.DataItem;
                    int availableCopies = Convert.ToInt32(rowView["AvailableCopies"]);

                    if (availableCopies <= 0)
                    {
                        btnBorrow.Enabled = false;
                        btnBorrow.Text = "已借完";
                    }
                }
            }
        }
    }


    // ====== 核心方法：執行進階查詢並綁定到 GridView ======
    private void BindBookData()
    {
        DataTable dt = new DataTable();
        string connString = GetConnectionString();

        StringBuilder whereClause = new StringBuilder();
        string baseSql = "SELECT BookID, Title, Author, ISBN, TotalCopies, AvailableCopies FROM Books";

        using (SQLiteConnection conn = new SQLiteConnection(connString))
        {
            using (SQLiteCommand cmd = new SQLiteCommand(conn))
            {
                string quickKeyword = txtQuickSearch.Text.Trim();

                // 1. 【新增】優先處理 快速查詢 邏輯
                if (!string.IsNullOrWhiteSpace(quickKeyword))
                {
                    whereClause.Append(" (Title LIKE @Keyword OR Author LIKE @Keyword OR ISBN LIKE @Keyword) AND ");
                    // 這裡的 @Keyword 參數會被用於三個欄位的模糊匹配
                    cmd.Parameters.AddWithValue("@Keyword", "%" + quickKeyword + "%");

                    // 備註：在快速查詢模式下，我們通常忽略進階查詢欄位。
                }
                else // 2. 處理 進階查詢 邏輯 (只有當快速查詢框為空時才執行)
                {
                    // 檢查並添加 書名 條件
                    if (!string.IsNullOrWhiteSpace(txtSearchTitle.Text))
                    {
                        whereClause.Append(" Title LIKE @TitleKeyword AND ");
                        cmd.Parameters.AddWithValue("@TitleKeyword", "%" + txtSearchTitle.Text.Trim() + "%");
                    }

                    // 檢查並添加 作者 條件
                    if (!string.IsNullOrWhiteSpace(txtSearchAuthor.Text))
                    {
                        whereClause.Append(" Author LIKE @AuthorKeyword AND ");
                        cmd.Parameters.AddWithValue("@AuthorKeyword", "%" + txtSearchAuthor.Text.Trim() + "%");
                    }

                    // 檢查並添加 ISBN 條件 (精確匹配)
                    if (!string.IsNullOrWhiteSpace(txtSearchISBN.Text))
                    {
                        whereClause.Append(" ISBN = @ISBN AND ");
                        cmd.Parameters.AddWithValue("@ISBN", txtSearchISBN.Text.Trim());
                    }
                }

                // 3. 檢查並添加 庫存 (> 0) 條件 (無論快速或進階，此條件都適用)
                if (chkAvailableOnly.Checked)
                {
                    whereClause.Append(" AvailableCopies > 0 AND ");
                }

                // 4. 組裝最終 SQL 語句
                if (whereClause.Length > 0)
                {
                    // 移除最後一個 " AND " (4個字元)
                    whereClause.Length -= 4;
                    baseSql += " WHERE " + whereClause.ToString();
                }

                // 5. 加入排序子句
                baseSql += $" ORDER BY {SortExpression} {SortDirection}";
                cmd.CommandText = baseSql;

                conn.Open();
                SQLiteDataAdapter da = new SQLiteDataAdapter(cmd);
                da.Fill(dt);
            }
        }

        gvBooks.DataSource = dt;
        gvBooks.DataBind();

        lblResultInfo.Text = $"共找到 {dt.Rows.Count} 本書籍，當前排序：{SortExpression} ({SortDirection})。";
    }

    // 5. 【預留】實作借閱按鈕的事件處理 (下一階段重點)
    // 我們將使用 RowCommand 來處理借閱邏輯
    // protected void gvBooks_RowCommand(object sender, GridViewCommandEventArgs e)
    // {
    //      // 這裡將處理 CommandName == "Borrow" 的邏輯
    // }
}