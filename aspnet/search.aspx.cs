using System;
using System.Data;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data.SQLite;
using System.Configuration;
using System.Text;
using System.Web.Security;

public partial class Search : Page
{
    private string SortExpression
    {
        get
        {
            return ViewState["SortExpression"] as string ??
"BookID";
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
            return ViewState["SortDirection"] as string ??
"ASC";
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
            // 初始化 PageSize 並將選單設定為該值
            if (ddlPageSize.Items.FindByValue(CurrentPageSize.ToString()) != null)
            {
                ddlPageSize.SelectedValue = CurrentPageSize.ToString();
            }
            gvBooks.PageSize = CurrentPageSize;
            BindBookData();
        }
        else
        {
            // 每次 PostBack 都確保 GridView PageSize 與 ViewState 一致
            gvBooks.PageSize = CurrentPageSize;
        }
    }

    protected void ddlPageSize_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (int.TryParse(ddlPageSize.SelectedValue, out int newPageSize))
        {
            CurrentPageSize = newPageSize;
            gvBooks.PageSize = newPageSize;
            gvBooks.PageIndex = 0; // 變更 PageSize 後重設頁碼
            BindBookData();
        }
    }

    protected void gvBooks_PageIndexChanging(object sender, GridViewPageEventArgs e)
    {
        gvBooks.PageIndex = e.NewPageIndex;
        BindBookData();
    }

    protected void gvBooks_Sorting(object sender, GridViewSortEventArgs e)
    {
        if (SortExpression == e.SortExpression)
        {
            SortDirection = (SortDirection == "ASC" ? "DESC" : "ASC");
        }
        else
        {
            SortExpression = e.SortExpression;
            SortDirection = "ASC";
        }

        gvBooks.PageIndex = 0;
        BindBookData();
    }

    protected void btnSearch_Click(object sender, EventArgs e)
    {
        gvBooks.PageIndex = 0;
        BindBookData();
    }
    protected void btnQuickSearch_Click(object sender, EventArgs e)
    {
        txtSearchTitle.Text = string.Empty;
        txtSearchAuthor.Text = string.Empty;
        txtSearchISBN.Text = string.Empty;

        gvBooks.PageIndex = 0;
        BindBookData();
    }


    protected void gvBooks_RowDataBound(object sender, GridViewRowEventArgs e)
    {
        if (e.Row.RowType == DataControlRowType.DataRow)
        {
            Button btnBorrow = (Button)e.Row.FindControl("btnBorrow");
            if (btnBorrow != null)
            {
                if (!User.Identity.IsAuthenticated)
                {
                    btnBorrow.Visible = false;
                }
                else
                {
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
                if (!string.IsNullOrWhiteSpace(quickKeyword))
                {
                    whereClause.Append(" (Title LIKE @Keyword OR Author LIKE @Keyword OR ISBN LIKE @Keyword) AND ");
                    cmd.Parameters.AddWithValue("@Keyword", "%" + quickKeyword + "%");

                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(txtSearchTitle.Text))
                    {

                        whereClause.Append(" Title LIKE @TitleKeyword AND ");
                        cmd.Parameters.AddWithValue("@TitleKeyword", "%" + txtSearchTitle.Text.Trim() + "%");
                    }

                    if (!string.IsNullOrWhiteSpace(txtSearchAuthor.Text))
                    {
                        whereClause.Append(" Author LIKE @AuthorKeyword AND ");
                        cmd.Parameters.AddWithValue("@AuthorKeyword", "%" + txtSearchAuthor.Text.Trim() + "%");
                    }

                    if (!string.IsNullOrWhiteSpace(txtSearchISBN.Text))
                    {
                        whereClause.Append(" ISBN = @ISBN AND ");
                        cmd.Parameters.AddWithValue("@ISBN", txtSearchISBN.Text.Trim());
                    }
                }

                if (chkAvailableOnly.Checked)
                {
                    whereClause.Append(" AvailableCopies > 0 AND ");
                }

                if (whereClause.Length > 0)
                {
                    whereClause.Length -= 4;
                    baseSql += " WHERE " + whereClause.ToString();
                }

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

    protected void gvBooks_RowCommand(object sender, GridViewCommandEventArgs e)
    {
        if (e.CommandName == "Borrow")
        {
            if (!User.Identity.IsAuthenticated)
            {
                lblResultInfo.Text = "錯誤：請先登入才能借閱書籍。";
                return;
            }

            int bookID;
            if (!int.TryParse(e.CommandArgument.ToString(), out bookID))
            {
                lblResultInfo.Text = "錯誤：書籍ID無效。";
                return;
            }

            string username = User.Identity.Name;
            string connString = GetConnectionString();

            using (SQLiteConnection conn = new SQLiteConnection(connString))
            {
                conn.Open();
                SQLiteTransaction transaction = conn.BeginTransaction();
                try
                {
                    // 1. 取得 UserID
                    int userID;
                    using (SQLiteCommand cmdUser = new SQLiteCommand("SELECT UserID FROM Users WHERE Username = @Username", conn, transaction))
                    {
                        cmdUser.Parameters.AddWithValue("@Username", username);
                        object result = cmdUser.ExecuteScalar();
                        if (result == null)
                        {
                            transaction.Rollback();
                            lblResultInfo.Text = "錯誤：找不到使用者帳號。";
                            return;
                        }
                        userID = Convert.ToInt32(result);
                    }

                    // 2. 檢查庫存 (使用 SELECT FOR UPDATE 概念，在 SQLite 中主要依靠 Transaction)
                    using (SQLiteCommand cmdCheck = new SQLiteCommand("SELECT AvailableCopies FROM Books WHERE BookID = @BookID", conn, transaction))
                    {

                        cmdCheck.Parameters.AddWithValue("@BookID", bookID);
                        object result = cmdCheck.ExecuteScalar();
                        if (result == null || Convert.ToInt32(result) <= 0)
                        {
                            transaction.Rollback();
                            lblResultInfo.Text = "借閱失敗：該書已無庫存可借閱。";
                            return;
                        }
                    }

                    // 3. 更新 Books (AvailableCopies - 1)
                    /*using (SQLiteCommand cmdUpdate = new SQLiteCommand("UPDATE Books SET AvailableCopies = AvailableCopies - 1 WHERE BookID = @BookID", conn, transaction))

                    {
                        cmdUpdate.Parameters.AddWithValue("@BookID", bookID);
                        cmdUpdate.ExecuteNonQuery();
                    }
                    */

                    // 4. 寫入 LendRecords 記錄
                    DateTime borrowDate = DateTime.Now;
                    DateTime dueDate = borrowDate.AddDays(14); // 預設借閱 14 天

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
                    lblResultInfo.Text = $"成功借閱書籍 (BookID: {bookID})，請在 {dueDate.ToString("yyyy-MM-dd")} 前歸還。";
                    // 重新綁定資料，更新顯示的可借閱數量
                    BindBookData();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    lblResultInfo.Text = "借閱處理發生錯誤：" + ex.Message;
                }
            }
        }
    }
}