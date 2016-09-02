using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Xml;
using umbraco.interfaces;

namespace Kraken
{
    public class UmbracoInstaller : IPackageAction
    {
        public string Alias()
        {
            return "InstallKraken";
        }

        public bool Execute(string packageName, System.Xml.XmlNode xmlData)
        {
            string source = System.Web.Hosting.HostingEnvironment.MapPath("/app_plugins/kraken/kraken.dll");
            string target = System.Web.Hosting.HostingEnvironment.MapPath("/bin/kraken.dll");
            string sourceLegacy = System.Web.Hosting.HostingEnvironment.MapPath("/app_plugins/kraken/krakenlegacy.dll");
            string targetLegacy = System.Web.Hosting.HostingEnvironment.MapPath("/bin/krakenlegacy.dll");
            
            // Staat er evt een oude versie? Flikker die dan weg
            if (File.Exists(target)) File.Delete(target);
            if (File.Exists(targetLegacy)) File.Delete(targetLegacy);

            // Bestaan beide source bestanden (onze DLLs)?
            if (File.Exists(source) && File.Exists(sourceLegacy))
            {
                if (IsLegacyInstallation())
                {
                    File.Delete(source);
                    File.Move(sourceLegacy, targetLegacy);
                }
                else
                {
                    File.Move(source,target);
                    File.Delete(sourceLegacy);
                }
            }

            // Gooi de PerplexKraken installer ook weg
            target = System.Web.Hosting.HostingEnvironment.MapPath("/bin/krakeninstaller.dll");
            if (File.Exists(target))
                File.Delete(target);

            return true;
        }

        public bool IsLegacyInstallation()
        {
            try
            {
                // Bepaal hier vanaf waar de NIEUWE versie gebruikt mag worden. Alles hiervoor = legacy
                // W: Eigeniljk mag dit 6.0.0 zijn, maar omdat Umbraco de DLL's gaat scannen loopt hij vast door 'Umbraco.Web.WebApi.UmbracoApiController' UmbracoApiController (ondanks dat deze niet wordt gebruikt)
                Version vMinimum = new Version("6.1.0");
                Version vCurrent = null;
                string current = ConfigurationManager.AppSettings["umbracoConfigurationStatus"];
                if (current != null && current.Contains('-'))
                    current = current.Split('-')[0];
                if (!String.IsNullOrEmpty(current)) vCurrent = new Version(current);

                if (vCurrent != null)
                    // Indien de current version lager is dan de minimum version dan is het legacy
                    return vCurrent.CompareTo(vMinimum) < 0;
                else
                    return false;
            }
            catch
            {
                // Ga er voor nu maar uit dat het een legacy installation is...
                return true;
            }
        }

        public XmlNode SampleXml()
        {
            var d = new XmlDocument();
            var xml = string.Format("<Action runat=\"install\" alias=\"{0}\" />", Alias());
            d.LoadXml(xml);
            return d.SelectSingleNode("Action");
        }

        public bool Undo(string packageName, System.Xml.XmlNode xmlData)
        {
            string root = System.Web.Hosting.HostingEnvironment.MapPath("/bin/");
            string target = root + "kraken.dll";
            string targetLegacy = root + "krakenlegacy.dll";
            string targetInstaller = root + "krakeninstaller.dll";

            // Gooi de binary files weg
            if (File.Exists(target)) 
                File.Delete(target);
            if (File.Exists(targetLegacy)) 
                File.Delete(targetLegacy);
            if (File.Exists(targetInstaller))
                File.Delete(targetInstaller);

            return true;
        }
    }
}
