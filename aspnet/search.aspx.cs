using System;
using System.Data;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data.SQLite;
using System.Configuration;
using System.Text;
using System.Web.Security;
using System.Collections.Generic;

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

            BindBookData();
            BindCategories();
        }
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

    private void BindCategories()
    {
        DataTable dt = new DataTable();
        string connString = GetConnectionString();
        string sql = "SELECT CategoryID, CategoryName FROM Categories ORDER BY CategoryName";

        using (SQLiteConnection conn = new SQLiteConnection(connString))
        using (SQLiteCommand cmd = new SQLiteCommand(sql, conn))
        {
            conn.Open();
            SQLiteDataAdapter da = new SQLiteDataAdapter(cmd);
            da.Fill(dt);
        }

        ddlCategory.DataSource = dt;
        ddlCategory.DataTextField = "CategoryName";
        ddlCategory.DataValueField = "CategoryID";
        ddlCategory.DataBind();

        ddlCategory.Items.Insert(0, new ListItem("所有類別", "0"));
    }

    private void BindBookData()
    {
        DataTable dt = new DataTable();
        string connString = GetConnectionString();
        StringBuilder sqlBuilder = new StringBuilder();
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

        // 1. 快速查詢條件
        if (!string.IsNullOrWhiteSpace(txtQuickSearch.Text))
        {
            string searchTerm = $"%{txtQuickSearch.Text.Trim()}%";
            sqlBuilder.Append(" AND (B.Title LIKE @SearchTerm OR B.Author LIKE @SearchTerm OR B.ISBN LIKE @SearchTerm)");
            parameters.Add(new SQLiteParameter("@SearchTerm", searchTerm));
        }

        // 2. 進階查詢條件 - 只有當面板可見時才應用這些篩選器
        if (pnlAdvancedSearch.Visible)
        {
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
            if (ddlCategory.SelectedValue != "0")
            {
                sqlBuilder.Append(" AND C.CategoryID = @CategoryID");
                parameters.Add(new SQLiteParameter("@CategoryID", ddlCategory.SelectedValue));
            }
        }

        sqlBuilder.Append(" GROUP BY B.BookID, B.Title, B.Author, B.ISBN, B.TotalCopies, B.AvailableCopies ");

        // 修正 SQL 歧義錯誤：為排序欄位加上表格別名 B.
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

        // 排序
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
    }

    protected void btnQuickSearch_Click(object sender, EventArgs e)
    {
        // 快速搜尋：隱藏進階面板，並執行查詢
        pnlAdvancedSearch.Visible = false;
        BindBookData();
        lblResultInfo.Text = "";
        lblResultInfo.CssClass = "result-message";
    }

    protected void btnAdvancedSearch_Click(object sender, EventArgs e)
    {
        // 進階搜尋：由於面板已經由 JavaScript 展開，我們只需要執行查詢即可
        BindBookData();
        lblResultInfo.Text = "";
        lblResultInfo.CssClass = "result-message";
    }

    protected void ddlPageSize_SelectedIndexChanged(object sender, EventArgs e)
    {
        CurrentPageSize = Convert.ToInt32(ddlPageSize.SelectedValue);
        gvBooks.PageSize = CurrentPageSize;
        BindBookData();
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
        BindBookData();
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
            int userID = GetUserIDByUserName(User.Identity.Name);
            string userName = User.Identity.Name;

            // 檢查借閱上限
            int maxBooks = GetMaxBooksPerUser();
            int currentCount = GetCurrentBorrowedCount(userName);

            if (currentCount >= maxBooks)
            {
                lblResultInfo.Text = $"借閱失敗：您已達到借閱上限 ({maxBooks} 本)。請先歸還書籍。";
                lblResultInfo.CssClass += " message-error";
                return;
            }

            // 檢查書籍是否可借
            int availableCopies = GetAvailableCopies(bookID);
            if (availableCopies <= 0)
            {
                lblResultInfo.Text = "借閱失敗：該書籍目前無庫存可供借閱。";
                lblResultInfo.CssClass += " message-error";
                return;
            }

            // 執行借閱交易
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
                catch (SQLiteException ex) when (ex.Message.Contains("AvailableCopies cannot be less than 0"))
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