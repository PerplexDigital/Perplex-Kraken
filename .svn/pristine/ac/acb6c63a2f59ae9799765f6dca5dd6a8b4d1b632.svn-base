using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Text;
using System.Web;
using System.Xml.Serialization;
using System.Linq;
using System.Configuration;
using System.Reflection;

namespace Kraken
{
    internal static class Helper
    {
        /// <summary>
        /// Returns the description that is filled in by enum. Like Public Enum AdvanceVerzekeringType Description("Nieuw voertuig") NieuwVoertuig = 1 End Enum
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        public static string GetEnumDescription(object value)
        {
            string result = string.Empty;

            if (value != null)
            {
                result = value.ToString();
                //// Get the type from the object.
                Type type = value.GetType();
                try
                {
                    result = Enum.GetName(type, value);
                    //// Get the member on the type that corresponds to the value passed in.
                    FieldInfo fieldInfo = type.GetField(result);
                    //// Now get the attribute on the field.
                    object[] attributeArray = fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), false);
                    DescriptionAttribute attribute = null;

                    if ((attributeArray.Length > 0))
                    {
                        attribute = (DescriptionAttribute)attributeArray[0];
                    }
                    if ((attribute != null))
                    {
                        result = attribute.Description;
                    }
                }
                catch (ArgumentNullException)
                {
                    ////We shouldn't ever get here, but it means that value was null, so we'll just go with the default.
                    result = string.Empty;
                }
                catch (ArgumentException)
                {
                    ////we didn't have an enum.
                    result = value.ToString();
                }
                //// Return the description.
            }
            return result;
        }

        public static T ParametersToObject<T>(string data) where T: class, new()
        {
            try
            {
                if (HttpContext.Current.Request.ContentType == "application/x-www-form-urlencoded")
                {
                    data = HttpUtility.UrlDecode(data);
                    var parameters = data.Split('&').Select(x => x.Split('='));
                    var result = new T();
                    var properties = typeof(T).GetProperties();
                    foreach (var p in parameters)
                        if (p == null || p.Length != 2)
                            continue;
                        else
                        {
                            var pi = properties.FirstOrDefault(x => x.Name == p[0]);
                            if (pi != null)
                                if (pi.PropertyType == typeof(int))
                                    pi.SetValue(result, int.Parse(p[1]), null);
                                else if (pi.PropertyType == typeof(bool))
                                    pi.SetValue(result, bool.Parse(p[1]), null);
                                else
                                    pi.SetValue(result, p[1], null);
                        }
                    return result;
                } else
                    return FromJSON<T>(data);
            }
            catch 
            {
                return default(T);
            }
        }

        public static string ToJSON(object obj)
        {
            using (var ms = new System.IO.MemoryStream())
            {
                var dcs = new System.Runtime.Serialization.Json.DataContractJsonSerializer(obj.GetType());
                dcs.WriteObject(ms, obj);
                ms.Position = 0;
                var sr = new System.IO.StreamReader(ms);
                return sr.ReadToEnd();
            }
        }

        public static T FromJSON<T>(string json) where T : class
        {
            using (var ms = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(json)))
            {
                var dcs = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(T));
                return dcs.ReadObject(ms) as T;
            }
        }

        /// <summary>
        /// Zoek een bestand op op het interweb en laad deze in het geheugen
        /// </summary>
        /// <param name="url">De volledige URL naar het bestand</param>
        /// <returns></returns>
        public static MemoryStream DownloadFile(string url)
        {
            try
            {
                return new MemoryStream(new WebClient().DownloadData(url));
            }
            catch 
            {
                return null;
            }
        }

        /// <summary>
        /// Zoek een bestand op op het interweb en laad deze in het geheugen
        /// </summary>
        /// <param name="url">De volledige URL naar het bestand</param>
        /// <returns></returns>
        public static bool DownloadFile(string url, string filepath)
        {
            try
            {
                if (filepath.StartsWith("/")) filepath = System.Web.Hosting.HostingEnvironment.MapPath(filepath);
                new WebClient().DownloadFile(url, filepath);
                return System.IO.File.Exists(filepath);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Returns an object parsed from the xml
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="xml"></param>
        /// <returns></returns>
        public static T FromXml<T>(String xml)
        {
            try
            {
                using (TextReader reader = new StringReader(xml))
                    return (T)new XmlSerializer(typeof(T)).Deserialize(reader);
            }
            catch
            {
                return default(T);
            }
        }

        public static void refreshMediaTree(string path = "-1")
        {
            string script = String.Format(umbraco.BasePages.ClientTools.Scripts.SyncTree, path, "true");
            System.Web.UI.ScriptManager.RegisterStartupScript(umbraco.BasePages.BasePage.Current, umbraco.BasePages.BasePage.Current.GetType(), "refreshUmbracoTree", script, true);
        }

        public static void refreshMediaSection(int umbracoMediaId)
        {
            if (umbracoMediaId < 1000)
                return;
            var m = new umbraco.cms.businesslogic.media.Media(umbracoMediaId);
            if (m == null)
                return;

            // Voer wat Umbraco UI opties uit
            if (umbraco.BasePages.BasePage.Current != null)
            {
                // Ververs media tree
                string path, url;
                
                var p = m.getProperty(Constants.UmbracoPropertyAliasStatus);
                if (p != null && (p.Value as String) == "Original")
                {
                    path = m.Parent.Path;
                    url = "editMedia.aspx?id=" + m.ParentId.ToString();
                } else 
                {
                    path = m.Path;
                    url = "editMedia.aspx?id=" + m.Id.ToString();
                }

                // Ververs de tree aan de linker kant (in de media section
                refreshMediaTree(path);
                
                // Open media node
                string script = String.Format(umbraco.BasePages.ClientTools.Scripts.ChangeContentFrameUrl(url));
                System.Web.UI.ScriptManager.RegisterStartupScript(umbraco.BasePages.BasePage.Current, umbraco.BasePages.BasePage.Current.GetType(), "showMediaId", script, true);
            }
        }

        public static Version UmbracoVersion
        {
            get
            {
                return new Version(ConfigurationManager.AppSettings["umbracoConfigurationStatus"]);
            }
        }

        /// <summary>
        /// Helper function that transforms a JSON string to use double quotes " around keys and replaces values that have single-quote string literals ' with double quotes ".
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        internal static string AddJsonKeyQuotes(string json)
        {
            var data = json.ToList();
            bool key = false;
            bool singleQuote = false;
            bool doubleQuote = false;
            for (int i = data.Count - 1; i >= 0; i--)
            {
                if (doubleQuote)
                {
                    if (data[i] == '\"' && data[i - 1] != '\\')
                        doubleQuote = false;
                    continue;
                }
                else if (!key && data[i] == '\"')
                {
                    doubleQuote = true;
                    continue;
                }

                if (singleQuote)
                {
                    if (data[i] == '\'' && data[i - 1] != '\\')
                    {
                        data[i] = '\"';
                        singleQuote = true;
                    }
                    continue;
                }
                else if (!key && data[i] == '\'')
                {
                    data[i] = '\"';
                    singleQuote = false;
                    continue;
                }

                if (data[i] == ':')
                {
                    key = true;
                    data.Insert(i, '\"');
                }
                else if (key)
                {
                    if (data[i] == '{' || data[i] == ',')
                    {
                        key = false;
                        data.Insert(i + 1, '\"');
                    }
                    else if (!Char.IsLetterOrDigit(data[i]))
                        data.RemoveAt(i);
                }
            }
            return new string(data.ToArray());
        }
    }
}
