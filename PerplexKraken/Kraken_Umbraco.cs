using System;
using System.IO;
using System.Web;
using Umbraco.Core.Models;
using System.Linq;
using Umbraco.Core.Services;
using Umbraco.Core;

namespace Kraken
{
    // All Umbraco related code here
    public partial class Kraken
    {
        internal const string umbracoCallbackUrl = "/Base/PerplexKraken/KrakenResults"; // For now we're using the Umbraco BASE for better backwards (and forwards) compatibility. Old API url ==> "/umbraco/perplex/KrakenCallbackApi/KrakenResults";

        /// <summary>
        /// The Umbraco Media service to fetch media content
        /// </summary>
        static IMediaService ms
        {
            get
            {
                return ApplicationContext.Current.Services.MediaService;
            }
        }

        public Kraken()
        {
            if (Umbraco.Web.UmbracoContext.Current != null && Umbraco.Web.UmbracoContext.Current.UmbracoUser != null)
                UmbracoUserId = Umbraco.Web.UmbracoContext.Current.UmbracoUser.Id;
            else
                UmbracoUserId = 0;
        }

        
        /// <summary>
        /// Save the kraked image to the associated Umbraco Media node (if applicable).
        /// </summary>
        /// <param name="keepOriginal"></param>
        /// <returns>Success status</returns>
        internal bool Save(bool? keepOriginal = null)
        {
            if (MediaId >= 1000)
                return Save(ms.GetById(MediaId), keepOriginal);
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
                return Save(ms.GetById(umbracoMediaId), keepOriginal);
            else
                throw new ArgumentException("Invalid Umbraco media id", "mediaId");
        }

        internal static EnmIsKrakable GetKrakStatus(int umbracoMediaId)
        {
            return GetKrakStatus(ms.GetById(umbracoMediaId));
        }

        internal static EnmIsKrakable GetKrakStatus(IMedia im)
        {
            // Je mag een Umbraco Media node krakken onder de volgende voorwaarden:
            var s = global::Kraken.Configuration.Settings;
            if (String.IsNullOrEmpty(s.ApiKey) || String.IsNullOrEmpty(s.ApiSecret))
                return EnmIsKrakable.MissingCredentials;

            // 1: Er zit wat in :)
            if (im == null || im.Id == 0)
                return EnmIsKrakable.Unkrakable;

            // 2: Het status veld dient aanwezig te zijn
            if (!im.HasProperty(Constants.UmbracoPropertyAliasStatus))
                return EnmIsKrakable.Unkrakable;

            // 3: Als hij geen afbeelding bevat, dan mag het niet
            if (!im.HasProperty(Constants.UmbracoPropertyAliasFile))
                return EnmIsKrakable.Unkrakable;

            // 4: Controleer de afbeelding. Als het een geldig bestandsformaat is dan is het OK.
            string img = GetImage(im);
            if (img == null ||
                (!img.EndsWith("jpg", StringComparison.OrdinalIgnoreCase) &&
                !img.EndsWith("jpeg", StringComparison.OrdinalIgnoreCase) &&
                !img.EndsWith("png", StringComparison.OrdinalIgnoreCase) &&
                !img.EndsWith("gif", StringComparison.OrdinalIgnoreCase)))
                return EnmIsKrakable.Unkrakable;

            // W 18-9-2015: Vanaf nu mag je afbeeldingen re-kraken
            // 5: En dit status veld dient ook nog eens LEEG te zijn (want als je al een status hebt dan is er dus al iets gebeurd)
            //    Status betekend dus echt EINDSTATUS
            var status = im.GetValue<String>(Constants.UmbracoPropertyAliasStatus);
            if (!String.IsNullOrEmpty(status))
            {
                if (status == EnmKrakStatus.Original.ToString())
                    return EnmIsKrakable.Original;
                else if (status == EnmKrakStatus.Compressed.ToString())
                    return EnmIsKrakable.Kraked;
                else
                    // Geen idee wat het is dus niet kraken!
                    return EnmIsKrakable.Unkrakable;
            }
            else
                // Yaay, deze media item mag gekrakt worden
                return EnmIsKrakable.Krakable;
        }

