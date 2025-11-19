<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="BookCategory.aspx.cs" MasterPageFile="~/Site.Master" Inherits="aspnet.BookCategory" %>

<asp:Content ID="HeadContent" ContentPlaceHolderID="HeadContent" runat="server">
    <%-- 沿用 books.aspx 提供的 CSS 樣式 --%>
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
        
        /* 頁籤/模式切換按鈕 */
        .mode-toggle-btn {
            background-color: #6c757d;
            color: white;
            font-weight: bold;
            padding: 8px 16px;
            border: none;
            border-radius: 4px;
            cursor: pointer;
            transition: background-color 0.3s;
        }
        .mode-toggle-btn:hover {
            background-color: #5a6268;
        }
        .mode-toggle-btn.active {
            background-color: #007bff; /* 活躍模式使用藍色 */
        }
        .mode-toggle-btn.active:hover {
            background-color: #0056b3;
        }

        /* 訊息框樣式 */
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

        /* ----------------------- 書籍導向模式專用樣式 ----------------------- */
        .book-category-container {
            display: flex;
            gap: 20px;
        }
        
        .book-list-panel {
            flex: 1; /* 佔用較多空間，例如 60% */
        }
        .category-management-panel {
            flex: 1; /* 佔用較少空間，例如 40% */
            min-width: 400px; /* 確保右側有最小寬度 */
        }

        /* GridView 樣式 (gv-style - 沿用 books.aspx) */
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
        
        .selected-row td {
            background-color: #fff3cd !important; /* 選中的列高亮顯示 */
            border-left: 5px solid #ffc107 !important;
        }

        /* 藥丸標籤樣式 */
        .pill-tag {
            display: inline-flex;
            align-items: center;
            background-color: #007bff;
            color: white;
            padding: 5px 10px;
            border-radius: 15px;
            margin-right: 8px;
            margin-bottom: 8px;
            font-size: 0.9em;
        }
        .pill-tag-delete {
            margin-left: 8px;
            cursor: pointer;
            font-weight: bold;
            font-size: 1.1em;
            line-height: 1;
        }
        .pill-tag-delete:hover {
            color: #ffc107;
        }

        .add-category-tag {
            background-color: #28a745;
            cursor: pointer;
            padding: 5px 10px;
            border-radius: 15px;
            color: white;
            margin-right: 8px;
            margin-bottom: 8px;
            display: inline-block;
            transition: background-color 0.3s;
        }
        .add-category-tag:hover {
            background-color: #218838;
        }

        /* ----------------------- 類別導向模式專用樣式 ----------------------- */
        .category-book-list {
            margin-top: 20px;
        }
        .category-book-list h3 {
            background-color: #f8f9fa;
            border-left: 5px solid #dc3545;
            padding: 10px;
            margin-bottom: 10px;
            font-size: 1.2em;
        }
        .category-book-list ul {
            list-style: none;
            padding: 0;
        }
        .category-book-list li {
            border: 1px solid #eee;
            padding: 10px;
            margin-bottom: 8px;
            display: flex;
            justify-content: space-between;
            align-items: center;
            border-radius: 4px;
        }
        .category-book-list li:nth-child(even) {
            background-color: #fcfcfc;
        }
        .category-book-info {
            flex-grow: 1;
        }
        .category-book-info strong {
            color: #007bff;
        }
        .category-book-action {
            margin-left: 15px;
        }
        .btn-delete-record {
            background-color: #dc3545;
            color: white;
            padding: 5px 10px;
            border: none;
            border-radius: 4px;
            cursor: pointer;
        }
        .btn-delete-record:hover {
            background-color: #c82333;
        }

    </style>
</asp:Content>


