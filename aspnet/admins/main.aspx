<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Main.aspx.cs" MasterPageFile="~/Site.Master" Inherits="aspnet.Admin.Main" %>

<%-- 假設您使用 Site.Master 或類似的母版頁，並在其中定義了 MainContent 和 HeadContent --%>
<asp:Content ID="HeadContent" ContentPlaceHolderID="HeadContent" runat="server">
    <%-- 沿用 Admin 頁面的一致性 CSS 樣式 --%>
    <style type="text/css">
        /* 基本管理區容器樣式，與您的其他頁面保持一致 */
        .admin-container {
            max-width: 1400px;
            margin: 20px auto;
            padding: 20px;
            background-color: #ffffff;
            border-radius: 8px;
            box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
            font-family: Arial, sans-serif;
        }

        /* 頁面標題樣式 */
        .page-header {
            color: #dc3545; /* 使用紅色作為管理區主色調 */
            font-size: 28px;
            font-weight: bold;
            border-bottom: 3px solid #dc3545;
            padding-bottom: 10px;
            margin-bottom: 20px;
        }
        
        /* 導覽列容器樣式 */
        .admin-nav {
            margin-bottom: 25px;
            padding: 10px;
            background-color: #f8f9fa;
            border-radius: 5px;
            box-shadow: 0 2px 4px rgba(0, 0, 0, 0.05);
            display: flex;
            gap: 15px;
        }

        /* 導覽列連結樣式 */
        .admin-nav a {
            text-decoration: none;
            color: #007bff;
            padding: 5px 10px;
            border-radius: 3px;
            transition: background-color 0.3s;
            font-weight: bold;
        }

        .admin-nav a:hover {
            background-color: #e2e6ea;
            color: #0056b3;
        }

        /* 作用中連結樣式，模仿您範例中的 .active 類別 */
        .admin-nav .active {
            background-color: #dc3545;
            color: white;
            pointer-events: none;
        }

        /* 儀表板卡片佈局 */
        .dashboard-grid {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
            gap: 25px;
            margin-top: 30px;
        }

        .dashboard-card {
            background-color: #ffffff;
            padding: 25px;
            border-radius: 8px;
            box-shadow: 0 2px 5px rgba(0, 0, 0, 0.1);
            text-align: center;
            transition: transform 0.3s, box-shadow 0.3s;
            border-left: 5px solid #007bff; /* 預設邊框顏色 */
        }

        .dashboard-card:hover {
            transform: translateY(-5px);
            box-shadow: 0 6px 15px rgba(0, 0, 0, 0.15);
        }

        .dashboard-card h3 {
            margin-top: 0;
            color: #343a40;
            font-size: 20px;
            border-bottom: 1px dashed #ced4da;
            padding-bottom: 10px;
            margin-bottom: 15px;
        }

        .dashboard-card p {
            font-size: 14px;
            color: #6c757d;
        }

        .dashboard-card a {
            display: inline-block;
            margin-top: 15px;
            padding: 10px 20px;
            background-color: #007bff;
            color: white;
            text-decoration: none;
            border-radius: 5px;
            font-weight: bold;
            transition: background-color 0.3s;
        }

        /* 模仿原始頁面的不同風格，設定卡片主題色 */
        .card-users { border-left-color: #28a745; }
        .card-books { border-left-color: #ffc107; }
        .card-category { border-left-color: #17a2b8; }
        .card-records { border-left-color: #6f42c1; }
        .card-boot-category { border-left-color: #dc3545; } /* 紅色 (新增) */
        
        .card-users a { background-color: #28a745; }
        .card-books a { background-color: #ffc107; color: #212529; }
        .card-category a { background-color: #17a2b8; }
        .card-records a { background-color: #6f42c1; }
        .card-boot-category a { background-color: #dc3545; } /* 紅色 (新增) */

    </style>
</asp:Content>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <div class="admin-container">
        <h2 class="page-header">管理員儀表板 (Admin Dashboard)</h2>

        <%-- 導覽列：將 Main.aspx 設為 active --%>
        <div class="admin-nav">
            <a href="/admins/Main.aspx" class="active">儀表板</a>
            <a href="/admins/Users.aspx">用戶管理</a>
            <a href="/admins/Books.aspx">書籍管理</a>
            <a href="/admins/category.aspx">類別管理</a>
            <a href="/admins/LendRecord.aspx">借閱記錄</a>
        </div>
        
        <%-- 訊息面板，與其他管理頁面保持一致 --%>
        <asp:Panel ID="pnlMessage" runat="server" Visible="false" CssClass="message-box message-box-info">
            <asp:Label ID="lblMessage" runat="server"></asp:Label>
        </asp:Panel>

        <div class="dashboard-grid">
            
            <%-- 用戶管理入口卡片 --%>
            <div class="dashboard-card card-users">
                <h3>用戶管理</h3>
                <p>新增、編輯或停用系統用戶帳號和權限。</p>
                <a href="/admins/Users.aspx">進入管理頁面</a>
            </div>

            <%-- 書籍管理入口卡片 --%>
            <div class="dashboard-card card-books">
                <h3>書籍管理</h3>
                <p>維護圖書館的書籍目錄與庫存資訊。</p>
                <a href="/admins/Books.aspx">進入管理頁面</a>
            </div>

            <%-- 類別管理入口卡片 --%>
            <div class="dashboard-card card-category">
                <h3>類別管理</h3>
                <p>定義書籍分類，方便組織和搜尋。</p>
                <a href="/admins/category.aspx">進入管理頁面</a>
            </div>

            <%-- 借閱記錄入口卡片 --%>
            <div class="dashboard-card card-records">
                <h3>借閱記錄</h3>
                <p>查詢、追蹤和管理所有用戶的借閱歷史。</p>
                <a href="/admins/LendRecord.aspx">進入管理頁面</a>
            </div>
            <div class="dashboard-card card-boot-category">
                <h3>書籍類別管理</h3>
                <p>查詢、追蹤和設定書籍所屬的類別。</p>
                <a href="/admins/BookCategory.aspx">進入管理頁面</a>
            </div>
        </div>

    </div>
</asp:Content>