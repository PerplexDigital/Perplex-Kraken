using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace Kraken.Controls
{
    public partial class Overview : System.Web.UI.UserControl
    {
        public void Page_Load(object sender, EventArgs e)
        {
            if (!Page.IsPostBack)
            {
                var settings = Configuration.Settings;
                txtApiKey.Text = settings.ApiKey;
                txtApiSecret.Text = settings.ApiSecret;
                cbHideKeys.Checked = settings.HideKeys;
                cbWait.Checked = settings.Wait;
                cbEnabled.Checked = settings.Enabled;
                cbKeepOriginal.Checked = settings.KeepOriginal;
                rblLossy.SelectedIndex = settings.Lossy ? 0 : 1;
            }
        }

        public void Page_PreRender(object sender, EventArgs e)
        {
            phKeys.Visible = !cbHideKeys.Checked;
        }

        public void lbSave_click(object sender, EventArgs e)
        {
            var settings = Configuration.Settings;
            if (phKeys.Visible)
            {
                settings.ApiKey = txtApiKey.Text;
                settings.ApiSecret = txtApiSecret.Text;
            }
            settings.HideKeys = cbHideKeys.Checked;
            settings.Wait = cbWait.Checked;
            settings.Enabled = cbEnabled.Checked;
            settings.KeepOriginal = cbKeepOriginal.Checked;
            settings.Lossy = rblLossy.SelectedIndex == 0;
            settings.Save();
            lblSaved.Visible = true;
        }

        public void lbKrakEverything_Click(object sender, EventArgs e)
        {
            // Eerst even alles opslaan voor het geval er iets is aangepast
            lbSave_click(null, null);
            Kraken.KrakEverything();
        }

        public void lbReKrakEverything_Click(object sender, EventArgs e)
        {
            // Eerst even alles opslaan voor het geval er iets is aangepast
            lbSave_click(null, null);
            Kraken.KrakEverything(true);
        }
    }
}
