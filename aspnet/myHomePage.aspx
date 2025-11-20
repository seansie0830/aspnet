<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="MyHomepage.aspx.cs" Inherits="aspnet.MyHomepage" MasterPageFile="~/Site.Master" %>

<asp:Content ID="HeadContent" ContentPlaceHolderID="HeadContent" runat="server">
    <style type="text/css">
        /* 個人頁面專屬的樣式 */
        .record-panel {
            border: 1px solid #007bff;
            padding: 15px;
            border-radius: 5px;
            margin-bottom: 20px;
            background-color: #f7f9fc;
        }

        /* GridView 標頭樣式 */
        #<%= gvLendRecords.ClientID %> thead th {
            background-color: #007bff !important;
            color: white !important;
            font-weight: bold;
            border: 1px solid #007bff !important;
        }
        
        /* GridView 內文邊框 */
        #<%= gvLendRecords.ClientID %> th, 
        #<%= gvLendRecords.ClientID %> td {
            border: 1px solid #c9c9c9;
            padding: 8px;
        }
        
        /* 隔行換色 */
        #<%= gvLendRecords.ClientID %> tr:nth-child(even) {
            background-color: #f8f8f8;
        }
        
        /* 逾期提醒文字 */
        .overdue {
            color: red;
            font-weight: bold;
        }
        
        .borrow-status {
            font-size: 1.1em;
            font-weight: bold;
            color: #28a745; /* 綠色 */
            display: block; 
            margin-bottom: 15px;
        }
        
        .borrow-limit-message {
            color: #dc3545; /* 紅色 */
        }
    </style>
</asp:Content>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <h2><asp:Label ID="lblPageTitle" runat="server" Text="我的借閱資訊"></asp:Label></h2>
    <hr />

    <asp:Panel ID="pnlRecords" runat="server" CssClass="record-panel">
        
        <asp:Label ID="lblUserInfo" runat="server" Text="歡迎回來，您目前的借閱狀態如下：" 
                    style="display: block; margin-bottom: 15px; font-weight: bold;"></asp:Label>
        
        <asp:Label ID="lblBorrowStatus" runat="server" CssClass="borrow-status"></asp:Label>
        
        <asp:Label ID="lblReturnMessage" runat="server" Text="" ForeColor="Green" Style="display: block; margin-bottom: 10px;"></asp:Label>

        <asp:GridView 
            ID="gvLendRecords" 
            runat="server" 
            AutoGenerateColumns="False" 
            EmptyDataText="目前沒有任何未歸還的借閱記錄。"
            DataKeyNames="LendRecordID,BookID"
            OnRowDataBound="gvLendRecords_RowDataBound"
            OnRowCommand="gvLendRecords_RowCommand">
       
            <Columns>
                <asp:BoundField DataField="Title" HeaderText="書名" />
                <asp:BoundField DataField="Author" HeaderText="作者" />
                <asp:BoundField DataField="ISBN" HeaderText="ISBN" />
                <asp:BoundField DataField="BorrowDate" HeaderText="借閱日期" DataFormatString="{0:yyyy/MM/dd}" />
                <asp:BoundField DataField="DueDate" 
HeaderText="應還日期" DataFormatString="{0:yyyy/MM/dd}" />
                <asp:TemplateField HeaderText="狀態">
                    <ItemTemplate>
                        <asp:Label ID="lblStatus" runat="server" Text="正常借閱中"></asp:Label>
                    </ItemTemplate>
              
            </asp:TemplateField>
                <asp:TemplateField HeaderText="操作">
                    <ItemTemplate>
                        <asp:Button ID="btnReturnBook" runat="server" Text="還書" 
                            CommandName="ReturnBook" 
    
                            CommandArgument='<%# Container.DataItemIndex %>' 
                            CssClass="btn btn-sm btn-success" />
                    </ItemTemplate>
                </asp:TemplateField>
      
            </Columns>
        </asp:GridView>
        
    </asp:Panel>

    <div style="margin-top: 20px;">
        <asp:LinkButton ID="lnkShowHistory" runat="server" Text="查看歷史歸還記錄" Visible="False" />
    </div>
</asp:Content>