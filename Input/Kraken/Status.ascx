<%@ Control Language="C#" AutoEventWireup="true" Inherits="Kraken.Controls.Status" %>
<div class="kraken">
    <style>
        .kraken .kraken-everything {display: inline-block; background: #F3C50F; color: #1a1a1a; text-transform: uppercase; font-family: Arial, Helvetica, sans-serif; text-decoration: none; font-weight: bolder; padding: 14px 16px; letter-spacing: 2px; margin-right:5px; -webkit-border-radius: 3px; -moz-border-radius: 3px; border-radius: 3px;}
        .kraken .kraken-everything:hover {background: #1a1a1a; color: #F3C50F;}
        .kraken p {margin: 30px 0 0; padding: 0; font-family: "Trebuchet MS", Helvetica, sans-serif;}
        .kraken a {color: #EC297B; text-decoration:underline;}
        .kraken a:hover {text-decoration:none;}
    </style>
    <%
        switch (status)
        {
            case EnmIsKrakable.MissingCredentials: %>
                <p>No valid API credentials could be found. A valid key can be obtained from <a href="https://kraken.io?cms=umbraco" target="_blank">kraken.io</a></p>
             <% break;
            case EnmIsKrakable.Unkrakable: %>
                <p>This media item cannot be optimized. Please make sure the following requirements are met:</p>
                <ul>
                     <li>1) The alias of the property containing this data type must be named 'status'</li>
                    <li>2) A property with the alias 'umbracoFile' must be present (File upload or an Image Cropper)</li>
                    <li>3) The property mentioned at 2 must contain a (relative) filepath to the image (generally speaking this is the relative URL)</li>
                    <li>4) The file must have the extension jpg, jpeg, png or gif. Other image types are not supported.</li>
                </ul>
            <% break;
            case EnmIsKrakable.Krakable: %>
                <asp:LinkButton runat="server" ID="btnCompress" Text="Optimize image" OnClick="btnCompress_Click" CssClass="kraken-everything" />
                <br /><br />This image will be optimized by <a href="https://kraken.io?cms=umbraco" target="_blank">kraken.io</a>.
            <% break;
            case EnmIsKrakable.Kraked: %>
                <p>Your image has been optimized by <a href="https://kraken.io?cms=umbraco" target="_blank">kraken.io</a></p> 
                <asp:LinkButton runat="server" ID="lbRecompress" Text="Re-Optimize image" OnClick="btnCompress_Click" CssClass="kraken-everything" />
            <% break;
            case EnmIsKrakable.Original: %> 
                <p>Your image has been optimized by <a href="https://kraken.io?cms=umbraco" target="_blank">kraken.io</a></p> 
            <% break;
        }
    %>
</div>