<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="search.aspx.cs" Inherits="Search" MasterPageFile="~/Site.Master" %>

<asp:Content ID="HeadContent" ContentPlaceHolderID="HeadContent" runat="server">
    <style type="text/css">
        .quick-search-divider {
            margin-bottom: 10px;
            padding: 10px 0;
            border-bottom: 2px solid #007bff;
        }

        .advanced-panel {
            border: 2px solid #ff9900;
            padding: 15px;
            border-radius: 8px;
            margin-bottom: 20px;
            background-color: #fffaf0;
        }

        #<%= gvBooks.ClientID %> thead th {
            background-color: #007bff !important;
            color: white !important;
            font-weight: bold;
            border: 1px solid #007bff !important;
        }

        #<%= gvBooks.ClientID %> th,
        #<%= gvBooks.ClientID %> td {
            border: 1px solid #c9c9c9;
            padding: 8px;
        }

        #<%= gvBooks.ClientID %> tr:nth-child(even) {
            background-color: #f8f8f8;
        }

        .btn-primary {
            background-color: #007bff;
            border-color: #007bff;
            color: white;
        }
        
        .result-message {
            font-size: 1.1em;
            margin: 15px 0;
            padding: 10px;
            border-radius: 5px;
            font-weight: bold;
        }
        .message-error {
            color: #dc3545;
            background-color: #f8d7da;
            border: 1px solid #f5c6cb;
        }
        .message-success {
            color: #28a745;
            background-color: #d4edda;
            border: 1px solid #c3e6cb;
        }
    </style>
</asp:Content>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <h2>書籍查詢與借閱</h2>
    <hr />

    <asp:Label ID="lblResultInfo" runat="server" CssClass="result-message"></asp:Label>

    <div class="quick-search-divider">
        <div style="display: flex; align-items: center; gap: 10px;">
            <asp:TextBox ID="txtQuickSearch" runat="server" Placeholder="輸入書名、作者或ISBN..." CssClass="form-control" Width="300px"></asp:TextBox>
            <asp:Button ID="btnQuickSearch" runat="server" Text="快速搜尋" OnClick="btnQuickSearch_Click" CssClass="btn btn-primary" />
            
            <asp:LinkButton ID="lnkToggleSearch" runat="server" Text="▼ 展開進階查詢" OnClientClick="toggleSearchPanel(); return false;" />
        </div>
    </div>

    <asp:Panel ID="pnlAdvancedSearch" runat="server" Visible="false" CssClass="advanced-panel">
        <div style="display: flex; gap: 20px; margin-bottom: 15px;">
            <asp:TextBox ID="txtTitle" runat="server" Placeholder="書名關鍵字" CssClass="form-control" />
            <asp:TextBox ID="txtAuthor" runat="server" Placeholder="作者名稱" CssClass="form-control" />
            <asp:TextBox ID="txtISBN" runat="server" Placeholder="ISBN" CssClass="form-control" />
        </div>
        <div style="display: flex; align-items: center; gap: 10px;">
            <asp:Label ID="Label1" runat="server" Text="書籍類別：" />
            <asp:DropDownList ID="ddlCategory" runat="server" CssClass="form-control" Width="200px" />
            <asp:Button ID="btnAdvancedSearch" runat="server" Text="進階搜尋" OnClick="btnAdvancedSearch_Click" CssClass="btn btn-primary" />
        </div>
    </asp:Panel>

    <div style="margin-bottom: 10px; text-align: right;">
        每頁顯示：
        <asp:DropDownList ID="ddlPageSize" runat="server" AutoPostBack="True" OnSelectedIndexChanged="ddlPageSize_SelectedIndexChanged">
            <asp:ListItem Value="10" Text="10" />
            <asp:ListItem Value="20" Text="20" />
            <asp:ListItem Value="50" Text="50" />
        </asp:DropDownList>
    </div>

    <asp:GridView 
        ID="gvBooks" 
        runat="server" 
        AutoGenerateColumns="False" 
        AllowPaging="True" 
        AllowSorting="True" 
        PageSize="10"
        DataKeyNames="BookID"
        OnPageIndexChanging="gvBooks_PageIndexChanging"
        OnSorting="gvBooks_Sorting"
        OnRowCommand="gvBooks_RowCommand"
        EmptyDataText="找不到符合條件的書籍。">
        
        <Columns>
            <asp:BoundField DataField="BookID" HeaderText="ID" ReadOnly="True" SortExpression="BookID" />
            <asp:BoundField DataField="Title" HeaderText="書名" SortExpression="Title" />
            <asp:BoundField DataField="Author" HeaderText="作者" SortExpression="Author" />
            <asp:BoundField DataField="ISBN" HeaderText="ISBN" SortExpression="ISBN" />
            <asp:BoundField DataField="Categories" HeaderText="類別" />
            <asp:BoundField DataField="TotalCopies" HeaderText="總數" SortExpression="TotalCopies" />
            <asp:BoundField DataField="AvailableCopies" HeaderText="可借數" SortExpression="AvailableCopies" />
  
            <asp:TemplateField HeaderText="動作">
                <ItemTemplate>
                    <asp:Button ID="btnBorrow" runat="server" Text="借閱" CssClass="btn btn-sm btn-primary"
                        CommandName="Borrow" CommandArgument='<%# Eval("BookID") %>' />
                </ItemTemplate>
            </asp:TemplateField>
        </Columns>
    </asp:GridView>

    <script type="text/javascript">
        // 負責切換進階查詢面板的 JavaScript
        function toggleSearchPanel() {
            var panelSelector = '#<%= pnlAdvancedSearch.ClientID %>';
            var link = $('#<%= lnkToggleSearch.ClientID %>');

            $(panelSelector).slideToggle(300, function () {
                if ($(panelSelector).is(':visible')) {
                    link.text('▲ 收合進階查詢');
                } else {
                    link.text('▼ 展開進階查詢');
                }
            });
            // 阻止 LinkButton 進行 PostBack
            return false; 
        }

        // 頁面載入時檢查是否需要顯示錯誤訊息的樣式
        $(document).ready(function () {
            var resultLabel = $('#<%= lblResultInfo.ClientID %>');
            if (resultLabel.text().includes('借閱失敗') || resultLabel.text().includes('錯誤')) {
                resultLabel.addClass('message-error').removeClass('message-success');
            } else if (resultLabel.text().includes('成功')) {
                resultLabel.addClass('message-success').removeClass('message-error');
            }
        });
    </script>
</asp:Content>