using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CTEK_Rich_Text_Editor
{
    [TransactionAttribute(TransactionMode.Manual)]
    [RegenerationAttribute(RegenerationOption.Manual)]
    class DebugCmd : IExternalCommand
    {
        public volatile static DebugForm debugForm = null;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            if (debugForm == null || debugForm.IsDisposed)
            {
                debugForm = new DebugForm();
            }

            debugForm.Show();

            return Result.Succeeded;
        }

        internal static void Update(string p)
        {
            if (debugForm != null)
            {
                if (debugForm.IsDisposed)
                    debugForm = null;
                else
                    debugForm.Update(p);
            }

        }
    }
}
