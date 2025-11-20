<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Main.aspx.cs" MasterPageFile="~/Site.Master" Inherits="aspnet.Admin.Main" %>

<asp:Content ID="HeadContent" ContentPlaceHolderID="HeadContent" runat="server">
    <style type="text/css">
        .admin-container {
            max-width: 1400px;
            margin: 20px auto;
            padding: 20px;
            background-color: #ffffff;
            border-radius: 8px;
            box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
            font-family: Arial, sans-serif;
        }

        .page-header {
            color: #dc3545;
            font-size: 28px;
            font-weight: bold;
            border-bottom: 3px solid #dc3545;
            padding-bottom: 10px;
            margin-bottom: 20px;
        }
        
        .admin-nav {
            margin-bottom: 25px;
            padding: 10px;
            background-color: #f8f9fa;
            border-radius: 5px;
            box-shadow: 0 2px 4px rgba(0, 0, 0, 0.05);
            display: flex;
            gap: 15px;
        }
        .admin-nav a {
            text-decoration: none;
            color: #007bff;
            font-weight: bold;
            padding: 8px 15px;
            border-radius: 4px;
            transition: background-color 0.3s;
        }
        .admin-nav a:hover {
            background-color: #e2e6ea;
        }
        .admin-nav .active {
            background-color: #007bff;
            color: white;
        }

        .dashboard-grid {
            display: grid;
            grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
            gap: 25px;
        }

        .dashboard-card {
            background-color: #ffffff;
            border: 1px solid #e0e0e0;
            border-radius: 8px;
            padding: 25px;
            box-shadow: 0 4px 8px rgba(0, 0, 0, 0.05);
            transition: transform 0.3s, box-shadow 0.3s;
            display: flex;
            flex-direction: column;
            justify-content: space-between;
        }
        .dashboard-card:hover {
            transform: translateY(-5px);
            box-shadow: 0 8px 16px rgba(0, 0, 0, 0.1);
        }

        .dashboard-card h3 {
            margin-top: 0;
            color: #333;
            font-size: 1.5em;
            border-bottom: 2px solid;
            padding-bottom: 10px;
            margin-bottom: 15px;
        }

        .dashboard-card p {
            color: #666;
            flex-grow: 1;
            margin-bottom: 20px;
        }

        .dashboard-card a {
            display: inline-block;
            background-color: #dc3545;
            color: white;
            text-decoration: none;
            padding: 10px 15px;
            border-radius: 5px;
            font-weight: bold;
            text-align: center;
            transition: background-color 0.3s;
        }
        .dashboard-card a:hover {
            background-color: #c82333;
        }

        /* 顏色主題 */
        .card-users h3 { border-color: #007bff; }
        .card-users a { background-color: #007bff; }
        .card-users a:hover { background-color: #0056b3; }

        .card-books h3 { border-color: #28a745; }
        .card-books a { background-color: #28a745; }
        .card-books a:hover { background-color: #1e7e34; }

        .card-category h3 { border-color: #ffc107; }
        .card-category a { background-color: #ffc107; color: #212529; }
        .card-category a:hover { background-color: #e0a800; }

        .card-records h3 { border-color: #17a2b8; }
        .card-records a { background-color: #17a2b8; }
        .card-records a:hover { background-color: #117a8b; }
        
        .card-mail h3 { border-color: #6f42c1; }
        .card-mail a { background-color: #6f42c1; }
        .card-mail a:hover { background-color: #5a34a0; }

        .card-boot-category h3 { border-color: #fd7e14; }
        .card-boot-category a { background-color: #fd7e14; }
        .card-boot-category a:hover { background-color: #d8680c; }


        /* 訊息區塊樣式 */
        .message-box {
            padding: 15px;
            border-radius: 6px;
            margin-bottom: 20px;
            border: 1px solid transparent;
        }
        .message-box-error {
            background-color: #f8d7da;
            border-color: #f5c6cb;
            color: #721c24;
        }
        .message-box-success {
            background-color: #d4edda;
            border-color: #c3e6cb;
            color: #155724;
        }
        .message-box-info {
            background-color: #cce5ff;
            border-color: #b8daff;
            color: #004085;
        }
    </style>
</asp:Content>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <div class="admin-container">
        <h1 class="page-header">管理員儀表板 (Administrator Dashboard)</h1>
        
        <asp:Panel ID="pnlMessage" runat="server" Visible="false" CssClass="message-box message-box-info">
            <asp:Label ID="lblMessage" runat="server"></asp:Label>
        </asp:Panel>

        <div class="dashboard-grid">
            
            <div class="dashboard-card card-users">
                <h3>用戶管理</h3>
                <p>新增、編輯或停用系統用戶帳號和權限。</p>
                <a href="/admins/Users.aspx">進入管理頁面</a>
            </div>

            <div class="dashboard-card card-books">
                <h3>書籍管理</h3>
                <p>維護圖書館的書籍目錄與庫存資訊。</p>
                <a href="/admins/Books.aspx">進入管理頁面</a>
            </div>

            <div class="dashboard-card card-category">
                <h3>類別管理</h3>
                <p>定義書籍分類，方便組織和搜尋。</p>
                <a href="/admins/Categories.aspx">進入管理頁面</a>
            </div>

            <div class="dashboard-card card-records">
                <h3>借閱記錄</h3>
                <p>查詢、追蹤和管理所有用戶的借閱歷史。</p>
                <a href="/admins/LendRecord.aspx">進入管理頁面</a>
            </div>

            <div class="dashboard-card card-mail">
                <h3>郵件佇列</h3>
                <p>管理逾期提醒、密碼重設等自動寄送的郵件。</p>
                <a href="/admins/mailQueue.aspx">進入管理頁面</a>
            </div>

            <div class="dashboard-card card-boot-category">
                <h3>書籍類別管理</h3>
                <p>查詢、追蹤和設定書籍所屬的類別。</p>
                <a href="/admins/BookCategory.aspx">進入管理頁面</a>
            </div>

        </div>
    </div>
</asp:Content>