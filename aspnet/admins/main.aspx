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
            padding: 5px 10px;
            border-radius: 3px;
            transition: background-color 0.3s;
            font-weight: bold;
        }

        .admin-nav a:hover {
            background-color: #e2e6ea;
            color: #0056b3;
        }

        .admin-nav .active {
            background-color: #dc3545;
            color: white;
            pointer-events: none;
        }

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
            border-left: 5px solid #007bff;
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

        .card-users { border-left-color: #28a745; }
        .card-books { border-left-color: #ffc107; }
        .card-category { border-left-color: #17a2b8; }
        .card-records { border-left-color: #6f42c1; }
        .card-boot-category { border-left-color: #dc3545; }
        .card-config { border-left-color: #000000; } /* 新增黑色邊框樣式 */
        
        .card-users a { background-color: #28a745; }
        .card-books a { background-color: #ffc107; color: #212529; }
        .card-category a { background-color: #17a2b8; }
        .card-records a { background-color: #6f42c1; }
        .card-boot-category a { background-color: #dc3545; }
        .card-config a { background-color: #000000; } /* 新增黑色按鈕樣式 */

        .message-box {
            padding: 15px;
            margin-bottom: 20px;
            border-radius: 4px;
            border: 1px solid transparent;
        }

        .message-box-info {
            color: #0c5460;
            background-color: #d1ecf1;
            border-color: #bee5eb;
        }

        .message-box-error {
            color: #721c24;
            background-color: #f8d7da;
            border-color: #f5c6cb;
        }
    </style>
</asp:Content>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <div class="admin-container">
        <h2 class="page-header">管理員儀表板 (Admin Dashboard)</h2>

        <div class="admin-nav">
            <a href="/admins/Main.aspx" class="active">儀表板</a>
            <a href="/admins/Users.aspx">用戶管理</a>
            <a href="/admins/Books.aspx">書籍管理</a>
            <a href="/admins/category.aspx">類別管理</a>
            <a href="/admins/LendRecord.aspx">借閱記錄</a>
            <a href="/admins/config.aspx">系統設定</a>
        </div>
        
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
                <a href="/admins/category.aspx">進入管理頁面</a>
            </div>

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
            
            <div class="dashboard-card card-config">
                <h3>系統設定</h3>
                <p>設定全域借閱規則，如借書上限和期限。</p>
                <a href="/admins/config.aspx">進入管理頁面</a>
            </div>

        </div>

    </div>
</asp:Content>