using System;

namespace Kraken.Controls
{
    public partial class Status : System.Web.UI.UserControl, umbraco.editorControls.userControlGrapper.IUsercontrolDataEditor
    {
        public EnmIsKrakable status;

        public object value
        {
            get { return ViewState["KrakInfo"]; }
            set
            {
                ViewState["KrakInfo"] = value;
                status = Kraken.GetKrakStatus(CurrentNodeId);
            }
        }

        public int CurrentNodeId
        {
            get
            {
                int tmp = 0;
                int.TryParse(Request["id"], out tmp);
                return tmp;
            }
        }

        public void btnCompress_Click(object sender, EventArgs e)
        {
            try
            {
                if (CurrentNodeId > 0)
                {
                    var result = Kraken.Compress(CurrentNodeId);
                    if (result != null && result.success)
                    {
                        // Opslaan in Umbraco
                        result.Save();
                        // Umbraco Media tree verversen
                        Helper.refreshMediaSection(CurrentNodeId);
                    }
                }
            }
            catch (KrakenException kex)
            {
                var ct = new umbraco.BasePages.ClientTools(Page);
                ct.ShowSpeechBubble(umbraco.BasePages.BasePage.speechBubbleIcon.error, "Optimization error", Helper.GetEnumDescription(kex.Status));
            }
            catch (Exception kex)
            {
                var ct = new umbraco.BasePages.ClientTools(Page);
                ct.ShowSpeechBubble(umbraco.BasePages.BasePage.speechBubbleIcon.error, "Unexpected internal error", "Image could not optimized");
            }
        }

        // W: Onderstaande laten staan ondanks dat het outcommented it. DIt is handig om de postbacks naar de API te testen!

        //protected void btnPost_Click(object sender, EventArgs e)
        //{
        //    string url = "http://localhost:59634/Base/KrakenCallback/KrakenResults";
        //    string json = "file_name=perplexkraken&original_size=58496&kraked_size=53238&saved_bytes=5258&kraked_url=https%3A%2F%2Fapi-worker-2.kraken.io%2F29c48cd5c7%2Fperplexkraken&success=true&id=29c48cd5c7";

        //    var req = WebRequest.Create(new Uri(url)) as HttpWebRequest;
        //    req.Method = "POST";
        //    req.ContentType = "application/x-www-form-urlencoded";

        //    // Encode the parameters as form data:
        //    byte[] formData = UTF8Encoding.UTF8.GetBytes(json);
        //    req.ContentLength = formData.Length;

        //    // Send the request:
        //    using (var post = req.GetRequestStream())
        //        post.Write(formData, 0, formData.Length);

        //    // Pick up the response:
        //    string result = null;
        //    using (HttpWebResponse resp = req.GetResponse() as HttpWebResponse)
        //    {
        //        var reader = new StreamReader(resp.GetResponseStream());
        //        result = reader.ReadToEnd();
        //    }
        //}
    }
}