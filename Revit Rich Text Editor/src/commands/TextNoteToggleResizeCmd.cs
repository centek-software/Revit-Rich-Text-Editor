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
    class TextNoteToggleResizeCmd : IExternalCommand
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
            if (ms == null)
            {
                TaskDialog.Show("Rich Text Editor", "Sorry, we can only edit Rich Text Notes.");
                return Result.Succeeded;
            }
            else
            {
                if (!ms.resizeBoxActivated)
                {
                    // Calc origin
                    TextNote sampleElement = uidoc.Document.GetElement(ms.sampleElement) as TextNote;
                    if (sampleElement == null)
                    {
                        TaskDialog.Show("Rich Text Editor", "ERROR: CORRUPT RICH TEXT NOTE");
                        return Result.Failed;
                    }
                    XYZ position = sampleElement.Coord;
                    XYZ relPosition = ms.sampleElementDeltaOrigin;
                    position = position.Subtract(relPosition);

                    // ==
                    View view = doc.GetElement(e.OwnerViewId) as View;

                    using (Transaction tr = new Transaction(uidoc.Document, "Drawing note resize lines"))
                    {
                        tr.Start();

                        double lineRetractAmt = ms.colWidth * 0.05;

                        DetailCurve right1 = MakeLine(uiApp, view, position.X + ms.colWidth, position.Y, position.X + ms.colWidth, position.Y - ms.colHeight);
                        DetailCurve right2 = MakeLine(uiApp, view, position.X + ms.colWidth + ms.colSeparation, position.Y, position.X + ms.colWidth + ms.colSeparation, position.Y - ms.colHeight);
                        DetailCurve bottom = MakeLine(uiApp, view, position.X - lineRetractAmt, position.Y - ms.colHeight, position.X + ms.colWidth + ms.colSeparation, position.Y - ms.colHeight);

                        ms.rbRight1 = right1.Id;
                        ms.rbRight2 = right2.Id;
                        ms.rbBottom = bottom.Id;
                        ms.resizeBoxActivated = true;

                        e.SetEntity(ms);

                        tr.Commit();
                    }
                }
                else
                {
                    using (Transaction tr = new Transaction(uidoc.Document, "Removing note resize lines"))
                    {
                        tr.Start();

                        if (doc.GetElement(ms.rbBottom) != null)
                            doc.Delete(ms.rbBottom);

                        if (doc.GetElement(ms.rbRight1) != null)
                            doc.Delete(ms.rbRight1);

                        if (doc.GetElement(ms.rbRight2) != null)
                            doc.Delete(ms.rbRight2);

                        ms.resizeBoxActivated = false;
                        e.SetEntity(ms);

                        tr.Commit();
                    }
                }

                return Result.Succeeded;
            }

        }

        private DetailCurve MakeLine(UIApplication uiApp, View view, double x1, double y1, double x2, double y2)
        {
            XYZ point1 = uiApp.Application.Create.NewXYZ(x1, y1, 0);
            XYZ point2 = uiApp.Application.Create.NewXYZ(x2, y2, 0);

            Document doc = uiApp.ActiveUIDocument.Document;
            //Create line
            Line line = Line.CreateBound(point1, point2);
            DetailCurve detailCurve = doc.Create.NewDetailCurve(view, line);

            return detailCurve;
        }

    }
}
