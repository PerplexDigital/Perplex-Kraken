using System;
using System.IO;
using System.Xml.Serialization;

namespace Kraken
{
    public class Configuration
    {
        static Configuration _config;

        string _apiKey;
        string _apiSecret;

        // De standaard instellingen
        bool _hideKeys = false;
        bool _enabled = false;
        bool _lossy = true;
        bool _keepOriginal = true;
        bool _wait = true;

        public string ApiKey { get { return _apiKey; } set { _apiKey = value;  } }
        public string ApiSecret {  get { return _apiSecret; } set { _apiSecret = value; } }
        public bool HideKeys { get { return _hideKeys; } set { _hideKeys = value; } }
        public bool Enabled { get { return _enabled; } set { _enabled = value; } }
        public bool Lossy { get { return _lossy; } set { _lossy = value; } }
        public bool KeepOriginal { get { return _keepOriginal; } set { _keepOriginal = value; } }
        public bool Wait { get { return _wait; } set { _wait = value; } }

        public static Configuration Settings
        {
            get
            {
                if (_config == null)
                {
                    // Absolute pad bepalen
                    string filepath = Constants.ConfigFile;
                    if (filepath.StartsWith("/"))
                        filepath = System.Web.Hosting.HostingEnvironment.MapPath(filepath);

                    if (filepath == null)
                    {
                        if (Constants.ConfigFile.StartsWith("/"))
                            filepath = Environment.CurrentDirectory + Constants.ConfigFile.Replace("/", "\\");
                        else
                            filepath = Environment.CurrentDirectory + "\\" + Constants.ConfigFile.Replace("/", "\\");
                    }
                    // Uitlezen als XML
                    if (System.IO.File.Exists(filepath))
                        using (StreamReader sr = new StreamReader(filepath))
                            _config = Helper.FromXml<Configuration>(sr.ReadToEnd());
                    else
                        _config = new Configuration();
                }
                return _config;
            }
        }

        /// <summary>
        /// Save all the changes to the Configuration object to the /config/Kraken.config file
        /// </summary>
        public void Save()
        {
            _config = this;
            try
            {
                // Absolute pad bepalen
                string filepath = Constants.ConfigFile;
                if (filepath.StartsWith("/")) filepath = System.Web.Hosting.HostingEnvironment.MapPath(filepath);
                // Opslaan als XML
                using (TextWriter writer = new StreamWriter(filepath, false))
                    new XmlSerializer(typeof(Configuration)).Serialize(writer, _config);
            }
            catch (Exception ex)
            {
                throw new Exception("Unable to save the Kraken configuration settings: " + ex.Message);
            }
        }
    }
}