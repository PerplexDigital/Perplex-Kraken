using System;
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
        public OptimizeResult Optimize(int imageId)
        {
            try
            {
                if (imageId > 0)
                {
                    var result = Kraken.Compress(imageId);
                    if (result != null && result.success)
                    {
                        // Save to Umbraco
                        result.Save();

                        return new OptimizeResult { Success = true };
                    }
                }

                // Invalid media id
                return new OptimizeResult { Success = false, Message = "Invalid media id" };
            }
            catch (KrakenException kex)
            {
                return new OptimizeResult
                {
                    Success = false,
                    Message = "Kraken.io error: (" + kex.Status.ToString("d") + ") " + Helper.GetEnumDescription(kex.Status)
                };
            }
            catch (Exception ex)
            {
                return new OptimizeResult
                {
                    Success = false,
                    Message = "An unexpected internal error has occurred; the image could not be optimized",
                };
            }
        }

        public class OptimizeResult
        {
            public bool Success { get; set; }
            public string Message { get; set; }
        }
    }
}