<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="AdminPage.aspx.cs" 
    Inherits="aspnet.AdminPage" MasterPageFile="~/Site.Master" %>

<%-- 1. 頁面專屬的 CSS 樣式 (對應 Site.Master 中的 HeadContent) --%>
<asp:Content ID="HeadContent" ContentPlaceHolderID="HeadContent" runat="server">
    <style type="text/css">
        /* 1. 頁面主要容器 (設定最大寬度和居中) */
        .admin-container {
            max-width: 1200px;
            margin: 20px auto;
            padding: 20px;
            background-color: #ffffff;
            border-radius: 8px;
            box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
            font-family: Arial, sans-serif;
        }

        /* 2. 標題樣式 (紅色主題) */
        .page-header {
            color: #dc3545; /* Red */
            font-size: 24px;
            font-weight: bold;
            border-bottom: 3px solid #dc3545;
            padding-bottom: 10px;
            margin-bottom: 20px;
        }
        
        /* 3. 控制項列樣式 (Dropdown, Button) */
        .control-row {
            display: flex; /* 使用 flexbox 模擬佈局 */
            align-items: center;
            gap: 20px;
            margin-bottom: 20px;
        }

        /* 4. GridView 樣式 (gv-style) */
        .gv-style table {
            width: 100%;
            border-collapse: collapse;
        }
        .gv-style th {
            background-color: #dc3545; /* Header: Red */
            color: white;
            padding: 12px;
            text-align: left;
            border: 1px solid #c82333;
        }
        .gv-style td {
            padding: 10px;
            border: 1px solid #ddd;
        }
        .gv-style tr:nth-child(even) td {
            background-color: #f8f8f8; /* Alternating Row */
        }
        .gv-style tr:hover td {
            background-color: #ffeaea; /* Light Red Hover */
        }
        /* 5. Footer Row 樣式 (用於新增紀錄) */
        .gv-style tfoot tr td {
            background-color: #e9ecef; /* Light Gray for Footer */
            font-weight: bold;
            padding: 10px;
            border-top: 2px solid #ccc;
        }
        
        /* 6. 編輯模式下的輸入框 */
        .gv-style input[type="text"] {
            border: 1px solid #ccc;
            padding: 4px;
            border-radius: 4px;
            width: 90%;
        }

        /* 7. 按鈕樣式 (標準風格) */
        .btn-action {
            background-color: #6c757d; /* Secondary Gray */
            color: white;
            font-weight: bold;
            padding: 8px 16px;
            border: none;
            border-radius: 4px;
            cursor: pointer;
            transition: background-color 0.3s;
        }
        .btn-action:hover {
            background-color: #5a6268;
        }
        /* 新增/儲存按鈕的特定顏色 */
        .btn-insert {
             background-color: #28a745; /* Green for Insert */
        }
        .btn-insert:hover {
             background-color: #218838;
        }

        /* 8. 狀態訊息樣式 */
        .message-box {
            padding: 15px;
            border-radius: 6px;
            margin-bottom: 15px;
            border: 1px solid transparent;
        }
        .message-box-error {
            background-color: #f8d7da; /* Light Red */
            border-color: #f5c6cb;
            color: #721c24; /* Dark Red Text */
        }
        .message-box-success {
            background-color: #d4edda; /* Light Green */
            border-color: #c3e6cb;
            color: #155724; /* Dark Green Text */
        }
        .message-box-info {
            background-color: #cce5ff; /* Light Blue */
            border-color: #b8daff;
            color: #004085; /* Dark Blue Text */
        }
    </style>
</asp:Content>


<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <div class="admin-container">
        <h1 class="page-header">圖書館管理員專區 - 資料表操作</h1>
        <p style="color: #721c24; margin-bottom: 15px; font-weight: bold;">警告：此區域允許直接操作系統資料庫，請謹慎使用。</p>

        <div class="control-row">
            <asp:Label runat="server" Text="選擇操作的資料表:"></asp:Label>
            <asp:DropDownList ID="ddlTables" runat="server" AutoPostBack="True" OnSelectedIndexChanged="ddlTables_SelectedIndexChanged" 
                style="padding: 5px; border: 1px solid #ccc; border-radius: 4px;">
            </asp:DropDownList>

        </div>


        <asp:Panel ID="pnlMessage" runat="server" Visible="false" 
            CssClass="message-box" role="alert">
            <asp:Label ID="lblMessage" runat="server" style="font-weight: bold;"></asp:Label>
        </asp:Panel>


        <asp:GridView ID="gvAdminData" runat="server" 
            AutoGenerateColumns="True" 
            DataKeyNames="DummyKey" 
            CssClass="gv-style" 
            AllowPaging="True" 
            PageSize="15"
            ShowFooter="True" 
            OnPageIndexChanging="gvAdminData_PageIndexChanging"
            OnRowEditing="gvAdminData_RowEditing"
            OnRowUpdating="gvAdminData_RowUpdating"
            OnRowDeleting="gvAdminData_RowDeleting"
            OnRowCancelingEdit="gvAdminData_RowCancelingEdit"
            OnRowCommand="gvAdminData_RowCommand"
            EmptyDataText="目前此資料表無數據或尚未選擇資料表。">
            
            <Columns>
                <asp:CommandField ShowEditButton="True" ShowDeleteButton="True" HeaderText="操作" 
                    EditText="編輯" UpdateText="更新" CancelText="取消" DeleteText="刪除" />
            </Columns>

            <HeaderStyle CssClass="gv-header" />
            <PagerStyle CssClass="gv-pager" />
        </asp:GridView>

        <div style="margin-top: 20px; text-align: center;">
            <a href="MyHomepage.aspx" style="color: #007bff; text-decoration: underline;">返回個人首頁</a>
        </div>
    </div>
</asp:Content>