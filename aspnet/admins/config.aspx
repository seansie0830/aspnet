<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="config.aspx.cs" MasterPageFile="~/Site.Master" Inherits="aspnet.Admin.config" %>

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

        /* 表單區域樣式 */
        .config-section {
            padding: 20px;
            border: 1px solid #dee2e6;
            border-radius: 5px;
            margin-top: 20px;
        }
        
        .config-item {
            display: flex;
            align-items: center;
            margin-bottom: 15px;
        }
        
        .config-label {
            flex: 0 0 250px; /* 固定標籤寬度 */
            font-weight: bold;
            color: #495057;
            text-align: right;
            padding-right: 20px;
        }
        
        .config-input {
            flex: 1;
        }
        
        .config-input input[type="text"] {
            width: 100%;
            max-width: 300px;
            padding: 8px;
            border: 1px solid #ced4da;
            border-radius: 4px;
        }
        
        .config-save-button {
            margin-top: 20px;
            text-align: center;
        }
        
        .config-save-button .btn-primary {
            background-color: #007bff;
            border-color: #007bff;
            color: white;
            padding: 10px 20px;
            border-radius: 5px;
            cursor: pointer;
            transition: background-color 0.3s;
        }
        
        .config-save-button .btn-primary:hover {
            background-color: #0056b3;
            border-color: #0056b3;
        }
        
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
        
        .message-box-success {
            color: #155724;
            background-color: #d4edda;
            border-color: #c3e6cb;
        }

    </style>
</asp:Content>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <div class="admin-container">
        <h2 class="page-header">系統設定管理 (System Configuration)</h2>

        <div class="admin-nav">
            <a href="/admins/Main.aspx">儀表板</a>
            <a href="/admins/Users.aspx">用戶管理</a>
            <a href="/admins/Books.aspx">書籍管理</a>
            <a href="/admins/category.aspx">類別管理</a>
            <a href="/admins/LendRecord.aspx">借閱記錄</a>
            <a href="/admins/config.aspx" class="active">系統設定</a>
        </div>
        
        <asp:Panel ID="pnlMessage" runat="server" Visible="false" CssClass="message-box message-box-info">
            <asp:Label ID="lblMessage" runat="server"></asp:Label>
        </asp:Panel>

        <div class="config-section">
            <h3>借閱規則設定</h3>
            <div class="config-item">
                <label for="<%= txtMaxBooksPerUser.ClientID %>" class="config-label">每人借書上限 (本)</label>
                <div class="config-input">
                    <asp:TextBox ID="txtMaxBooksPerUser" runat="server" TextMode="Number" />
                </div>
            </div>
            
            <div class="config-item">
                <label for="<%= txtLendingPeriodDays.ClientID %>" class="config-label">每人借書期限 (天)</label>
                <div class="config-input">
                    <asp:TextBox ID="txtLendingPeriodDays" runat="server" TextMode="Number" />
                </div>
            </div>

            <div class="config-item">
                <label for="<%= txtRenewLimit.ClientID %>" class="config-label">續借次數限制</label>
                <div class="config-input">
                    <asp:TextBox ID="txtRenewLimit" runat="server" TextMode="Number" />
                </div>
            </div>
        </div>

        <div class="config-save-button">
            <asp:Button ID="btnSave" runat="server" Text="儲存設定" CssClass="btn-primary" OnClick="btnSave_Click" />
        </div>

    </div>
</asp:Content>