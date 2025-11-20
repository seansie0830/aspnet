using System;
using System.Data;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data.SQLite;
using System.Configuration;
using System.Text;
using System.Web.Security;
using System.Collections.Generic;
using System.Linq;
using System.Web;

public partial class Search : Page
{
    private const string AppConfigKey = "ApplicationConfig";

    private string SortExpression
    {
        get
        {
            return ViewState["SortExpression"] as string ?? "BookID";
        }
        set
        {
            ViewState["SortExpression"] = value;
        }
    }

    private string SortDirection
    {
        get
        {
            return ViewState["SortDirection"] as string ?? "ASC";
        }
        set
        {
            ViewState["SortDirection"] = value;
        }
    }

    private int CurrentPageSize
    {
        get { return ViewState["PageSize"] as int? ?? 10; }
        set { ViewState["PageSize"] = value; }
    }

    private string GetConnectionString()
    {
        return ConfigurationManager.ConnectionStrings["LibraryDBConnection"].ConnectionString;
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            if (ddlPageSize.Items.FindByValue(CurrentPageSize.ToString()) != null)
            {
                ddlPageSize.SelectedValue = CurrentPageSize.ToString();
            }

            if (!User.Identity.IsAuthenticated)
            {
                Response.Redirect("~/Login.aspx");
                return;
            }

            BindCategories(); // 綁定 ddlAvailableCategories
            LoadSearchParameters();
            BindBookData();
        }
        else
        {
            pnlAdvancedSearch.Visible = hidPanelVisible.Value.Equals("true", StringComparison.OrdinalIgnoreCase);
        }
    }

    // 從 HiddenField 解析已選的類別 ID
    private List<string> GetSelectedCategoryIDs()
    {
        string catData = hidSelectedCategories.Value;
        if (string.IsNullOrWhiteSpace(catData))
        {
            return new List<string>();
        }

        // 格式: "ID1|Name1,ID2|Name2"
        return catData.Split(',')
                      .Select(item => item.Split('|')[0])
                      .ToList();
    }

    // 從 URL 參數載入已選的類別 ID 和 Name，並設定 HiddenField
    private void LoadCategoriesFromQueryString(string catQueryString)
    {
        if (string.IsNullOrWhiteSpace(catQueryString))
        {
            hidSelectedCategories.Value = string.Empty;
            return;
        }

        // URL 格式: "ID1,ID2"
        string[] selectedIDs = catQueryString.Split(',');

        // 獲取所有類別名稱的字典 {ID: Name}
        Dictionary<string, string> categoryMap = GetCategoryMap();

        List<string> selectedCategoryPairs = new List<string>();

        foreach (string id in selectedIDs)
        {
            if (categoryMap.ContainsKey(id))
            {
                // 儲存格式: "ID|Name"
                selectedCategoryPairs.Add($"{id}|{categoryMap[id]}");
            }
        }

        // 寫入 HiddenField: "ID1|Name1,ID2|Name2"
        hidSelectedCategories.Value = string.Join(",", selectedCategoryPairs);
    }

    // 獲取 CategoryID 到 CategoryName 的映射
    private Dictionary<string, string> GetCategoryMap()
    {
        Dictionary<string, string> map = new Dictionary<string, string>();
        string connString = GetConnectionString();
        string sql = "SELECT CategoryID, CategoryName FROM Categories";

        using (SQLiteConnection conn = new SQLiteConnection(connString))
        using (SQLiteCommand cmd = new SQLiteCommand(sql, conn))
        {
            conn.Open();
            using (SQLiteDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    map.Add(reader["CategoryID"].ToString(), reader["CategoryName"].ToString());
                }
            }
        }
        return map;
    }

    // 載入 URL 參數到控制項
    private void LoadSearchParameters()
    {
        if (!string.IsNullOrEmpty(Request.QueryString["q"]))
        {
            txtQuickSearch.Text = Request.QueryString["q"];
            hidPanelVisible.Value = "false";
        }
        else if (!string.IsNullOrWhiteSpace(Request.QueryString["bookid"]) ||
                 !string.IsNullOrWhiteSpace(Request.QueryString["title"]) ||
                 !string.IsNullOrWhiteSpace(Request.QueryString["author"]) ||
                 !string.IsNullOrWhiteSpace(Request.QueryString["isbn"]) ||
                 !string.IsNullOrWhiteSpace(Request.QueryString["cat"]))
        {
            txtBookID.Text = Request.QueryString["bookid"];
            txtTitle.Text = Request.QueryString["title"];
            txtAuthor.Text = Request.QueryString["author"];
            txtISBN.Text = Request.QueryString["isbn"];

            LoadCategoriesFromQueryString(Request.QueryString["cat"]);

            pnlAdvancedSearch.Visible = true;
            hidPanelVisible.Value = "true";
        }

        // 載入排序和分頁
        if (!string.IsNullOrEmpty(Request.QueryString["sort"]))
        {
            SortExpression = Request.QueryString["sort"];
        }
        if (!string.IsNullOrEmpty(Request.QueryString["dir"]))
        {
            SortDirection = Request.QueryString["dir"];
        }
        if (!string.IsNullOrEmpty(Request.QueryString["size"]) && ddlPageSize.Items.FindByValue(Request.QueryString["size"]) != null)
        {
            CurrentPageSize = Convert.ToInt32(Request.QueryString["size"]);
            gvBooks.PageSize = CurrentPageSize;
            ddlPageSize.SelectedValue = CurrentPageSize.ToString();
        }
    }

    // 儲存參數到 URL
    private void SaveSearchParameters()
    {
        var parameters = new List<string>();

        if (!string.IsNullOrWhiteSpace(txtQuickSearch.Text))
        {
            parameters.Add($"q={HttpUtility.UrlEncode(txtQuickSearch.Text.Trim())}");
        }
        else
        {
            var selectedCategories = GetSelectedCategoryIDs();

            if (!string.IsNullOrWhiteSpace(txtBookID.Text))
            {
                parameters.Add($"bookid={HttpUtility.UrlEncode(txtBookID.Text.Trim())}");
            }
            if (!string.IsNullOrWhiteSpace(txtTitle.Text))
            {
                parameters.Add($"title={HttpUtility.UrlEncode(txtTitle.Text.Trim())}");
            }
            if (!string.IsNullOrWhiteSpace(txtAuthor.Text))
            {
                parameters.Add($"author={HttpUtility.UrlEncode(txtAuthor.Text.Trim())}");
            }
            if (!string.IsNullOrWhiteSpace(txtISBN.Text))
            {
                parameters.Add($"isbn={HttpUtility.UrlEncode(txtISBN.Text.Trim())}");
            }

            if (selectedCategories.Any())
            {
                // URL 儲存格式: "ID1,ID2"
                parameters.Add($"cat={string.Join(",", selectedCategories)}");
            }
        }

        // 排序和分頁
        if (SortExpression != "BookID" || SortDirection != "ASC")
        {
            parameters.Add($"sort={SortExpression}");
            parameters.Add($"dir={SortDirection}");
        }
        if (CurrentPageSize != 10)
        {
            parameters.Add($"size={CurrentPageSize}");
        }

        string queryString = parameters.Any() ? "?" + string.Join("&", parameters) : string.Empty;
        Response.Redirect(Request.Path + queryString);
    }

    private int GetMaxBooksPerUser()
    {
        var config = Application[AppConfigKey] as Dictionary<string, string>;

        if (config != null && config.ContainsKey("MaxBooksPerUser") && int.TryParse(config["MaxBooksPerUser"], out int maxBooks))
        {
            return maxBooks;
        }

        return 5;
    }

    private int GetCurrentBorrowedCount(string userName)
    {
        string connString = GetConnectionString();
        string sql = "SELECT BorrowedBookCount FROM Users WHERE UserName = @UserName";

        using (SQLiteConnection conn = new SQLiteConnection(connString))
        using (SQLiteCommand cmd = new SQLiteCommand(sql, conn))
        {
            cmd.Parameters.AddWithValue("@UserName", userName);
            conn.Open();
            object result = cmd.ExecuteScalar();
            return (result != null && result != DBNull.Value) ? Convert.ToInt32(result) : 0;
        }
    }

    // 綁定可用類別到 DropDownList
    private void BindCategories(string searchKeyword = "")
    {
        DataTable dt = new DataTable();
        string connString = GetConnectionString();
        string sql = "SELECT CategoryID, CategoryName FROM Categories WHERE 1=1 ";

        List<SQLiteParameter> parameters = new List<SQLiteParameter>();

        if (!string.IsNullOrWhiteSpace(searchKeyword))
        {
            sql += " AND CategoryName LIKE @Keyword ";
            parameters.Add(new SQLiteParameter("@Keyword", $"%{searchKeyword.Trim()}%"));
        }

        sql += " ORDER BY CategoryName";

        using (SQLiteConnection conn = new SQLiteConnection(connString))
        using (SQLiteCommand cmd = new SQLiteCommand(sql, conn))
        {
            cmd.Parameters.AddRange(parameters.ToArray());
            conn.Open();
            SQLiteDataAdapter da = new SQLiteDataAdapter(cmd);
            da.Fill(dt);
        }

        ddlAvailableCategories.DataSource = dt;
        ddlAvailableCategories.DataTextField = "CategoryName";
        ddlAvailableCategories.DataValueField = "CategoryID";
        ddlAvailableCategories.DataBind();
        ddlAvailableCategories.Items.Insert(0, new ListItem("-- 請選擇類別 --", "0"));
    }

    protected void btnFilterCategories_Click(object sender, EventArgs e)
    {
        BindCategories(txtCategorySearch.Text);
        pnlAdvancedSearch.Visible = true;
        hidPanelVisible.Value = "true";
    }

    private void BindBookData()
    {
        DataTable dt = new DataTable();
        string connString = GetConnectionString();
        StringBuilder sqlBuilder = new StringBuilder();

        bool isAdvancedSearchActive =
            !string.IsNullOrWhiteSpace(txtBookID.Text) ||
            !string.IsNullOrWhiteSpace(txtTitle.Text) ||
            !string.IsNullOrWhiteSpace(txtAuthor.Text) ||
            !string.IsNullOrWhiteSpace(txtISBN.Text) ||
            GetSelectedCategoryIDs().Any();

        sqlBuilder.Append(@"
            SELECT 
                B.BookID, 
                B.Title, 
                B.Author, 
                B.ISBN, 
                B.TotalCopies, 
                B.AvailableCopies,
                GROUP_CONCAT(C.CategoryName) AS Categories
            FROM Books B
            LEFT JOIN CategoryRecords CR ON B.BookID = CR.BookID
            LEFT JOIN Categories C ON CR.CategoryID = C.CategoryID
            WHERE 1=1
        ");

        List<SQLiteParameter> parameters = new List<SQLiteParameter>();

        // 1. 快速查詢條件 (優先)
        if (!string.IsNullOrWhiteSpace(txtQuickSearch.Text))
        {
            string searchTerm = $"%{txtQuickSearch.Text.Trim()}%";
            sqlBuilder.Append(" AND (B.Title LIKE @SearchTerm OR B.Author LIKE @SearchTerm OR B.ISBN LIKE @SearchTerm)");
            parameters.Add(new SQLiteParameter("@SearchTerm", searchTerm));
        }
        // 2. 進階查詢條件
        else if (isAdvancedSearchActive)
        {
            if (!string.IsNullOrWhiteSpace(txtBookID.Text))
            {
                // BookID 使用精確匹配
                sqlBuilder.Append(" AND B.BookID = @BookID");
                parameters.Add(new SQLiteParameter("@BookID", txtBookID.Text.Trim()));
            }
            if (!string.IsNullOrWhiteSpace(txtTitle.Text))
            {
                sqlBuilder.Append(" AND B.Title LIKE @Title");
                parameters.Add(new SQLiteParameter("@Title", $"%{txtTitle.Text.Trim()}%"));
            }
            if (!string.IsNullOrWhiteSpace(txtAuthor.Text))
            {
                sqlBuilder.Append(" AND B.Author LIKE @Author");
                parameters.Add(new SQLiteParameter("@Author", $"%{txtAuthor.Text.Trim()}%"));
            }
            if (!string.IsNullOrWhiteSpace(txtISBN.Text))
            {
                sqlBuilder.Append(" AND B.ISBN LIKE @ISBN");
                parameters.Add(new SQLiteParameter("@ISBN", $"%{txtISBN.Text.Trim()}%"));
            }

            var selectedCategories = GetSelectedCategoryIDs();

            if (selectedCategories.Any())
            {
                string categoryPlaceholders = string.Join(",", selectedCategories.Select((id, index) => $"@CategoryID{index}"));
                sqlBuilder.Append($" AND C.CategoryID IN ({categoryPlaceholders})");

                for (int i = 0; i < selectedCategories.Count; i++)
                {
                    parameters.Add(new SQLiteParameter($"@CategoryID{i}", selectedCategories[i]));
                }
            }
        }

        sqlBuilder.Append(" GROUP BY B.BookID, B.Title, B.Author, B.ISBN, B.TotalCopies, B.AvailableCopies ");

        string finalSortExpression = SortExpression;
        if (finalSortExpression.Equals("BookID", StringComparison.OrdinalIgnoreCase) ||
            finalSortExpression.Equals("Title", StringComparison.OrdinalIgnoreCase) ||
            finalSortExpression.Equals("Author", StringComparison.OrdinalIgnoreCase) ||
            finalSortExpression.Equals("ISBN", StringComparison.OrdinalIgnoreCase) ||
            finalSortExpression.Equals("TotalCopies", StringComparison.OrdinalIgnoreCase) ||
            finalSortExpression.Equals("AvailableCopies", StringComparison.OrdinalIgnoreCase))
        {
            finalSortExpression = "B." + finalSortExpression;
        }

        sqlBuilder.Append($" ORDER BY {finalSortExpression} {SortDirection}");

        using (SQLiteConnection conn = new SQLiteConnection(connString))
        using (SQLiteCommand cmd = new SQLiteCommand(sqlBuilder.ToString(), conn))
        {
            cmd.Parameters.AddRange(parameters.ToArray());
            conn.Open();
            SQLiteDataAdapter da = new SQLiteDataAdapter(cmd);
            da.Fill(dt);
        }

        gvBooks.DataSource = dt;
        gvBooks.DataBind();

        if (dt.Rows.Count == 0 && (!string.IsNullOrWhiteSpace(txtQuickSearch.Text) || isAdvancedSearchActive))
        {
            lblResultInfo.Text = "找不到符合條件的書籍。請嘗試其他關鍵字。";
            lblResultInfo.CssClass += " message-error";
        }
        else
        {
            lblResultInfo.Text = "";
            lblResultInfo.CssClass = "result-message";
        }
    }

    protected void btnQuickSearch_Click(object sender, EventArgs e)
    {
        // 快速搜尋：清除進階面板的欄位
        txtBookID.Text = string.Empty;
        txtTitle.Text = string.Empty;
        txtAuthor.Text = string.Empty;
        txtISBN.Text = string.Empty;
        hidSelectedCategories.Value = string.Empty;

        SaveSearchParameters();
    }

    protected void btnAdvancedSearch_Click(object sender, EventArgs e)
    {
        // 進階搜尋：清除快速搜尋欄位
        txtQuickSearch.Text = string.Empty;

        SaveSearchParameters();
    }

    protected void ddlPageSize_SelectedIndexChanged(object sender, EventArgs e)
    {
        CurrentPageSize = Convert.ToInt32(ddlPageSize.SelectedValue);
        gvBooks.PageSize = CurrentPageSize;
        SaveSearchParameters();
    }

    protected void gvBooks_PageIndexChanging(object sender, GridViewPageEventArgs e)
    {
        gvBooks.PageIndex = e.NewPageIndex;
        BindBookData();
    }

    protected void gvBooks_Sorting(object sender, GridViewSortEventArgs e)
    {
        if (e.SortExpression == SortExpression)
        {
            SortDirection = (SortDirection == "ASC") ? "DESC" : "ASC";
        }
        else
        {
            SortExpression = e.SortExpression;
            SortDirection = "ASC";
        }

        SaveSearchParameters();
    }

    protected void gvBooks_RowCommand(object sender, GridViewCommandEventArgs e)
    {
        lblResultInfo.Text = "";
        lblResultInfo.CssClass = "result-message";

        if (e.CommandName == "Borrow")
        {
            if (!User.Identity.IsAuthenticated)
            {
                lblResultInfo.Text = "請先登入才能借閱書籍。";
                lblResultInfo.CssClass += " message-error";
                return;
            }

            int bookID = Convert.ToInt32(e.CommandArgument);
            string userName = User.Identity.Name;
            int userID = GetUserIDByUserName(userName);

            int maxBooks = GetMaxBooksPerUser();
            int currentCount = GetCurrentBorrowedCount(userName);

            if (currentCount >= maxBooks)
            {
                lblResultInfo.Text = $"借閱失敗：您已達到借閱上限 ({maxBooks} 本)。請先歸還書籍。";
                lblResultInfo.CssClass += " message-error";
                return;
            }

            int availableCopies = GetAvailableCopies(bookID);
            if (availableCopies <= 0)
            {
                lblResultInfo.Text = "借閱失敗：該書籍目前無庫存可供借閱。";
                lblResultInfo.CssClass += " message-error";
                return;
            }

            PerformBorrowTransaction(bookID, userID);
        }
    }

    private int GetUserIDByUserName(string userName)
    {
        string connString = GetConnectionString();
        string sql = "SELECT UserID FROM Users WHERE UserName = @UserName";

        using (SQLiteConnection conn = new SQLiteConnection(connString))
        using (SQLiteCommand cmd = new SQLiteCommand(sql, conn))
        {
            cmd.Parameters.AddWithValue("@UserName", userName);
            conn.Open();
            object result = cmd.ExecuteScalar();
            return (result != null && result != DBNull.Value) ? Convert.ToInt32(result) : -1;
        }
    }

    private int GetAvailableCopies(int bookID)
    {
        string connString = GetConnectionString();
        string sql = "SELECT AvailableCopies FROM Books WHERE BookID = @BookID";

        using (SQLiteConnection conn = new SQLiteConnection(connString))
        using (SQLiteCommand cmd = new SQLiteCommand(sql, conn))
        {
            cmd.Parameters.AddWithValue("@BookID", bookID);
            conn.Open();
            object result = cmd.ExecuteScalar();
            return (result != null && result != DBNull.Value) ? Convert.ToInt32(result) : 0;
        }
    }

    private void PerformBorrowTransaction(int bookID, int userID)
    {
        string connString = GetConnectionString();

        int lendingPeriodDays = 14;
        var config = Application[AppConfigKey] as Dictionary<string, string>;
        if (config != null && config.ContainsKey("LendingPeriodDays") && int.TryParse(config["LendingPeriodDays"], out int days))
        {
            lendingPeriodDays = days;
        }

        using (SQLiteConnection conn = new SQLiteConnection(connString))
        {
            conn.Open();
            using (SQLiteTransaction transaction = conn.BeginTransaction())
            {
                try
                {
                    DateTime borrowDate = DateTime.Now;
                    DateTime dueDate = borrowDate.AddDays(lendingPeriodDays);

                    string sqlLend = "INSERT INTO LendRecords (BookID, UserID, BorrowDate, DueDate) VALUES (@BookID, @UserID, @BorrowDate, @DueDate)";
                    using (SQLiteCommand cmdLend = new SQLiteCommand(sqlLend, conn, transaction))
                    {
                        cmdLend.Parameters.AddWithValue("@BookID", bookID);
                        cmdLend.Parameters.AddWithValue("@UserID", userID);
                        cmdLend.Parameters.AddWithValue("@BorrowDate", borrowDate.ToString("yyyy-MM-dd HH:mm:ss"));
                        cmdLend.Parameters.AddWithValue("@DueDate", dueDate.ToString("yyyy-MM-dd"));
                        cmdLend.ExecuteNonQuery();
                    }

                    transaction.Commit();
                    lblResultInfo.Text = $"成功借閱書籍！ (應還日期: {dueDate.ToString("yyyy-MM-dd")})";
                    lblResultInfo.CssClass += " message-success";
                    BindBookData();
                }
                catch (SQLiteException ex) when (ex.Message.Contains("ABORT") || ex.Message.Contains("The book is currently out of stock or reserved."))
                {
                    transaction.Rollback();
                    lblResultInfo.Text = "借閱失敗：該書籍目前無庫存可供借閱 (資料庫檢查)。";
                    lblResultInfo.CssClass += " message-error";
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    lblResultInfo.Text = "借閱處理發生錯誤：" + ex.Message;
                    lblResultInfo.CssClass += " message-error";
                }
            }
        }
    }
}