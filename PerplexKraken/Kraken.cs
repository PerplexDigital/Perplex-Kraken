using System;
using System.IO;
using System.Threading;
using System.Web;
using System.Linq;
using System.Xml;

namespace Kraken
{
    // Alles wat niet gerelateerd is aan Umbraco hier
    internal partial class Kraken
    {
        static Thread _krakThread;

        public string file_name { get; set; }

        public int original_size { get; set; }

        public int kraked_size { get; set; }

        public int saved_bytes { get; set; }

        public string kraked_url { get; set; }

        public bool success { get; set; }

        public string id { get; set; }

        public string message { get; set; }

        //public enmStatus status { get; set; }

        int _mediaId;
        public int MediaId
        {
            get
            {
                if (_mediaId > 0)
                    return _mediaId;
                else if (!String.IsNullOrEmpty(id) && HttpRuntime.Cache["kraken_" + id] != null)
                    _mediaId = (int)HttpRuntime.Cache["kraken_" + id];
                return _mediaId;
            }
            set
            {
                _mediaId = value;
                if (!String.IsNullOrEmpty(id))
                    // Bewaar de bijbehorende media id voor 15 min
                    HttpRuntime.Cache.Add("kraken_" + id, value, null, DateTime.Now.AddMinutes(15), System.Web.Caching.Cache.NoSlidingExpiration, System.Web.Caching.CacheItemPriority.Normal, null); 
            }
        }

        int _umbracoUserId;
        int UmbracoUserId
        {
            get
            {
                if (!String.IsNullOrEmpty(id) && HttpRuntime.Cache["kraken_" + id + "_user"] != null)
                    _umbracoUserId = (int)HttpRuntime.Cache["kraken_" + id + "_user"];
                return _umbracoUserId;
            }
            set
            {
                _umbracoUserId = value;
                HttpRuntime.Cache.Add("kraken_" + id + "_user", value, null, DateTime.Now.AddMinutes(15), System.Web.Caching.Cache.NoSlidingExpiration, System.Web.Caching.CacheItemPriority.Normal, null); 
            }
        }

        /// <summary>
        /// Save the kraked image to a specific target filepath on the server.
        /// </summary>
        /// <param name="filepath">A relative or absolute filepath</param>
        /// <returns>Success status</returns>
        public bool Save(string filepath)
        {
            if (filepath.StartsWith("/")) filepath = System.Web.Hosting.HostingEnvironment.MapPath(filepath);
            return Helper.DownloadFile(kraked_url, filepath);
        }


        public static Kraken Compress(Uri url, bool? wait = null)
        {
            var request = new KrakenRequest(url);
            return request.GetResponse(wait);
        }

        public static Kraken Compress(string filepath, bool? wait = null)
        {
            if (filepath == null || String.IsNullOrEmpty(filepath))
                return null;

            if (filepath.StartsWith("/"))
                filepath = System.Web.Hosting.HostingEnvironment.MapPath(filepath);

            if (!System.IO.File.Exists(filepath))
                return null;

            using (var fs = new FileStream(filepath, FileMode.Open))
                return Compress(fs, wait);
        }

        public static Kraken Compress(Stream imagestream, bool? wait = null)
        {
            var request = new KrakenRequest(imagestream);
            return request.GetResponse(wait);
        }
        
        /// <summary>
        /// Gooi alle plaatjes in de Media map door de Kraken API heen
        /// </summary>
        /// <param name="reKrak">Alles opnieuw krakken (ook al is het plaatje voorheen al gekrakt)?</param>
        public static void KrakEverything(bool reKrak = false)
        {
            if (!IsKraking)
            {
                _krakThread = new Thread(StartKraking);
                _krakThread.Name = "Kraking module";
                _krakThread.IsBackground = true;
                _krakThread.Start(reKrak);
            }
        }

        public static bool IsKraking
        {
            get
            {
                return _krakThread != null && _krakThread.ThreadState == ThreadState.Running;
            }
        }

        /// <summary>
        /// Install PerplexKraken on this Umbraco installation. You must also specify on which media types through the 'selectedMediaTypes' parameter
        /// </summary>
        /// <param name="selectedMediaTypes">Umbraco Mediatype (= ContentType) Id's to use with PerplexKraken</param>
        internal static void Install()
        {
            addDashboardTab();
            installUmbracoSpecifics();
        }

