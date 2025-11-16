<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Register.aspx.cs" 
    Inherits="aspnet.Register" MasterPageFile="~/Site.Master" %>

<asp:Content ID="HeadContent" ContentPlaceHolderID="HeadContent" runat="server">
    <style type="text/css">
        /* 註冊頁面專屬樣式 */
        .register-container {
            max-width: 450px;
            margin: 50px auto;
            padding: 30px;
            background-color: #ffffff;
            border-radius: 10px;
            box-shadow: 0 4px 15px rgba(0, 0, 0, 0.1);
            font-family: Arial, sans-serif;
            text-align: center;
        }

        .page-title {
            color: #4a4a4a; 
            font-size: 28px;
            font-weight: bold;
            margin-bottom: 25px;
            border-bottom: 2px solid #ddd;
            padding-bottom: 10px;
        }

        /* === 對齊修正：使用 Flexbox === */
        .form-group {
            margin-bottom: 20px;
            text-align: left;
            display: flex; /* 啟用 Flexbox */
            align-items: center; /* 垂直居中對齊 */
        }

        .form-group label {
            width: 100px; /* 【關鍵】固定的標籤寬度 */
            flex-shrink: 0; /* 防止標籤被壓縮 */
            text-align: right; /* 【關鍵】標籤文字右對齊 */
            padding-right: 15px; /* 標籤與輸入框間隔 */
            margin-bottom: 0; /* 移除 ASP:Label 轉換為 span 時可能帶來的底部間距 */
            
            font-weight: bold;
            color: #555;
        }

        /* 鎖定所有輸入控制項 */
        .form-input { 
            flex-grow: 1; /* 【關鍵】讓輸入框填滿剩餘的空間 */
            
            width: auto; /* 確保 flex-grow: 1 能正常工作 */
            padding: 10px;
            border: 1px solid #ccc;
            border-radius: 6px;
            box-sizing: border-box; /* 確保 padding 不會增加寬度 */
            transition: border-color 0.3s;
        }
        
        .form-input:focus {
            border-color: #007bff; /* Focus Blue */
            outline: none;
        }
        /* === 對齊修正結束 === */

        /* 註冊按鈕樣式 */
        .btn-register {
            width: 100%;
            background-color: #007bff; /* Blue */
            color: white;
            font-weight: bold;
            padding: 12px 20px;
            border: none;
            border-radius: 6px;
            cursor: pointer;
            transition: background-color 0.3s, transform 0.1s;
            font-size: 18px;
        }
        .btn-register:hover {
            background-color: #0056b3;
        }
        .btn-register:active {
            transform: scale(0.99);
        }

        /* 訊息樣式 */
        .message-box {
            padding: 15px;
            border-radius: 6px;
            margin-top: 20px;
            font-weight: bold;
        }
        .message-box-error {
            background-color: #f8d7da; /* Light Red */
            color: #721c24; /* Dark Red Text */
            border: 1px solid #f5c6cb;
        }
        .message-box-success {
            background-color: #d4edda; /* Light Green */
            color: #155724; /* Dark Green Text */
            border: 1px solid #c3e6cb;
        }
    </style>
</asp:Content>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <div class="register-container">
        <h2 class="page-title">新使用者註冊</h2>

        <%-- 使用 Flexbox 結構，將 asp:Label 換為標準 HTML <label> 並使用 for 屬性 --%>
        
        <div class="form-group">
            <label for="<%= txtUsername.ClientID %>">ID:</label>
            <asp:TextBox ID="txtUsername" runat="server" CssClass="form-input"></asp:TextBox>
        </div>

        <div class="form-group">
            <label for="<%= txtEmail.ClientID %>">電子郵件:</label>
            <%-- 註冊時必須使用 Email 欄位 --%>
            <asp:TextBox ID="txtEmail" runat="server" TextMode="Email" CssClass="form-input"></asp:TextBox>
        </div>

        <div class="form-group">
            <label for="<%= txtPassword.ClientID %>">密碼:</label>
            <asp:TextBox ID="txtPassword" runat="server" TextMode="Password" CssClass="form-input"></asp:TextBox>
        </div>

        <div class="form-group">
            <label for="<%= txtConfirmPassword.ClientID %>">確認密碼:</label>
            <asp:TextBox ID="txtConfirmPassword" runat="server" TextMode="Password" CssClass="form-input"></asp:TextBox>
        </div>

        <asp:Button ID="btnRegister" runat="server" Text="註冊帳號" OnClick="btnRegister_Click" 
            CssClass="btn-register" />

        <asp:Label ID="lblMessage" runat="server" CssClass="message-box"></asp:Label>

        <div style="margin-top: 15px; font-size: 14px;">
            已經有帳號了？<a href="Login.aspx" style="color: #007bff; text-decoration: none;">點此登入</a>
        </div>
    </div>
</asp:Content>