using System;
using System.Collections.Generic;
using System.Linq;
using umbraco.cms.businesslogic.packager;
using Umbraco.Core.Events;
using Umbraco.Core.Models;
using Umbraco.Core.Services;

namespace Kraken
{
    public class UmbracoEvent : Umbraco.Core.ApplicationEventHandler
    {
        UmbracoEventMediaData _umbracoEventMediaData;

        public UmbracoEvent()
        {
            MediaService.Saving += MediaService_Saving;
            MediaService.Saved += MediaService_Saved;
            InstalledPackage.BeforeDelete += InstalledPackage_BeforeDelete;
        }

        void InstalledPackage_BeforeDelete(InstalledPackage sender, EventArgs e)
        {
            if (sender.Data.Name == Constants.UmbracoPackageName)
            {
                Kraken.Uninstall();
            }
        }

        void MediaService_Saving(IMediaService sender, SaveEventArgs<IMedia> e)
        {
            try
            {
                if (Configuration.Settings.Enabled)
                    // We want to know if the media files are going to change.
                    // To do this, store the state of all the IMedia nodes before they get changed
                    _umbracoEventMediaData = new UmbracoEventMediaData(e.SavedEntities);
            }
            catch
            {
            }
        }

        void MediaService_Saved(IMediaService sender, SaveEventArgs<IMedia> e)
        {
            try
            {
                if (Configuration.Settings.Enabled)
                    foreach (IMedia im in e.SavedEntities)
                        try
                        {
                            // Detect if the media file has been changed in any way
                            bool hasChanged = _umbracoEventMediaData.HasChanged(im);
                            // Get the current krak-status of the media node
                            var status = Kraken.GetKrakStatus(im);
                            // Determine if we are allowed to krak this media node
                            if (status == EnmIsKrakable.Krakable || // It is krakable
                                (status == EnmIsKrakable.Kraked && hasChanged)) // OR it has been kraked before, but a new file has been uploaded
                            {
                                // Compress the image
                                var result = Kraken.Compress(im);
                                // Did the Kraken API yield a valid result?
                                if (result != null && result.success)
                                    // Save the kraked Image to Umbraco
                                    result.Save(im, null, hasChanged);
                            }
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
                                    continue;
                            }
                        }
            }
            catch
            {
                // Als de hel los breekt, ga dan in ieder geval door. Anders verpesten we (mogelijK) de media save event voor de gebruiker
            }
        }

        #region Internal helper class
        /// <summary>
        /// This class is used to detect changes in media images
        /// </summary>
        class UmbracoEventMediaData
        {
            List<UmbracoEventFile> _eventFiles = new List<UmbracoEventFile>();

            public UmbracoEventMediaData(IEnumerable<IMedia> mediaNodes)
            {
                foreach (var im in mediaNodes)
                    _eventFiles.Add(new UmbracoEventFile(im));
            }

            public bool HasChanged(IMedia im)
            {
                if (im != null)
                {
                    // Keep in mind: new media nodes are always 0 in the "saving" event
                    var uef = _eventFiles.FirstOrDefault(x => x.Id == im.Id);
                    if (uef != null)
                    {
                        return uef.File != Kraken.GetImage(im) ||
                               uef.Size != im.GetValue<int>(Constants.UmbracoPropertyAliasSize) ||
                               uef.Height != im.GetValue<int>(Constants.UmbracoPropertyAliasHeight) ||
                               uef.Width != im.GetValue<int>(Constants.UmbracoPropertyAliasWidth);
                    }
                }
                return false;
            }

            class UmbracoEventFile
            {
                public int Id { get; private set; }
                public string File { get; private set; }
                public int Height { get; private set; }
                public int Width { get; private set; }
                public int Size { get; private set; }

                public UmbracoEventFile(IMedia im)
                {
                    Id = im.Id;
                    File = Kraken.GetImage(im); // Filename 
                    Size = im.GetValue<int>(Constants.UmbracoPropertyAliasSize);
                    Height = im.GetValue<int>(Constants.UmbracoPropertyAliasHeight);
                    Width = im.GetValue<int>(Constants.UmbracoPropertyAliasWidth);
                }
            }
        }
        #endregion
    }
}