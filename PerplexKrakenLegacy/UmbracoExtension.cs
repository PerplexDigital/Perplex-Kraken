using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using umbraco.cms.businesslogic.media;

namespace Kraken
{
    internal static class Extensions
    {
        static bool CreateThumbnail(string filepath, int width, string postfix = "_thumb")
        {
            // Geldige parameters?
            if (String.IsNullOrEmpty(filepath) || width < 1)
                return false;
            
            // Relatief pad?
            if (filepath.StartsWith("/")) 
                // Converteren naar absoluut pad
                filepath = System.Web.Hosting.HostingEnvironment.MapPath(filepath);

            // Alleen doorgaan als het plaatje bestaat
            if (!System.IO.File.Exists(filepath))
                return false;

            string ext = ".jpg";
            if (width != 100)
            {
                postfix = postfix + "_" + width.ToString();
                ext = Path.GetExtension(filepath);
            }

            // Bepaal het fysieke pad naar het bestand (gewoon ervoor plakken)
            string targetFilepath = Path.GetDirectoryName(filepath) + '\\' + Path.GetFileNameWithoutExtension(filepath) + postfix + ext; ;

            try
            {
                using (FileStream fs = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.Read))
                using(var imgThumbnail = System.Drawing.Image.FromStream(fs))
	            {
                    if (imgThumbnail == null || imgThumbnail.Width == 0)
                        // Hier kunnen we niks mee
                        return false;
                    else if (imgThumbnail.Width <= width)
                    {
                        //Indien het plaatje kleiner is dan de verwachte thumbnail dan valt er niks te thumbnailen
                        imgThumbnail.Save(targetFilepath);
                        return true;
                    }
                    else
                    {
                        var scale = (float)width / imgThumbnail.Width;
                        int height = (int)(imgThumbnail.Height * scale);
                        if (height == 0) height = 1;

                        using (Bitmap bp = new Bitmap(width, height))
                        using (Graphics g = Graphics.FromImage(bp))
                        {
                            g.SmoothingMode = SmoothingMode.AntiAlias;
                            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            var rect = new Rectangle(0, 0, bp.Width, bp.Height);
                            g.DrawImage(imgThumbnail, rect, 0, 0, imgThumbnail.Width, imgThumbnail.Height, GraphicsUnit.Pixel);

                            if (filepath.ToLower().EndsWith("png"))
                                bp.Save(targetFilepath, ImageFormat.Png);
                            else
                            {
                                ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();
                                ImageCodecInfo codec = null;
                                for (int i = 0; i <= codecs.Length - 1; i++)
                                    if (codecs[i].MimeType.Equals("image/jpeg"))
                                        codec = codecs[i];

                                EncoderParameters ep = new EncoderParameters();
                                ep.Param[0] = new EncoderParameter(Encoder.Quality, 90L);

                                bp.Save(targetFilepath, codec, ep);
                            }
                        }
                        return false;
                    }
	            }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// This method adds a file to any Umbraco media item. Any thumbnails specified on the Umbraco Upload data type will also be created.
        /// </summary>
        /// <param name="m">The Umbraco media item</param>
        /// /// <param name="name">The name of the image</param>
        /// <param name="name">The name of the image</param>
        /// <param name="fileStream">The image itself as a stream</param>
        /// <returns>Success/failure status</returns>
        public static bool AddFile(this Media m, string krakenFile, string name)
        {
            // Parameter controle
            if (m == null || String.IsNullOrEmpty(krakenFile) || String.IsNullOrEmpty(name))
                return false;

            // Controleer of de benodigde property (om het plaatje op te slaan) aanwezig is. Wellicht dubbelop maar toch even ter controle
            var p = m.getProperty(Constants.UmbracoPropertyAliasFile);
            if (p == null) return false;

            bool directoryIsNew = false;
            bool result = false;

            // W: Dit is niet helemaal juist. Het ID hoort uit de databaase te komen en komt nooit overeen met het node ID.
            //    Echter, in Umbraco 4 was hier nog geen handige API functie voor. Dan maar zo. Het KAN dus zijn dat je hier per ongeluk plaatjes gaat toevoegen
            //    aan een map waar al plaatjes in zitten. Opzich geen ramp want de namen van de plaatjes verschillen.
            string relativeFolder = "/media/" + m.Id.ToString() + "/";
            string absoluteFolder = System.Web.Hosting.HostingEnvironment.MapPath(relativeFolder);
            string relativeFile = relativeFolder + name;
            string absoluteFile = absoluteFolder + name;

            try
            {
                if (!System.IO.Directory.Exists(absoluteFolder))
                {
                    System.IO.Directory.CreateDirectory(absoluteFolder);
                    directoryIsNew = true;
                }

                // Download het plaatje naar de media map
                var wc = new System.Net.WebClient();
                wc.DownloadFile(krakenFile, absoluteFile);

                // Valideer het plaatje: Probeer hem als een ECHT plaatje uit te lezen. Als dat lukt, dan geloven we dat het een echt plaatje is
                // Als het dus geen echt plaatje is dan gooit hij hier een exception
                using (var img = System.Drawing.Image.FromFile(absoluteFile))
                    if (img.Size.Height == 0 && img.Size.Width == 0)
                    {
                        // Als het plaatje geen dimensies heeft dan is er iets verkeerd gegaan
                        result = false;
                        return result;
                    }

                p.Value = relativeFile;

                // Bepalen welke thumbnails we gaan genereren
                CreateThumbnail(absoluteFile, 100); // Umbraco thumbnail

                // Bepaal of het om het native uploadfield gaat van Umbraco
                if (p.PropertyType.DataTypeDefinition.DataType.Id == new Guid("5032a6e6-69e3-491d-bb28-cd31cd11086c"))
                {
                    //Get Prevalues by the DataType's Id: property.PropertyType.DataTypeId
                    string thumbnailsizes = umbraco.library.GetPreValueAsString(p.PropertyType.Id);
                    //Additional thumbnails configured as prevalues on the DataType
                    if (thumbnailsizes != null)
                    {
                        char seperator = ';';
                        if (!thumbnailsizes.Contains(';'))
                            seperator = ',';
                        foreach (string thumb in thumbnailsizes.Split(seperator))
                        {
                            int size;
                            if (thumb != "" && int.TryParse(thumb, out size) && size != 100) // 100 hebben we al, dat is de default umbraco thumbnail
                                CreateThumbnail(absoluteFile, size);
                        }
                    }
                }

                // De media node is aangepast maar nog niet opgeslagen! Geef een success status terug
                result = System.IO.File.Exists(absoluteFile); // Als het plaatje bestaat dan is het gelukt
            }
            catch
            {
                result = false;
            }
            finally
            {
                if (!result)
                {
                    // Er is iets verkeerd gegaan.
                    if (directoryIsNew)
                    {
                        // We hebben zojuist een directory aangemaakt. Als hij er nog is, gooi hem dan weg
                        if (Directory.Exists(absoluteFolder))
                            // ROLLBACK: Gooi alle rommel weg
                            Directory.Delete(absoluteFolder);
                    }
                    else if (File.Exists(absoluteFile)) 
                    {
                        // Het mapje bestond al, maar gooi in ieder geval het gedownloade plaatje weg
                        File.Delete(absoluteFile);
                    }
                }
            }
            return result;
        }
    }
}