        /// <summary>
        /// Save the kraked image to a specific Umbraco Media node
        /// </summary>
        /// <param name="imKrakTarget">Target media node</param>
        /// <param name="keepOriginal">Save the original image in Umbraco? Pass NULL to use global settings</param>
        /// <param name="hasChanged">Has a new image been selected for the media node just now?</param>
        /// <returns>Success status</returns>
        internal bool Save(IMedia imKrakTarget, bool? keepOriginal = null, bool hasChanged = false)
        {
            // Validate parameters
            var status = GetKrakStatus(imKrakTarget);
            if (status == EnmIsKrakable.Unkrakable || status == EnmIsKrakable.Original || String.IsNullOrEmpty(kraked_url))
                // This image is unkrakable, do not proceed
                return false;

            // Determine the path and the name of the image
            var relativeFilepath = GetImage(imKrakTarget);
            var relativeDirectory = System.IO.Path.GetDirectoryName(relativeFilepath);
            var absoluteDirectory = System.Web.Hosting.HostingEnvironment.MapPath("~" + relativeDirectory);
            string filename = Path.GetFileName(relativeFilepath);
            if (keepOriginal == null)
                keepOriginal = Configuration.Settings.KeepOriginal;

            // Has this media node already been Kraked before?
            int originalSize = 0;
            if (status == EnmIsKrakable.Kraked)
            {
                var propertyValue = imKrakTarget.GetValue(Constants.UmbracoPropertyAliasOriginalSize);
                if (propertyValue is int)
                    originalSize = (int)propertyValue;
                else
                    int.TryParse(propertyValue as String, out originalSize);
            }
            if (originalSize == 0)
                originalSize = original_size;
            
            var compressionRate = (((decimal)(originalSize - kraked_size)) / originalSize).ToString("p2");
            
            // Download the image from kraken.io
            var image = Helper.DownloadFile(kraked_url);

            // The following might seem redundant, but Umbraco's "SetValue" extension method used below actually does a lot of magic for us.
            // Umbraco will create a new media folder us to store the new image. However the Original URL has to remain unchanged.
            // After we have called SetValue, we will read the location of the new image, and then restore the imKrakTarget to the original file.

            var originalUmbracoFilePropertyData = imKrakTarget.GetValue<String>(Constants.UmbracoPropertyAliasFile); // Get the original property data
            
            imKrakTarget.SetValue(Constants.UmbracoPropertyAliasFile, filename, image); // Save the image and let Umbraco do some magic here

            // Extract the absolute directory path
            var newRelativeFilepath = GetImage(imKrakTarget); // Retrieve the relative filepath to the new image location
            var newRelativeDirectory = System.IO.Path.GetDirectoryName(newRelativeFilepath); // Extract the relative directoryname
            var newAbsoluteDirectory = System.Web.Hosting.HostingEnvironment.MapPath("~" + newRelativeDirectory); // Convert to it's absolute variant

            // Revert changes so the media node remains unchanged (remember, we don't want the file location to change!). So we will manually swap the underlying file in a moment.
            imKrakTarget.SetValue(Constants.UmbracoPropertyAliasFile, originalUmbracoFilePropertyData);

            // If an "original" media node is already present under the current node, then save our original data to that node.
            // Else we will keep creating new nodes under the current node each time we save, and we never want more then 1 original node!
            IMedia imOriginal = imKrakTarget.Children().FirstOrDefault(x => x.Name == EnmKrakStatus.Original.ToString() && x.HasProperty(Constants.UmbracoPropertyAliasStatus) && x.GetValue<String>(Constants.UmbracoPropertyAliasStatus) == "Original");

            // Does the original media node already exist?
            bool originalExists = imOriginal != null;

            // Do we need to keep a backup of the originally kraked image?
            if (keepOriginal.Value)
            {                
                if (!originalExists)
                    // No. Simply create a new "Original" media node under the current node, which will be used to store our "backup"
                    imOriginal = ms.CreateMedia(EnmKrakStatus.Original.ToString(), imKrakTarget.Id, imKrakTarget.ContentType.Alias, imKrakTarget.CreatorId);

                // We are only allowed to MODIFY the ORIGINAL media node if the FILE has CHANGED! If the original file has not been modified, then we are ONLY allowed to create a NEW media node (aka it didn't exist before)
                if (hasChanged || !originalExists)
                {
                    // Copy all properties of the current media node to the original (aka: BACKUP)
                    foreach (var p in imOriginal.Properties)
                        p.Value = imKrakTarget.GetValue(p.Alias);

                    // The image has been modified during the saving proces before, so correct that by specifying the correct original imag
                    if (imOriginal.HasProperty(Constants.UmbracoPropertyAliasFile))
                        imOriginal.SetValue(Constants.UmbracoPropertyAliasFile, 
                            // Save the original data, but replace the old relative filepath with the new one
                            originalUmbracoFilePropertyData.Replace(relativeFilepath, newRelativeFilepath));

                    // The same for filesize
                    if (imOriginal.HasProperty(Constants.UmbracoPropertyAliasSize))
                        imOriginal.SetValue(Constants.UmbracoPropertyAliasSize, originalSize);

                    // Set the "status" of the original image to "Original", so we know in the future this is the original image
                    if (imOriginal.HasProperty(Constants.UmbracoPropertyAliasStatus))
                        imOriginal.SetValue(Constants.UmbracoPropertyAliasStatus, EnmKrakStatus.Original.ToString());

                    // Save the original node. It will be placed directly underneath the current media node
                    ms.Save(imOriginal, UmbracoUserId, false);

                    // Now swap the folders so everything is correct again
                    string tmpFolder = absoluteDirectory + "_tmp";
                    System.IO.Directory.Move(absoluteDirectory, tmpFolder);
                    System.IO.Directory.Move(newAbsoluteDirectory, absoluteDirectory);
                    System.IO.Directory.Move(tmpFolder, newAbsoluteDirectory);
                } else {
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
                    var originalFilePath = GetImage(imOriginal);
                    var originalRelativeDirectory = System.IO.Path.GetDirectoryName(originalFilePath);
                    var originalAbsoluteDirectory = System.Web.Hosting.HostingEnvironment.MapPath("~" + originalRelativeDirectory);
                    ms.Delete(imOriginal);
                    if (System.IO.Directory.Exists(originalAbsoluteDirectory))
                        System.IO.Directory.Delete(originalAbsoluteDirectory, true);
                }
                if (System.IO.Directory.Exists(absoluteDirectory))
                    System.IO.Directory.Delete(absoluteDirectory, true);
                System.IO.Directory.Move(newAbsoluteDirectory, absoluteDirectory);
            }

            // Show the original size
            if (imKrakTarget.HasProperty(Constants.UmbracoPropertyAliasOriginalSize))
                imKrakTarget.SetValue(Constants.UmbracoPropertyAliasOriginalSize, originalSize);

            // Show the kraked status
            if (imKrakTarget.HasProperty(Constants.UmbracoPropertyAliasStatus))
                imKrakTarget.SetValue(Constants.UmbracoPropertyAliasStatus, EnmKrakStatus.Compressed.ToString());

            // Show the kraked date
            if (imKrakTarget.HasProperty(Constants.UmbracoPropertyAliasCompressionDate))
                imKrakTarget.SetValue(Constants.UmbracoPropertyAliasCompressionDate, DateTime.Now.ToString());

            // Show how many bytes we by kraking the image
            if (imKrakTarget.HasProperty(Constants.UmbracoPropertyAliasSaved))
                imKrakTarget.SetValue(Constants.UmbracoPropertyAliasSaved, compressionRate);

            // Save the newly (kraked) media item
            ms.Save(imKrakTarget, UmbracoUserId, false);

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
                return Compress(ms.GetById(umbracoMediaId), wait);
            else
                throw new ArgumentException("Invalid Umbraco media id", "mediaId");
        }

