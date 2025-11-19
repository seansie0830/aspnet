<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="mailQueue.aspx.cs" MasterPageFile="~/Site.Master" Inherits="aspnet.mailQueue" %>

<asp:Content ID="HeadContent" ContentPlaceHolderID="HeadContent" runat="server">
    <%--
        **********************************************************
        * 引入 Bootstrap 和 Bootstrap Datepicker 資源
        * 請確認您的專案中已透過 NuGet 安裝並確保以下路徑正確
        * 假設您已在 Site.Master 或此處引入 jQuery
        **********************************************************
    --%>
    <link href="/Content/bootstrap.min.css" rel="stylesheet" />
    <link href="/Content/bootstrap-datepicker3.min.css" rel="stylesheet" />
    <script src="/Scripts/bootstrap.bundle.min.js"></script>
    <script src="/Scripts/bootstrap-datepicker.min.js"></script>
    <script src="/Scripts/bootstrap-datepicker.zh-TW.min.js"></script>

    <style type="text/css">
        .admin-container {
            max-width: 1400px;
            margin: 20px auto;
            padding: 20px;
            background-color: #ffffff;
            border-radius: 8px;
            box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
            font-family: Arial, sans-serif;
        }

        .page-header {
            color: #dc3545;
            font-size: 28px;
            font-weight: bold;
            border-bottom: 3px solid #dc3545;
            padding-bottom: 10px;
            margin-bottom: 20px;
        }
        
        .admin-nav {
            margin-bottom: 20px;
            padding: 10px;
            background-color: #f8f9fa;
            border-radius: 5px;
            box-shadow: 0 2px 4px rgba(0, 0, 0, 0.05);
        }
        .admin-nav a {
            text-decoration: none;
            color: #007bff;
            font-weight: bold;
            padding: 8px 15px;
            margin-right: 10px;
            border-radius: 4px;
            transition: background-color 0.3s;
        }
        .admin-nav a:hover {
            background-color: #e2e6ea;
        }
        .admin-nav .active {
            background-color: #007bff;
            color: white;
        }

        .control-row {
            display: flex;
            align-items: center;
            gap: 15px;
            margin-bottom: 20px;
            flex-wrap: wrap;
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
        .btn-send {
            background-color: #007bff;
            color: white;
            font-weight: bold;
            padding: 8px 16px;
            border: none;
            border-radius: 4px;
            cursor: pointer;
            transition: background-color 0.3s;
        }
        .btn-send:hover {
            background-color: #0056b3;
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
            cursor: default;
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
        .overdue-row td {
            background-color: #ffdddd !important; 
            color: #cc0000 !important;
            font-weight: bold;
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

        .section-header {
            font-size: 24px;
            color: #007bff;
            border-bottom: 2px solid #007bff;
            padding-bottom: 8px;
            margin-top: 30px;
            margin-bottom: 15px;
        }
        .reminder-placeholder {
            padding: 20px;
            border: 1px dashed #ccc;
            border-radius: 4px;
            text-align: center;
            color: #888;
            margin-bottom: 20px;
        }
    </style>

    <script type="text/javascript">
        $(document).ready(function () {
            // 初始化所有帶有 datepicker-input 類別的 TextBox
            initializeDatepickers();

            // 處理 GridView (在 UpdatePanel 內) 重新綁定後的 Datepicker 初始化問題
            var prm = Sys.WebForms.PageRequestManager.getInstance();
            if (prm) {
                prm.add_endRequest(function () {
                    initializeDatepickers();
                });
            }
        });
        function initializeDatepickers() {
            // 啟用 Datepicker
            $('.datepicker-input').datepicker({
                // 設定日期格式為 YYYY-MM-DD，符合 SQLite TEXT 儲存的最佳實踐
                format: 'yyyy-mm-dd',
                autoclose: true,
                todayHighlight: true,
                language: 'zh-TW', // 假設您已載入中文語言包
                // 將 Datepicker 附加到 body，以解決 GridView 編輯模式下的溢出問題
                container: 'body'
            });
        }
    </script>
</asp:Content>


<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <div class="admin-container">
        <h1 class="page-header">郵件佇列管理 (Mail Queue)</h1>
        <div class="admin-nav">
            <a href="/admins/AdminPage.aspx">Users</a>
            <a href="/admins/Books.aspx">Books</a>
            <a href="/admins/Categories.aspx">Categories</a>
            <a href="/admins/LendRecord.aspx">LendRecords</a>
            <a href="/admins/mailQueue.aspx" class="active">Mail Queue</a>
        </div>

        
        <asp:Panel ID="pnlMessage" runat="server" Visible="false" CssClass="message-box" role="alert">
            <asp:Label ID="lblMessage" runat="server" style="font-weight: bold;"></asp:Label>
        </asp:Panel>

        <%-- SECTION 1: 未歸還/逾期提醒郵件 --%>
        <h2 class="section-header">📚 未歸還/逾期提醒郵件</h2>
        <div class="control-row">
            <asp:Label ID="lblOverdueDays" runat="server" Text="只顯示逾期："></asp:Label>
            <asp:DropDownList ID="ddlOverdueDays" runat="server" style="padding: 5px; border: 1px solid #ccc; border-radius: 4px;" AutoPostBack="True" OnSelectedIndexChanged="ddlOverdueDays_SelectedIndexChanged">
                <asp:ListItem Text="所有未歸還 (All In Hand)" Value="AllInHand" Selected="True"></asp:ListItem>
                <asp:ListItem Text="已逾期 (Overdue)" Value="Overdue"></asp:ListItem>
                <asp:ListItem Text="7天內到期 (Due in 7 days)" Value="DueIn7"></asp:ListItem>
                <asp:ListItem Text="今天到期 (Due Today)" Value="DueToday"></asp:ListItem>
            </asp:DropDownList>
            <asp:Button ID="btnSendOverdueReminders" runat="server" Text="📧 寄送勾選的提醒郵件" OnClick="btnSendOverdueReminders_Click" CssClass="btn-send" />
            <asp:Button ID="btnRefreshReminders" runat="server" Text="🔄 重新整理" OnClick="btnRefreshReminders_Click" CssClass="btn-action" />
        </div>

        <asp:GridView ID="gvOverdueReminders" runat="server" 
            AutoGenerateColumns="False" 
            DataKeyNames="LendRecordID" 
            CssClass="gv-style" 
            AllowPaging="True"
            PageSize="15"
            OnPageIndexChanging="gvOverdueReminders_PageIndexChanging"
            OnRowDataBound="gvOverdueReminders_RowDataBound">

            <Columns>
                <asp:TemplateField HeaderText="選取">
                    <ItemTemplate>
                        <asp:CheckBox ID="chkSelect" runat="server" />
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:BoundField DataField="LendRecordID" HeaderText="記錄ID" ReadOnly="True" />
                <asp:BoundField DataField="Username" HeaderText="使用者" ReadOnly="True" />
                <asp:BoundField DataField="UserEmail" HeaderText="Email" ReadOnly="True" />
                <asp:BoundField DataField="BookTitle" HeaderText="書籍名稱" ReadOnly="True" />
                <asp:BoundField DataField="BorrowDate" HeaderText="借閱日" DataFormatString="{0:yyyy-MM-dd}" ReadOnly="True" />
                <asp:BoundField DataField="DueDate" HeaderText="應還日" DataFormatString="{0:yyyy-MM-dd}" ReadOnly="True" />
                <asp:TemplateField HeaderText="狀態">
                    <ItemTemplate>
                        <asp:Label runat="server" ID="lblStatus"></asp:Label>
                    </ItemTemplate>
                </asp:TemplateField>
            </Columns>
        </asp:GridView>


        <%-- SECTION 2: 其他異常提醒 (Placeholder) --%>
        <h2 class="section-header">🚨 其他異常提醒郵件 (例如：書籍損壞)</h2>
        <div class="reminder-placeholder">
            <p style="font-size: 1.2em;">**此處為其他類型提醒郵件佇列的 Placeholder**</p>
            <p>例如：書籍損壞通知、罰款通知等。</p>
            <p>郵件類型：<asp:DropDownList ID="ddlExceptionType" runat="server" style="margin: 0 5px;"><asp:ListItem Text="損壞通知" Value="Damage"></asp:ListItem><asp:ListItem Text="罰款通知" Value="Fine"></asp:ListItem></asp:DropDownList></p>
            <asp:Button ID="btnShowExceptionQueue" runat="server" Text="查看異常清單 (待實作)" CssClass="btn-action" />
        </div>

    </div>
</asp:Content>