        /// <summary>
        /// This function adds a new tab to the Umbraco Media section.
        /// This is done by modifying the file '/config/dashboard.config' and inserting a new tab in the media section
        /// </summary>
        static void addDashboardTab()
        {
            try
            {
                // Bepaal de pad naar het config bestand
                string filepath = System.Web.Hosting.HostingEnvironment.MapPath("/config/dashboard.config");

                // Laad het config bestand in
                var doc = new XmlDocument();

                if (File.Exists(filepath))
                    doc.Load(filepath);
                else
                    return; // ff pech

                var nDashboard = doc.SelectSingleNode("dashBoard");
                if (nDashboard == null)
                    nDashboard = doc.AppendChild(doc.CreateElement("dashBoard"));

                var nSection = nDashboard.SelectSingleNode("section[@alias='StartupMediaDashboardSection']");
                if (nSection == null)
                {
                    var section = doc.CreateElement("section");
                    section.SetAttribute("alias", "StartupMediaDashboardSection");
                    nSection = nDashboard.AppendChild(section);
                }

                var nAreas = nSection.SelectSingleNode("areas");
                if (nAreas == null)
                    nAreas = nSection.AppendChild(doc.CreateElement("areas"));
                var nArea = nAreas.SelectSingleNode("area");
                if (nArea == null || nArea.InnerText != "media")
                {
                    var area = doc.CreateElement("area");
                    area.InnerText = "media";
                    nArea = nAreas.AppendChild(area);
                }

                var nTab = nSection.SelectSingleNode("tab[@caption='" + Constants.UmbracoMediaTabnameKraken +"']");
                if (nTab == null)
                {
                    // Maak het control element aan
                    var control = doc.CreateElement("control");
                    control.SetAttribute("showOnce", "false");
                    control.SetAttribute("addPanel", "false");
                    control.SetAttribute("panelCaption", "");
                    control.InnerText = "/App_Plugins/Kraken/Overview.ascx";

                    // Maak het tab element aan
                    var tab = doc.CreateElement("tab");
                    tab.SetAttribute("caption", Constants.UmbracoMediaTabnameKraken);

                    // Voeg de inhoud toe aan de tab
                    tab.AppendChild(control);

                    // Voeg de tab achteraan toe
                    nSection.AppendChild(tab);

                    // Opslaan!
                    doc.Save(filepath);
                }
            }
            catch
            {
                // Toon een error aan de gbruiker
            }
        }


        internal static void Uninstall()
        {
            removeDashboardTab();
            uninstallUmbracoSpecifics();
            removeFiles();
        }

        static void removeFiles()
        {
            // Bepaal welke bestanden we weg gaan gooien
            string[] _cleanupFiles = new string[] 
            {
                "/config/Kraken.config",
                "/bin/Kraken.dll",
                "/bin/KrakenLegacy.dll",
                "/bin/KrakenInstaller.dll",
            };

            // Gooi ze maar weg!
            foreach (string file in _cleanupFiles.Select(x => System.Web.Hosting.HostingEnvironment.MapPath(x)))
                if (File.Exists(file)) 
                    File.Delete(file);

            // Gooi het mapje ook weg!
            string krakenDirectory = System.Web.Hosting.HostingEnvironment.MapPath("/App_Plugins/Kraken/");
            
            try
            {
                if (Directory.Exists(krakenDirectory))
                    Directory.Delete(krakenDirectory);
            }
            catch
            {
                // Als ie niet leeg is krijg je hier een exception. Ok dan niet!
            }
        }

        /// <summary>
        /// This function removes the PerplexKraken tab from the Umbraco Media section.
        /// This is done by modifying the file '/config/dashboard.config'
        /// </summary>
        static void removeDashboardTab()
        {
            try
            {
                // Bepaal de pad naar het config bestand
                string filepath = System.Web.Hosting.HostingEnvironment.MapPath("/config/dashboard.config");

                // Laad het config bestand in
                var doc = new XmlDocument();
                doc.Load(filepath);
                // Ga op zoek naar de Umbraco Media section
                var n = doc.SelectSingleNode("dashBoard/section[@alias='StartupMediaDashboardSection']");
                // gevonden?
                if (n != null)
                {
                    // Probeer de PerplexKraken tab uit te lezen
                    var tab = n.SelectSingleNode("tab[@caption='" + Constants.UmbracoMediaTabnameKraken +"']");
                    // Gevonden?
                    if (tab != null)
                    {
                        // Haal de tab weg
                        n.RemoveChild(tab);
                        // Wijzigingen opslaan!
                        doc.Save(filepath);
                    }
                }
            }
            catch
            {
                // Toon een foutmelding
            }
        }
    }
}
