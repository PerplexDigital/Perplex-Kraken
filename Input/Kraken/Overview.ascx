<%@ Control Language="C#" AutoEventWireup="true" Inherits="Kraken.Controls.Overview" %>

<style>
.kraken .info {display: inline-block; background: #EC297B url("/App_Plugins/Kraken/info.png") 2px 1px no-repeat; width: 16px; height: 16px; -webkit-border-radius: 50%; -moz-border-radius: 50%; border-radius: 50%; position: relative;}
.kraken .info:hover {background-color: #4d4d4d;}
.kraken .info:hover .info-block {display: block;}
.kraken .info-block {display: none; position: absolute; width: 300px; height: auto; background: #4d4d4d; color: white; left: 25px; top: -12px; font-family: Arial, Helvetica, sans-serif; font-size: 11px; padding: 22px 18px 18px; -webkit-border-radius: 3px; -moz-border-radius: 3px; border-radius: 3px;}
.kraken .info-block strong {display: block; padding: 0; margin: 0 0 8px; text-transform: uppercase; font-size: 12px; letter-spacing: 1px;}
.kraken .info-block span {display: block; margin-bottom: 12px; line-height: 1.4; color: #e6e6e6; letter-spacing: 1px; font-weight: normal;}
.kraken .kraken-everything {display: inline-block; background: #F3C50F; color: #1a1a1a; text-transform: uppercase; font-family: Arial, Helvetica, sans-serif; text-decoration: none; font-size: 14px; font-weight: bolder; padding: 14px 16px; letter-spacing: 2px; margin-right:5px; -webkit-border-radius: 3px; -moz-border-radius: 3px; border-radius: 3px;}
.kraken .kraken-everything:hover {background: #1a1a1a; color: #F3C50F;}
.kraken .kraken-save {display: inline-block; background: #ccc; color: #1a1a1a; text-transform: uppercase; font-family: Arial, Helvetica, sans-serif; text-decoration: none; font-size: 10px; font-weight: normal; padding: 12px 15px; letter-spacing: 1px; -webkit-border-radius: 3px; -moz-border-radius: 3px; border-radius: 3px;}
.kraken .kraken-save:hover {background: #e0e0e0;}
.kraken p {margin: 30px 0 0; padding: 0; font-family: "Trebuchet MS", Helvetica, sans-serif;}
.kraken a {color: #EC297B; text-decoration:underline;}
.kraken a:hover {text-decoration:none;}
</style>
<table cellspacing="20" border="0" class="kraken" style="width:500px;">
    <tbody>
        <tr><td colspan="3"><h3>Settings</h3></td></tr>
        <asp:PlaceHolder runat="server" ID="phKeys">
        <tr>
            <td width="20%">
                <asp:Label runat="server" Text="API key" AssociatedControlID="txtApiKey" /> 
            </td>
            <td width="30%">
                <asp:TextBox runat="server" ID="txtApiKey" style="width:300px" />
            </td>
            <td width="50%">
                <div class="info">
	                <div class="info-block">
		                <strong>Kraken API keys</strong>
		                <span>The usage of the Kraken API requires a valid API key which you can obtain on <a href="https://kraken.io/plans?cms=umbraco" target="_blank">kraken.io</a></span>
	                </div>
                </div>
            </td>
        </tr>
        <tr>
            <td>
                <asp:Label runat="server" Text="API secret" AssociatedControlID="txtApiSecret" />
            </td>
            <td>
                <asp:TextBox runat="server" ID="txtApiSecret" style="width:300px" />
            </td>
            <td></td>
        </tr>
        <tr>
            <td>
                <asp:Label runat="server" Text="Hide API keys?" AssociatedControlID="cbHideKeys" />
            </td>
            <td>
                <asp:CheckBox runat="server" ID="cbHideKeys" Text="Hide keys?"  />
            </td>
            <td>
                <div class="info">
	                <div class="info-block">
		                <strong>Kraken API keys</strong>
		                <span>Enabling this option will hide the API keys (above) from this UI. They keys can still be found in the /config/perplexkraken.config file.</span>
	                </div>
                </div>
            </td>
        </tr>
        </asp:PlaceHolder>
        <tr>
            <td>
                <asp:Label runat="server" Text="Wait" AssociatedControlID="cbWait" ToolTip="Enabling this feature slightly increases the time required to optimize the Image (depending on file size), however backoffice users that are uploading and saving images will directly be able to view the results without having to refresh the page." />
            </td>
            <td>
                <asp:CheckBox runat="server" ID="cbWait"  />
            </td>
            <td>
                <div class="info">
	                <div class="info-block">
		                <strong>Asynchronous processing</strong>
		                <span>Kraken offers two ways to individually optimize images. Whenever a user clicks the "Optimize" button or presses save (and the "Automatic compression" feature is enabled), Kraken will begin optimizing your image.
                                The images are then sent to <a href="http://kraken.io/cms=umbraco" target="_blank">kraken.io</a> for processing after which it is sent back to Umbraco to be saved.</span>
                        <span>Large images generally take up more processing time which may force the user to wait for a response from <a href="http://kraken.io/cms=umbraco" target="_blank">kraken.io</a> when saving an image.</span>
                        <span>Enabling this feature will not block the saving proces and lets the user proceed with other tasks while Kraken optimizes the images. When the image is optimized, Kraken sends the Umage back to Umbraco after which Umbraco saves the optimized images to the media section.
                            Enabling this feature also causes the user to not be notified of any optimization results and will require a refresh on the Media item after the image has been optimized and saved.</span>
	                </div>
                </div>
            </td>
        </tr>
        <tr>
            <td>
                <asp:Label runat="server" Text="Automatic compression" AssociatedControlID="cbEnabled" />
            </td>
            <td>
                <asp:CheckBox runat="server" ID="cbEnabled" />
            </td>
            <td>
                <div class="info">
	                <div class="info-block">
		                <strong>Automatic compression</strong>
		                <span>Enabling this feature will cause Kraken to automatically compress any image that is saved or uploaded in the Umbraco Media section according to the settings specified on this page.</span>
                        <span>When this feature is not enabled, you can still optimize images manually by using the 'Krak everything</span>
	                </div>
                </div>
            </td>
        </tr>
        <tr>
            <td>
                <asp:Label runat="server" Text="Keep original" AssociatedControlID="cbKeepOriginal" />
            </td>
            <td>
                <asp:CheckBox runat="server" ID="cbKeepOriginal" />
            </td>
            <td>
                <div class="info">
	                <div class="info-block">
		                <strong>Keep original</strong>
                        <span>Whenever any Umbraco Media image is optimized, Kraken will update the Media image in your library and replace the original image with the optimized image.</span>
                        <span>When this feature is enabled a backup of the original media image will be placed underneath the optimized image with the name 'Original'.</span>
                        <span>This feature usefull for backup/rollback purposes and thus enabled by default. When backups are not a concern or harddisc space is a concern this feature may safely be disabled.</span>
	                </div>
                </div>
            </td>
        </tr>
         <tr>
            <td>
                <asp:Label runat="server" Text="Optimization type:" AssociatedControlID="rblLossy" ToolTip="When lossy is selected, Kraken will modify the image slightly (unnoticably) in order to greatly decrease file size. We strongly recommend keeping this option enabled. For more information see https://kraken.io/web-interface" />
            </td>
            <td>
                <asp:RadioButtonList runat="server" ID="rblLossy" RepeatDirection="Horizontal" RepeatLayout="Flow">
                    <asp:ListItem Text="Lossy" />
                    <asp:ListItem Text="Lossless" />
                </asp:RadioButtonList>
            </td>
            <td>
                <div class="info">
	                <div class="info-block">
		                <strong>Lossless</strong>
		                <span>This mode will push your images to the extreme without changing a single pixel. Lossless option is perfect when image quality is the most important factor. Keep in mind that this mode is more time consuming.</span>
			
		                <strong>Lossy</strong>
		                <span>When you decide to sacrifice just a small amount of image quality (unnoticeable to the human eye), you will be able to save up to 90% (!) of the initial file weight. Lossy optimization will give you outstanding results with just a fraction of image quality loss.</span>
	                </div>
                </div>
            </td>
        </tr>
        <tr>
            <td colspan="3">
                <asp:LinkButton runat="server" ID="lbSave" Text="Save settings" OnClick="lbSave_click" CssClass="kraken-save" />
                <asp:Label runat="server" ID="lblSaved" Visible="false" Text="<p>Your settings have been saved.</p>" ForeColor="Green" Font-Bold="true"/>
            </td>
        </tr>
        <tr>
            <td colspan="3" >
                <h3>Optimize all images</h3>
                <p>All your images will be optimised through the <a href="https://kraken.io?cms=umbraco" target="_blank">kraken.io</a>. The usage of the Kraken API requires a key which you can obtain on <a href="https://kraken.io/plans?cms=umbraco" target="_blank">kraken.io</a>. The optimization will be started in a seperate thread.</p><br /><br />
                <asp:LinkButton runat="server" ID="lbKrakEverything" Text="Optimize all images" CssClass="kraken-everything" OnClick="lbKrakEverything_Click" OnClientClick="return confirm('This process automatically optimizes all images in the Media section. The update progress may take several minutes depending on the number of Images in your Media section. You do not need to keep the browser open during the optimization proces. Proceed?')" />
                <asp:LinkButton runat="server" ID="lbReKrakEverything" Text="Re-Optimize all images" style="margin-right:0" CssClass="kraken-everything" OnClick="lbReKrakEverything_Click" OnClientClick="return confirm('This process automatically optimizes all images in the Media section. The update progress may take several minutes depending on the number of Images in your Media section. You do not need to keep the browser open during the optimization proces. Proceed?')" /><br />
            </td>
        </tr>
    </tbody>
   
</table>
