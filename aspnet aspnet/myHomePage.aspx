<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="myHomePage.aspx.cs" Inherits="aspnet.MyHomepage" MasterPageFile="~/Site.Master" %>

<asp:Content ID="HeadContent" ContentPlaceHolderID="HeadContent" runat="server">
    <style type="text/css">
        .record-panel {
            border: 1px solid #007bff;
            padding: 15px;
            border-radius: 5px;
            margin-bottom: 20px;
            background-color: #f7f9fc;
        }

        /* 假設您使用了 Bootstrap 樣式，這裡使用類名來確保樣式分離 */
        #<%= gvLendRecords.ClientID %> thead th {
            background-color: #007bff !important;
            color: white !important;
            font-weight: bold;
            border: 1px solid #007bff !important;
        }
        
        #<%= gvLendRecords.ClientID %> th, 
        #<%= gvLendRecords.ClientID %> td {
            border: 1px solid #c9c9c9; 
            padding: 8px;
        }
        
        #<%= gvLendRecords.ClientID %> tr:nth-child(even) {
            background-color: #f8f8f8; 
        }
        
        .overdue {
            color: red;
            font-weight: bold;
        }
        .near-due {
            color: orange;
            font-weight: bold;
        }
    </style>
</asp:Content>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <h2><asp:Label ID="lblPageTitle" runat="server" Text="我的借閱記錄"></asp:Label></h2>
    <hr />

    <div class="row mb-3">
        <div class="col-md-6">
            <div class="input-group">
                <asp:TextBox ID="txtSearch" runat="server" CssClass="form-control" placeholder="書名、作者、ISBN 快速搜尋"></asp:TextBox>
                <div class="input-group-append">
                    <asp:Button ID="btnSearch" runat="server" Text="搜尋" OnClick="btnSearch_Click" CssClass="btn btn-primary" />
                    <asp:LinkButton ID="lnkClearSearch" runat="server" Text="清除" OnClick="lnkClearSearch_Click" CssClass="btn btn-secondary" Visible="False" Style="margin-left: 5px;" />
                </div>
            </div>
        </div>
        <div class="col-md-6 text-right">
            <asp:LinkButton ID="lnkToggleAdvanced" runat="server" Text="進階搜尋" OnClick="lnkToggleAdvanced_Click" />
        </div>
    </div>

    <asp:Panel ID="pnlAdvancedSearch" runat="server" Visible="False" CssClass="record-panel" Style="margin-bottom: 20px;">
        <h4>進階搜尋</h4>
        <div class="form-row">
            <div class="form-group col-md-3">
                <asp:Label ID="Label1" runat="server" Text="書名："></asp:Label>
                <asp:TextBox ID="txtSearchTitle" runat="server" CssClass="form-control"></asp:TextBox>
            </div>
            <div class="form-group col-md-3">
                <asp:Label ID="Label2" runat="server" Text="作者："></asp:Label>
                <asp:TextBox ID="txtSearchAuthor" runat="server" CssClass="form-control"></asp:TextBox>
            </div>
            <div class="form-group col-md-3">
                <asp:Label ID="Label3" runat="server" Text="ISBN："></asp:Label>
                <asp:TextBox ID="txtSearchISBN" runat="server" CssClass="form-control"></asp:TextBox>
            </div>
            <div class="form-group col-md-3">
                <asp:Label ID="Label4" runat="server" Text="狀態："></asp:Label>
                <asp:DropDownList ID="ddlStatusFilter" runat="server" CssClass="form-control">
                    <asp:ListItem Text="全部" Value="All"></asp:ListItem>
                    <asp:ListItem Text="已逾期" Value="Overdue"></asp:ListItem>
                    <asp:ListItem Text="即將到期" Value="NearDue"></asp:ListItem>
                    <asp:ListItem Text="正常借閱中" Value="Normal"></asp:ListItem>
                </asp:DropDownList>
            </div>
        </div>
        <asp:Button ID="btnAdvancedSearch" runat="server" Text="進階搜尋" OnClick="btnAdvancedSearch_Click" CssClass="btn btn-info mr-2" />
    </asp:Panel>

    <asp:Panel ID="pnlRecords" runat="server" CssClass="record-panel">
        
        <asp:Label ID="lblUserInfo" runat="server" Text="用戶資訊：您目前有 N 筆借閱記錄未歸還" 
                    style="display: block; margin-bottom: 15px; font-weight: bold;"></asp:Label>
        
        <asp:GridView 
            ID="gvLendRecords" 
            DataKeyNames="LendRecordID, BookID"
            runat="server" 
            AutoGenerateColumns="False" 
            EmptyDataText="目前沒有任何未歸還的借閱記錄。"
            AllowSorting="True" 
            OnSorting="gvLendRecords_Sorting"
            AllowPaging="True"
            PageSize="10"
            OnPageIndexChanging="gvLendRecords_PageIndexChanging"
            OnRowDataBound="gvLendRecords_RowDataBound"
            OnRowCommand="gvLendRecords_RowCommand">
            <Columns>
                <asp:BoundField DataField="Title" HeaderText="書名" SortExpression="Title" />
                <asp:BoundField DataField="Author" HeaderText="作者" SortExpression="Author" />
                <asp:BoundField DataField="ISBN" HeaderText="ISBN" SortExpression="ISBN" />
                <asp:BoundField DataField="BorrowDate" HeaderText="借閱日期" DataFormatString="{0:yyyy/MM/dd}" SortExpression="BorrowDate" />
                <asp:BoundField DataField="DueDate" HeaderText="應還日期" DataFormatString="{0:yyyy/MM/dd}" SortExpression="DueDate" />
                <asp:TemplateField HeaderText="狀態" SortExpression="Status">
                    <ItemTemplate>
                        <asp:Label ID="lblStatus" runat="server" Text="正常借閱中"></asp:Label>
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField HeaderText="操作">
                    <ItemTemplate>
                        <asp:LinkButton ID="btnReturn" runat="server" Text="歸還" CommandName="ReturnBook" CommandArgument='<%# Eval("LendRecordID") + ";" + Eval("BookID") %>' CssClass="btn btn-primary btn-sm" OnClientClick="return confirm('確定要歸還這本書嗎？');" />
                    </ItemTemplate>
                </asp:TemplateField>
            </Columns>
            <HeaderStyle BackColor="#007bff" ForeColor="White" Font-Bold="True" />
            <RowStyle BackColor="#f8f8f8" />
            <AlternatingRowStyle BackColor="#ffffff" />
            <PagerStyle CssClass="pagination-ys" />
        </asp:GridView>
        
        <asp:Label ID="lblMessage" runat="server" Text="" ForeColor="Green" Style="display: block; margin-top: 10px; font-weight: bold;"></asp:Label>
        
    </asp:Panel>

    <div style="margin-top: 20px;">
        <asp:LinkButton ID="lnkShowHistory" runat="server" Text="查看已歸還記錄" Visible="False" />
    </div>
</asp:Content>