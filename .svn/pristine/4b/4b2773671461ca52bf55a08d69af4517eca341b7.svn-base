using System.Web.Http;
using Umbraco.Web.WebApi;

namespace Kraken
{
    [Umbraco.Web.Mvc.PluginController("Kraken")]
    public class KrakenApiController : UmbracoAuthorizedApiController // Deze is beschikbaar vanav Umbraco 6.1.0+, maar eigenlijk gebruiken we hem alleen voor Umbraco 7 installaties
    {
        [HttpGet]
        public EnmIsKrakable GetStatus(int imageId, string propVal)
        {
            return Kraken.GetKrakStatus(imageId);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="imageId"></param>
        /// <returns></returns>
        [HttpGet]
        public bool Optimize(int imageId)
        {
            try
            {
                if (imageId > 0)
                {
                    var result = Kraken.Compress(imageId);
                    if (result != null && result.success)
                    {
                        // Opslaan in Umbraco
                        result.Save();

                        // return result
                        return true;
                    }
                }

                // Not good?
                return false;
            }
            catch
            {
                // Toon een foutmelding aan de gebruiker
                return false;
            }
        }
    }
}