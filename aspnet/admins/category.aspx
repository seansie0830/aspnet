<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="categories.aspx.cs" MasterPageFile="~/Site.Master" Inherits="aspnet.Categories" %>

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
        
        .nav-links a {
            margin-right: 15px;
            text-decoration: none;
            color: #007bff;
            font-weight: bold;
        }
        .nav-links a:hover {
            color: #0056b3;
            text-decoration: underline;
        }
        .nav-links .active {
            color: #dc3545;
            text-decoration: underline;
        }

        .control-row, .pagination-row {
            display: flex;
            align-items: center;
            gap: 20px;
            margin-bottom: 20px;
            flex-wrap: wrap;
        }

        .btn-new-record, .btn-action {
            color: white;
            font-weight: bold;
            padding: 8px 16px;
            border: none;
            border-radius: 4px;
            cursor: pointer;
            transition: background-color 0.3s;
        }
        .btn-new-record {
            background-color: #28a745;
        }
        .btn-new-record:hover {
            background-color: #218838;
        }
        .btn-submit {
            background-color: #007bff;
        }
        .btn-submit:hover {
            background-color: #0056b3;
        }
        .btn-cancel {
            background-color: #6c757d;
        }
        .btn-cancel:hover {
            background-color: #5a6268;
        }
        .btn-delete {
            background-color: #dc3545;
        }
        .btn-delete:hover {
            background-color: #c82333;
        }


        .insert-form-panel {
            border: 1px solid #ccc;
            padding: 20px;
            border-radius: 6px;
            background-color: #f8f9fa; 
            margin-top: 20px;
            margin-bottom: 20px;
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
        .color-picker-input {
            width: 100px !important;
            height: 38px;
            padding: 0;
            border: none;
            cursor: pointer;
            margin-right: 10px;
        }
        .form-actions {
            margin-top: 15px;
            text-align: right;
            border-top: 1px solid #eee;
            padding-top: 15px;
        }
        .form-actions .btn-action {
            margin-left: 10px;
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
            cursor: pointer;
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
        
        .gv-style input[type="text"] {
            border: 1px solid #ccc;
            padding: 4px;
            border-radius: 4px;
            width: 90%;
            box-sizing: border-box;
        }
        .gv-style input[type="color"] {
            width: 40px;
            height: 25px;
            padding: 0;
            border: none;
            cursor: pointer;
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
        <h1 class="page-header">圖書館管理員專區 - 📚 書籍類別 (Categories) 管理</h1>

        <div class="nav-links">
            <a href="/admins/Categories.aspx" class="active">Categories</a>
            <a href="/admins/LendRecords.aspx">LendRecords</a>
            <a href="/admins/Books.aspx">Books</a>
            <a href="/admins/Users.aspx">Users</a>
        </div>
        <hr />

        <asp:Panel ID="pnlMessage" runat="server" Visible="false" 
            CssClass="message-box" role="alert">
            <asp:Label ID="lblMessage" runat="server" style="font-weight: bold;"></asp:Label>
        </asp:Panel>

        <asp:Panel ID="pnlInsertForm" runat="server" Visible="False" CssClass="insert-form-panel">
            <h3 style="margin-top: 0;">✚ 新增類別</h3>
            <table class="insert-form-table">
                <tr>
                    <td>CategoryName:</td>
                    <td><asp:TextBox ID="txtInsertCategoryName" runat="server" CssClass="input-insert-form" MaxLength="50"></asp:TextBox></td>
                </tr>
                <tr>
                    <td>ColorHex:</td>
                    <td>
                        <asp:TextBox ID="txtInsertColorHex" runat="server" CssClass="input-insert-form color-picker-input" TextMode="Color" Text="#CCCCCC"></asp:TextBox>
                    </td>
                </tr>
            </table>
            
            <div class="form-actions">
                <asp:Button ID="btnCancelInsert" runat="server" Text="取消新增" OnClick="btnCancelInsert_Click" CssClass="btn-action btn-cancel" />
                <asp:Button ID="btnInsertRecord" runat="server" Text="確認新增並儲存" OnClick="btnInsertRecord_Click" CssClass="btn-action btn-submit" />
            </div>
        </asp:Panel>
        
        <asp:Panel ID="pnlSearch" runat="server" CssClass="insert-form-panel">
            <h3 style="margin-top: 0;">🔎 搜尋與篩選</h3>
            <div class="control-row">
                <asp:Label runat="server" Text="快速搜尋:"></asp:Label>
                <asp:TextBox ID="txtQuickSearch" runat="server" placeholder="輸入名稱或色碼" CssClass="input-insert-form" Width="200px"></asp:TextBox>
                <asp:Button ID="btnQuickSearch" runat="server" Text="搜尋" OnClick="btnQuickSearch_Click" CssClass="btn-action btn-submit" />
                <asp:Button ID="btnShowInsert" runat="server" Text="✚ 新增紀錄" OnClick="btnShowInsert_Click" CssClass="btn-new-record" />
                <asp:Button ID="btnToggleAdvancedSearch" runat="server" Text="進階搜尋切換" OnClick="btnToggleAdvancedSearch_Click" CssClass="btn-action btn-cancel" />
            </div>

            <asp:Panel ID="pnlAdvancedSearch" runat="server" Visible="false">
                <hr />
                <h4 style="margin-top: 0;">進階搜尋</h4>
                <div class="control-row">
                    <asp:Label runat="server" Text="類別名稱 (模糊):"></asp:Label>
                    <asp:TextBox ID="txtAdvCategoryName" runat="server" CssClass="input-insert-form" Width="180px"></asp:TextBox>
                    <asp:Label runat="server" Text="色碼 (精確):"></asp:Label>
                    <asp:TextBox ID="txtAdvColorHex" runat="server" CssClass="input-insert-form" Width="100px"></asp:TextBox>
                    <asp:Button ID="btnAdvancedSearch" runat="server" Text="進階搜尋" OnClick="btnAdvancedSearch_Click" CssClass="btn-action btn-submit" />
                    <asp:Button ID="btnClearSearch" runat="server" Text="清除搜尋" OnClick="btnClearSearch_Click" CssClass="btn-action btn-cancel" />
                </div>
            </asp:Panel>

        </asp:Panel>


        <div class="pagination-row">
            <asp:Label runat="server" Text="每頁顯示筆數:"></asp:Label>
            <asp:DropDownList ID="ddlPageSize" runat="server" AutoPostBack="True" OnSelectedIndexChanged="ddlPageSize_SelectedIndexChanged" 
                style="padding: 5px; border: 1px solid #ccc; border-radius: 4px;">
                <asp:ListItem Text="10" Value="10" />
                <asp:ListItem Text="15" Value="15" Selected="True" />
                <asp:ListItem Text="20" Value="20" />
                <asp:ListItem Text="50" Value="50" />
            </asp:DropDownList>
        </div>

        <asp:GridView ID="gvCategories" runat="server" 
            AutoGenerateColumns="False" 
            DataKeyNames="CategoryID" 
            CssClass="gv-style" 
            AllowPaging="True" 
            AllowSorting="True"
            PageSize="15"
            ShowHeaderWhenEmpty="True"
            OnPageIndexChanging="gvCategories_PageIndexChanging"
            OnSorting="gvCategories_Sorting"
            OnRowEditing="gvCategories_RowEditing"
            OnRowCancelingEdit="gvCategories_RowCancelingEdit"
            OnRowUpdating="gvCategories_RowUpdating"
            OnRowDeleting="gvCategories_RowDeleting"
            OnRowDataBound="gvCategories_RowDataBound">
            
            <Columns>
                <asp:BoundField DataField="CategoryID" HeaderText="ID" SortExpression="CategoryID" ReadOnly="True" />
                <asp:BoundField DataField="CategoryName" HeaderText="名稱" SortExpression="CategoryName" />
                
                <asp:TemplateField HeaderText="色碼" SortExpression="ColorHex">
                    <ItemTemplate>
                        <asp:Label runat="server" ID="lblColorHexItem" Text='<%# Eval("ColorHex") %>'></asp:Label>
                    </ItemTemplate>
                    <EditItemTemplate>
                        <asp:TextBox ID="txtColorHexEdit" runat="server" Text='<%# Bind("ColorHex") %>' TextMode="Color" CssClass="gv-style input[type='color']"></asp:TextBox>
                    </EditItemTemplate>
                </asp:TemplateField>

                <asp:CommandField ShowEditButton="True" EditText="編輯" UpdateText="更新" CancelText="取消" ItemStyle-Width="150px" />
                <asp:CommandField ShowDeleteButton="True" DeleteText="刪除" ItemStyle-Width="70px" ControlStyle-CssClass="btn-delete" />
            </Columns>
            <EmptyDataTemplate>
                目前沒有任何類別資料。
            </EmptyDataTemplate>
        </asp:GridView>
    </div>
</asp:Content>