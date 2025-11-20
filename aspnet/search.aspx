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
        .category-search-container {
            width: 100%;
        }
        .category-search-container .form-control {
            margin-bottom: 5px;
        }

        .selected-categories-list {
            margin-top: 5px;
            padding: 5px;
            border: 1px solid #ccc;
            min-height: 40px;
            border-radius: 4px;
        }

        .category-tag {
            display: inline-block;
            background-color: #007bff;
            color: white;
            padding: 3px 8px;
            border-radius: 12px;
            margin-right: 5px;
            margin-bottom: 5px;
            font-size: 0.9em;
        }

        .remove-cat {
            cursor: pointer;
            margin-left: 5px;
            font-weight: bold;
            color: #fff;
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
            
            <asp:LinkButton ID="lnkToggleSearch" runat="server" Text="▼ 展開進階查詢" OnClientClick="toggleSearchPanel(); return false;"
 />
        </div>
    </div>

    <asp:Panel ID="pnlAdvancedSearch" runat="server" CssClass="advanced-panel">
        <div style="display: flex; gap: 20px; margin-bottom: 15px;">
            <asp:TextBox ID="txtBookID" runat="server" Placeholder="書本ID (精確搜尋)" CssClass="form-control" />
            <asp:TextBox ID="txtTitle" runat="server" Placeholder="書名關鍵字" CssClass="form-control" />
            <asp:TextBox ID="txtAuthor" runat="server" Placeholder="作者名稱" CssClass="form-control" />
            <asp:TextBox ID="txtISBN" runat="server" Placeholder="ISBN" CssClass="form-control" />
        </div>
       
        <div style="display: flex; align-items: flex-start; gap: 20px;">
            <div style="width: 350px;">
                <asp:Label ID="Label1" runat="server" Text="書籍類別篩選：" />
                <div style="display: flex; gap: 5px; margin-bottom: 5px;">
                    <asp:TextBox ID="txtCategorySearch" runat="server" Placeholder="輸入類別關鍵字..." CssClass="form-control" /> 
                    <asp:Button ID="btnFilterCategories" runat="server" Text="篩選" OnClick="btnFilterCategories_Click" CssClass="btn btn-secondary btn-sm" />
                </div>
                <div style="display: flex; gap: 5px; margin-bottom: 5px;">
                    <asp:DropDownList ID="ddlAvailableCategories" runat="server" CssClass="form-control" Width="250px" />
                    <button type="button" class="btn btn-info btn-sm" onclick="addCategoryToSearch(); return false;">新增</button>
                </div>
                <asp:Label ID="Label2" runat="server" Text="已選類別：" />
                <div id="selectedCategoriesDisplay" class="selected-categories-list">
                    </div>
                <asp:HiddenField ID="hidSelectedCategories" runat="server" Value="" />
            </div>
  
            <asp:Button ID="btnAdvancedSearch" runat="server" Text="進階搜尋" OnClick="btnAdvancedSearch_Click" CssClass="btn btn-primary" Style="margin-top: 25px;"
 />
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

        // 從 HiddenField 讀取類別資料
        function getSelectedCategories() {
            var catData = $('#<%= hidSelectedCategories.ClientID %>').val();
            if (catData) {
                // 格式為 "ID1|Name1,ID2|Name2"
                return catData.split(',').map(function(item) {
                    var parts = item.split('|');
                    return { id: parts[0], name: parts[1] };
                });
            }
            return [];
        }

        // 將類別清單寫回 HiddenField
        function updateHiddenField(categories) {
            var catData = categories.map(function(item) {
                return item.id + '|' + item.name;
            }).join(',');
            $('#<%= hidSelectedCategories.ClientID %>').val(catData);
        }

        // 渲染已選類別標籤
        function renderSelectedCategories() {
            var categories = getSelectedCategories();
            var $display = $('#selectedCategoriesDisplay');
            $display.empty();

            categories.forEach(function(cat) {
                var tag = $('<span class="category-tag" data-cat-id="' + cat.id + '">' + cat.name + ' <span class="remove-cat">x</span></span>');
                $display.append(tag);
            });
        }

        // 新增類別到已選清單
        function addCategoryToSearch() {
            var $ddl = $('#<%= ddlAvailableCategories.ClientID %>');
            var categoryID = $ddl.val();
            var categoryName = $ddl.find('option:selected').text();
            
            if (!categoryID || categoryID === "0") return;

            var categories = getSelectedCategories();
            
            // 檢查是否已存在
            if (categories.some(function(cat) { return cat.id === categoryID; })) {
                alert('該類別已新增！');
                return;
            }

            categories.push({ id: categoryID, name: categoryName });
            updateHiddenField(categories);
            renderSelectedCategories();
        }

        // 移除類別
        $('#selectedCategoriesDisplay').on('click', '.remove-cat', function() {
            var $tag = $(this).closest('.category-tag');
            var categoryIDToRemove = $tag.attr('data-cat-id');

            var categories = getSelectedCategories();
            var newCategories = categories.filter(function(cat) {
                return cat.id !== categoryIDToRemove;
            });

            updateHiddenField(newCategories);
            renderSelectedCategories();
            
            // 由於這是純前端操作，不需要 PostBack
            // 如果要讓移除後立即觸發 BindBookData，則需要額外的 PostBack 機制
        });

        // 頁面載入時檢查是否需要顯示錯誤訊息的樣式和面板狀態
        $(document).ready(function () {
            var resultLabel = $('#<%= lblResultInfo.ClientID %>');
            if (resultLabel.text().includes('借閱失敗') || resultLabel.text().includes('錯誤')) {
                resultLabel.addClass('message-error').removeClass('message-success');
            } else if (resultLabel.text().includes('成功')) {
                resultLabel.addClass('message-success').removeClass('message-error');
            }

            // 根據隱藏欄位的值來設定面板的初始狀態
            var panelVisible = $('#<%= hidPanelVisible.ClientID %>').val() === 'true';
            var panelSelector = '#<%= pnlAdvancedSearch.ClientID %>';
            var link = $('#<%= lnkToggleSearch.ClientID %>');

            if (panelVisible) {
                $(panelSelector).show();
                link.text('▲ 收合進階查詢');
            } else {
                $(panelSelector).hide();
                link.text('▼ 展開進階查詢');
            }

            // 頁面載入時，根據 HiddenField 渲染已選類別
            renderSelectedCategories();
        });
    </script>

    <asp:HiddenField ID="hidPanelVisible" runat="server" Value="false" />
</asp:Content>