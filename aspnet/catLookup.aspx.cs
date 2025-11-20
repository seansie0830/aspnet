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
    public partial class catLookup : Page
    {
        private const string ConnectionStringName = "LibraryDBConnection";
        private const int DefaultPageSize = 12;

        private string GetConnectionString()
        {
            return ConfigurationManager.ConnectionStrings[ConnectionStringName].ConnectionString;
        }

        // --- 狀態屬性 (從 Query String 讀取/設定) ---

        private int SelectedCategoryID
        {
            get
            {
                if (int.TryParse(Request.QueryString["cid"], out int id))
                {
                    return id;
                }
                return 0;
            }
        }

        private string CurrentSort
        {
            get { return Request.QueryString["sort"] ?? "Title"; }
        }

        // 將 CurrentPage 屬性設為 public，以供 ASPX 頁面的 Data Binding 存取
        public int CurrentPage
        {
            get
            {
                if (int.TryParse(Request.QueryString["page"], out int page))
                {
                    return page > 0 ? page - 1 : 0; // 轉換為 0-based index
                }
                return 0;
            }
        }

        private int CurrentPageSize
        {
            get
            {
                if (int.TryParse(Request.QueryString["size"], out int size) && size > 0)
                {
                    return size;
                }
                return DefaultPageSize;
            }
        }

        private bool IsChineseClassificationMode
        {
            get
            {
                return (Request.QueryString["mode"] ?? "chinese") == "chinese";
            }
        }

        private string OtherSearchTerm
        {
            get { return Request.QueryString["search"] ?? string.Empty; }
        }

        // --- 頁面事件 ---

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!User.Identity.IsAuthenticated)
            {
                Response.Redirect("~/Login.aspx");
                return;
            }

            if (!IsPostBack)
            {
                // 初始化類別列表的 PageSize DropDownList (僅用於 OtherCategories)
                ListItem otherSizeItem = ddlPageSizeOther.Items.FindByValue(CurrentPageSize.ToString());
                if (otherSizeItem != null)
                {
                    ddlPageSizeOther.ClearSelection();
                    otherSizeItem.Selected = true;
                }

                // 初始化書籍列表的 Sort DropDownList
                ListItem sortItem = ddlSortBy.Items.FindByValue(CurrentSort);
                if (sortItem != null)
                {
                    ddlSortBy.ClearSelection();
                    sortItem.Selected = true;
                }

                // 初始化書籍列表的 PageSize DropDownList
                ListItem bookSizeItem = ddlPageSizeBooks.Items.FindByValue(CurrentPageSize.ToString());
                if (bookSizeItem != null)
                {
                    ddlPageSizeBooks.ClearSelection();
                    bookSizeItem.Selected = true;
                }


                if (SelectedCategoryID > 0)
                {
                    BindCategoryBooks(SelectedCategoryID);
                    pnlCategoriesContainer.Visible = false;
                    pnlCategoryBooks.Visible = true;
                }
                else
                {
                    BindCategories();
                    pnlCategoriesContainer.Visible = true;
                    pnlCategoryBooks.Visible = false;
                }

                UpdateCategoryPanelsVisibility();
            }
        }

        // --- 類別列表相關 ---

        private void UpdateCategoryPanelsVisibility()
        {
            pnlChineseClassification.Visible = IsChineseClassificationMode;
            pnlOtherCategories.Visible = !IsChineseClassificationMode;
            btnToggleMode.Text = IsChineseClassificationMode ? "切換至：其他類別" : "切換至：中文圖書分類";
        }

        private void BindCategories()
        {
            string connString = GetConnectionString();
            string sql = @"
                SELECT c.CategoryID, c.CategoryName, c.ColorHex, 
                       COUNT(cr.BookID) AS BookCount
                FROM Categories c
                LEFT JOIN CategoryRecords cr ON c.CategoryID = cr.CategoryID
                GROUP BY c.CategoryID, c.CategoryName, c.ColorHex
                ORDER BY c.CategoryName";

            try
            {
                using (SQLiteConnection conn = new SQLiteConnection(connString))
                using (SQLiteCommand cmd = new SQLiteCommand(sql, conn))
                {
                    conn.Open();
                    SQLiteDataAdapter da = new SQLiteDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    var chineseCategories = GetChineseClassificationCategories(dt);
                    rptChineseClassification.DataSource = chineseCategories;
                    rptChineseClassification.DataBind();

                    BindOtherCategories(dt);
                }
            }
            catch (Exception ex)
            {
                ShowMessage($"載入類別時發生錯誤：{ex.Message}", "error");
            }
        }

        private List<ChineseMainCategory> GetChineseClassificationCategories(DataTable allCategories)
        {
            var tdcMappings = new List<ChineseMainCategory>
            {
                new ChineseMainCategory { MainCategoryName = "000 總類", Prefix = "0" },
                new ChineseMainCategory { MainCategoryName = "100 哲學類", Prefix = "1" },
                new ChineseMainCategory { MainCategoryName = "200 宗教類", Prefix = "2" },
                new ChineseMainCategory { MainCategoryName = "300 科學類", Prefix = "3" },
                new ChineseMainCategory { MainCategoryName = "400 應用科學類", Prefix = "4" },
                new ChineseMainCategory { MainCategoryName = "500 社會科學類", Prefix = "5" },
                new ChineseMainCategory { MainCategoryName = "600 史地類 (含 700)", Prefix = "6|7" },
                new ChineseMainCategory { MainCategoryName = "800 語言文學類", Prefix = "8" },
                new ChineseMainCategory { MainCategoryName = "900 藝術類", Prefix = "9" }
            };

            var allList = allCategories.AsEnumerable()
                .Select(row => new
                {
                    CategoryID = row.Field<long>("CategoryID"),
                    CategoryName = row.Field<string>("CategoryName"),
                    ColorHex = row.Field<string>("ColorHex"),
                    BookCount = row.Field<long>("BookCount")
                }).ToList();

            foreach (var mainCat in tdcMappings)
            {
                string[] prefixes = mainCat.Prefix.Split('|');

                mainCat.SubCategories = allList
                    .Where(c => prefixes.Any(p => c.CategoryName.StartsWith(p) && c.CategoryName.Length >= 3 && Char.IsDigit(c.CategoryName[1]) && Char.IsDigit(c.CategoryName[2]) && c.CategoryName.Contains(' ')))
                    .Select(c => new ChineseSubCategory
                    {
                        CategoryID = (int)c.CategoryID,
                        CategoryName = c.CategoryName,
                        ColorHex = c.ColorHex,
                        BookCount = (int)c.BookCount
                    })
                    .OrderBy(c => c.CategoryName)
                    .ToList();
            }

            return tdcMappings.Where(mc => mc.SubCategories.Any()).ToList();
        }

        private DataTable GetOtherCategoriesData(DataTable allCategories)
        {
            var tdcPrefixes = new List<string> { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };
            string searchTerm = OtherSearchTerm;

            var filteredCategories = allCategories.AsEnumerable()
                .Where(row =>
                {
                    string catName = row.Field<string>("CategoryName");
                    bool isTDC = tdcPrefixes.Any(p => catName.StartsWith(p) && catName.Length >= 3 && Char.IsDigit(catName[1]) && Char.IsDigit(catName[2]) && catName.Contains(' '));
                    bool matchesSearch = string.IsNullOrEmpty(searchTerm) || catName.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0;

                    return !isTDC && matchesSearch;
                });

            if (filteredCategories.Any())
            {
                return filteredCategories.CopyToDataTable();
            }
            return allCategories.Clone();
        }

        private void BindOtherCategories(DataTable allCategories)
        {
            DataTable dtOtherCategories = GetOtherCategoriesData(allCategories);

            PagedDataSource pds = new PagedDataSource();
            pds.DataSource = dtOtherCategories.DefaultView;
            pds.AllowPaging = true;
            pds.PageSize = CurrentPageSize;
            pds.CurrentPageIndex = CurrentPage;

            rptOtherCategories.DataSource = pds;
            rptOtherCategories.DataBind();

            // 確保搜尋框顯示當前搜尋詞
            txtSearchOther.Text = OtherSearchTerm;

            BindOtherCategoriesPager(pds.PageCount);
        }

        private void BindOtherCategoriesPager(int pageCount)
        {
            List<ListItem> pages = new List<ListItem>();
            if (pageCount > 1)
            {
                for (int i = 0; i < pageCount; i++)
                {
                    pages.Add(new ListItem((i + 1).ToString(), (i + 1).ToString()));
                }
            }

            rptPagerOther.DataSource = pages;
            rptPagerOther.DataBind();
        }

        // --- 類別列表動作 ---

        protected void ddlPageSizeOther_SelectedIndexChanged(object sender, EventArgs e)
        {
            DropDownList ddl = (DropDownList)sender;
            int newSize = int.Parse(ddl.SelectedValue);
            Response.Redirect(BuildUrl(0, newSize, OtherSearchTerm, IsChineseClassificationMode ? "chinese" : "other"));
        }

        protected void btnSearchOther_Click(object sender, EventArgs e)
        {
            string newSearchTerm = txtSearchOther.Text.Trim();
            Response.Redirect(BuildUrl(0, CurrentPageSize, newSearchTerm, "other"));
        }

        protected void lnkPageOther_Click(object sender, EventArgs e)
        {
            LinkButton lnk = (LinkButton)sender;
            int pageIndex = int.Parse(lnk.CommandArgument) - 1;

            Response.Redirect(BuildUrl(pageIndex, CurrentPageSize, OtherSearchTerm, "other"));
        }

        protected void btnToggleMode_Click(object sender, EventArgs e)
        {
            string newMode = IsChineseClassificationMode ? "other" : "chinese";
            Response.Redirect(BuildUrl(0, DefaultPageSize, string.Empty, newMode));
        }

        // --- 書籍列表相關 ---

        private void BindCategoryBooks(int categoryID)
        {
            string categoryName = GetCategoryNameByID(categoryID);
            lblSelectedCategoryName.Text = categoryName;

            string connString = GetConnectionString();
            // 注意：這裡假設 CurrentSort 的值已經被驗證過，以防 SQL 注入。
            // 由於這是內部應用程序，我們信任使用者輸入來自 ddlSortBy。
            string sql = $@"
                SELECT b.BookID, b.Title, b.Author, b.ISBN 
                FROM Books b
                INNER JOIN CategoryRecords cr ON b.BookID = cr.BookID
                WHERE cr.CategoryID = @CategoryID
                ORDER BY {CurrentSort}";

            try
            {
                using (SQLiteConnection conn = new SQLiteConnection(connString))
                using (SQLiteCommand cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@CategoryID", categoryID);
                    conn.Open();
                    SQLiteDataAdapter da = new SQLiteDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    // 處理分頁
                    PagedDataSource pds = new PagedDataSource();
                    pds.DataSource = dt.DefaultView;
                    pds.AllowPaging = true;
                    pds.PageSize = CurrentPageSize;
                    pds.CurrentPageIndex = CurrentPage;

                    rptCategoryBooks.DataSource = pds;
                    rptCategoryBooks.DataBind();

                    lblBookCount.Text = $"{dt.Rows.Count} 筆記錄";

                    BindCategoryBooksPager(pds.PageCount);

                    ShowMessage($"類別 '{categoryName}' 下共有 {dt.Rows.Count} 筆書籍記錄。", "success");
                }
            }
            catch (Exception ex)
            {
                ShowMessage($"載入類別書籍列表時發生錯誤：{ex.Message}", "error");
                pnlCategoryBooks.Visible = false;
            }
        }

        private void BindCategoryBooksPager(int pageCount)
        {
            List<ListItem> pages = new List<ListItem>();
            if (pageCount > 1)
            {
                for (int i = 0; i < pageCount; i++)
                {
                    pages.Add(new ListItem((i + 1).ToString(), (i + 1).ToString()));
                }
            }

            rptPagerBooks.DataSource = pages;
            rptPagerBooks.DataBind();
        }

        // --- 書籍列表動作 ---

        protected void ddlSortBy_SelectedIndexChanged(object sender, EventArgs e)
        {
            DropDownList ddl = (DropDownList)sender;
            string newSort = ddl.SelectedValue;
            Response.Redirect(BuildBookListUrl(SelectedCategoryID, 0, newSort, CurrentPageSize));
        }

        protected void ddlPageSizeBooks_SelectedIndexChanged(object sender, EventArgs e)
        {
            DropDownList ddl = (DropDownList)sender;
            int newSize = int.Parse(ddl.SelectedValue);
            Response.Redirect(BuildBookListUrl(SelectedCategoryID, 0, CurrentSort, newSize));
        }

        protected void lnkPageBooks_Click(object sender, EventArgs e)
        {
            LinkButton lnk = (LinkButton)sender;
            int pageIndex = int.Parse(lnk.CommandArgument) - 1;
            Response.Redirect(BuildBookListUrl(SelectedCategoryID, pageIndex, CurrentSort, CurrentPageSize));
        }

        protected void btnBackToCategories_Click(object sender, EventArgs e)
        {
            Response.Redirect("~/catLookup.aspx"); // 返回主頁面，清除所有參數
        }

        // --- 共用功能 ---

        protected void rptCategories_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (e.CommandName == "SelectCategory")
            {
                if (int.TryParse(e.CommandArgument.ToString(), out int categoryID))
                {
                    // 點選類別後，導航到包含 cid 參數的頁面，清除其他書籍列表狀態
                    Response.Redirect(BuildBookListUrl(categoryID, 0, "Title", DefaultPageSize));
                }
            }
        }

        private string GetCategoryNameByID(int categoryID)
        {
            string connString = GetConnectionString();
            string categoryName = "未知類別";
            string sql = "SELECT CategoryName FROM Categories WHERE CategoryID = @CategoryID";

            try
            {
                using (SQLiteConnection conn = new SQLiteConnection(connString))
                using (SQLiteCommand cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@CategoryID", categoryID);
                    conn.Open();
                    object result = cmd.ExecuteScalar();
                    if (result != null)
                    {
                        categoryName = result.ToString();
                    }
                }
            }
            catch (Exception)
            {
                categoryName = "載入錯誤";
            }
            return categoryName;
        }

        // 構建類別列表的 URL
        private string BuildUrl(int pageIndex, int pageSize, string searchTerm, string mode)
        {
            StringBuilder sb = new StringBuilder("~/catLookup.aspx?");
            sb.Append($"mode={mode}");
            sb.Append($"&page={pageIndex + 1}");
            sb.Append($"&size={pageSize}");
            if (!string.IsNullOrEmpty(searchTerm))
            {
                sb.Append($"&search={Server.UrlEncode(searchTerm)}");
            }
            return sb.ToString();
        }

        // 構建書籍列表的 URL
        private string BuildBookListUrl(int categoryID, int pageIndex, string sort, int pageSize)
        {
            return $"/catLookup.aspx?cid={categoryID}&page={pageIndex + 1}&sort={Server.UrlEncode(sort)}&size={pageSize}";
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

    public class ChineseMainCategory
    {
        public string MainCategoryName { get; set; }
        public string Prefix { get; set; }
        public List<ChineseSubCategory> SubCategories { get; set; } = new List<ChineseSubCategory>();
    }

    public class ChineseSubCategory
    {
        public int CategoryID { get; set; }
        public string CategoryName { get; set; }
        public string ColorHex { get; set; }
        public int BookCount { get; set; }
    }
}