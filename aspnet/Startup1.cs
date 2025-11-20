using Hangfire;
using Hangfire.SQLite;
using Owin;
using System.Configuration;
using Microsoft.Owin; // ✅ 新增這一行來解決 [assembly: OwinStartup] 的錯誤

// 確保 [assembly: OwinStartup] 屬性在任何 namespace 之外
[assembly: OwinStartup(typeof(aspnet.Startup1))]

namespace aspnet // 這裡應該不會再有錯誤
{
    // 請確認 class 括號 } 是在檔案末尾結束
    public class Startup1
    {
        public void Configuration(IAppBuilder app)
        {
            // 1. 從 Web.config 讀取 SQLite 連線字串
            string connectionString =
                ConfigurationManager.ConnectionStrings["HangfireDBConnection"].ConnectionString;
            // ⚠️ 注意：我假設您已將 Web.config 中的名稱修正為 HangfireDBConnection

            // 2. 配置 Hangfire 儲存 (SQLite)
            GlobalConfiguration.Configuration
                .UseSQLiteStorage(connectionString);

            // 3. 啟動 Hangfire Server
            app.UseHangfireServer();

            // 4. 啟用 Hangfire Dashboard (控制面板)
            app.UseHangfireDashboard("/hangfire", new DashboardOptions
            {
                AppPath = "/",
                IgnoreAntiforgeryToken = true
            });
        }
    }
}