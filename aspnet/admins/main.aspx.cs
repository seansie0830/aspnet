using System;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace aspnet.Admin
{
    public partial class Main : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                if (!User.Identity.IsAuthenticated)
                {
                    Response.Redirect("~/Login.aspx");
                    return;
                }

                if (!IsUserAdminOrStaff(User.Identity.Name))
                {
                    ShowMessage("存取遭拒：您不具備管理員或工作人員權限。", "error");
                    Response.Redirect("~/MyHomepage.aspx?AccessDenied=True");
                    return;
                }
            }
        }

        private bool IsUserAdminOrStaff(string username)
        {
            return true;
        }

        private void ShowMessage(string message, string type)
        {
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