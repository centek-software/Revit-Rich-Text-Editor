using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CTEK_Rich_Text_Editor
{
    /**
     * FUTURE CHANGES TO MAKE
     * 
     * When 2016 is the base API:
     * 1) Switch LookupParameterCTEK (from CustomExtensions) to LookupParameter everywhere
     * 2) Switch text note generation to use only the new way (ie no reflection).
     *      See 2e6dff5e-d602-4fe8-a229-7e6bcf78aed0 in ColumnHandler
     * 3) See c887c806-8fd0-45ab-9542-8394ee60cf49 in TextNoteCreatorCmd
     */
    class RevitVersionHandler
    {
        private static int revitVersion = -1;

        public static MethodInfo createTextNote2016 {get; private set;}

        public static bool needsUpdate(string build)
        {
            // https://knowledge.autodesk.com/support/revit-products/troubleshooting/caas/sfdcarticles/sfdcarticles/How-to-tie-the-Build-number-with-the-Revit-update.html
            
            switch (build)
            {
                case "20140223_1515(x64)":   // First Customer Ship
                case "20140322_1515(x64)":   // Update Release 1
                case "20140323_1530(x64)":   // Update Release 2
                //case "20140606_1530(x64)":   // Update Release 3 (this one works)
                    return true;
            }

            return false;
        }

        private static int identifyRevitVersion()
        {
            createTextNote2016 = typeof(TextNote).GetMethod("Create", new[] { typeof(Document), typeof(ElementId), typeof(XYZ), typeof(string), typeof(ElementId) });

            // == Check 2017 ==
            if (typeof(TextNote).GetMethod("GetFormattedText") != null)
                return 2017;

            // == Check 2016 ==
            if (createTextNote2016 != null)
                return 2016;

            // == Check 2015 ==
            if (typeof(UIDocument).GetProperty("ActiveGraphicalView") != null)
                return 2015;

            // == Assume 2014 ==
            return 2014;
        }

        public static int getRevitVersion()
        {
            if (revitVersion < 0)
                revitVersion = identifyRevitVersion();

            return revitVersion;
        }


    }
}
