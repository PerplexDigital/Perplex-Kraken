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
            var s = global::Kraken.Configuration.Settings;
            if (String.IsNullOrEmpty(s.ApiKey) || String.IsNullOrEmpty(s.ApiSecret))
                return EnmIsKrakable.MissingCredentials;

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
            p = im.getProperty(Constants.UmbracoPropertyAliasStatus);
            if (p != null && p.Value != null && !String.IsNullOrEmpty(p.Value as String))
            {
                if (p.Value as String == EnmKrakStatus.Original.ToString())
                    return EnmIsKrakable.Original;
                else if (p.Value as String == EnmKrakStatus.Compressed.ToString())
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
        /// <param name="imKrakTarget">Target media node</param>
        /// <param name="keepOriginal">Save the original image in Umbraco? Pass NULL to use global settings</param>
        /// <param name="hasChanged">Has a new image been selected for the media node just now?</param>
        /// <returns>Success status</returns>
        internal bool Save(Media mKrakTarget, bool? keepOriginal = null, bool hasChanged = false)
        {
            umbraco.cms.businesslogic.property.Property p;
            // Validate parameters
            var status = GetKrakStatus(mKrakTarget);
            if (status == EnmIsKrakable.Unkrakable || status == EnmIsKrakable.Original || String.IsNullOrEmpty(kraked_url))
                // This image is unkrakable, do not proceed
                return false;

            // Determine the path and the name of the image
            var relativeFilepath = mKrakTarget.getProperty(Constants.UmbracoPropertyAliasFile).Value.ToString();
            var relativeDirectory = System.IO.Path.GetDirectoryName(relativeFilepath);
            var absoluteDirectory = System.Web.Hosting.HostingEnvironment.MapPath("~" + relativeDirectory);
            string filename = Path.GetFileName(relativeFilepath);
            if (keepOriginal == null)
                keepOriginal = Configuration.Settings.KeepOriginal;

            // Has this media node already been Kraked before?
            int originalSize = 0;
            if (status == EnmIsKrakable.Kraked)
            {
                p = mKrakTarget.getProperty(Constants.UmbracoPropertyAliasOriginalSize);
                if (p != null && p.Value != null)
                    int.TryParse(p.Value.ToString(), out originalSize);
            }
            if (originalSize == 0)
                originalSize = original_size;

            var compressionRate = (((decimal)(originalSize - kraked_size)) / originalSize).ToString("p2");

            // The following might seem redundant, but Umbraco's "SetValue" extension method used below actually does a lot of magic for us.
            // However, Umbraco will also create a new media folder for us to contain the new image which we do NOT want (the url to the image has to remain unchanged).
            // So some extra efforts are required to make sure the compressed image will be switched in the place of the original image.

            var originalUmbracoFilePropertyData = mKrakTarget.getProperty(Constants.UmbracoPropertyAliasFile).Value.ToString(); // Get the original property data
            if (!mKrakTarget.AddFile(kraked_url, filename))
                return false; // Krak failed
            // Extract the absolute directory path
            var newRelativeFilepath = mKrakTarget.getProperty(Constants.UmbracoPropertyAliasFile).Value.ToString(); // Retrieve the relative filepath to the new image location
            var newRelativeDirectory = System.IO.Path.GetDirectoryName(newRelativeFilepath); // Extract the relative directoryname
            var newAbsoluteDirectory = System.Web.Hosting.HostingEnvironment.MapPath("~" + newRelativeDirectory); // Convert to it's absolute variant
            mKrakTarget.getProperty(Constants.UmbracoPropertyAliasFile).Value = originalUmbracoFilePropertyData; // Put the original property data back in place

            // If an "original" media node is already present under the current node, then save our original data to that node.
            // Else we will keep creating new nodes under the current node each time we save, and we never want more then 1 original node!
            var mOriginal = mKrakTarget.Children.FirstOrDefault(x => x.Text == EnmKrakStatus.Original.ToString() && x.getProperty(Constants.UmbracoPropertyAliasStatus) != null && x.getProperty(Constants.UmbracoPropertyAliasStatus).Value as String == "Original");

            // Does the original media node already exist?
            bool originalExists = mOriginal != null;

            // Do we need to keep a backup of the originally kraked image?
            if (keepOriginal.Value)
            {
                if (!originalExists)
                    // No. Simply create a new "Original" media node under the current node, which will be used to store our "backup"
                    mOriginal = Media.MakeNew(EnmKrakStatus.Original.ToString(), MediaType.GetByAlias(mKrakTarget.ContentType.Alias), mKrakTarget.User, mKrakTarget.Id);

                // We are only allowed to MODIFY the ORIGINAL media node if the FILE has CHANGED! If the original file has not been modified, then we are ONLY allowed to create a NEW media node (aka it didn't exist before)
                if (hasChanged || !originalExists)
                {
                    // Copy all properties of the current media node to the original (aka: BACKUP)
                    foreach (var p2 in mOriginal.GenericProperties)
                        p2.Value = mKrakTarget.getProperty(p2.PropertyType.Alias).Value;

                    // The image has been modified during the saving proces before, so correct that by specifying the correct original imag
                    p = mOriginal.getProperty(Constants.UmbracoPropertyAliasFile);
                    if (p != null)
                        // Save the original data, but replace the old relative filepath with the new one
                        p.Value = originalUmbracoFilePropertyData.Replace(relativeFilepath, newRelativeFilepath);

                    // The same for filesize
                    p = mOriginal.getProperty(Constants.UmbracoPropertyAliasSize);
                    if (p != null)
                        p.Value = originalSize;

                    // Set the "status" of the original image to "Original", so we know in the future this is the original image
                    p = mOriginal.getProperty(Constants.UmbracoPropertyAliasStatus);
                    if (p != null)
                        p.Value = EnmKrakStatus.Original.ToString();

                    // Save the original node. It will be placed directly underneath the current media node
                    mOriginal.Save();

                    // Now swap the folders so everything is correct again
                    string tmpFolder = absoluteDirectory + "_tmp";
                    System.IO.Directory.Move(absoluteDirectory, tmpFolder);
                    System.IO.Directory.Move(newAbsoluteDirectory, absoluteDirectory);
                    System.IO.Directory.Move(tmpFolder, newAbsoluteDirectory);
                }
                else
                {
                    // Leave the original alone! So just replace the target folder with the compressed version
                    if (System.IO.Directory.Exists(absoluteDirectory))
                        System.IO.Directory.Delete(absoluteDirectory, true);
                    System.IO.Directory.Move(newAbsoluteDirectory, absoluteDirectory);
                }
            }
            else
            {
                if (originalExists)
                {
                    var originalFilePath = mOriginal.getProperty(Constants.UmbracoPropertyAliasFile).Value.ToString();
                    var originalRelativeDirectory = System.IO.Path.GetDirectoryName(originalFilePath);
                    var originalAbsoluteDirectory = System.Web.Hosting.HostingEnvironment.MapPath("~" + originalRelativeDirectory);
                    mOriginal.delete(true);
                    if (System.IO.Directory.Exists(originalAbsoluteDirectory))
                        System.IO.Directory.Delete(originalAbsoluteDirectory, true);
                }
                if (System.IO.Directory.Exists(absoluteDirectory))
                    System.IO.Directory.Delete(absoluteDirectory, true);
                System.IO.Directory.Move(newAbsoluteDirectory, absoluteDirectory);
            }


            // Show the original size
            p = mKrakTarget.getProperty(Constants.UmbracoPropertyAliasOriginalSize);
            if (p != null)
                p.Value = originalSize;

            // Show the kraked status
            p = mKrakTarget.getProperty(Constants.UmbracoPropertyAliasStatus);
            if (p != null)
                p.Value = EnmKrakStatus.Compressed.ToString();

            // Show the kraked date
            p = mKrakTarget.getProperty(Constants.UmbracoPropertyAliasCompressionDate);
            if (p != null)
                p.Value = DateTime.Now.ToString();

            // Show how many bytes we by kraking the image
            p = mKrakTarget.getProperty(Constants.UmbracoPropertyAliasSaved);
            if (p != null)
                p.Value = compressionRate;

            // Save the newly (kraked) media item
            mKrakTarget.Save();

            // Clean up the cache
            HttpRuntime.Cache.Remove("kraken_" + id);
            HttpRuntime.Cache.Remove("kraken_" + id + "_user");

            // W 8-1-2016: Obsolete as the media URL should never change in the first place
            // Refresh the Umbraco Media cache (else you might end up getting the old media node URL when fetching the filename)
            //Umbraco.Web.Cache.DistributedCache.Instance.Refresh(new Guid(Umbraco.Web.Cache.DistributedCache.MediaCacheRefresherId), imKrakTarget.Id);

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
                Kraken result = null;

                try
                {
                    result = Compress(img, wait); // UPLOAD
                }
                catch (KrakenException)
                {
                    try
                    {
                        string imageUrl = HttpContext.Current.Request.Url.GetLeftPart(UriPartial.Authority) + img;
                        Uri uri;
                        if (Uri.TryCreate(imageUrl, UriKind.Absolute, out uri))
                            result = Compress(uri, wait); // URI
                    }
                    catch (KrakenException)
                    {

                    }
                }
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
                    catch (System.Threading.ThreadAbortException taex)
                    {
                        // Sometimes Umbraco attempts to abort a thread after a media item has been saved (possibly to redirect the user to the media node).
                        // Cancel and proceed (really dirty code)
                        System.Threading.Thread.ResetAbort();
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
