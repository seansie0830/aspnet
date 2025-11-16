using System;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace aspnet.Admin
{
    // 繼承自 System.Web.UI.Page
    public partial class Main : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                // **【模擬權限檢查】**
                // 為了與您其他管理頁面保持一致，這裡會進行基本的登入和權限檢查。
                // 實際的權限驗證邏輯應在 IsUserAdminOrStaff 方法中實現。

                if (!User.Identity.IsAuthenticated)
                {
                    // 假設未登入則導向登入頁
                    Response.Redirect("~/Login.aspx");
                    return;
                }

                // 檢查是否具有管理員或工作人員權限 (此方法為模擬，不含 DB 查詢)
                if (!IsUserAdminOrStaff(User.Identity.Name))
                {
                    ShowMessage("存取遭拒：您不具備管理員或工作人員權限。", "error");
                    // 導向一般用戶的首頁
                    Response.Redirect("~/MyHomepage.aspx?AccessDenied=True");
                    return;
                }

                // 此為儀表板頁面，不需要執行其他資料綁定或業務邏輯
            }
        }

        // 模擬其他 Admin 頁面使用的權限檢查方法 (不含資料庫查詢的虛擬實作)
        // 在實際應用中，您應該根據您的 Forms Authentication 或 Role Provider 實作此方法。
        private bool IsUserAdminOrStaff(string username)
        {
            // 由於要求「不做資料庫查詢」，這裡假設只要用戶已登入 (User.Identity.IsAuthenticated)，
            // 且能成功到達 /admins/ 路徑，就視為有權限。
            // **請注意：在實際產品中，您需要在此處實作嚴格的權限查詢和驗證。**
            return true;
        }

        // 模擬其他 Admin 頁面使用的訊息顯示方法
        private void ShowMessage(string message, string type)
        {
            // 檢查控制項是否存在，避免 NullReferenceException
            if (pnlMessage != null && lblMessage != null)
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
}