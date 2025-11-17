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

    </style>
</asp:Content>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <div class="lookup-container">
        <h1 class="page-header">📚 依類別瀏覽書籍</h1>

        <asp:Panel ID="pnlMessage" runat="server" Visible="false" CssClass="message-box" role="alert">
            <asp:Label ID="lblMessage" runat="server"></asp:Label>
        </asp:Panel>

        <asp:Panel ID="pnlCategories" runat="server" Visible="true">
            <h2>所有書籍類別</h2>
          
            <asp:Repeater ID="rptCategories" runat="server" OnItemCommand="rptCategories_ItemCommand">
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
                            <span class="category-count">共 
                                <%# Eval("BookCount") %> 本書</span>
                        </asp:LinkButton>
                    </div>
                </ItemTemplate>
                <FooterTemplate>
                    </div>
 
                </FooterTemplate>
            </asp:Repeater>
        </asp:Panel>

        <asp:Panel ID="pnlCategoryBooks" runat="server" Visible="false" CssClass="category-book-list">
            <div class="book-list-header">
                正在瀏覽類別：<asp:Label ID="lblSelectedCategoryName" runat="server" ForeColor="#007bff" />
          
                <asp:Button ID="btnBackToCategories" runat="server" Text="← 返回類別列表" OnClick="btnBackToCategories_Click" CssClass="btn-back" CausesValidation="False" />
            </div>
            
            <asp:Repeater ID="rptCategoryBooks" runat="server">
                <HeaderTemplate>
                    <ul>
            
                </HeaderTemplate>
                <ItemTemplate>
                    <li>
                        <div class="book-info">
                            <strong>書名: <%# Eval("Title") %></strong> (ID: <%# Eval("BookID") 
                            %>) <br/>
                            作者: <%# Eval("Author") %>, ISBN: <%# Eval("ISBN") %>
                        </div>
                    </li>
                </ItemTemplate>
    
                <FooterTemplate>
                    </ul>
                </FooterTemplate>
            </asp:Repeater>
        </asp:Panel>
    </div>
</asp:Content>