<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <div class="admin-container">
        <h1 class="page-header">圖書館管理員專區 - 書籍類別關聯管理</h1>

        <asp:Panel ID="pnlMessage" runat="server" Visible="false" CssClass="message-box" role="alert">
            <asp:Label ID="lblMessage" runat="server" style="font-weight: bold;"></asp:Label>
        </asp:Panel>

        <div class="control-row">
            <asp:Button ID="btnBookMode" runat="server" Text="📚 書籍導向模式" OnClick="btnMode_Click" CssClass="mode-toggle-btn active" CommandArgument="BookMode" />
            <asp:Button ID="btnCategoryMode" runat="server" Text="🏷️ 類別導向模式" OnClick="btnMode_Click" CssClass="mode-toggle-btn" CommandArgument="CategoryMode" />
        </div>

        <asp:Panel ID="pnlBookMode" runat="server" Visible="true">
            <h2>書籍導向：管理單本書的類別</h2>
            
            <%-- 搜尋控制項 --%>
            <div class="control-row">
                <asp:DropDownList ID="ddlSearchColumn" runat="server" CssClass="search-input" Width="100px">
                    <asp:ListItem Value="Title" Text="書名"></asp:ListItem>
                    <asp:ListItem Value="Author" Text="作者"></asp:ListItem>
                    <asp:ListItem Value="ISBN" Text="ISBN"></asp:ListItem>
                </asp:DropDownList>
                <asp:TextBox ID="txtSearchKeyword" runat="server" CssClass="search-input" Placeholder="輸入書籍關鍵字..."></asp:TextBox>
                <asp:Button ID="btnSearch" runat="server" Text="🔍 搜尋" OnClick="btnSearch_Click" CssClass="search-btn" />
                <asp:Button ID="btnClearSearch" runat="server" Text="清除搜尋" OnClick="btnClearSearch_Click" CssClass="btn-action" />
            </div>

            <div class="book-category-container">
                <%-- 左側：書籍列表 --%>
                <asp:Panel ID="pnlBookList" runat="server" CssClass="book-list-panel">
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
                        OnRowCommand="gvBooks_RowCommand"
                        OnRowDataBound="gvBooks_RowDataBound">
                        <Columns>
                            <asp:BoundField DataField="BookID" HeaderText="ID" ReadOnly="True" SortExpression="BookID" />
                            <asp:BoundField DataField="Title" HeaderText="書名" SortExpression="Title" />
                            <asp:BoundField DataField="Author" HeaderText="作者" SortExpression="Author" />
                            <asp:TemplateField HeaderText="動作">
                                <ItemTemplate>
                                    <asp:LinkButton ID="lnkSelectBook" runat="server" Text="管理" CommandName="SelectBook" CommandArgument='<%# Eval("BookID") %>' />
                                </ItemTemplate>
                            </asp:TemplateField>
                        </Columns>
                    </asp:GridView>
                </asp:Panel>

                <%-- 右側：類別管理區 --%>
                <asp:Panel ID="pnlCategoryManagement" runat="server" CssClass="category-management-panel" Visible="false">
                    <h3>正在管理：<asp:Label ID="lblSelectedBookTitle" runat="server" ForeColor="#007bff" /> (ID: <asp:Label ID="lblSelectedBookID" runat="server" />)</h3>
                    
                    <h4>已關聯類別：</h4>
                    <asp:Repeater ID="rptBookCategories" runat="server" OnItemCommand="rptBookCategories_ItemCommand">
                        <ItemTemplate>
                            <span class="pill-tag" style="background-color: <%# Eval("ColorHex") %>;">
                                <%# Eval("CategoryName") %>
                                <asp:LinkButton ID="lnkDeleteCategory" runat="server" Text="&times;" CommandName="DeleteCategory" CommandArgument='<%# Eval("CategoryID") %>' CssClass="pill-tag-delete" ToolTip="刪除此關聯" />
                            </span>
                        </ItemTemplate>
                    </asp:Repeater>

                    <h4>新增類別：</h4>
                    <div class="control-row">
                        <asp:DropDownList ID="ddlAvailableCategories" runat="server" CssClass="search-input" Width="200px" />
                        <asp:Button ID="btnAddCategory" runat="server" Text="✚ 新增關聯" OnClick="btnAddCategory_Click" CssClass="add-category-tag" />
                    </div>
                </asp:Panel>
            </div>
        </asp:Panel>

        <asp:Panel ID="pnlCategoryMode" runat="server" Visible="false">
            <h2>類別導向：管理單一類別下的書籍</h2>
            
            <div class="control-row">
                <asp:DropDownList ID="ddlSelectCategory" runat="server" AutoPostBack="True" OnSelectedIndexChanged="ddlSelectCategory_SelectedIndexChanged" CssClass="search-input" Width="250px" />
            </div>

            <asp:Panel ID="pnlCategoryBooks" runat="server" CssClass="category-book-list" Visible="false">
                <h3>類別：<asp:Label ID="lblSelectedCategoryName" runat="server" ForeColor="#007bff" /> (ID: <asp:Label ID="lblSelectedCategoryID" runat="server" />)</h3>
                
                <asp:Repeater ID="rptCategoryBooks" runat="server" OnItemCommand="rptCategoryBooks_ItemCommand">
                    <HeaderTemplate>
                        <ul>
                    </HeaderTemplate>
                    <ItemTemplate>
                        <li>
                            <div class="category-book-info">
                                <strong>書名: <%# Eval("Title") %></strong> (ID: <%# Eval("BookID") %>) <br/>
                                作者: <%# Eval("Author") %>, ISBN: <%# Eval("ISBN") %>
                            </div>
                            <div class="category-book-action">
                                <asp:LinkButton ID="lnkDeleteBookFromCategory" runat="server" Text="從類別中刪除" CommandName="DeleteBookCategory" CommandArgument='<%# Eval("BookID") %>' CssClass="btn-delete-record" />
                            </div>
                        </li>
                    </ItemTemplate>
                    <FooterTemplate>
                        </ul>
                    </FooterTemplate>
                </asp:Repeater>
            </asp:Panel>
        </asp:Panel>
    </div>
</asp:Content>