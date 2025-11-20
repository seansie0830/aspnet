<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="catLookup.aspx.cs" MasterPageFile="~/Site.Master" Inherits="aspnet.catLookup" %>

<asp:Content ID="HeadContent" ContentPlaceHolderID="HeadContent" runat="server">
    <style type="text/css">
        .lookup-container {
            max-width: 1000px;
            margin: 20px auto;
            padding: 20px;
            background-color: #ffffff;
            border-radius: 8px;
            box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
            font-family: Arial, sans-serif;
        }

        .page-header {
            color: #17a2b8;
            font-size: 24px;
            font-weight: bold;
            border-bottom: 3px solid #17a2b8;
            padding-bottom: 10px;
            margin-bottom: 20px;
        }
        
        .message-box {
            padding: 15px;
            border-radius: 6px;
            margin-bottom: 15px;
            border: 1px solid transparent;
            font-weight: bold;
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
        
        /* New Control Panel Layout */
        .control-panel {
            display: flex;
            justify-content: space-between;
            align-items: center;
            margin-bottom: 20px;
        }
        
        .book-control-panel {
            display: flex;
            justify-content: space-between;
            align-items: center;
            margin-top: 15px;
            padding: 10px 0;
            border-bottom: 1px solid #eee;
        }
        
        .book-control-group {
            display: flex;
            align-items: center;
            gap: 15px;
        }
        
        .search-panel {
            display: flex;
            gap: 10px;
            flex-grow: 1;
            margin-right: 20px;
        }
        
        .page-size-panel {
            display: flex;
            align-items: center;
            gap: 5px;
            white-space: nowrap;
            font-size: 0.9em;
        }

        .search-box, .sort-dropdown, .page-size-dropdown {
            padding: 8px 10px;
            border: 1px solid #ccc;
            border-radius: 4px;
        }

        .search-box {
            flex-grow: 1;
        }

        .search-btn {
            background-color: #007bff;
            color: white;
            border: none;
            padding: 10px 15px;
            border-radius: 4px;
            cursor: pointer;
            transition: background-color 0.3s;
        }
        .search-btn:hover {
            background-color: #0056b3;
        }
        /* End New Control Panel Layout */


        .category-grid {
            display: grid;
            grid-template-columns: repeat(auto-fill, minmax(220px, 1fr));
            gap: 20px;
            margin-top: 20px;
        }

        .category-card {
            background-color: #f8f9fa;
            border-radius: 8px;
            box-shadow: 0 2px 4px rgba(0, 0, 0, 0.05);
            transition: transform 0.2s, box-shadow 0.2s;
            overflow: hidden;
            text-align: center;
        }

        .category-card:hover {
            transform: translateY(-3px);
            box-shadow: 0 6px 12px rgba(0, 0, 0, 0.1);
        }

        .category-link {
            display: block;
            padding: 15px;
            text-decoration: none;
            color: #343a40;
            font-weight: bold;
        }

        .category-name {
            font-size: 1.2em;
            margin-bottom: 5px;
            display: block;
        }

        .category-count {
            font-size: 0.9em;
            color: #6c757d;
        }
        
        .category-color-tag {
            height: 5px;
            width: 100%;
            margin-bottom: 10px;
            display: block;
        }
        
        .book-list-header {
            background-color: #e9ecef;
            border-left: 5px solid #17a2b8;
            padding: 10px;
            margin-bottom: 15px;
            font-size: 1.4em;
            display: flex;
            justify-content: space-between;
            align-items: center;
        }

        .category-book-list ul {
            list-style: none;
            padding: 0;
        }
        .category-book-list li {
            border: 1px solid #eee;
            padding: 10px;
            margin-bottom: 8px;
            border-radius: 4px;
        }
        .category-book-list li:nth-child(even) {
            background-color: #fcfcfc;
        }
        .book-info strong {
            color: #007bff;
        }
        .book-info a {
            color: #007bff;
            text-decoration: none;
            font-weight: bold;
        }
        .book-info a:hover {
            text-decoration: underline;
        }
        .btn-back {
            background-color: #6c757d;
            color: white;
            padding: 8px 16px;
            border: none;
            border-radius: 4px;
            cursor: pointer;
            text-decoration: none;
            transition: background-color 0.3s;
        }
        .btn-back:hover {
            background-color: #5a6268;
        }
        
        /* Chinese Classification Specific Styles */
        .chinese-main-cat {
            background-color: #f0f8ff;
            border: 1px solid #cce5ff;
            padding: 15px;
            margin-bottom: 20px;
            border-radius: 6px;
        }
        .chinese-main-cat h3 {
            color: #007bff;
            border-bottom: 2px solid #007bff;
            padding-bottom: 5px;
            margin-top: 0;
            margin-bottom: 10px;
            font-size: 1.5em;
            font-weight: bold;
        }
        .chinese-sub-cat-list {
            list-style: none;
            padding: 0;
            display: flex;
            flex-wrap: wrap;
            gap: 10px;
        }
        .chinese-sub-cat-list li {
            background-color: #ffffff;
            border: 1px solid #e9ecef;
            border-radius: 4px;
            transition: box-shadow 0.2s;
        }
        .chinese-sub-cat-list li:hover {
            box-shadow: 0 2px 5px rgba(0, 0, 0, 0.1);
        }
        .chinese-sub-cat-link {
            display: block;
            padding: 10px 15px;
            text-decoration: none;
            color: #343a40;
            font-weight: normal;
            font-size: 0.9em;
        }
        .chinese-sub-cat-link span {
            margin-left: 5px;
            color: #6c757d;
            font-size: 0.9em;
        }
        .toggle-section {
            display: flex;
            justify-content: flex-end;
            margin-bottom: 15px;
        }
        .toggle-section .btn-back {
            padding: 10px 20px;
            background-color: #17a2b8;
        }
        .toggle-section .btn-back:hover {
            background-color: #138496;
        }

        /* Pagination Styles */
        .pagination-panel {
            text-align: center;
            margin-top: 20px;
            padding-top: 10px;
            border-top: 1px solid #eee;
        }
        .page-link {
            display: inline-block;
            padding: 8px 12px;
            margin: 0 4px;
            border: 1px solid #ccc;
            text-decoration: none;
            color: #007bff;
            border-radius: 4px;
        }
        .page-link:hover {
            background-color: #e9ecef;
        }
        .current-page {
            background-color: #007bff;
            color: white;
            border-color: #007bff;
            font-weight: bold;
        }
        .current-page:hover {
            background-color: #0056b3;
            color: white;
        }
    </style>
</asp:Content>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <div class="lookup-container">
        <h1 class="page-header">📚 依類別瀏覽書籍</h1>

        <asp:Panel ID="pnlMessage" runat="server" Visible="false" CssClass="message-box" role="alert">
            <asp:Label ID="lblMessage" runat="server"></asp:Label>
        </asp:Panel>
        
        <div class="toggle-section">
            <asp:Button ID="btnToggleMode" runat="server" OnClick="btnToggleMode_Click" CssClass="btn-back" CausesValidation="False" Text="切換至：其他類別" />
        </div>

        <asp:Panel ID="pnlCategoriesContainer" runat="server" Visible="true">
            
            <asp:Panel ID="pnlChineseClassification" runat="server" Visible="true">
                <h2>中文圖書分類 (TDC)</h2>
                <asp:Repeater ID="rptChineseClassification" runat="server" OnItemCommand="rptCategories_ItemCommand">
                    <ItemTemplate>
                        <div class="chinese-main-cat">
                            <h3><%# Eval("MainCategoryName") %></h3>
                            <asp:Repeater ID="rptSubCategories" runat="server" DataSource='<%# Eval("SubCategories") %>' OnItemCommand="rptCategories_ItemCommand">
                                <HeaderTemplate>
                                    <ul class="chinese-sub-cat-list">
                                </HeaderTemplate>
                                <ItemTemplate>
                                    <li>
                                        <asp:LinkButton ID="lnkSelectSubCategory" runat="server" 
                                            CommandName="SelectCategory" 
                                            CommandArgument='<%# Eval("CategoryID") %>' 
                                            CssClass="chinese-sub-cat-link">
                                            <%# Eval("CategoryName") %> <span>(共 <%# Eval("BookCount") %> 本)</span>
                                        </asp:LinkButton>
                                    </li>
                                </ItemTemplate>
                                <FooterTemplate>
                                    </ul>
                                </FooterTemplate>
                            </asp:Repeater>
                        </div>
                    </ItemTemplate>
                </asp:Repeater>
            </asp:Panel>

            <asp:Panel ID="pnlOtherCategories" runat="server" Visible="false">
                <h2>其他類別 (非中文圖書分類)</h2>
                
                <div class="control-panel">
                    <div class="search-panel">
                        <asp:TextBox ID="txtSearchOther" runat="server" CssClass="search-box" placeholder="輸入類別名稱進行篩選..." />
                        <asp:Button ID="btnSearchOther" runat="server" Text="搜尋" OnClick="btnSearchOther_Click" CssClass="search-btn" CausesValidation="False" />
                    </div>
                    <div class="page-size-panel">
                        <label for="<%= ddlPageSizeOther.ClientID %>">每頁顯示:</label>
                        <asp:DropDownList ID="ddlPageSizeOther" runat="server" AutoPostBack="True" OnSelectedIndexChanged="ddlPageSizeOther_SelectedIndexChanged" CssClass="page-size-dropdown">
                            <asp:ListItem Text="12 筆" Value="12" />
                            <asp:ListItem Text="24 筆" Value="24" />
                            <asp:ListItem Text="48 筆" Value="48" />
                            <asp:ListItem Text="所有" Value="9999" />
                        </asp:DropDownList>
                    </div>
                </div>
                <asp:Repeater ID="rptOtherCategories" runat="server" OnItemCommand="rptCategories_ItemCommand">
                    <HeaderTemplate>
                        <div class="category-grid">
                    </HeaderTemplate>
                    <ItemTemplate>
                        <div class="category-card">
                            <span class="category-color-tag" style="background-color: <%# Eval("ColorHex") %>;"></span>
                            <asp:LinkButton ID="lnkSelectCategory" runat="server" 
                                CommandName="SelectCategory" 
                                CommandArgument='<%# Eval("CategoryID") %>' 
                                CssClass="category-link">
                                <span class="category-name"><%# Eval("CategoryName") %></span>
                                <span class="category-count">共 <%# Eval("BookCount") %> 本書</span>
                            </asp:LinkButton>
                        </div>
                    </ItemTemplate>
                    <FooterTemplate>
                        </div>
                    </FooterTemplate>
                </asp:Repeater>

                <asp:Panel ID="pnlPaginationOther" runat="server" CssClass="pagination-panel">
                    <asp:Repeater ID="rptPagerOther" runat="server">
                        <ItemTemplate>
                            <asp:LinkButton ID="lnkPageOther" runat="server" 
                                CommandName="Page" 
                                CommandArgument='<%# Eval("Value") %>' 
                                OnClick="lnkPageOther_Click"
                                CssClass='<%# (Convert.ToInt32(Eval("Value")) - 1) == CurrentPage ?
"page-link current-page" : "page-link" %>'
                                Text='<%# Eval("Text") %>' CausesValidation="False" />
                        </ItemTemplate>
                    </asp:Repeater>
                </asp:Panel>
            </asp:Panel>

        </asp:Panel>

        <asp:Panel ID="pnlCategoryBooks" runat="server" Visible="false" CssClass="category-book-list">
            <div class="book-list-header">
                正在瀏覽類別：<asp:Label ID="lblSelectedCategoryName" runat="server" ForeColor="#007bff" />
                <asp:Button ID="btnBackToCategories" runat="server" Text="← 返回類別列表" OnClick="btnBackToCategories_Click" CssClass="btn-back" CausesValidation="False" />
            </div>
            
            <div class="book-control-panel">
                <asp:Label ID="lblBookCount" runat="server" ForeColor="#6c757d" />
                <div class="book-control-group">
                    <div class="page-size-panel">
                        <label for="<%= ddlSortBy.ClientID %>">排序依據:</label>
                        <asp:DropDownList ID="ddlSortBy" runat="server" AutoPostBack="True" OnSelectedIndexChanged="ddlSortBy_SelectedIndexChanged" CssClass="sort-dropdown">
                            <asp:ListItem Text="書名 (預設)" Value="Title" />
                            <asp:ListItem Text="作者" Value="Author" />
                            <asp:ListItem Text="ISBN" Value="ISBN" />
                        </asp:DropDownList>
                    </div>
                    <div class="page-size-panel">
                        <label for="<%= ddlPageSizeBooks.ClientID %>">每頁顯示:</label>
                        <asp:DropDownList ID="ddlPageSizeBooks" runat="server" AutoPostBack="True" OnSelectedIndexChanged="ddlPageSizeBooks_SelectedIndexChanged" CssClass="page-size-dropdown">
                            <asp:ListItem Text="12 筆" Value="12" />
                            <asp:ListItem Text="24 筆" Value="24" />
                            <asp:ListItem Text="48 筆" Value="48" />
                            <asp:ListItem Text="所有" Value="9999" />
                        </asp:DropDownList>
                    </div>
                </div>
            </div>

            <asp:Repeater ID="rptCategoryBooks" runat="server">
                <HeaderTemplate>
                    <ul>
                </HeaderTemplate>
                <ItemTemplate>
                    <li>
                        <div class="book-info">
                            <a href='<%# "Search.aspx?bookid=" + Eval("BookID") %>'>
                                <strong>書名: <%# Eval("Title") %></strong>
                            </a> (ID: <%# Eval("BookID") %>) <br/>
                            作者: <%# Eval("Author") %>, ISBN: <%# Eval("ISBN") %>
                        </div>
                    </li>
                </ItemTemplate>
                <FooterTemplate>
                    </ul>
                </FooterTemplate>
            </asp:Repeater>

            <asp:Panel ID="pnlPaginationBooks" runat="server" CssClass="pagination-panel">
                <asp:Repeater ID="rptPagerBooks" runat="server">
                    <ItemTemplate>
                        <asp:LinkButton ID="lnkPageBooks" runat="server" 
                            CommandName="Page" 
                            CommandArgument='<%# Eval("Value") %>' 
                            OnClick="lnkPageBooks_Click"
                            CssClass='<%# (Convert.ToInt32(Eval("Value")) - 1) == CurrentPage ?
"page-link current-page" : "page-link" %>'
                            Text='<%# Eval("Text") %>' CausesValidation="False" />
                    </ItemTemplate>
                </asp:Repeater>
            </asp:Panel>
        </asp:Panel>
    </div>
</asp:Content>