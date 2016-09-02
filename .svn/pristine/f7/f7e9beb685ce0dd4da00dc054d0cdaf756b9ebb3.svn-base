using System;
using System.IO;
using System.Text;
using System.Web;

namespace Kraken
{
    // W: Deze wordt ingesteld vanuit /config/restextensions.config
    internal class KrakenCallback
    {
        //    /Base/KrakenCallback/KrakenResults
        public static void KrakenResults()
        {
            try
            {
                string data = new StreamReader(HttpContext.Current.Request.InputStream, Encoding.UTF8).ReadToEnd();
                var result = Helper.ParametersToObject<Kraken>(data);
                // Bepaal of het is gelukt
                if (result != null && !String.IsNullOrEmpty(result.id) && result.success && result.MediaId > 0)
                    // Sla maar op!
                    result.Save(Configuration.Settings.KeepOriginal);
            }
            catch
            {
                // :(
            }
        }
    }
}