<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="AdminPage.aspx.cs" 
    Inherits="aspnet.AdminPage" MasterPageFile="~/Site.Master" %>

<asp:Content ID="HeadContent" ContentPlaceHolderID="HeadContent" runat="server">
    <style type="text/css">
        .admin-container {
            max-width: 1200px;
            margin: 20px auto;
            padding: 20px;
            background-color: #ffffff;
            border-radius: 8px;
            box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
            font-family: Arial, sans-serif;
        }

        .page-header {
            color: #dc3545;
            font-size: 24px;
            font-weight: bold;
            border-bottom: 3px solid #dc3545;
            padding-bottom: 10px;
            margin-bottom: 20px;
        }
        
        .control-row {
            display: flex;
            align-items: center;
            gap: 20px;
            margin-bottom: 20px;
        }

        /* 新增按鈕樣式 */
        .btn-new-record {
            background-color: #28a745; 
            color: white;
            font-weight: bold;
            padding: 8px 16px;
            border: none;
            border-radius: 4px;
            cursor: pointer;
            transition: background-color 0.3s;
            margin-left: 20px;
        }
        .btn-new-record:hover {
            background-color: #218838;
        }

        /* 新增表單 Panel 樣式 */
        .insert-form-panel {
            border: 1px solid #ccc;
            padding: 20px;
            border-radius: 6px;
            background-color: #f8f9fa; /* Light background */
            margin-top: 20px;
            margin-bottom: 20px;
        }
        .insert-form-header {
            background-color: #007bff;
            color: white;
            padding: 10px;
            font-size: 1.1em;
            text-align: left;
        }
        .insert-form-table {
            width: 100%;
            border-collapse: separate;
            border-spacing: 0 10px;
        }
        .insert-form-table td {
            padding: 5px 0;
        }
        .insert-form-table td:first-child {
            width: 150px;
            font-weight: bold;
            text-align: right;
            padding-right: 15px;
        }
        .input-insert-form {
             border: 1px solid #ced4da;
            padding: 8px;
            border-radius: 4px;
            width: 90%;
            box-sizing: border-box;
        }
        .form-actions {
            margin-top: 15px;
            text-align: right;
            border-top: 1px solid #eee;
            padding-top: 15px;
        }
        .btn-submit {
             background-color: #28a745; /* Green */
             margin-left: 10px;
        }
        .btn-submit:hover {
             background-color: #218838;
        }
        .btn-cancel {
             background-color: #dc3545; /* Red */
        }
        .btn-cancel:hover {
             background-color: #c82333;
        }


        /* GridView 樣式 (gv-style) */
        .gv-style table {
            width: 100%;
            border-collapse: collapse;
        }
        .gv-style th {
            background-color: #dc3545;
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
            background-color: #f8f8f8;
        }
        .gv-style tr:hover td {
            background-color: #ffeaea;
        }
        /* 由於移除了 Footer，這裡的 Footer 樣式不再需要 */
        
        .gv-style input[type="text"] {
            border: 1px solid #ccc;
            padding: 4px;
            border-radius: 4px;
            width: 90%;
        }

        .btn-action {
            background-color: #6c757d;
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

        .message-box {
            padding: 15px;
            border-radius: 6px;
            margin-bottom: 15px;
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
        <h1 class="page-header">圖書館管理員專區 - 資料表操作</h1>
        <p style="color: #721c24; margin-bottom: 15px; font-weight: bold;">警告：此區域允許直接操作系統資料庫，請謹慎使用。</p>

        <div class="control-row">
            <asp:Label runat="server" Text="選擇操作的資料表:"></asp:Label>
            <asp:DropDownList ID="ddlTables" runat="server" AutoPostBack="True" OnSelectedIndexChanged="ddlTables_SelectedIndexChanged" 
                style="padding: 5px; border: 1px solid #ccc; border-radius: 4px;">
            </asp:DropDownList>
            
            <%-- 【新增】顯式新增紀錄按鈕 --%>
            <asp:Button ID="btnShowInsert" runat="server" Text="✚ 新增紀錄" 
                OnClick="btnShowInsert_Click" CssClass="btn-new-record" />

        </div>


        <asp:Panel ID="pnlMessage" runat="server" Visible="false" 
            CssClass="message-box" role="alert">
            <asp:Label ID="lblMessage" runat="server" style="font-weight: bold;"></asp:Label>
        </asp:Panel>
        
        <%-- 【新增】獨立的新增紀錄表單 Panel --%>
        <asp:Panel ID="pnlInsertForm" runat="server" Visible="False" CssClass="insert-form-panel">
            <h3>新增紀錄</h3>
            <asp:PlaceHolder ID="phInsertFormControls" runat="server"></asp:PlaceHolder>
            
            <div class="form-actions">
                <asp:Button ID="btnCancelInsert" runat="server" Text="取消新增" OnClick="btnCancelInsert_Click" CssClass="btn-action btn-cancel" />
                <asp:Button ID="btnInsertRecord" runat="server" Text="確認新增並儲存" OnClick="btnInsertRecord_Click" CssClass="btn-action btn-submit" />
            </div>
        </asp:Panel>


        <asp:GridView ID="gvAdminData" runat="server" 
            AutoGenerateColumns="True" 
            DataKeyNames="DummyKey" 
            CssClass="gv-style" 
            AllowPaging="True" 
            PageSize="15"
            ShowFooter="False" 
            OnPageIndexChanging="gvAdminData_PageIndexChanging"
            OnRowEditing="gvAdminData_RowEditing"
            OnRowUpdating="gvAdminData_RowUpdating"
            OnRowDeleting="gvAdminData_RowDeleting"
            OnRowCancelingEdit="gvAdminData_RowCancelingEdit"
            OnRowDataBound="gvAdminData_RowDataBound" 
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