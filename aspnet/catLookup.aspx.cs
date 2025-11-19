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

        private string GetConnectionString()
        {
            return ConfigurationManager.ConnectionStrings[ConnectionStringName].ConnectionString;
        }

        private int SelectedCategoryID
        {
            get { return (int)(ViewState["SelectedCategoryID"] ?? 0); }
            set { ViewState["SelectedCategoryID"] = value; }
        }

        private string SelectedCategoryName
        {
            get { return ViewState["SelectedCategoryName"] as string ?? "所有類別"; }
            set { ViewState["SelectedCategoryName"] = value; }
        }

        private bool IsChineseClassificationMode
        {
            get { return (bool)(ViewState["IsChineseClassificationMode"] ?? true); }
            set { ViewState["IsChineseClassificationMode"] = value; }
        }

        public int CurrentPage_Other
        {
            get { return (int)(ViewState["CurrentPage_Other"] ?? 0); }
            set { ViewState["CurrentPage_Other"] = value; }
        }

        private string SearchTerm_Other
        {
            get { return ViewState["SearchTerm_Other"] as string ?? string.Empty; }
            set { ViewState["SearchTerm_Other"] = value; }
        }

        public int PageSize_Other_Setting
        {
            get { return (int)(ViewState["PageSize_Other_Setting"] ?? 12); }
            set { ViewState["PageSize_Other_Setting"] = value; }
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

                // 初始化 PageSize DropDownList
                ddlPageSizeOther.SelectedValue = PageSize_Other_Setting.ToString();

                BindCategories();
                pnlCategoryBooks.Visible = false;
                UpdateCategoryPanelsVisibility();
            }
            else if (pnlOtherCategories.Visible)
            {
                txtSearchOther.Text = SearchTerm_Other;
            }
        }

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
            string searchTerm = SearchTerm_Other;

            var filteredCategories = allCategories.AsEnumerable()
                .Where(row =>
                {
                    string catName = row.Field<string>("CategoryName");
                    // 1. 判斷是否為中文圖書分類 (TDC)
                    bool isTDC = tdcPrefixes.Any(p => catName.StartsWith(p) && catName.Length >= 3 && Char.IsDigit(catName[1]) && Char.IsDigit(catName[2]) && catName.Contains(' '));

                    // 2. 判斷是否符合搜尋詞
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
            pds.PageSize = PageSize_Other_Setting;
            pds.CurrentPageIndex = CurrentPage_Other;

            rptOtherCategories.DataSource = pds;
            rptOtherCategories.DataBind();

            // 設定 DropDownList 的選定值
            ListItem selectedItem = ddlPageSizeOther.Items.FindByValue(PageSize_Other_Setting.ToString());
            if (selectedItem != null)
            {
                ddlPageSizeOther.ClearSelection();
                selectedItem.Selected = true;
            }

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

        protected void ddlPageSizeOther_SelectedIndexChanged(object sender, EventArgs e)
        {
            DropDownList ddl = (DropDownList)sender;
            PageSize_Other_Setting = int.Parse(ddl.SelectedValue);
            CurrentPage_Other = 0;
            BindCategories();
            UpdateCategoryPanelsVisibility();
        }

        protected void btnSearchOther_Click(object sender, EventArgs e)
        {
            CurrentPage_Other = 0;
            SearchTerm_Other = txtSearchOther.Text.Trim();
            BindCategories();
            UpdateCategoryPanelsVisibility();
        }

        protected void lnkPageOther_Click(object sender, EventArgs e)
        {
            LinkButton lnk = (LinkButton)sender;
            int pageIndex = int.Parse(lnk.CommandArgument) - 1;

            CurrentPage_Other = pageIndex;
            BindCategories();
            UpdateCategoryPanelsVisibility();
        }

        protected void rptChineseClassification_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            rptCategories_ItemCommand(source, e);
        }

        protected void rptOtherCategories_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            rptCategories_ItemCommand(source, e);
        }

        protected void rptCategories_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (e.CommandName == "SelectCategory")
            {
                if (int.TryParse(e.CommandArgument.ToString(), out int categoryID))
                {
                    string categoryName = GetCategoryNameByID(categoryID);

                    SelectedCategoryID = categoryID;
                    SelectedCategoryName = categoryName;

                    BindCategoryBooks(categoryID);
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
            catch (Exception ex)
            {
                ShowMessage($"獲取類別名稱時發生錯誤：{ex.Message}", "error");
            }

            return categoryName;
        }

        private void BindCategoryBooks(int categoryID)
        {
            if (categoryID <= 0)
            {
                pnlCategoryBooks.Visible = false;
                ShowMessage("請先從上方選擇一個類別。", "info");
                return;
            }

            string connString = GetConnectionString();
            string sql = @"
                SELECT b.BookID, b.Title, b.Author, b.ISBN 
                FROM Books b
                INNER JOIN CategoryRecords cr ON b.BookID = cr.BookID
                WHERE cr.CategoryID = @CategoryID
                ORDER BY b.Title";

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

                    lblSelectedCategoryName.Text = SelectedCategoryName;
                    rptCategoryBooks.DataSource = dt;
                    rptCategoryBooks.DataBind();

                    pnlCategoryBooks.Visible = true;
                    pnlCategoriesContainer.Visible = false;

                    ShowMessage($"類別 '{SelectedCategoryName}' 下共有 {dt.Rows.Count} 筆書籍記錄。", "success");
                }
            }
            catch (Exception ex)
            {
                ShowMessage($"載入類別書籍列表時發生錯誤：{ex.Message}", "error");
                pnlCategoryBooks.Visible = false;
            }
        }

        protected void btnBackToCategories_Click(object sender, EventArgs e)
        {
            SelectedCategoryID = 0;
            SelectedCategoryName = "所有類別";
            pnlCategoryBooks.Visible = false;
            pnlCategoriesContainer.Visible = true;
            BindCategories();
            UpdateCategoryPanelsVisibility();
            ShowMessage("已返回類別列表。", "info");
        }

        protected void btnToggleMode_Click(object sender, EventArgs e)
        {
            IsChineseClassificationMode = !IsChineseClassificationMode;
            UpdateCategoryPanelsVisibility();
            ShowMessage(IsChineseClassificationMode ? "已切換至「中文圖書分類」模式。" : "已切換至「其他類別」模式。", "info");
            BindCategories();
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