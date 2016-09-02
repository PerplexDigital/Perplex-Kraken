using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Xml;

namespace Kraken.Controls
{
    public partial class Installer : System.Web.UI.UserControl
    {
        public void Page_Load(object sender, EventArgs e)
        {
            if (!Page.IsPostBack)
                // Begin de installatie!
                Kraken.Install();
        }
    }
}