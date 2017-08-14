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
    class TextNoteResizeCmd : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiApp = commandData.Application;

            UIDocument uidoc = uiApp.ActiveUIDocument;
            Document doc = uidoc.Document;

            ElementId eid = SelectionTools.SelectNote(uiApp);
            if (eid == null)
                return Result.Cancelled;

            Element e = uidoc.Document.GetElement(eid);

            MasterSchema ms = e.GetEntity<MasterSchema>();

            using (TransactionGroup transGroup = new TransactionGroup(uidoc.Document, "Resize rich text note"))
            {
                transGroup.Start();
                //===

                UpdateHandler uh = new UpdateHandler(e as Group, uiApp);
                uh.UpdateSize();
                uh.Regenerate();

                //===
                transGroup.Assimilate();
            }

            return Result.Succeeded;
        }

    }
}
