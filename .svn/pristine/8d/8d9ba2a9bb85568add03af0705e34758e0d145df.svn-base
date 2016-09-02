using System;
using System.IO;
using System.Web;
using System.Linq;
using umbraco.cms.businesslogic.media;
using System.Xml;

namespace Kraken
{
    // Alle umbraco gerelateerde code hier
    internal partial class Kraken
    {
        internal const string umbracoCallbackUrl = "/Base/PerplexKraken/KrakenResults";

        public Kraken()
        {
            if (HttpContext.Current != null)
            {
                var u = umbraco.BusinessLogic.User.GetCurrent();
                if (u != null)
                    UmbracoUserId = u.Id;
                else
                    UmbracoUserId = 0;
            }
            else
                UmbracoUserId = 0;
        }

        internal static EnmIsKrakable GetKrakStatus(int umbracoMediaId)
        {
            return GetKrakStatus(new Media(umbracoMediaId));
        }

        internal static EnmIsKrakable GetKrakStatus(Media im)
        {
            // Je mag een Umbraco Media node krakken onder de volgende voorwaarden:

            // 1: Er zit wat in :)
            if (im == null || im.Id == 0)
                return EnmIsKrakable.Unkrakable;

            var p = im.getProperty(Constants.UmbracoPropertyAliasStatus);
            // 2: Het status veld dient aanwezig te zijn
            if (p == null)
                return EnmIsKrakable.Unkrakable;

            // 3: Als hij geen afbeelding bevat, dan mag het niet
            p = im.getProperty(Constants.UmbracoPropertyAliasFile);
            if (p == null || p.Value == null || String.IsNullOrEmpty(p.Value.ToString()))
                return EnmIsKrakable.Unkrakable;

            // 4: Als het geen jpeg, gif of png is dan kan het bestand niet gekrakt worden
            string img = p.Value.ToString();
            if (img == null ||
               (!img.EndsWith("jpg", StringComparison.OrdinalIgnoreCase) &&
                !img.EndsWith("jpeg", StringComparison.OrdinalIgnoreCase) &&
                !img.EndsWith("png", StringComparison.OrdinalIgnoreCase) &&
                !img.EndsWith("gif", StringComparison.OrdinalIgnoreCase)))
                return EnmIsKrakable.Unkrakable;

            // W 18-9-2015: Vanaf nu mag je afbeeldingen re-kraken
            // 3: En dit status veld dient ook nog eens LEEG te zijn (want als je al een status hebt dan is er dus al iets gebeurd)
            //    Status betekend dus echt EINDSTATUS
            if (p.Value != null && !String.IsNullOrEmpty(p.Value.ToString()))
            {
                if (p.Value == EnmKrakStatus.Original.ToString())
                    return EnmIsKrakable.Original;
                else if (p.Value == EnmKrakStatus.Compressed.ToString())
                    return EnmIsKrakable.Kraked;
                else
                    // Weet niet wat het is dus niet kraken!
                    return EnmIsKrakable.Unkrakable;
            }
            else
                // Yaay, deze media item mag gekrakt worden
                return EnmIsKrakable.Krakable;
        }
        
        /// <summary>
        /// Save the kraked image to the associated Umbraco Media node (if applicable).
        /// </summary>
        /// <param name="keepOriginal"></param>
        /// <returns>Success status</returns>
        internal bool Save(bool? keepOriginal = null)
        {
            if (MediaId >= 1000)
                return Save(new Media(MediaId), keepOriginal);
            else
                throw new Exception("There is no Umbraco Media Id associated with this Kraken object");
        }

        /// <summary>
        /// Save the kraked image to a specific Umbraco Media node
        /// </summary>
        /// <param name="umbracoMediaId">Target media node</param>
        /// <param name="keepOriginal">Save the original image in Umbraco? When this parameter is not specified, the default configuration will be used</param>
        /// <returns>Success status</returns>
        internal bool Save(int umbracoMediaId, bool? keepOriginal = null)
        {
            if (umbracoMediaId >= 1000)
                return Save(new Media(umbracoMediaId), keepOriginal);
            else
                throw new ArgumentException("Invalid Umbraco media id", "mediaId");
        }

