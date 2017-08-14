//   Revit Rich Text Editor
//   Copyright (C) 2014 Centek Engineering

using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;


namespace CTEK_Rich_Text_Editor
{
    /// <summary>
    /// Handles inserting images
    /// </summary>
    class ImageHandler
    {
        private const float REVIT_DPI = 72.0f;

        /// <summary>
        /// Inserts an image
        /// </summary>
        /// <param name="htmlData">img src in the form data:image/png;base64,iVBORw0KGgoAAAANSUhEU...</param>
        /// <param name="uidoc"></param>
        /// <param name="location">Upper left corner</param>
        /// <param name="view"></param>
        /// <param name="width">Width (pixels)</param>
        /// <param name="height">Height (pixels)</param>
        /// <returns></returns>
        public static Element insertImage(String htmlData, UIDocument uidoc, XYZ location, View view, int width, int height)
        {
            Document doc = uidoc.Document;

            ImageImportOptions iio = new ImageImportOptions();
            iio.Placement = BoxPlacement.Center;
            iio.RefPoint = location;

            string path = saveDataURI(htmlData);        // Save the data image as a file in a temp dir

            Element element;
            doc.Import(path, iio, view, out element);   // Import that image into the doc

            File.Delete(path);                          // Delete that image file
            
            // Sets the width of the image (aspect ratio is seemingly maintained automatically)
            if (height > 0 && width > 0)
                element.LookupParameterCTEK(BuiltInParameter.GENERIC_WIDTH).Set(pixelsToFeet(width, view.Scale));

            // Now move it so it's in the upper left
            element.Location.Move(new XYZ(element.LookupParameterCTEK(BuiltInParameter.GENERIC_WIDTH).AsDouble() / 2.0,
                -element.LookupParameterCTEK(BuiltInParameter.GENERIC_HEIGHT).AsDouble() / 2.0, 0));

            return element;
        }

        public static double pixelsToFeet(int pixels, int scale)
        {
            // The number 864.0 was confirmed experimentally by inserting an image of width 332px into a revit sheet of scale 48.
            // The resulting image width was 18.4444444444', so the conversion factor is (332*48)/18.4444444444 = 864
            // The factor is (72 DPI * 12 in/ 1ft)
            return pixels * scale / (REVIT_DPI * 12.0);
        }

        private static string saveDataURI(string data)
        {
            Match match = Regex.Match(data, @"data:image/(?<type>.+?),(?<data>.+)");

            string path = null;

            if (match.Success)
            {
                string type;
                byte[] binData = getBinData(data, match, out type);

                string filename = Guid.NewGuid() + "." + type;
                path = Path.Combine(Path.GetTempPath(), filename);

                File.WriteAllBytes(path, binData);
            }
            else
            {
                // TODO: THIS DOES NOT WORK
                /*string filename = Guid.NewGuid() + "." + Path.GetExtension(data);
                path = Path.Combine(Path.GetTempPath(), filename);

                TaskDialog.Show("Rich Text Editor", Path.GetTempPath());

                WebClient webClient = new WebClient();
                webClient.DownloadFile(data, path);*/
            }

            return path;
        }

        private static byte[] getBinData(string data, Match match, out string type)
        {
            string base64Data = match.Groups["data"].Value;
            type = match.Groups["type"].Value;
            string[] parts = type.Split(';');
            if (parts.Length > 1)
                type = parts[0];

            return Convert.FromBase64String(base64Data);
        }

        public static Image getBitmap(string htmlData)
        {
            Match match = Regex.Match(htmlData, @"data:image/(?<type>.+?),(?<data>.+)");

            string kapusta;
            using (var ms = new MemoryStream(getBinData(htmlData, match, out kapusta)))
            {
                return Image.FromStream(ms);
            }
        }
    }
}
