<%@ Control Language="C#" AutoEventWireup="true" Inherits="Kraken.Controls.Installer" %>

<div class="kraken">
    <h1>The Kraken.io plugin has been installed!</h1>
    <p>To get started you will need to obtain an API key. If you do not have one yet you can purchase one at <a href="https://kraken.io/plans?cms=umbraco" style="color: #EC297B; text-decoration:underline;" target="_blank">kraken.io/plans?cms=umbraco</a></p>
    <p>You can enter your <a href="http://kraken.io?cms=umbraco" style="color: #EC297B; text-decoration:underline;" target="_blank">kraken.io?cms=umbraco</a> credentials in the Kraken tab in the Umbraco media section and start kraking.</p>
</div>
<% if (new Version(ConfigurationManager.AppSettings["umbracoConfigurationStatus"]).Major >= 7) { %>
    <h2>Important</h2>
    <p style="font-weight:bold">In order to finalize your installation, you will need to change the property editor of the data type 'Status' to 'Status'.</p>
<% } %>