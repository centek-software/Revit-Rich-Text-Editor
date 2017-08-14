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
    /// <summary>
    /// Handles the format painter command (ie applying formatting from one note to others)
    /// </summary>
    [TransactionAttribute(TransactionMode.Manual)]
    [RegenerationAttribute(RegenerationOption.Manual)]
    class FormatPainterCmd : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiApp = commandData.Application;

            UIDocument uidoc = uiApp.ActiveUIDocument;
            Document doc = uidoc.Document;

            Element e = null;

            ElementId eid = SelectionTools.SelectNote(uiApp, true, "Pick the note to copy formatting from");
            if (eid == null)
                return Result.Cancelled;

            e = doc.GetElement(eid);


            MasterSchema ms = e.GetEntity<MasterSchema>();

            using (TransactionGroup transGroup = new TransactionGroup(uidoc.Document, "Format Painter"))
            {
                transGroup.Start();
                //===

                while (true)
                {
                    ElementId eid2 = SelectionTools.SelectNote(uiApp, true, "Pick a note to apply the formatting to. Press ESC to end.", false);
                    if (eid2 == null)
                        break;

                    Element e2 = doc.GetElement(eid2);
                    MasterSchema ms2 = e2.GetEntity<MasterSchema>();
                    ms2.fontP = ms.fontP;
                    ms2.fontH1 = ms.fontH1;
                    ms2.fontH2 = ms.fontH2;
                    ms2.fontH3 = ms.fontH3;
                    ms2.fontH4 = ms.fontH4;
                    ms2.fontH5 = ms.fontH5;

                    ms2.colHeight = ms.colHeight;
                    ms2.colSeparation = ms.colSeparation;
                    ms2.colWidth = ms.colWidth;

                    using (Transaction tr = new Transaction(uidoc.Document, "Updating text info"))
                    {
                        tr.Start();

                        e2.SetEntity(ms2);

                        tr.Commit();
                    }

                    UpdateHandler uh = new UpdateHandler(e2 as Group, uiApp);
                    uh.updateManyThings();
                    uh.regenerate();
                }

                transGroup.Assimilate();
            }

            return Result.Succeeded;
        }
    }


}
