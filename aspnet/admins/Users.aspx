<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Users.aspx.cs" MasterPageFile="~/Site.Master" Inherits="aspnet.Users" %>

<asp:Content ID="HeadContent" ContentPlaceHolderID="HeadContent" runat="server">
    <style type="text/css">
        .admin-container {
            max-width: 1400px;
            margin: 20px auto;
            padding: 25px;
            background-color: #ffffff;
            border-radius: 10px;
            box-shadow: 0 4px 15px rgba(0, 0, 0, 0.15);
            font-family: Arial, sans-serif;
        }

        .page-header {
            color: #dc3545;
            font-size: 28px;
            font-weight: bold;
            border-bottom: 4px solid #dc3545;
            padding-bottom: 15px;
            margin-bottom: 25px;
        }
        
        .nav-link-group {
            margin-bottom: 20px;
            padding: 10px;
            background-color: #f8f9fa;
            border-radius: 5px;
            border: 1px solid #e9ecef;
        }
        .nav-link-group a {
            color: #007bff;
            text-decoration: none;
            font-weight: bold;
            margin-right: 20px;
            transition: color 0.3s;
        }
        .nav-link-group a:hover {
            color: #0056b3;
            text-decoration: underline;
        }
        .nav-link-group .active {
            color: #dc3545;
        }

        .control-row {
            display: flex;
            align-items: center;
            gap: 15px;
            margin-bottom: 20px;
            flex-wrap: wrap;
        }

        .btn-style {
            background-color: #007bff;
            color: white;
            font-weight: bold;
            padding: 8px 16px;
            border: none;
            border-radius: 4px;
            cursor: pointer;
            transition: background-color 0.3s;
        }
        .btn-style:hover {
            background-color: #0056b3;
        }

        .btn-new-record {
            background-color: #28a745;
        }
        .btn-new-record:hover {
            background-color: #218838;
        }
        
        .input-text {
            border: 1px solid #ced4da;
            padding: 8px;
            border-radius: 4px;
            width: 250px;
            box-sizing: border-box;
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

        .insert-form-panel, .search-form-panel {
            border: 1px solid #ccc;
            padding: 20px;
            border-radius: 6px;
            background-color: #f8f9fa; 
            margin-top: 20px;
            margin-bottom: 20px;
        }
        .form-table {
            width: 100%;
            border-collapse: separate;
            border-spacing: 0 10px;
        }
        .form-table td {
            padding: 5px 0;
        }
        .form-table td:first-child {
            width: 180px;
            font-weight: bold;
            text-align: right;
            padding-right: 15px;
        }
        .form-actions {
            margin-top: 15px;
            text-align: right;
            border-top: 1px solid #eee;
            padding-top: 15px;
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
        
        .gv-style input[type="text"], .gv-style input[type="email"], .gv-style select {
            border: 1px solid #ccc;
            padding: 4px;
            border-radius: 4px;
            width: 90%;
            box-sizing: border-box;
        }
        .gv-style a {
            color: #dc3545;
        }

        .pager-row {
            margin-top: 15px;
            display: flex;
            justify-content: flex-end;
            align-items: center;
            gap: 10px;
        }
    </style>
</asp:Content>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <div class="admin-container">
        <h1 class="page-header">👤 使用者帳號管理 (Users)</h1>
        <p style="color: #721c24; margin-bottom: 15px; font-weight: bold;">警告：此區域允許操作系統使用者資料，請謹慎使用。IsAdmin 0=普通用戶, 1=管理員, 2=工作人員。</p>

        <div class="nav-link-group">
            <a href="/admins/Category.aspx">類別管理</a>
            <a href="/admins/LendRecord.aspx">借閱記錄</a>
            <a href="/admins/Books.aspx">書籍管理</a>
            <a href="/admins/Users.aspx" class="active">使用者帳號</a>
        </div>

        <asp:Panel ID="pnlMessage" runat="server" Visible="false" 
            CssClass="message-box" role="alert">
            <asp:Label ID="lblMessage" runat="server" style="font-weight: bold;"></asp:Label>
        </asp:Panel>

        <%-- 搜尋/控制列 --%>
        <div class="control-row">
            <asp:TextBox ID="txtSearch" runat="server" CssClass="input-text" placeholder="輸入關鍵字搜尋 (帳號/Email)"></asp:TextBox>
            <asp:Button ID="btnSearch" runat="server" Text="🔍 搜尋" OnClick="btnSearch_Click" CssClass="btn-style" />
            <asp:Button ID="btnToggleAdvancedSearch" runat="server" Text="⚙️ 進階搜尋" OnClick="btnToggleAdvancedSearch_Click" CssClass="btn-style" />
            <asp:Button ID="btnClearSearch" runat="server" Text="🔄 清除搜尋" OnClick="btnClearSearch_Click" CssClass="btn-style" />
            <asp:Button ID="btnShowInsert" runat="server" Text="✚ 建立新帳號" OnClick="btnShowInsert_Click" CssClass="btn-style btn-new-record" />
        </div>

        <%-- 進階搜尋面板 --%>
        <asp:Panel ID="pnlAdvancedSearch" runat="server" Visible="False" CssClass="search-form-panel">
            <h3>進階搜尋</h3>
            <table class="form-table">
                <tr>
                    <td>帳號 (Username):</td>
                    <td><asp:TextBox ID="txtAdvUsername" runat="server" CssClass="input-text" Width="90%"></asp:TextBox></td>
                </tr>
                <tr>
                    <td>電子郵件 (Email):</td>
                    <td><asp:TextBox ID="txtAdvEmail" runat="server" CssClass="input-text" Width="90%"></asp:TextBox></td>
                </tr>
                <tr>
                    <td>權限等級 (IsAdmin):</td>
                    <td>
                        <asp:DropDownList ID="ddlAdvIsAdmin" runat="server" CssClass="input-text" Width="90%">
                            <asp:ListItem Text="-- 所有權限 --" Value=""></asp:ListItem>
                            <asp:ListItem Text="0 - 普通用戶" Value="0"></asp:ListItem>
                            <asp:ListItem Text="1 - 管理員" Value="1"></asp:ListItem>
                            <asp:ListItem Text="2 - 工作人員" Value="2"></asp:ListItem>
                        </asp:DropDownList>
                    </td>
                </tr>
                <tr>
                    <td></td>
                    <td><asp:Button ID="btnPerformAdvancedSearch" runat="server" Text="執行進階搜尋" OnClick="btnPerformAdvancedSearch_Click" CssClass="btn-style" /></td>
                </tr>
            </table>
        </asp:Panel>

        <%-- 新增帳號面板 --%>
        <asp:Panel ID="pnlInsertForm" runat="server" Visible="False" CssClass="insert-form-panel">
            <h3>建立新帳號</h3>
            <table class="form-table">
                <tr>
                    <td>帳號名稱 (Username):</td>
                    <td><asp:TextBox ID="txtInsert_Username" runat="server" CssClass="input-text" Width="90%"></asp:TextBox>
                        <asp:RequiredFieldValidator runat="server" ControlToValidate="txtInsert_Username" ErrorMessage="帳號名稱為必填。" ForeColor="Red" Display="Dynamic" />
                    </td>
                </tr>
                <tr>
                    <td>密碼 (Password):</td>
                    <td><asp:TextBox ID="txtInsert_Password" runat="server" TextMode="Password" CssClass="input-text" Width="90%"></asp:TextBox>
                        <asp:RequiredFieldValidator runat="server" ControlToValidate="txtInsert_Password" ErrorMessage="密碼為必填。" ForeColor="Red" Display="Dynamic" />
                    </td>
                </tr>
                <tr>
                    <td>電子郵件 (Email):</td>
                    <td><asp:TextBox ID="txtInsert_Email" runat="server" TextMode="Email" CssClass="input-text" Width="90%"></asp:TextBox>
                        <asp:RequiredFieldValidator runat="server" ControlToValidate="txtInsert_Email" ErrorMessage="電子郵件為必填。" ForeColor="Red" Display="Dynamic" />
                        <asp:RegularExpressionValidator runat="server" ControlToValidate="txtInsert_Email" ValidationExpression="\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*" ErrorMessage="電子郵件格式不正確。" ForeColor="Red" Display="Dynamic" />
                    </td>
                </tr>
                <tr>
                    <td>權限等級 (IsAdmin):</td>
                    <td>
                        <asp:DropDownList ID="ddlInsert_IsAdmin" runat="server" CssClass="input-text" Width="90%">
                            <asp:ListItem Text="0 - 普通用戶" Value="0"></asp:ListItem>
                            <asp:ListItem Text="1 - 管理員" Value="1"></asp:ListItem>
                            <asp:ListItem Text="2 - 工作人員" Value="2"></asp:ListItem>
                        </asp:DropDownList>
                    </td>
                </tr>
            </table>
            <div class="form-actions">
                <asp:Button ID="btnCancelInsert" runat="server" Text="取消新增" OnClick="btnCancelInsert_Click" CssClass="btn-style" Style="background-color: #dc3545;" />
                <asp:Button ID="btnInsertRecord" runat="server" Text="確認新增並儲存" OnClick="btnInsertRecord_Click" CssClass="btn-style btn-new-record" />
            </div>
        </asp:Panel>

        <%-- 分頁/每頁筆數控制 --%>
        <div class="pager-row">
            <asp:Label runat="server" Text="每頁顯示筆數:"></asp:Label>
            <asp:DropDownList ID="ddlPageSize" runat="server" AutoPostBack="True" OnSelectedIndexChanged="ddlPageSize_SelectedIndexChanged" 
                style="padding: 5px; border: 1px solid #ccc; border-radius: 4px;">
                <asp:ListItem Text="10" Value="10"></asp:ListItem>
                <asp:ListItem Text="15" Value="15"></asp:ListItem>
                <asp:ListItem Text="25" Value="25"></asp:ListItem>
                <asp:ListItem Text="50" Value="50"></asp:ListItem>
            </asp:DropDownList>
        </div>

        <%-- GridView 顯示資料 --%>
        <asp:GridView ID="gvUsers" runat="server" 
            AutoGenerateColumns="False" 
            DataKeyNames="UserID" 
            CssClass="gv-style" 
            AllowPaging="True" 
            AllowSorting="True"
            PageSize="15"
            ShowFooter="False" 
            OnPageIndexChanging="gvUsers_PageIndexChanging"
            OnSorting="gvUsers_Sorting"
            OnRowEditing="gvUsers_RowEditing"
            OnRowCancelingEdit="gvUsers_RowCancelingEdit"
            OnRowUpdating="gvUsers_RowUpdating"
            OnRowDeleting="gvUsers_RowDeleting"
            OnRowDataBound="gvUsers_RowDataBound">
            <Columns>
                <asp:BoundField DataField="UserID" HeaderText="ID" ReadOnly="True" SortExpression="UserID" />
                <asp:TemplateField HeaderText="帳號名稱" SortExpression="Username">
                    <ItemTemplate>
                        <asp:Label ID="lblUsername" runat="server" Text='<%# Eval("Username") %>'></asp:Label>
                    </ItemTemplate>
                    <EditItemTemplate>
                        <asp:TextBox ID="txtUsernameEdit" runat="server" Text='<%# Bind("Username") %>'></asp:TextBox>
                        <asp:RequiredFieldValidator runat="server" ControlToValidate="txtUsernameEdit" ErrorMessage="必填" ForeColor="Red" Display="Dynamic" />
                    </EditItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField HeaderText="密碼" SortExpression="Password">
                    <ItemTemplate>
                        <asp:Label ID="lblPasswordDisplay" runat="server" Text="***"></asp:Label>
                    </ItemTemplate>
                    <EditItemTemplate>
                        <asp:TextBox ID="txtPasswordEdit" runat="server" TextMode="Password" placeholder="留空則不更改密碼"></asp:TextBox>
                    </EditItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField HeaderText="電子郵件" SortExpression="Email">
                    <ItemTemplate>
                        <asp:Label ID="lblEmail" runat="server" Text='<%# Eval("Email") %>'></asp:Label>
                    </ItemTemplate>
                    <EditItemTemplate>
                        <asp:TextBox ID="txtEmailEdit" runat="server" Text='<%# Bind("Email") %>' TextMode="Email"></asp:TextBox>
                        <asp:RequiredFieldValidator runat="server" ControlToValidate="txtEmailEdit" ErrorMessage="必填" ForeColor="Red" Display="Dynamic" />
                        <asp:RegularExpressionValidator runat="server" ControlToValidate="txtEmailEdit" ValidationExpression="\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*" ErrorMessage="格式錯誤" ForeColor="Red" Display="Dynamic" />
                    </EditItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField HeaderText="權限" SortExpression="IsAdmin">
                    <ItemTemplate>
                        <asp:Label ID="lblIsAdmin" runat="server" Text='<%# GetAdminStatusText(Eval("IsAdmin")) %>'></asp:Label>
                    </ItemTemplate>
                    <EditItemTemplate>
                        <asp:DropDownList ID="ddlIsAdminEdit" runat="server" SelectedValue='<%# Bind("IsAdmin") %>'>
                            <asp:ListItem Text="0 - 普通用戶" Value="0"></asp:ListItem>
                            <asp:ListItem Text="1 - 管理員" Value="1"></asp:ListItem>
                            <asp:ListItem Text="2 - 工作人員" Value="2"></asp:ListItem>
                        </asp:DropDownList>
                    </EditItemTemplate>
                </asp:TemplateField>
                <asp:CommandField ShowEditButton="True" EditText="編輯" UpdateText="更新" CancelText="取消" />
                <asp:CommandField ShowDeleteButton="True" DeleteText="刪除" />
            </Columns>
        </asp:GridView>
    </div>
</asp:Content>