<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="login.aspx.cs" Inherits="Login" MasterPageFile="~/Site.Master" %>
<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <h2>使用者登入</h2>
    <p>帳號：<asp:TextBox ID="txtUsername" runat="server"></asp:TextBox></p>
    <p>密碼：<asp:TextBox ID="txtPassword" runat="server" TextMode="Password"></asp:TextBox></p>
    <p>
        <asp:Button ID="btnLogin" runat="server" Text="登入" OnClick="btnLogin_Click" />
    </p>
    <p>
        <asp:Label ID="lblMessage" runat="server" ForeColor="Red"></asp:Label>
    </p>
</asp:Content>