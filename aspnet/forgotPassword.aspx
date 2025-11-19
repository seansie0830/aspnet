<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="forgotPassword.aspx.cs" Inherits="ForgotPassword" MasterPageFile="~/Site.Master" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    
    <style>
        /* 樣式保持不變... */
        .login-container {
            max-width: 400px;
            margin: 50px auto;
            padding: 30px;
            border: 1px solid #ddd;
            border-radius: 10px;
            box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
            background-color: #ffffff;
            font-family: Arial, sans-serif;
        }

        .login-container h2 {
            text-align: center;
            color: #333;
            margin-bottom: 25px;
            border-bottom: 2px solid #5cb85c;
            padding-bottom: 10px;
        }

        .form-group {
            margin-bottom: 20px;
            display: flex;
            flex-direction: column;
        }

            .form-group label {
                font-weight: bold;
                color: #555;
                margin-bottom: 5px;
            }

        .form-group input[type="text"],
        .form-group input[type="password"],
        .form-group input[type="email"] { 
            width: 100% !important; 
            display: block; 
            
            padding: 10px 15px;
            border: 1px solid #ccc;
            border-radius: 5px;
            box-sizing: border-box; 
            transition: border-color 0.3s;
        }

        .form-group input[type="text"]:focus,
        .form-group input[type="password"]:focus,
        .form-group input[type="email"]:focus {
            border-color: #5cb85c;
            outline: none;
        }

        #<%= btnReset.ClientID %>, #<%= btnConfirm.ClientID %> { /* 新增了 #btnConfirm */
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

        #<%= btnReset.ClientID %>:hover, #<%= btnConfirm.ClientID %>:hover {
            background-color: #4cae4c;
        }

        #<%= btnReset.ClientID %>:active, #<%= btnConfirm.ClientID %>:active {
            transform: scale(0.99);
        }

        .message-box {
            text-align: center;
            margin-top: 15px;
            padding: 5px;
            font-weight: bold;
        }

        .login-links {
            margin-top: 15px;
            text-align: center;
            font-size: 0.9em;
        }
        
    </style>
<div class="login-container">
    <h2>忘記密碼</h2>

    <asp:Panel ID="pnlEmailInput" runat="server">
        <div class="form-group">
            <label for="<%= txtEmail.ClientID %>">請輸入註冊信箱：</label>
            <asp:TextBox ID="txtEmail" runat="server" Width="100%" TextMode="Email"></asp:TextBox>
        </div>

        <div class="form-group">
            <asp:Button ID="btnReset" runat="server" Text="發送重設連結" OnClick="btnReset_Click" />
        </div>
    </asp:Panel>

    <asp:Panel ID="pnlVerification" runat="server" Visible="false">
        <p style="text-align: center; color: #5cb85c; font-weight: bold;">驗證碼已寄出，請檢查信箱並輸入以下資訊。</p>
        
        <div class="form-group">
            <label for="<%= txtCode.ClientID %>">驗證碼：</label>
            <asp:TextBox ID="txtCode" runat="server" Width="100%" MaxLength="6"></asp:TextBox>
        </div>

        <div class="form-group">
            <label for="<%= txtNewPassword.ClientID %>">新密碼：</label>
            <asp:TextBox ID="txtNewPassword" runat="server" Width="100%" TextMode="Password"></asp:TextBox>
        </div>

        <div class="form-group">
            <label for="<%= txtConfirmPassword.ClientID %>">確認新密碼：</label>
            <asp:TextBox ID="txtConfirmPassword" runat="server" Width="100%" TextMode="Password"></asp:TextBox>
        </div>

        <div class="form-group">
            <asp:Button ID="btnConfirm" runat="server" Text="確認並重設密碼" OnClick="btnConfirm_Click" />
        </div>
    </asp:Panel>

    <div class="login-links">
        <a href="\login" id="linkLogin">返回登入</a>
    </div>
    <p class="message-box">
        <asp:Label ID="lblMessage" runat="server" ForeColor="Red"></asp:Label>
    </p>

</div>
</asp:Content>