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
color: white;
            border: 1px solid #0056b3;
            padding: 6px 12px;
            border-radius: 4px;
            cursor: pointer;
        }

        .btn-secondary {
            background-color: #6c757d;
color: white;
            border: 1px solid #545b62;
            padding: 6px 12px;
            border-radius: 4px;
            cursor: pointer;
        }

        .btn-primary:hover {
            background-color: #0056b3;
        }

        .btn-secondary:hover {
            background-color: #545b62;
        }
    </style>
</asp:Content>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <h2>書籍查詢與瀏覽</h2>

    <div runat="server" CssClass="quick-search-divider">
        <asp:Label ID="lblQuickSearch" runat="server" Text="快速查詢 (書名/作者/ISBN)："></asp:Label>
        <asp:TextBox ID="txtQuickSearch" runat="server" Width="300px"></asp:TextBox>
        <asp:Button ID="btnQuickSearch" runat="server" Text="快速搜尋" OnClick="btnQuickSearch_Click" CssClass="btn btn-secondary btn-sm" />
    </div>

    <div style="margin-bottom: 10px; text-align: left;">
        <asp:LinkButton ID="lnkToggleSearch" runat="server" Text="▼ 展開進階查詢"
            OnClientClick="toggleSearchPanel(); return false;"
/>
    </div>

    <asp:Panel ID="pnlAdvancedSearch" runat="server"
        CssClass="advanced-panel"
        style="display: none;">

        <table class="table-no-border">
            <tr>
                <td style="width: 120px;"><asp:Label ID="lblTitle" runat="server" Text="書名包含："></asp:Label></td>
                <td><asp:TextBox ID="txtSearchTitle" runat="server" Width="250px"></asp:TextBox></td>
            
                <td style="width: 120px;"><asp:Label ID="lblAuthor" runat="server" Text="作者包含："></asp:Label></td>
                <td><asp:TextBox ID="txtSearchAuthor" runat="server" Width="250px"></asp:TextBox></td>
            </tr>
            <tr>
                <td><asp:Label ID="lblISBN" runat="server" Text="ISBN："></asp:Label></td>
                <td><asp:TextBox ID="txtSearchISBN" runat="server" Width="250px"></asp:TextBox></td>
          
                <td><asp:Label ID="lblAvailable" runat="server" Text="庫存大於 0："></asp:Label></td>
                <td><asp:CheckBox ID="chkAvailableOnly" runat="server" Text="只顯示可借閱 (Available > 0)" Checked="true" /></td>
            </tr>
        </table>

        <div style="margin-top: 15px;
text-align: right;">
            <asp:Button ID="btnSearch" runat="server" Text="開始精準搜尋" OnClick="btnSearch_Click" CssClass="btn btn-primary" />
        </div>
    </asp:Panel>

    <asp:Label ID="lblResultInfo" runat="server" ForeColor="Blue" Text="" style="margin-bottom: 10px;
display: block;"></asp:Label>

    <div style="margin-bottom: 10px; text-align: right;">
        <asp:Label ID="lblPageSize" runat="server" Text="每頁顯示筆數: " AssociatedControlID="ddlPageSize"></asp:Label>
        <asp:DropDownList ID="ddlPageSize" runat="server" AutoPostBack="True" OnSelectedIndexChanged="ddlPageSize_SelectedIndexChanged">
            <asp:ListItem Text="5 筆" Value="5"></asp:ListItem>
            <asp:ListItem Text="10 筆" Value="10" Selected="True"></asp:ListItem>
            <asp:ListItem Text="20 筆" Value="20"></asp:ListItem>
            <asp:ListItem Text="50 筆" Value="50"></asp:ListItem>
        </asp:DropDownList>
    </div>

    <asp:GridView
        ID="gvBooks"
        runat="server"
        AutoGenerateColumns="False"
        AllowPaging="True"
        PageSize="10"
        AllowSorting="True"
        OnPageIndexChanging="gvBooks_PageIndexChanging"
        OnSorting="gvBooks_Sorting"
        OnRowDataBound="gvBooks_RowDataBound"
        OnRowCommand="gvBooks_RowCommand"
        EmptyDataText="找不到符合條件的書籍資料。">
       
        <Columns>
            <asp:BoundField DataField="BookID" HeaderText="ID" ReadOnly="True" SortExpression="BookID" />
            <asp:BoundField DataField="Title" HeaderText="書名" SortExpression="Title" />
            <asp:BoundField DataField="Author" HeaderText="作者" SortExpression="Author" />
            <asp:BoundField DataField="ISBN" HeaderText="ISBN" SortExpression="ISBN" />
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

            return false;
        }
    </script>
</asp:Content>