        /// <summary>
        /// Save the kraked image to a specific Umbraco Media node
        /// </summary>
        /// <param name="umbracoMedia">Target media node</param>
        /// <param name="keepOriginal">Save the original image in Umbraco?</param>
        /// <returns>Success status</returns>
        internal bool Save(Media umbracoMedia, bool? keepOriginal = null)
        {
            // Invoer controle
            var status = GetKrakStatus(umbracoMedia);
            if (status == EnmIsKrakable.Unkrakable || String.IsNullOrEmpty(kraked_url))
                // We kunnen niet verder
                return false;

            // Moet het plaatje gebackupped worden?
            if (keepOriginal == null)
                keepOriginal = Configuration.Settings.KeepOriginal;

            // Bepaal het pad en de naam van het huidige plaatje
            string relativeFilepath = umbracoMedia.getProperty(Constants.UmbracoPropertyAliasFile).Value.ToString();
            string filename = Path.GetFileName(relativeFilepath);

            // En sla hem op op de Umbraco Media item
            var success = umbracoMedia.AddFile(kraked_url, filename);
            if (!success)
                // Als we het plaatje NIET konden krakken, downloaden of opslaan om wat voor een reden dan ook => dan STOPPEN!
                return false;

            umbraco.cms.businesslogic.property.Property p;
            if (keepOriginal.Value)
            {
                // STEL er hangt al een originele media item onder, gebruik dan die als original media item.
                // Anders blijven we elke keer nieuwe nodes aanmaken onder hetzelfde plaatje en dat is onzin
                Media original = umbracoMedia.Children.FirstOrDefault(x => x.Text == "Original" && x.getProperty(Constants.UmbracoPropertyAliasStatus) != null && x.getProperty(Constants.UmbracoPropertyAliasStatus).Value != null && x.getProperty(Constants.UmbracoPropertyAliasStatus).Value.ToString() == "Original");

                // Maak een nieuwe media item aan. Deze komt te hangen ONDER de bestaande media item en zal een backup zijn van het origineel (maar wel met een andere media ID!)
                // Was er al een original onder het huidige media item gevonden?
                if (original == null)
                    // Nee, in dat geval mag je hem aanmaken. Dezelfde naam, alias etc
                    original = Media.MakeNew("Original", MediaType.GetByAlias(umbracoMedia.ContentType.Alias), umbracoMedia.User, umbracoMedia.Id);

                // Kopieer alle properties van de originele afbeelding
                foreach (var prop in original.GenericProperties)
                    prop.Value = umbracoMedia.getProperty(prop.PropertyType.Alias).Value;

                // Het plaatje is alleen wel voorheen aangepast dus de filename moeten we even goed zetten
                p = original.getProperty(Constants.UmbracoPropertyAliasFile);
                if (p != null)
                    p.Value = relativeFilepath;

                // Stel op de "backup" een status in om aan te geven dat het het origineel is
                p = original.getProperty(Constants.UmbracoPropertyAliasStatus);
                if (p != null)
                    p.Value = "Original";

                // Sla hem op. Hierdoor komt direct het origineel onder de bestaande te hangen
                original.Save();
            }

            // Toon de nieuwe afbeelding size
            p = umbracoMedia.getProperty(Constants.UmbracoPropertyAliasSize);
            if (p != null)
                p.Value = kraked_size;

            // Toon de gekrakte size
            p = umbracoMedia.getProperty(Constants.UmbracoPropertyAliasOriginalSize);
            if (p != null)
                p.Value = original_size;

            // Toon de krak status
            p = umbracoMedia.getProperty(Constants.UmbracoPropertyAliasStatus);
            if (p != null)
                p.Value = "Compressed";

            // Toon de krak date
            p = umbracoMedia.getProperty(Constants.UmbracoPropertyAliasCompressionDate);
            if (p != null)
                p.Value = DateTime.Now.ToString();

            // Toon de krak date
            p = umbracoMedia.getProperty(Constants.UmbracoPropertyAliasCompressionDate);
            if (p != null)
                p.Value = DateTime.Now.ToString();

            // Plaatje breedte
            p = umbracoMedia.getProperty(Constants.UmbracoPropertyAliasWidth);
            if (p != null)
                p.Value = p.Value;

            // Plaatje hoogte
            p = umbracoMedia.getProperty(Constants.UmbracoPropertyAliasHeight);
            if (p != null)
                p.Value = p.Value;

            // Plaatje type
            p = umbracoMedia.getProperty(Constants.UmbracoPropertyAliasExtension);
            if (p != null)
                p.Value = p.Value;

            // Toon de besparing
            p = umbracoMedia.getProperty(Constants.UmbracoPropertyAliasSaved);
            if (original_size > 0 && saved_bytes > 0 && p != null)
                p.Value = ((decimal)saved_bytes / original_size).ToString("p2");

            // Nieuwe afbeelding opslaan
            umbracoMedia.Save();

            // Cache opruimen
            HttpRuntime.Cache.Remove("kraken_" + id);
            HttpRuntime.Cache.Remove("kraken_" + id + "_user");
            
            // Ververs de Umbraco Media cache (anders krijg je de oude URL geserveerd)
            umbraco.library.ClearLibraryCacheForMedia(umbracoMedia.Id);

            return true;
        }

