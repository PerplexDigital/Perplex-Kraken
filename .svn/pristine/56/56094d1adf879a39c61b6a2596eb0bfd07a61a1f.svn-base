using RestSharp;
using System;
using System.IO;
using System.Runtime.Serialization;

namespace Kraken
{
    internal class KrakenRequest
    {
        byte[] _file;
        string _url;
        RestClient _client = new RestClient(Constants.KrakenApiEndpoint);

        public Kraken GetResponse(bool? wait = null)
        {
            // Wordt de functie aangeroepen als async of niet?
            if (wait == null)
                // Geen dus haal maar op uit de configuratie (default)
                wait = Configuration.Settings.Wait;
            else
                // Specifiek instellen
                wait = wait.Value;

            string jsonData = Helper.ToJSON(new KrakenRequestMessage(wait.Value, _url));

            var request = new RestRequest(Method.ToString(), RestSharp.Method.POST);
            
            if (Method == enmMethod.upload)
            {
                request.AddParameter("json", jsonData);
                request.AddFile("image_bytes", _file, "perplexkraken");
            }
            else if (Method == enmMethod.url)
                request.AddParameter("application/json", jsonData, ParameterType.RequestBody);

            var response = _client.Execute<Kraken>(request);
            if (response != null)
            {
                var status = (enmStatus)(int)response.StatusCode;
                if (status != enmStatus.Ok)
                    throw new KrakenException(status);

                if (response.Data != null)
                    return response.Data;
                else
                    return new Kraken();
            }
            else
                throw new Exception("Kraken API yielded no response");
        }

        public KrakenRequest(Uri url)
        {
            _url = url.ToString();
        }

        public KrakenRequest(Stream imagestream)
        {
            using (var ms = new MemoryStream())
            {
                imagestream.CopyTo(ms);
                _file = ms.ToArray();
            }
        }

        enmMethod Method 
        { 
            get 
            {
                if (!String.IsNullOrEmpty(_url))
                    return enmMethod.url;
                else if (_file != null)
                    return enmMethod.upload;
                else
                    throw new Exception("Unable to determine method. Specify a filename or a URL.");
            }
        }


        [DataContract]
        public class KrakenRequestMessage
        {
            public KrakenRequestMessage()
            {

            }

            public KrakenRequestMessage(bool wait, string url)
            {
                this.wait = wait;
                this.url = url;
            }

            [DataMember(IsRequired = true)]
            public KrakenAuth auth { get { return KrakenAuth.Credentials; } private set { } }

            [DataMember(EmitDefaultValue = false)]
            public string url { get; private set; }

            [DataMember(IsRequired = true)]
            public bool wait { get; private set; }

            [DataMember]
            public bool json // W 25-11-2015: Callback URL altijd als json
            {
                get
                {
                    return true;
                }
                set
                {

                }
            }

            string _callbackUrl;
            [DataMember(EmitDefaultValue = false)]
            public string callback_url
            {
                get
                {
                    if (wait)
                        // Indien je niet Async aan het wachten bent, dan heeft het geen zin om een callback URL op te geven
                        return null;
                    else if (!String.IsNullOrEmpty(_callbackUrl))
                        return _callbackUrl;
                    else
                    {
                        //return "http://requestb.in/wrpmhgwr";
                        if (System.Web.HttpContext.Current != null)
                            return System.Web.HttpContext.Current.Request.Url.Scheme + Uri.SchemeDelimiter + System.Web.HttpContext.Current.Request.Url.Host + Kraken.umbracoCallbackUrl;
                        else
                            return null;
                    }
                }
                set
                {
                    _callbackUrl = value;
                }
            }

            [DataMember(IsRequired = true)]
            public bool lossy { get { return Configuration.Settings.Lossy; } private set { } }
        }

        [DataContract]
        public class KrakenAuth
        {
            static KrakenAuth _auth = new KrakenAuth();
            [DataMember]
            public static KrakenAuth Credentials { get { return _auth; } }
            [DataMember(IsRequired = true)]
            public string api_key { get { return Configuration.Settings.ApiKey; } private set {} }
            [DataMember(IsRequired = true)]
            public string api_secret { get { return Configuration.Settings.ApiSecret; } private set { } }
        }
    }
}
