using System;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Collections.Generic;

namespace aspnet.Admin
{
    public partial class config : Page
    {
        private const string AppConfigKey = "ApplicationConfig";

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

                LoadConfiguration();
            }
        }

        protected void btnSave_Click(object sender, EventArgs e)
        {
            if (!IsUserAdminOrStaff(User.Identity.Name))
            {
                ShowMessage("存取遭拒：您不具備管理員或工作人員權限。", "error");
                return;
            }

            try
            {
                var config = new Dictionary<string, string>
                {
                    { "MaxBooksPerUser", txtMaxBooksPerUser.Text.Trim() },
                    { "LendingPeriodDays", txtLendingPeriodDays.Text.Trim() },
                    { "RenewLimit", txtRenewLimit.Text.Trim() }
                };

                // 將設定儲存到 Application State
                Application[AppConfigKey] = config;

                ShowMessage("系統設定已成功儲存並應用。", "success");
            }
            catch (Exception ex)
            {
                ShowMessage("儲存設定時發生錯誤：" + ex.Message, "error");
            }
        }

        private void LoadConfiguration()
        {
            var config = Application[AppConfigKey] as Dictionary<string, string>;

            // 首次載入或未設定時使用預設值
            if (config == null)
            {
                config = new Dictionary<string, string>
                {
                    { "MaxBooksPerUser", "5" },
                    { "LendingPeriodDays", "14" },
                    { "RenewLimit", "1" }
                };
                Application[AppConfigKey] = config;
            }

            if (config.ContainsKey("MaxBooksPerUser"))
            {
                txtMaxBooksPerUser.Text = config["MaxBooksPerUser"];
            }
            if (config.ContainsKey("LendingPeriodDays"))
            {
                txtLendingPeriodDays.Text = config["LendingPeriodDays"];
            }
            if (config.ContainsKey("RenewLimit"))
            {
                txtRenewLimit.Text = config["RenewLimit"];
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