        internal static Kraken Compress(int umbracoMediaId, bool? wait = null)
        {
            if (umbracoMediaId >= 1000)
                return Compress(new Media(umbracoMediaId), wait);
            else
                throw new ArgumentException("Invalid Umbraco media id", "mediaId");
        }

        internal static Kraken Compress(Media umbracoMedia, bool? wait = null)
        {
            if (umbracoMedia == null || umbracoMedia.Id == 0)
                throw new ArgumentException("Invalid Umbraco Media node", "umbracoMedia");

            var p = umbracoMedia.getProperty(Constants.UmbracoPropertyAliasFile);
            if (p != null && p.Value != null)
            {
                string img = p.Value.ToString();
                string imageUrl = HttpContext.Current.Request.Url.GetLeftPart(UriPartial.Authority) + img;
                var uri = new Uri(imageUrl);
                var result = Compress(uri, wait);
                if (result != null)
                    result.MediaId = umbracoMedia.Id;
                return result;
            }
            else
                throw new Exception("Target Umbraco media item has no file");
        }

        static void StartKraking(object reKrak)
        {

            foreach (Media imRoot in Media.GetRootMedias())
                processChildren(imRoot, (bool)reKrak);
        }

        static void processChildren(Media im, bool reKrak)
        {
            if (im != null)
            {
                var status = GetKrakStatus(im);
                if (status == EnmIsKrakable.Krakable || (reKrak && status == EnmIsKrakable.Kraked))
                    try
                    {
                        // Ga het bestanc compressen. Niet asynchroon wachten, we zitten namelijk al in een apparte thread dus de gebruiker heeft hier toch geen last van
                        // en er is dan een kleinere kans dat het verkeerd gaat (anders moet het namelijk weer via de API van de website!
                        var result = Compress(im, true);
                        if (result != null)
                            result.Save(Configuration.Settings.KeepOriginal);
                    }
                    catch (KrakenException kex)
                    {
                        switch (kex.Status)
                        {
                            // In het geval van deze foutmeldingen kunnen we niet verder gaan
                            case enmStatus.BadRequest:
                            case enmStatus.Unauthorized:
                            case enmStatus.Forbidden:
                            case enmStatus.RequestLimitReached:
                            case enmStatus.UnexpectedError:
                                return;
                            // En in de onderstaande gevallen mogen we gewoon door gaan
                            case enmStatus.Ok:
                            case enmStatus.FileTooLarge:
                            case enmStatus.UnsupportedMediaType:
                            case enmStatus.UnprocessableEntity:
                            default:
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        // gg
                    }

                foreach (Media imChild in im.Children)
                    processChildren(imChild, reKrak);
            }
        }

        static void installUmbracoSpecifics()
        {
            addKrakenToUmbracoMediaTypes();
            addRESTsettings();
        }

        static void addKrakenToUmbracoMediaTypes()
        {
            try
            {
                var mt = umbraco.cms.businesslogic.media.MediaType.GetByAlias(Constants.DefaultUmbracoMediaType);
                // Haal de tab op (indien aanwezig)
                var tab = mt.getVirtualTabs.FirstOrDefault(x => x.Caption == Constants.UmbracoMediaTabnameKraken);
                // Bepaal het tab id
                int tabId;
                if (tab == null)
                {
                    // Niet gevonden, maak maar een nieuwe aan
                    tabId = mt.AddVirtualTab(Constants.UmbracoMediaTabnameKraken);
                    mt.Save();
                }
                else
                    // Gevonden, haal het nummer op
                    tabId = tab.Id;

                ;
                
                // Ga op zoek naar de datatypes die we (evt) gaan gebruiken
                var dtLabel = umbraco.cms.businesslogic.datatype.DataTypeDefinition.GetDataTypeDefinition(-92);
                // TODO: GETALL WEGHALEN. op GUID zoeken!
                var dtStatus = umbraco.cms.businesslogic.datatype.DataTypeDefinition.GetAll().FirstOrDefault(x => x != null && x.Text == Constants.UmbracoDatatypeTextStatus);

                bool hasChanged = false;
                if (dtLabel != null)
                {
                    // Property: Compressed on (label)
                    hasChanged = addProperty(Constants.UmbracoPropertyAliasCompressionDate, Constants.UmbracoPropertyTextCompressionDate, tabId, mt, dtLabel) || hasChanged;
                    // Property: Original size (label)
                    hasChanged = addProperty(Constants.UmbracoPropertyAliasOriginalSize, Constants.UmbracoPropertyTextOriginalSize, tabId, mt, dtLabel) || hasChanged;
                    // Property: Percent saved (label)
                    hasChanged = addProperty(Constants.UmbracoPropertyAliasSaved, Constants.UmbracoPropertyTextSaved, tabId, mt, dtLabel) || hasChanged;
                }
                if (dtStatus != null)
                    // Property: Status (custom usercontrol)
                    hasChanged = addProperty(Constants.UmbracoPropertyAliasStatus, Constants.UmbracoPropertyTextStatus, tabId, mt, dtStatus) || hasChanged;
                    
                if (hasChanged)
                    mt.Save();
            }
            catch
            {
                // Toon een foutmelding
            }
        }

        /// <summary>
        /// This function adds an REST extension for the PerplexKraken Umbraco installatino.
        /// This allows Kraken to asynchronously Krak images with the PerplexUmbraco system.
        /// </summary>
        static void addRESTsettings()
        {
            try
            {
                // Bepaal de pad naar het config bestand
                string filepath = System.Web.Hosting.HostingEnvironment.MapPath("/config/restExtensions.config");

                // Deze structuur dient aanwezig te zijn in '/config/restExtensions.config' om het te laten werken

                //<RestExtensions>
                //  <ext assembly="PerplexKraken" type="Perplex.KrakenCallback" alias="KrakenCallback">
                //    <permission method="KrakenResults" allowAll="true" />
                //  </ext>
                //</RestExtensions>

                // Laad het config bestand in
                var doc = new XmlDocument();
                doc.Load(filepath);

                var nodeRestExtensions = doc.SelectSingleNode("RestExtensions");
                if (nodeRestExtensions == null)
                    nodeRestExtensions = doc.AppendChild(doc.CreateElement("RestExtensions"));

                var nodeExt = nodeRestExtensions.SelectSingleNode("ext[@assembly='PerplexKraken']");
                if (nodeExt == null)
                {
                    var ext = doc.CreateElement("ext");
                    ext.SetAttribute("assembly", "PerplexKraken");
                    ext.SetAttribute("type", "Perplex.KrakenCallback");
                    ext.SetAttribute("alias", "KrakenCallback");
                    nodeExt = nodeRestExtensions.AppendChild(ext);
                }

                var nodePermission = nodeExt.SelectSingleNode("permission[@method='KrakenResults']");
                if (nodePermission == null)
                {
                    var permission = doc.CreateElement("permission");
                    permission.SetAttribute("method", "KrakenResults");
                    permission.SetAttribute("allowAll", "true");
                    permission.IsEmpty = true;
                    nodeExt.AppendChild(permission);
                    doc.Save(filepath);
                }
            }
            catch
            {
                // Toon een error aan de gbruiker
            }
        }

        static void uninstallUmbracoSpecifics()
        {
            removePerplexKrakenTabsAndProperties();
            removeRESTsettings();
        }

        /// <summary>
        /// This function adds an REST extension for the PerplexKraken Umbraco installatino.
        /// This allows Kraken to asynchronously Krak images with the PerplexUmbraco system.
        /// </summary>
        static void removeRESTsettings()
        {
            try
            {
                // Bepaal de pad naar het config bestand
                string filepath = System.Web.Hosting.HostingEnvironment.MapPath("/config/restExtensions.config");

                // Laad het config bestand in
                var doc = new XmlDocument();
                doc.Load(filepath);
                // Ga op zoek naar de PerplexKraken node
                var n = doc.SelectSingleNode("RestExtensions");
                // gevonden?
                if (n != null)
                {
                    var ext = n.SelectSingleNode("ext[@assembly='PerplexKraken']");
                    // Gevonden?
                    if (ext != null)
                    {
                        // Weggooien en opslaan
                        n.RemoveChild(ext);
                        doc.Save(filepath);
                    }
                }
            }
            catch
            {
                // Toon een foutmelding ofzo
            }
        }

        static void removePerplexKrakenTabsAndProperties()
        {
            try
            {
                // Haal alle media types op
                var mt = umbraco.cms.businesslogic.media.MediaType.GetByAlias(Constants.DefaultUmbracoMediaType);
                if (mt != null && mt.getVirtualTabs != null)
                {
                    // Is er een tab met de naam "compression"?
                    var tab = mt.getVirtualTabs.Where(x => x != null).FirstOrDefault(x => x.Caption == Constants.UmbracoMediaTabnameKraken);
                    if (tab != null && tab.PropertyTypes != null)
                    {
                        bool hasChanged = false;

                        // Loop door alle properties op de tab
                        foreach (var pt in tab.PropertyTypes.ToList())
                            // Indien het om "onze" properties gaat, weggooien
                            if (pt != null && (pt.Alias == Constants.UmbracoPropertyAliasOriginalSize ||
                                                pt.Alias == Constants.UmbracoPropertyAliasCompressionDate ||
                                                pt.Alias == Constants.UmbracoPropertyAliasSaved || 
                                                pt.Alias == Constants.UmbracoPropertyAliasStatus))
                            {
                                pt.delete();
                                hasChanged = true;
                            }

                        if (hasChanged)
                            mt.Save();

                        // Indien er geen properties meer staan op de tab...
                        if (tab.PropertyTypes.Length == 0)
                        {
                            // Gooi de tab dan maar ook weg
                            mt.DeleteVirtualTab(tab.Id);
                            mt.Save();
                        }
                    }
                }
            }
            catch
            {
                // Toon een foutmelding
            }
        }

        static bool addProperty(string alias, string text, int tabId, umbraco.cms.businesslogic.media.MediaType mt, umbraco.cms.businesslogic.datatype.DataTypeDefinition dt)
        {
            // Controleer of hij nog niet bestaat
            var p = mt.PropertyTypes.FirstOrDefault(x => x.Alias == alias);
            if (p == null)
            {
                // Maak de property aan op de media type!
                // mt.AddPropertyType(dt, alias, text); // Je zou denken "waarom roep je dit niet aan". Het antwoord is: Legacy rommel
                var method = mt.GetType().GetMethod("AddPropertyType", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                method.Invoke(mt, new object[] { dt, alias, text });

                p = mt.PropertyTypes.FirstOrDefault(x => x.Alias == alias);
                if (p != null)
                    p.TabId = tabId;
                return true;
            }
            else  // De property bestaat al. Controleer of hij op de juiste tab staat
                if (p.TabId != tabId)
                {
                    // Verplaats de property naar de juiste tab
                    p.TabId = tabId;
                    return true;
                }
            return false;
        }
    }
}
