<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="books.aspx.cs" MasterPageFile="~/Site.Master" Inherits="aspnet.books" %>

<asp:Content ID="HeadContent" ContentPlaceHolderID="HeadContent" runat="server">
    <%-- 沿用 AdminPage.aspx 提供的 CSS 樣式 --%>
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

        .insert-form-panel {
            border: 1px solid #ccc;
            padding: 20px;
            border-radius: 6px;
            background-color: #f8f9fa; 
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
            background-color: #28a745;
            margin-left: 10px;
        }
        .btn-submit:hover {
            background-color: #218838;
        }
        .btn-cancel {
            background-color: #dc3545;
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
            cursor: pointer; /* 新增：排序指示 */
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
        /* 搜尋樣式 */
        .search-container {
            display: flex;
            align-items: center;
            gap: 10px;
        }
        .search-input {
            padding: 8px;
            border: 1px solid #ccc;
            border-radius: 4px;
            width: 250px;
        }
        .search-btn {
            background-color: #007bff;
            color: white;
            padding: 8px 16px;
            border: none;
            border-radius: 4px;
            cursor: pointer;
            transition: background-color 0.3s;
        }
        .search-btn:hover {
            background-color: #0056b3;
        }
    </style>
</asp:Content>


<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <div class="admin-container">
        <h1 class="page-header">圖書館管理員專區 - 書籍管理 (Books)</h1>
        
        <asp:Panel ID="pnlMessage" runat="server" Visible="false" CssClass="message-box" role="alert">
            <asp:Label ID="lblMessage" runat="server" style="font-weight: bold;"></asp:Label>
        </asp:Panel>
        
        <div class="control-row">
            <%-- 搜尋控制項 --%>
            <asp:Panel ID="pnlSearch" runat="server" CssClass="search-container">
                <asp:DropDownList ID="ddlSearchColumn" runat="server" CssClass="search-input" Width="100px">
                    <asp:ListItem Value="Title" Text="書名"></asp:ListItem>
                    <asp:ListItem Value="Author" Text="作者"></asp:ListItem>
                    <asp:ListItem Value="ISBN" Text="ISBN"></asp:ListItem>
                </asp:DropDownList>
                <asp:TextBox ID="txtSearchKeyword" runat="server" CssClass="search-input" Placeholder="輸入關鍵字..."></asp:TextBox>
                <asp:Button ID="btnSearch" runat="server" Text="🔍 搜尋" OnClick="btnSearch_Click" CssClass="search-btn" />
                <asp:Button ID="btnClearSearch" runat="server" Text="清除搜尋" OnClick="btnClearSearch_Click" CssClass="btn-action" />
            </asp:Panel>
            
            <%-- 每頁筆數選擇與新增按鈕 --%>
            <asp:Label runat="server" Text="每頁筆數:"></asp:Label>
            <asp:DropDownList ID="ddlPageSize" runat="server" AutoPostBack="True" OnSelectedIndexChanged="ddlPageSize_SelectedIndexChanged" 
                style="padding: 5px; border: 1px solid #ccc; border-radius: 4px;">
                <asp:ListItem Value="10" Text="10"></asp:ListItem>
                <asp:ListItem Value="15" Text="15" Selected="True"></asp:ListItem>
                <asp:ListItem Value="20" Text="20"></asp:ListItem>
                <asp:ListItem Value="50" Text="50"></asp:ListItem>
            </asp:DropDownList>
            
            <asp:Button ID="btnShowInsert" runat="server" Text="✚ 新增書籍" OnClick="btnShowInsert_Click" CssClass="btn-new-record" />
        </div>

        <asp:Panel ID="pnlInsertForm" runat="server" Visible="False" CssClass="insert-form-panel">
            <asp:Literal ID="litInsertHeader" runat="server"></asp:Literal>
            <asp:PlaceHolder ID="phInsertFormControls" runat="server">
                <table class="insert-form-table">
                    <tr>
                        <td>書名 (Title):</td>
                        <td><asp:TextBox ID="txtInsert_Title" runat="server" CssClass="input-insert-form" MaxLength="200" ToolTip="必填欄位"></asp:TextBox></td>
                    </tr>
                    <tr>
                        <td>作者 (Author):</td>
                        <td><asp:TextBox ID="txtInsert_Author" runat="server" CssClass="input-insert-form" MaxLength="100"></asp:TextBox></td>
                    </tr>
                    <tr>
                        <td>ISBN:</td>
                        <td><asp:TextBox ID="txtInsert_ISBN" runat="server" CssClass="input-insert-form" MaxLength="20" ToolTip="必須是唯一值"></asp:TextBox></td>
                    </tr>
                    <tr>
                        <td>總本數 (TotalCopies):</td>
                        <td><asp:TextBox ID="txtInsert_TotalCopies" runat="server" CssClass="input-insert-form" TextMode="Number" Text="1" ToolTip="必須是正整數"></asp:TextBox></td>
                    </tr>
                    <tr>
                        <td>可借閱本數 (AvailableCopies):</td>
                        <td><asp:TextBox ID="txtInsert_AvailableCopies" runat="server" CssClass="input-insert-form" TextMode="Number" Text="1" ToolTip="必須是正整數，且不能大於總本數"></asp:TextBox></td>
                    </tr>
                </table>
            </asp:PlaceHolder>
            
            <div class="form-actions">
                <asp:Button ID="btnCancelInsert" runat="server" Text="取消新增" OnClick="btnCancelInsert_Click" CssClass="btn-action btn-cancel" />
                <asp:Button ID="btnInsertRecord" runat="server" Text="確認新增並儲存" OnClick="btnInsertRecord_Click" CssClass="btn-action btn-submit" />
            </div>
        </asp:Panel>

        <asp:GridView ID="gvBooks" runat="server" 
            AutoGenerateColumns="False" 
            DataKeyNames="BookID" 
            CssClass="gv-style" 
            AllowPaging="True" 
            PageSize="15"
            AllowSorting="True"
            ShowFooter="False" 
            OnPageIndexChanging="gvBooks_PageIndexChanging"
            OnSorting="gvBooks_Sorting"
            OnRowEditing="gvBooks_RowEditing"
            OnRowCancelingEdit="gvBooks_RowCancelingEdit"
            OnRowUpdating="gvBooks_RowUpdating"
            OnRowDeleting="gvBooks_RowDeleting"
            OnRowDataBound="gvBooks_RowDataBound">
            
            <Columns>
                <asp:BoundField DataField="BookID" HeaderText="ID" ReadOnly="True" SortExpression="BookID" />
                <asp:BoundField DataField="Title" HeaderText="書名" SortExpression="Title" />
                <asp:BoundField DataField="Author" HeaderText="作者" SortExpression="Author" />
                <asp:BoundField DataField="ISBN" HeaderText="ISBN" SortExpression="ISBN" />
                <asp:BoundField DataField="TotalCopies" HeaderText="總本數" SortExpression="TotalCopies" />
                <asp:BoundField DataField="AvailableCopies" HeaderText="可借數" SortExpression="AvailableCopies" />
                <asp:CommandField ShowEditButton="True" EditText="編輯" UpdateText="更新" CancelText="取消" />
                <asp:CommandField ShowDeleteButton="True" DeleteText="刪除" />
            </Columns>
        </asp:GridView>
    </div>
</asp:Content>