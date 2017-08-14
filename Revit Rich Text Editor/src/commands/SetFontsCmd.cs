//   Revit Rich Text Editor
//   Copyright (C) 2014 Centek Engineering

using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VCExtensibleStorageExtension;
using VCExtensibleStorageExtension.ElementExtensions;

namespace CTEK_Rich_Text_Editor
{
    [TransactionAttribute(TransactionMode.Manual)]
    [RegenerationAttribute(RegenerationOption.Manual)]
    class SetFontsCmd : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiApp = commandData.Application;

            UIDocument uidoc = uiApp.ActiveUIDocument;

            ICollection<ElementId> collection = SelectionTools.SelectNotes(uiApp);

            if (collection == null)
                return Result.Cancelled;

            using (TransactionGroup transGroup = new TransactionGroup(uidoc.Document, "Set Rich Text Note fonts"))
            {
                transGroup.Start();
                //===

                using (var form = new FontForm(uiApp, collection))
                {
                    form.ShowDialog();
                }

                //===
                transGroup.Assimilate();
            }

            return Result.Succeeded;
        }
    }
}
