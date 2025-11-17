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

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                if (!User.Identity.IsAuthenticated)
                {
                    Response.Redirect("~/Login.aspx");
                    return;
                }

                BindCategories();
                pnlCategoryBooks.Visible = false;
            }
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

                    rptCategories.DataSource = dt;
                    rptCategories.DataBind();
                }
            }
            catch (Exception ex)
            {
                ShowMessage($"載入類別時發生錯誤：{ex.Message}", "error");
            }
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
                    pnlCategories.Visible = false;

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
            pnlCategories.Visible = true;
            BindCategories();
            ShowMessage("已返回類別列表。", "info");
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