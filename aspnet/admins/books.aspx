<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="books.aspx.cs" Inherits="aspnet.books" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
<meta http-equiv="Content-Type" content="text/html; charset=utf-8"/>
    <title>書籍管理</title>
    <style>
        /* 繼承自其他檔案的通用樣式應在此處引用或定義 */
        body {
            font-family: Arial, sans-serif;
            margin: 0;
            padding: 0;
            background-color: #f4f4f9;
        }

        .container {
            max-width: 1200px;
            margin: 20px auto;
            padding: 20px;
            background-color: #fff;
            box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
            border-radius: 8px;
        }

        /* --------------------------------- */
        /* 綠色系麵包屑導覽列 CSS (Breadcrumb) */
        /* --------------------------------- */
        .breadcrumb {
            padding: 10px 0;
            margin-bottom: 20px;
            list-style: none;
            background-color: transparent;
            border-bottom: 1px solid #e0e0e0;
        }

        .breadcrumb > li {
            display: inline-block;
        }

        .breadcrumb > li + li:before {
            padding: 0 8px;
            color: #ccc;
            content: "〉"; /* 使用全形或半形分隔符 */
        }

        .breadcrumb > .active {
            color: #10B981; /* 綠色系的亮點 */
            font-weight: bold;
        }

        .breadcrumb a {
            color: #059669; /* 較深的綠色連結 */
            text-decoration: none;
            transition: color 0.2s;
        }

        .breadcrumb a:hover {
            color: #047857; /* 鼠標懸停時更深的綠色 */
        }

        /* 訊息框樣式 (沿用 .cs 檔案中的類別) */
        .message-box {
            padding: 15px;
            margin-bottom: 20px;
            border: 1px solid transparent;
            border-radius: 4px;
            font-weight: bold;
        }

        .message-box-error {
            color: #721c24;
            background-color: #f8d7da;
            border-color: #f5c6cb;
        }

        .message-box-success {
            color: #0f5132;
            background-color: #d1e7dd;
            border-color: #badbcc;
        }
        
        .header-section {
            display: flex;
            justify-content: space-between;
            align-items: center;
            margin-bottom: 20px;
        }

        .search-panel {
            display: flex;
            gap: 10px;
            align-items: center;
        }
        
        /* GridView 基礎樣式 */
        .gridview {
            width: 100%;
            border-collapse: collapse;
            margin-top: 15px;
            box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
        }

        .gridview th {
            background-color: #D1FAE5; /* 綠色系的表頭 */
            color: #047857;
            padding: 12px;
            text-align: left;
            border: 1px solid #E5E7EB;
            cursor: pointer;
        }

        .gridview td {
            padding: 10px 12px;
            border: 1px solid #E5E7EB;
        }

        .gridview tr:nth-child(even) {
            background-color: #F9FAFB;
        }
        
        .gridview tr:hover {
            background-color: #F0FDF4;
        }

        .gridview a {
            color: #059669;
            text-decoration: none;
        }
        
        .gridview input[type="text"] {
            padding: 5px;
            border: 1px solid #D1D5DB;
            border-radius: 4px;
        }
        
        .gridview .edit-button, .gridview .delete-button, .gridview .update-button, .gridview .cancel-button {
            padding: 5px 10px;
            border: none;
            border-radius: 4px;
            cursor: pointer;
            margin-right: 5px;
            transition: background-color 0.2s;
        }

        .gridview .edit-button { background-color: #6EE7B7; color: #065F46; }
        .gridview .edit-button:hover { background-color: #34D399; }

        .gridview .delete-button { background-color: #FECACA; color: #B91C1C; }
        .gridview .delete-button:hover { background-color: #FCA5A5; }

        .gridview .update-button { background-color: #A7F3D0; color: #065F46; }
        .gridview .update-button:hover { background-color: #6EE7B7; }
        
        .gridview .cancel-button { background-color: #D1D5DB; color: #374151; }
        .gridview .cancel-button:hover { background-color: #9CA3AF; }

        /* 分頁樣式 */
        .pager-row td {
            background-color: #E5F3F6;
            text-align: right;
            padding: 10px;
        }
        .pager-row a, .pager-row span {
            padding: 5px 10px;
            margin: 0 2px;
            border: 1px solid #D1D5DB;
            border-radius: 4px;
            text-decoration: none;
            color: #059669;
        }
        .pager-row span {
            font-weight: bold;
            background-color: #059669;
            color: white;
            border-color: #059669;
        }
        
        .search-panel input[type="text"], .search-panel select {
            padding: 8px;
            border: 1px solid #D1D5DB;
            border-radius: 4px;
        }

        .search-panel .btn {
            padding: 8px 15px;
            border: none;
            border-radius: 4px;
            cursor: pointer;
            font-weight: bold;
        }
        
        .search-panel .btn-primary {
            background-color: #059669; /* 綠色 */
            color: white;
        }
        .search-panel .btn-primary:hover {
            background-color: #047857;
        }

        .search-panel .btn-secondary {
            background-color: #D1FAE5; /* 淺綠色 */
            color: #047857;
        }
        .search-panel .btn-secondary:hover {
            background-color: #A7F3D0;
        }

    </style>
</head>
<body>
    <form id="form1" runat="server">
        <div class="container">
            
            <!-- 舊的導覽列已被移除 -->

            <!-- 新的麵包屑導覽列 -->
            <ol class="breadcrumb">
                <li><a href="main.aspx">管理員</a></li>
                <li class="active">書籍管理</li>
            </ol>
            
            <h1 style="color: #047857; border-bottom: 2px solid #34D399; padding-bottom: 5px;">書籍管理</h1>

            <!-- 訊息面板 -->
            <asp:Panel ID="pnlMessage" runat="server" Visible="false" CssClass="message-box">
                <asp:Label ID="lblMessage" runat="server"></asp:Label>
            </asp:Panel>
            
            <div class="header-section">
                <!-- 搜尋面板 -->
                <div class="search-panel">
                    <asp:DropDownList ID="ddlSearchColumn" runat="server">
                        <asp:ListItem Value="Title">書名</asp:ListItem>
                        <asp:ListItem Value="Author">作者</asp:ListItem>
                        <asp:ListItem Value="ISBN">ISBN</asp:ListItem>
                    </asp:DropDownList>
                    <asp:TextBox ID="txtSearch" runat="server" placeholder="輸入關鍵字"></asp:TextBox>
                    <asp:Button ID="btnSearch" runat="server" Text="搜尋" OnClick="btnSearch_Click" CssClass="btn btn-primary" />
                    <asp:Button ID="btnClearSearch" runat="server" Text="清除搜尋" OnClick="btnClearSearch_Click" CssClass="btn btn-secondary" />
                </div>
                
                <!-- 假設新增書籍按鈕 (如果您的應用程式需要) -->
                <%-- <asp:Button ID="btnAddBook" runat="server" Text="新增書籍" CssClass="btn btn-primary" /> --%>
            </div>

            <!-- GridView -->
            <asp:GridView ID="gvBooks" runat="server" 
                AutoGenerateColumns="false" 
                DataKeyNames="BookID" 
                AllowPaging="true" 
                PageSize="10" 
                AllowSorting="true"
                OnPageIndexChanging="gvBooks_PageIndexChanging" 
                OnSorting="gvBooks_Sorting"
                OnRowEditing="gvBooks_RowEditing"
                OnRowCancelingEdit="gvBooks_RowCancelingEdit"
                OnRowUpdating="gvBooks_RowUpdating"
                OnRowDeleting="gvBooks_RowDeleting"
                OnRowDataBound="gvBooks_RowDataBound"
                CssClass="gridview"
                PagerStyle-CssClass="pager-row">
                <Columns>
                    <asp:BoundField DataField="BookID" HeaderText="ID" SortExpression="BookID" ReadOnly="true" />
                    <asp:BoundField DataField="Title" HeaderText="書名" SortExpression="Title" />
                    <asp:BoundField DataField="Author" HeaderText="作者" SortExpression="Author" />
                    <asp:BoundField DataField="ISBN" HeaderText="ISBN" SortExpression="ISBN" />
                    <asp:TemplateField HeaderText="總本數" SortExpression="TotalCopies">
                        <ItemTemplate>
                            <asp:Label ID="lblTotalCopies" runat="server" Text='<%# Eval("TotalCopies") %>'></asp:Label>
                        </ItemTemplate>
                        <EditItemTemplate>
                            <asp:TextBox ID="txtTotalCopies" runat="server" Text='<%# Bind("TotalCopies") %>' Width="50px"></asp:TextBox>
                        </EditItemTemplate>
                    </asp:TemplateField>
                    <asp:TemplateField HeaderText="可借數" SortExpression="AvailableCopies">
                        <ItemTemplate>
                            <asp:Label ID="lblAvailableCopies" runat="server" Text='<%# Eval("AvailableCopies") %>'></asp:Label>
                        </ItemTemplate>
                        <EditItemTemplate>
                            <asp:TextBox ID="txtAvailableCopies" runat="server" Text='<%# Bind("AvailableCopies") %>' Width="50px"></asp:TextBox>
                        </EditItemTemplate>
                    </asp:TemplateField>
                    <asp:TemplateField HeaderText="操作">
                        <ItemTemplate>
                            <asp:Button ID="btnEdit" runat="server" CommandName="Edit" Text="編輯" CssClass="edit-button" />
                            <asp:Button ID="btnDelete" runat="server" CommandName="Delete" Text="刪除" CssClass="delete-button" OnClientClick="return confirm('確定要刪除這本書籍嗎?');" />
                        </ItemTemplate>
                        <EditItemTemplate>
                            <asp:Button ID="btnUpdate" runat="server" CommandName="Update" Text="更新" CssClass="update-button" />
                            <asp:Button ID="btnCancel" runat="server" CommandName="Cancel" Text="取消" CssClass="cancel-button" />
                        </EditItemTemplate>
                    </asp:TemplateField>
                </Columns>
            </asp:GridView>

        </div>
    </form>
</body>
</html>