        internal static Kraken Compress(IMedia umbracoMedia, bool? wait = null)
        {
            if (umbracoMedia == null || umbracoMedia.Id == 0)
                throw new ArgumentException("Invalid Umbraco Media node", "umbracoMedia");

            if (umbracoMedia.HasProperty(Constants.UmbracoPropertyAliasFile))
            {
                string img = GetImage(umbracoMedia);
                Kraken result = null;

                try
                {
                    result = Compress(img, wait); // UPLOAD
                }
                catch (KrakenException kex)
                {
                    // Does the exception indicate that recovery is possible?
                    if (kex.Status == enmStatus.FileTooLarge ||
                        kex.Status == enmStatus.BadRequest ||
                        kex.Status == enmStatus.UnexpectedError ||
                        kex.Status == enmStatus.UnprocessableEntity ||
                        kex.Status == enmStatus.UnsupportedMediaType)
                    {
                        // Try to have the image optimised by passing the URL directly to kraken.io
                        string imageUrl = HttpContext.Current.Request.Url.GetLeftPart(UriPartial.Authority) + img;
                        Uri uri;
                        if (Uri.TryCreate(imageUrl, UriKind.Absolute, out uri))
                            result = Compress(uri, wait); // URI
                    }
                    else
                        throw; // Recovery not possible
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
            if (Umbraco.Web.UmbracoContext.Current == null)
                try
                {
                    var request  = new HttpRequest(null, "http://localhost/umbraco/", null);
                    var response = new HttpResponse(null);
                    var context = new HttpContext(request,response);
                    var contextWrapper = new HttpContextWrapper(context);
                    HttpContext.Current = context;
                    Umbraco.Web.UmbracoContext.EnsureContext(contextWrapper, Umbraco.Core.ApplicationContext.Current);
                }
                catch
                {
                    // This is probably going to mean we might run into trouble when saving our images to Umbrco
                }
            foreach (IMedia imRoot in ms.GetRootMedia())
                processChildren(imRoot, (bool)reKrak);
        }

        static void processChildren(IMedia im, bool reKrak)
        {
            if (im != null)
            {
                var status = GetKrakStatus(im);
                if (status == EnmIsKrakable.Krakable || (reKrak && status == EnmIsKrakable.Kraked))
                    try
                    {
                        // Ga het bestand compressen. Niet asynchroon wachten, we zitten namelijk al in een apparte thread dus de gebruiker heeft hier toch geen last van
                        // en er is dan een kleinere kans dat het verkeerd gaat (anders moet het namelijk weer via de API van de website!)
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
                foreach (IMedia imChild in im.Children())
                    processChildren(imChild, reKrak);
            }
        }

        static void installUmbracoSpecifics()
        {
            // We gaan het media type toevoegen aan de default: We voegen een kraken tab toe, met wat properties
            try
            {
                bool hasChanged = false;
                var mt = Umbraco.Core.ApplicationContext.Current.Services.ContentTypeService.GetMediaType(Constants.DefaultUmbracoMediaType);

                if (mt != null)
                {
                    // Bestaat de tab met de naam al?
                    if (!mt.PropertyGroups.Any(x => x.Name == Constants.UmbracoMediaTabnameKraken))
                    {
                        // Niet gevonden, maak maar een nieuwe aan
                        mt.AddPropertyGroup(Constants.UmbracoMediaTabnameKraken);
                        hasChanged = true;
                    }

                    
                    // Haal het label datatype op
                    var dtdLabel = Umbraco.Core.ApplicationContext.Current.Services.DataTypeService.GetDataTypeDefinitionById(-92);

                    // Haal het status datatype op
                    var dtdStatus = Umbraco.Core.ApplicationContext.Current.Services.DataTypeService.GetDataTypeDefinitionById(new Guid(Constants.UmbracoDatatypeGuidStatus));

                    if (Helper.UmbracoVersion.Major >= 7)
                    {
                        // We zitten in Umbraco 7+. Pas het datatype definition aan om de angular variant te gebruiken ipv de usercontrol
                        dtdStatus.PropertyEditorAlias = "status";
                        ApplicationContext.Current.Services.DataTypeService.Save(dtdStatus);
                    }

                    // Property: Status
                    if (dtdStatus!= null)
                        hasChanged = addProperty(Constants.UmbracoPropertyAliasStatus, Constants.UmbracoPropertyTextStatus, mt, dtdStatus) || hasChanged;
                        
                    // Property: Compressed on (label)
                    hasChanged = addProperty(Constants.UmbracoPropertyAliasCompressionDate, Constants.UmbracoPropertyTextCompressionDate, mt, dtdLabel) || hasChanged;
                    // Property: Original size (label)
                    hasChanged = addProperty(Constants.UmbracoPropertyAliasOriginalSize, Constants.UmbracoPropertyTextOriginalSize, mt, dtdLabel) || hasChanged;
                    // Property: Percent saved (label)
                    hasChanged = addProperty(Constants.UmbracoPropertyAliasSaved, Constants.UmbracoPropertyTextSaved, mt, dtdLabel) || hasChanged;

                    if (hasChanged)
                        Umbraco.Core.ApplicationContext.Current.Services.ContentTypeService.Save(mt);
                }
            }
            catch
            {
                // Toon een foutmelding
            }
        }

        static void uninstallUmbracoSpecifics()
        {
            removePerplexKrakenTabsAndProperties();
        }

        static void removePerplexKrakenTabsAndProperties()
        {
            try
            {
                // Haal alle media types op
                var mt = Umbraco.Core.ApplicationContext.Current.Services.ContentTypeService.GetMediaType(Constants.DefaultUmbracoMediaType);
                if (mt != null && mt.PropertyGroups != null)
                {
                    bool hasChanged = false;
                    // Haal al onze properties weg
                    if (mt.PropertyTypeExists(Constants.UmbracoPropertyAliasCompressionDate)) { mt.RemovePropertyType(Constants.UmbracoPropertyAliasCompressionDate); hasChanged = true; }
                    if (mt.PropertyTypeExists(Constants.UmbracoPropertyAliasOriginalSize)) { mt.RemovePropertyType(Constants.UmbracoPropertyAliasOriginalSize); hasChanged = true; }
                    if (mt.PropertyTypeExists(Constants.UmbracoPropertyAliasSaved)) { mt.RemovePropertyType(Constants.UmbracoPropertyAliasSaved); hasChanged = true; }
                    if (mt.PropertyTypeExists(Constants.UmbracoPropertyAliasStatus)) { mt.RemovePropertyType(Constants.UmbracoPropertyAliasStatus); hasChanged = true; }
                            
                    // Is er iets aangepast?
                    if (hasChanged) 
                        // Update dan de media type
                        Umbraco.Core.ApplicationContext.Current.Services.ContentTypeService.Save(mt);
                            
                    // Bepaal of onze tab nog aanwezig is
                    var tab = mt.PropertyGroups.FirstOrDefault(t => t.Name == Constants.UmbracoMediaTabnameKraken);
                    // Indien er geen properties meer inzitten dan mag hij weggegooid worden
                    if (tab != null && tab.PropertyTypes.Count == 0)
                    {
                        mt.RemovePropertyGroup(tab.Name);
                        // Helaas mogen we niet in 1x alles saven: als je de tab weghaald samen met de properties dan blijven alle properties staan
                        Umbraco.Core.ApplicationContext.Current.Services.ContentTypeService.Save(mt);
                    }
                }
            }
            catch
            {
                // Toon een foutmelding
            }
        }

        static bool addProperty(string alias, string text, Umbraco.Core.Models.IMediaType mt, Umbraco.Core.Models.IDataTypeDefinition dt)
        {
            // Controleer of hij nog niet bestaat
            if (!mt.PropertyTypeExists(alias))
            {
                // De property bestaat nog niet. Ga op zoek naar het benodigde data type

                // Gevonden?
                if (dt != null)
                {
                    // Maak de property aan op de media type!
                    var pt = new PropertyType(dt);
                    pt.Name = text;
                    pt.Alias = alias;
                    pt.SortOrder = 0;
                    mt.AddPropertyType(pt, Constants.UmbracoMediaTabnameKraken);
                    return true;
                }
            }
            else // Onze property bestaat al. Controleer of hij op de juiste tab staat
                if (!mt.PropertyGroups.FirstOrDefault(x => x.Name == Constants.UmbracoMediaTabnameKraken).PropertyTypes.Any(x => x.Alias == alias))
                {
                    // Verplaats de property naar de juiste tab
                    mt.MovePropertyType(alias, Constants.UmbracoMediaTabnameKraken);
                    return true;
                }
            return false;
        }

        internal static string GetImage(Umbraco.Core.Models.IMedia im)
        {
            // Haal de data uit de property
            string data = im.GetValue<string>(Constants.UmbracoPropertyAliasFile);
            
            // Als er niks in zit, zijn we gelijk klaar
            if (String.IsNullOrEmpty(data)) 
                return null;
            else if (data[0] == '/')
                // (waarschijnlijk) relatieve bestandsnaam
                return data;
            else
            {
                // Controleer het data type zodat we weet om wat voor een data het gaat.
                var p = im.PropertyTypes.FirstOrDefault(x => x.Alias == Constants.UmbracoPropertyAliasFile);

                // Is het een Image Cropper? En zit er wat JSON data in ofzo?
                if (p.DataTypeId.ToString() == Constants.UmbracoDataTypeGuidImageCropper ||
                    p.DataTypeId.ToString() == Constants.UmbracoDataTypeGuidImageCropper_new)
                    try
                    {
                        // Sometimes Umbraco stores JSON with unquoted keys and/or single quote values. The built in .NET json serializer can only handle double quotes.
                        // So transform the JSON string to use double quotes on all keys and string values.
                        data = Helper.AddJsonKeyQuotes(data);
                        var cropper = Helper.FromJSON<CropperData>(data);
                        if (cropper != null)
                            return cropper.src;
                        else
                            return null;
                    }
                    catch { }
            }
            return null;
        }

        [System.Runtime.Serialization.DataContract]
        private class CropperData
        {
            [System.Runtime.Serialization.DataMember]
            public string src { get; set; }
        }
    }
}