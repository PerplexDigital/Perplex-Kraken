using System;
using System.IO;
using System.Text;
using System.Web;
using System.Web.Http;
using Umbraco.Web.WebApi;

namespace Kraken
{
    [Umbraco.Web.Mvc.PluginController("Kraken")]
    public class KrakenCallbackApiController : UmbracoApiController
    {
        [HttpPost]
        //public void KrakenResults(Kraken results) // W: Toch maar liever alle klasses internal houden
        public void KrakenResults()
        {
            try
            {
                string data;
                using (var sr = new StreamReader(HttpContext.Current.Request.InputStream, Encoding.UTF8))
                    data = sr.ReadToEnd(); // Of moeten we de data uit de parameter vissel ala ==> (string data)
                var result = Helper.ParametersToObject<Kraken>(data);
                // Bepaal of het is gelukt
                if (result != null && !String.IsNullOrEmpty(result.id) && result.success && result.MediaId > 0)
                    // Sla maar op!
                    result.Save(global::Kraken.Configuration.Settings.KeepOriginal);
            }
            catch
            {
                // :(
            }
        }
    }
}