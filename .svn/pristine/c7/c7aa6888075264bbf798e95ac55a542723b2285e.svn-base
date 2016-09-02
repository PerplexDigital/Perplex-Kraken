using System;
using umbraco.BusinessLogic;
using umbraco.cms.businesslogic.media;
using umbraco.cms.businesslogic.packager;

namespace Kraken
{
    public class UmbracoEvents : ApplicationBase
    {
        public UmbracoEvents()
        {
            Media.AfterSave += Media_AfterSave;
            InstalledPackage.BeforeDelete += InstalledPackage_BeforeDelete;
        }

        void InstalledPackage_BeforeDelete(InstalledPackage sender, EventArgs e)
        {
            if (sender.Data.Name == Constants.UmbracoPackageName)
            {
                Kraken.Uninstall();
            }
        }

        void Media_AfterSave(Media sender, umbraco.cms.businesslogic.SaveEventArgs e)
        {
            try
            {
                if (Configuration.Settings.Enabled)
                    if (Kraken.GetKrakStatus(sender) == EnmIsKrakable.Krakable)
                    {
                        // In elkaar krakken
                        var result = Kraken.Compress(sender);
                        // Goed uitgekrakt?
                        if (result != null && result.success)
                            // Opslaan in Umbraco
                            result.Save(sender);
                    }
            }
            catch
            {
                // Als de hel los breekt, ga dan in ieder geval door. Anders verpesten we (mogelijK) de media save event voor de gebruiker
            }
        }
    }
}
