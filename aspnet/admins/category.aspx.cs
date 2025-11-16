using System;
using System.Data;
using System.Configuration;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data.SQLite;
using System.Text;
using System.Collections.Generic;
using System.Drawing;

namespace aspnet
{
    public partial class Categories : Page
    {
        private const string ConnectionStringName = "LibraryDBConnection";
        private string SortExpression
        {
            get { return ViewState["SortExpression"] as string ?? "CategoryID"; }
            set { ViewState["SortExpression"] = value; }
        }
        private SortDirection SortDirection
        {
            get { return (SortDirection)(ViewState["SortDirection"] ?? SortDirection.Ascending); }
            set { ViewState["SortDirection"] = value; }
        }

        private string GetConnectionString()
        {
            return ConfigurationManager.ConnectionStrings[ConnectionStringName].ConnectionString;
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

                if (!IsUserAdmin(User.Identity.Name))
                {
                    ShowMessage("存取遭拒：您不具備管理員權限。", "error");
                    Response.Redirect("~/MyHomepage.aspx?AccessDenied=True");
                    return;
                }

                if (Session["Categories_PageSize"] != null)
                {
                    gvCategories.PageSize = (int)Session["Categories_PageSize"];
                    ddlPageSize.SelectedValue = gvCategories.PageSize.ToString();
                }

                BindCategoriesData();
            }
        }

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
                    System.Diagnostics.Debug.WriteLine($"管理員檢查錯誤: {ex.Message}");
                }
            }
            return false;
        }

        private void BindCategoriesData()
        {
            string connString = GetConnectionString();
            DataTable dt = new DataTable();
            StringBuilder whereClause = new StringBuilder();
            List<SQLiteParameter> parameters = new List<SQLiteParameter>();

            if (pnlAdvancedSearch.Visible)
            {
                if (!string.IsNullOrEmpty(txtAdvCategoryName.Text))
                {
                    whereClause.Append(" AND CategoryName LIKE @CategoryName");
                    parameters.Add(new SQLiteParameter("@CategoryName", $"%{txtAdvCategoryName.Text.Trim()}%"));
                }
                if (!string.IsNullOrEmpty(txtAdvColorHex.Text))
                {
                    whereClause.Append(" AND ColorHex = @ColorHex");
                    parameters.Add(new SQLiteParameter("@ColorHex", txtAdvColorHex.Text.Trim()));
                }
            }
            else if (!string.IsNullOrEmpty(txtQuickSearch.Text))
            {
                string searchTerm = $"%{txtQuickSearch.Text.Trim()}%";
                whereClause.Append(" AND (CategoryName LIKE @SearchTerm OR ColorHex LIKE @SearchTerm)");
                parameters.Add(new SQLiteParameter("@SearchTerm", searchTerm));
            }

            string sql = $"SELECT CategoryID, CategoryName, ColorHex FROM Categories WHERE 1=1 {whereClause.ToString()} ORDER BY {SortExpression} {(SortDirection == SortDirection.Ascending ? "ASC" : "DESC")}";

            try
            {
                using (SQLiteConnection conn = new SQLiteConnection(connString))
                using (SQLiteCommand cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddRange(parameters.ToArray());
                    conn.Open();
                    SQLiteDataAdapter da = new SQLiteDataAdapter(cmd);
                    da.Fill(dt);
                }

                gvCategories.DataSource = dt;
                gvCategories.DataBind();

                ShowMessage($"已成功載入 **書籍類別** 資料 (共 {dt.Rows.Count} 筆記錄)。", "success");
            }
            catch (Exception ex)
            {
                ShowMessage($"載入資料時發生錯誤：{ex.Message}", "error");
            }
            pnlInsertForm.Visible = false;
            Session["IsInsertingCategories"] = null;
        }

        protected void ddlPageSize_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (int.TryParse(ddlPageSize.SelectedValue, out int newPageSize))
            {
                gvCategories.PageSize = newPageSize;
                Session["Categories_PageSize"] = newPageSize;
                gvCategories.PageIndex = 0;
                BindCategoriesData();
            }
        }

        protected void gvCategories_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            gvCategories.PageIndex = e.NewPageIndex;
            BindCategoriesData();
        }

        protected void gvCategories_Sorting(object sender, GridViewSortEventArgs e)
        {
            if (e.SortExpression.Equals(SortExpression))
            {
                SortDirection = (SortDirection == SortDirection.Ascending) ? SortDirection.Descending : SortDirection.Ascending;
            }
            else
            {
                SortExpression = e.SortExpression;
                SortDirection = SortDirection.Ascending;
            }

            gvCategories.PageIndex = 0;
            BindCategoriesData();
        }

        protected void gvCategories_RowEditing(object sender, GridViewEditEventArgs e)
        {
            pnlInsertForm.Visible = false;
            Session["IsInsertingCategories"] = null;
            gvCategories.EditIndex = e.NewEditIndex;
            BindCategoriesData();
        }

        protected void gvCategories_RowCancelingEdit(object sender, GridViewCancelEditEventArgs e)
        {
            gvCategories.EditIndex = -1;
            BindCategoriesData();
        }

        protected void gvCategories_RowUpdating(object sender, GridViewUpdateEventArgs e)
        {
            GridViewRow row = gvCategories.Rows[e.RowIndex];
            int categoryId = Convert.ToInt32(gvCategories.DataKeys[e.RowIndex].Value);
            string newCategoryName = (row.Cells[1].Controls[0] as TextBox)?.Text.Trim();

            TextBox txtColorHex = row.FindControl("txtColorHexEdit") as TextBox;
            string newColorHex = txtColorHex?.Text.Trim() ?? "#CCCCCC";

            if (string.IsNullOrEmpty(newCategoryName))
            {
                ShowMessage("更新失敗：類別名稱不能為空。", "error");
                e.Cancel = true;
                return;
            }

            string connString = GetConnectionString();
            string updateSql = "UPDATE Categories SET CategoryName = @CategoryName, ColorHex = @ColorHex WHERE CategoryID = @CategoryID";

            try
            {
                using (SQLiteConnection conn = new SQLiteConnection(connString))
                using (SQLiteCommand cmd = new SQLiteCommand(updateSql, conn))
                {
                    cmd.Parameters.AddWithValue("@CategoryName", newCategoryName);
                    cmd.Parameters.AddWithValue("@ColorHex", newColorHex);
                    cmd.Parameters.AddWithValue("@CategoryID", categoryId);

                    conn.Open();
                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        ShowMessage($"成功更新類別 ID {categoryId} 的記錄。", "success");
                    }
                    else
                    {
                        ShowMessage("更新失敗：沒有找到匹配的記錄或數據未變更。", "error");
                    }
                }
            }
            catch (Exception ex)
            {
                ShowMessage($"更新錯誤：{ex.Message}", "error");
            }

            gvCategories.EditIndex = -1;
            BindCategoriesData();
        }

        protected void gvCategories_RowDeleting(object sender, GridViewDeleteEventArgs e)
        {
            int categoryId = Convert.ToInt32(gvCategories.DataKeys[e.RowIndex].Value);
            string connString = GetConnectionString();
            string deleteSql = "DELETE FROM Categories WHERE CategoryID = @CategoryID";

            try
            {
                using (SQLiteConnection conn = new SQLiteConnection(connString))
                using (SQLiteCommand cmd = new SQLiteCommand(deleteSql, conn))
                {
                    cmd.Parameters.AddWithValue("@CategoryID", categoryId);
                    conn.Open();
                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        ShowMessage($"成功刪除類別 ID {categoryId} 的記錄。", "success");
                    }
                    else
                    {
                        ShowMessage("刪除失敗：沒有找到匹配的記錄。", "error");
                    }
                }
            }
            catch (SQLiteException ex) when (ex.Message.Contains("FOREIGN KEY constraint failed"))
            {
                ShowMessage($"刪除失敗：此類別 ID {categoryId} 仍有關聯的書籍，請先移除所有關聯後再刪除。", "error");
            }
            catch (Exception ex)
            {
                ShowMessage($"刪除錯誤：{ex.Message}", "error");
            }

            gvCategories.EditIndex = -1;
            BindCategoriesData();
        }

        protected void gvCategories_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.Header)
            {
                for (int i = 0; i < e.Row.Cells.Count; i++)
                {
                    if (e.Row.Cells[i].Controls.Count > 0 && e.Row.Cells[i].Controls[0] is LinkButton)
                    {
                        LinkButton sortLink = (LinkButton)e.Row.Cells[i].Controls[0];
                        if (sortLink.CommandArgument.Equals(SortExpression))
                        {
                            sortLink.Text += (SortDirection == SortDirection.Ascending) ? " ▲" : " ▼";
                        }
                    }
                }
            }

            // 修正點：使用 DataControlRowState.Alternate 而非 DataControlRowType.Alternate
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                if ((e.Row.RowState & DataControlRowState.Normal) != 0 ||
                    (e.Row.RowState & DataControlRowState.Alternate) != 0)
                {
                    Label lblColorHexItem = (Label)e.Row.FindControl("lblColorHexItem");
                    if (lblColorHexItem != null)
                    {
                        string colorHex = lblColorHexItem.Text;
                        lblColorHexItem.Style.Add("background-color", colorHex);
                        lblColorHexItem.Style.Add("color", IsColorDark(colorHex) ? "white" : "black");
                        lblColorHexItem.Style.Add("padding", "2px 5px");
                        lblColorHexItem.Style.Add("border-radius", "3px");
                        lblColorHexItem.Style.Add("display", "inline-block");
                    }
                }
                else if ((e.Row.RowState & DataControlRowState.Edit) > 0)
                {
                    TextBox txtColorHexEdit = (TextBox)e.Row.FindControl("txtColorHexEdit");
                    if (txtColorHexEdit != null)
                    {
                        txtColorHexEdit.TextMode = TextBoxMode.Color;
                    }
                }
            }
        }

        private bool IsColorDark(string hex)
        {
            if (string.IsNullOrEmpty(hex) || !hex.StartsWith("#") || hex.Length < 4) return false;
            try
            {
                Color color = ColorTranslator.FromHtml(hex);
                double brightness = (color.R * 299 + color.G * 587 + color.B * 114) / 1000;
                return brightness < 128;
            }
            catch
            {
                return false;
            }
        }

        protected void btnShowInsert_Click(object sender, EventArgs e)
        {
            gvCategories.EditIndex = -1;
            BindCategoriesData();
            pnlInsertForm.Visible = true;
            ShowMessage("請在下方表單中輸入新的類別記錄。", "info");
            Session["IsInsertingCategories"] = true;
        }

        protected void btnCancelInsert_Click(object sender, EventArgs e)
        {
            pnlInsertForm.Visible = false;
            ShowMessage("已取消新增操作。", "info");
            Session["IsInsertingCategories"] = null;
        }

        protected void btnInsertRecord_Click(object sender, EventArgs e)
        {
            string newCategoryName = txtInsertCategoryName.Text.Trim();
            string newColorHex = txtInsertColorHex.Text.Trim();
            string connString = GetConnectionString();

            if (string.IsNullOrEmpty(newCategoryName))
            {
                ShowMessage("新增失敗：類別名稱不能為空。", "error");
                return;
            }

            if (string.IsNullOrEmpty(newColorHex))
            {
                newColorHex = "#CCCCCC";
            }

            string insertSql = "INSERT INTO Categories (CategoryName, ColorHex) VALUES (@CategoryName, @ColorHex)";
            try
            {
                using (SQLiteConnection conn = new SQLiteConnection(connString))
                using (SQLiteCommand cmd = new SQLiteCommand(insertSql, conn))
                {
                    cmd.Parameters.AddWithValue("@CategoryName", newCategoryName);
                    cmd.Parameters.AddWithValue("@ColorHex", newColorHex);
                    conn.Open();
                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        ShowMessage($"成功新增類別：{newCategoryName}。", "success");
                        txtInsertCategoryName.Text = string.Empty;
                        txtInsertColorHex.Text = "#CCCCCC";
                    }
                    else
                    {
                        ShowMessage("新增失敗：數據未被插入。", "error");
                    }
                }
            }
            catch (SQLiteException ex) when (ex.Message.Contains("UNIQUE constraint failed"))
            {
                ShowMessage($"新增資料庫錯誤：類別名稱 '{newCategoryName}' 已存在。", "error");
            }
            catch (Exception ex)
            {
                ShowMessage($"新增錯誤：{ex.Message}", "error");
            }

            pnlInsertForm.Visible = false;
            Session["IsInsertingCategories"] = null;
            BindCategoriesData();
        }

        protected void btnQuickSearch_Click(object sender, EventArgs e)
        {
            pnlAdvancedSearch.Visible = false;
            ClearAdvancedSearchFields();
            gvCategories.PageIndex = 0;
            BindCategoriesData();
        }

        protected void btnAdvancedSearch_Click(object sender, EventArgs e)
        {
            txtQuickSearch.Text = string.Empty;
            gvCategories.PageIndex = 0;
            BindCategoriesData();
        }

        protected void btnToggleAdvancedSearch_Click(object sender, EventArgs e)
        {
            pnlAdvancedSearch.Visible = !pnlAdvancedSearch.Visible;
            if (!pnlAdvancedSearch.Visible)
            {
                ClearAdvancedSearchFields();
                gvCategories.PageIndex = 0;
                BindCategoriesData();
            }
        }

        protected void btnClearSearch_Click(object sender, EventArgs e)
        {
            txtQuickSearch.Text = string.Empty;
            ClearAdvancedSearchFields();
            gvCategories.PageIndex = 0;
            BindCategoriesData();
        }

        private void ClearAdvancedSearchFields()
        {
            txtAdvCategoryName.Text = string.Empty;
            txtAdvColorHex.Text = string.Empty;
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