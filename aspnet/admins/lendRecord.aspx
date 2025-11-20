<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="LendRecord.aspx.cs" MasterPageFile="~/Site.Master" Inherits="aspnet.LendRecord" %>

<asp:Content ID="HeadContent" ContentPlaceHolderID="HeadContent" runat="server">
    <%-- 
        **********************************************************
        * 引入 Bootstrap 和 Bootstrap Datepicker 資源
        * 請確認您的專案中已透過 NuGet 安裝並確保以下路徑正確
        **********************************************************
    --%>
    <link href="/Content/bootstrap.min.css" rel="stylesheet" />
    <link href="/Content/bootstrap-datepicker3.min.css" rel="stylesheet" />
    <%-- 假設您已在 Site.Master 中引入 jQuery，如果沒有，請在此處加入 --%>
    <%-- <script src="/Scripts/jquery-3.x.x.min.js"></script> --%> 
    <script src="/Scripts/bootstrap.bundle.min.js"></script>
    <script src="/Scripts/bootstrap-datepicker.min.js"></script>
    <%-- 引入中文語系包 (可選，但推薦) --%>
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
        .btn-new-record {
            background-color: #28a745;
            color: white;
            font-weight: bold;
            padding: 8px 16px;
            border: none;
            border-radius: 4px;
            cursor: pointer;
            transition: background-color 0.3s;
        }
        .btn-new-record:hover {
            background-color: #218838;
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

        .overdue-row td {
            background-color: #ffdddd !important;
            /* 紅字反白 */
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

        .insert-form-panel {
            border: 1px solid #ccc;
            padding: 20px;
            border-radius: 6px;
            background-color: #f8f9fa; 
            margin-top: 20px;
            margin-bottom: 20px;
        }
        .insert-form-table {
            width: 100%;
            border-collapse: separate;
            border-spacing: 0 10px;
        }
        .insert-form-table td:first-child {
            width: 180px;
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
        .btn-cancel {
            background-color: #dc3545;
        }
        .btn-cancel:hover {
            background-color: #c82333;
        }
        .btn-submit {
            background-color: #28a745;
            margin-left: 10px;
        }
        .btn-submit:hover {
            background-color: #218838;
        }
        
        /* 替換 Calendar 相關樣式 */
        .calendar-container {
            position: relative;
            display: inline-block;
        }
    </style>

    <script type="text/javascript">
        // 刪除原有的 toggleCalendar 函數，改用 Bootstrap Datepicker 初始化

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
        <h1 class="page-header">借閱記錄管理 (LendRecords)</h1>


        
        <asp:Panel ID="pnlMessage" runat="server" Visible="false" CssClass="message-box" role="alert">
            <asp:Label ID="lblMessage" runat="server" style="font-weight: bold;"></asp:Label>
        </asp:Panel>

        <div class="control-row">
            <asp:Label ID="lblPageSize" runat="server" Text="每頁筆數:"></asp:Label>
            <asp:DropDownList ID="ddlPageSize" runat="server" AutoPostBack="True" OnSelectedIndexChanged="ddlPageSize_SelectedIndexChanged" 
                style="padding: 5px; border: 1px solid #ccc; border-radius: 4px;">
                <asp:ListItem Text="10" Value="10"></asp:ListItem>
                <asp:ListItem Text="15" Value="15" Selected="True"></asp:ListItem>
                <asp:ListItem Text="25" Value="25"></asp:ListItem>
                <asp:ListItem Text="50" Value="50"></asp:ListItem>
            </asp:DropDownList>

          
            <asp:TextBox ID="txtSearch" runat="server" placeholder="書名/用戶名/ID 搜尋..." CssClass="input-insert-form" Width="200px"></asp:TextBox>
            <asp:Button ID="btnSearch" runat="server" Text="基本搜尋" OnClick="btnSearch_Click" CssClass="btn-action" />
            <asp:Button ID="btnShowAdvancedSearch" runat="server" Text="進階搜尋" OnClick="btnShowAdvancedSearch_Click" CssClass="btn-action" />
            
            <asp:Button ID="btnShowInsert" runat="server" Text="✚ 新增借閱" OnClick="btnShowInsert_Click" CssClass="btn-new-record" />

        </div>

        <asp:Panel ID="pnlAdvancedSearch" runat="server" Visible="False" CssClass="insert-form-panel">
            <h3>進階搜尋 (使用 Bootstrap Datepicker)</h3>
            <table class="insert-form-table">
                <tr>
                    <td>預計還書日:</td>
                    <td>
                        <%-- 已移除 calDueDateStart 和 calDueDateEnd --%>
                        <asp:TextBox ID="txtDueDateStart" runat="server" placeholder="起始日期 (YYYY-MM-DD)" CssClass="input-insert-form datepicker-input" Width="180px" ToolTip="點擊以選取日期"></asp:TextBox>
                        ~
                        <asp:TextBox ID="txtDueDateEnd" runat="server" placeholder="結束日期 (YYYY-MM-DD)" CssClass="input-insert-form datepicker-input" Width="180px" ToolTip="點擊以選取日期"></asp:TextBox>
                    </td>
                </tr>
                <tr>
                    <td>狀態:</td>
                    <td>
                        <asp:DropDownList ID="ddlStatus" runat="server" CssClass="input-insert-form" Width="150px">
                            <asp:ListItem Text="全部" Value="All" Selected="True"></asp:ListItem>
                            <asp:ListItem Text="未歸還 (In Hand)" Value="InHand"></asp:ListItem>
                            <asp:ListItem Text="已歸還 (Returned)" Value="Returned"></asp:ListItem>
                            <asp:ListItem Text="已逾期 (Overdue)" Value="Overdue"></asp:ListItem>
                        </asp:DropDownList>
                    </td>
                </tr>
                <tr>
                    <td colspan="2" class="form-actions">
                        <asp:Button ID="btnExecuteAdvancedSearch" runat="server" Text="執行進階搜尋" OnClick="btnExecuteAdvancedSearch_Click" CssClass="btn-action btn-submit" />
                        <asp:Button ID="btnClearSearch" runat="server" Text="清除搜尋" OnClick="btnClearSearch_Click" CssClass="btn-action btn-cancel" />
                    </td>
                </tr>
            </table>
        </asp:Panel>
        
        <asp:Panel ID="pnlInsertForm" runat="server" Visible="False" CssClass="insert-form-panel">
            <h3>新增借閱紀錄</h3>
            <asp:PlaceHolder ID="phInsertFormControls" runat="server"></asp:PlaceHolder>
            
            <div class="form-actions">
                <asp:Button ID="btnCancelInsert" runat="server" Text="取消新增" OnClick="btnCancelInsert_Click" CssClass="btn-action btn-cancel" />
                <asp:Button ID="btnInsertRecord" runat="server" Text="確認新增並儲存" OnClick="btnInsertRecord_Click" CssClass="btn-action btn-submit" />
            </div>
        </asp:Panel>


        <asp:GridView ID="gvLendRecords" runat="server" 
            AutoGenerateColumns="False" 
            DataKeyNames="LendRecordID" 
            CssClass="gv-style" 
            AllowPaging="True"
            AllowSorting="True"
            PageSize="15"
            OnPageIndexChanging="gvLendRecords_PageIndexChanging"
            OnSorting="gvLendRecords_Sorting"
            OnRowEditing="gvLendRecords_RowEditing"
            OnRowCancelingEdit="gvLendRecords_RowCancelingEdit"
            OnRowUpdating="gvLendRecords_RowUpdating"
            OnRowDeleting="gvLendRecords_RowDeleting"
            OnRowDataBound="gvLendRecords_RowDataBound">

            <Columns>
                <asp:BoundField DataField="LendRecordID" HeaderText="ID" ReadOnly="True" SortExpression="LendRecordID" />
                <asp:BoundField DataField="BookTitle" HeaderText="書名" ReadOnly="True" SortExpression="Title" />
                <asp:BoundField DataField="Username" HeaderText="使用者" ReadOnly="True" SortExpression="Username" />
                
                <%-- 1. 借閱日 TemplateField --%>
                <asp:TemplateField HeaderText="借閱日" SortExpression="BorrowDate">
                    <ItemTemplate>
                        <asp:Label runat="server" Text='<%# Eval("BorrowDate", "{0:yyyy-MM-dd}") %>'></asp:Label>
                    </ItemTemplate>
                    <EditItemTemplate>
                        <%-- 替換為 TextBox + datepicker-input --%>
                        <asp:TextBox ID="txtEditBorrowDate" runat="server" Text='<%# Bind("BorrowDate", "{0:yyyy-MM-dd}") %>' CssClass="input-insert-form datepicker-input" Width="100px" ToolTip="點擊選取日期"></asp:TextBox>
                    </EditItemTemplate>
                </asp:TemplateField>

                <%-- 2. 應還日 TemplateField --%>
                <asp:TemplateField HeaderText="應還日" SortExpression="DueDate">
                    <ItemTemplate>
                        <asp:Label runat="server" Text='<%# Eval("DueDate", "{0:yyyy-MM-dd}") %>'></asp:Label>
                    </ItemTemplate>
                    <EditItemTemplate>
                        <%-- 替換為 TextBox + datepicker-input --%>
                        <asp:TextBox ID="txtEditDueDate" runat="server" Text='<%# Bind("DueDate", "{0:yyyy-MM-dd}") %>' CssClass="input-insert-form datepicker-input" Width="100px" ToolTip="點擊選取日期"></asp:TextBox>
                    </EditItemTemplate>
                </asp:TemplateField>

                <%-- 3. 歸還日 TemplateField --%>
                <asp:TemplateField HeaderText="歸還日" SortExpression="ReturnDate">
                    <ItemTemplate>
                        <asp:Label runat="server" Text='<%# Eval("ReturnDate", "{0:yyyy-MM-dd}") %>'></asp:Label>
                    </ItemTemplate>
                    <EditItemTemplate>
                        <%-- 替換為 TextBox + datepicker-input --%>
                        <asp:TextBox ID="txtEditReturnDate" runat="server" Text='<%# Bind("ReturnDate", "{0:yyyy-MM-dd}") %>' CssClass="input-insert-form datepicker-input" Width="100px" ToolTip="點擊選取日期"></asp:TextBox>
                    </EditItemTemplate>
                </asp:TemplateField>
                
                <%-- 備註 TemplateField --%>
                <asp:TemplateField HeaderText="備註" SortExpression="ExceptionNotes">
                    <ItemTemplate>
                        <asp:Label runat="server" Text='<%# Eval("ExceptionNotes") %>' ToolTip='<%# Eval("ExceptionNotes") %>'></asp:Label>
                    </ItemTemplate>
                    <EditItemTemplate>
                        <asp:TextBox ID="txtExceptionNotesEdit" runat="server" Text='<%# Bind("ExceptionNotes") %>' TextMode="MultiLine" Rows="3" Width="95%"></asp:TextBox>
                    </EditItemTemplate>
                </asp:TemplateField>

                <asp:CommandField ShowEditButton="True" ShowDeleteButton="True" EditText="編輯" UpdateText="更新" CancelText="取消" DeleteText="刪除" />
            </Columns>
        </asp:GridView>
    </div>
</asp:Content>