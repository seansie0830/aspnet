<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="login.aspx.cs" Inherits="Login" MasterPageFile="~/Site.Master" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    
    <style>
        /* 整體容器樣式 */
        .login-container {
            max-width: 400px;
            margin: 50px auto; /* 居中並留出上下邊距 */
            padding: 30px;
            border: 1px solid #ddd;
            border-radius: 10px;
            box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
            background-color: #ffffff;
            font-family: Arial, sans-serif;
        }

        /* 標題樣式 */
        .login-container h2 {
            text-align: center;
            color: #333;
            margin-bottom: 25px;
            border-bottom: 2px solid #5cb85c;
            padding-bottom: 10px;
        }

        /* 輸入欄位群組 */
        .form-group {
            margin-bottom: 20px;
            display: flex;
            flex-direction: column;
        }

        /* 標籤和輸入框樣式 */
        .form-group label {
            font-weight: bold;
            color: #555;
            margin-bottom: 5px;
        }

        /* **** 關鍵修正在這裡：強制寬度和顯示類型 **** */
        .form-group input[type="text"],
        .form-group input[type="password"] {
            width: 100% !important; /* 使用 !important 覆寫所有外部樣式 */
            display: block; /* 確保它是區塊元素，可以佔滿一行 */
            
            padding: 10px 15px;
            border: 1px solid #ccc;
            border-radius: 5px;
            box-sizing: border-box; 
            transition: border-color 0.3s;
        }
        /* ************************************** */

        .form-group input[type="text"]:focus,
        .form-group input[type="password"]:focus {
            border-color: #5cb85c;
            outline: none;
        }

        /* 登入按鈕樣式 */
        #<%= btnLogin.ClientID %> {
            width: 100%;
            padding: 12px;
            background-color: #5cb85c;
            color: white;
            border: none;
            border-radius: 5px;
            font-size: 16px;
            cursor: pointer;
            transition: background-color 0.3s, transform 0.1s;
        }

        #<%= btnLogin.ClientID %>:hover {
            background-color: #4cae4c;
        }

        #<%= btnLogin.ClientID %>:active {
            transform: scale(0.99);
        }

        /* 錯誤訊息樣式 */
        .message-box {
            text-align: center;
            margin-top: 15px;
            padding: 5px;
            font-weight: bold;
        }

    </style>
<div class="login-container">
    <h2>使用者登入</h2>

    <div class="form-group">
        <label for="<%= txtUsername.ClientID %>">帳號：</label>
        <asp:TextBox ID="txtUsername" runat="server" Width="100%"></asp:TextBox>
    </div>

    <div class="form-group">
        <label for="<%= txtPassword.ClientID %>">密碼：</label>
        <asp:TextBox ID="txtPassword" runat="server" TextMode="Password" Width="100%"></asp:TextBox>
    </div>

    <div class="form-group">
        <asp:Button ID="btnLogin" runat="server" Text="登入" OnClick="btnLogin_Click" />
    </div>

    <div class="login-links">
        <a href="#" id="linkRegister">註冊</a>
        
        <span class="link-separator">|</span>
        
        <a href="#" id="linkForgotPassword">忘記密碼?</a>
    </div>
    <p class="message-box">
        <asp:Label ID="lblMessage" runat="server" ForeColor="Red"></asp:Label>
    </p>

</div>
</asp:Content>