//   Revit Rich Text Editor
//   Copyright (C) 2014 Centek Engineering

using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VCExtensibleStorageExtension;
using VCExtensibleStorageExtension.ElementExtensions;

namespace CTEK_Rich_Text_Editor
{
    /// <summary>
    /// Handles updating stuff
    /// </summary>
    class UpdateHandler
    {
        Group group;
        UIDocument uidoc;

        public UpdateHandler(Group group, UIApplication uiApp)
        {
            this.group = group;

            uidoc = uiApp.ActiveUIDocument;
        }

        /// <summary>
        /// Redraws all the elements in the group
        /// </summary>
        public void regenerate()
        {
            MasterSchema ms = group.GetEntity<MasterSchema>();

            Autodesk.Revit.DB.View view = uidoc.Document.GetElement(group.OwnerViewId) as View;

            FundamentalProps tnt = new FundamentalProps(ms.colHeight, ms.colWidth, ms.colSeparation);

            string html = ms.html;


            ColumnHandler ch = null;
            ch = new ColumnHandler(uidoc, tnt, group.Id, html);

            if (!ActivationHandler.activated)
                html += "<br /><p><em>This note was created with the free version of the Centek Rich Text Editor. To remove this text, purchase the software at software.centekeng.com</em></p>";

            RichTextPlacer rtp = new RichTextPlacer(html, uidoc, tnt, view.Scale, ch, group);

            rtp.DoParse();

            using (Transaction tr = new Transaction(uidoc.Document, "Grouping text nodes together"))
            {
                tr.Start();

                if (!ch.hasElements())      // If there are no elements to group, then don't do anything (so the note isn't destroyed)
                {
                    tr.Commit();
                    return;
                }

                ch.deleteMaster();

                group = ch.groupColumns();
                ms = group.GetEntity<MasterSchema>();

                tr.Commit();
            }

            //using (Transaction tr = new Transaction(uidoc.Document, "Making slaves"))
            //{
            //    tr.Start();
            //    tr.Commit();
            //}
        }

        public void updateHTML(string html)
        {
            MasterSchema ms = group.GetEntity<MasterSchema>();

            ms.html = html;

            using (Transaction tr = new Transaction(uidoc.Document, "Setting RTN HTML"))
            {
                tr.Start();
                group.SetEntity(ms);
                tr.Commit();
            }
        }

        public void updateManyThings()
        {
            updateSize();
        }

        /// <summary>
        /// Updates the size info based on the resize box
        /// </summary>
        public void updateSize()
        {
            MasterSchema ms = group.GetEntity<MasterSchema>();
            Document doc = uidoc.Document;

            if (!ms.resizeBoxActivated)
                return;

            DetailCurve bottom = doc.GetElement(ms.rbBottom) as DetailCurve;
            DetailCurve right1 = doc.GetElement(ms.rbRight1) as DetailCurve;
            DetailCurve right2 = doc.GetElement(ms.rbRight2) as DetailCurve;

            XYZ position = getOrigin();

            if (position == null)
            {
                TaskDialog.Show("Rich Text Editor", "ERROR: CORRUPT RICH TEXT NOTE");
                return;
            }

            if (bottom != null)
            {
                Line bl = (bottom.Location as LocationCurve).Curve as Line;
                double b = Math.Max(bl.GetEndPoint(0).Y, bl.GetEndPoint(1).Y);

                double h = position.Y - b;
                if (h > 0)
                    ms.colHeight = h;
            }

            DetailCurve rightLeft = null;
            DetailCurve rightRight = null;

            if (right1 != null && right2 == null)
                rightLeft = right1;
            else if (right1 == null && right2 != null)
                rightLeft = right2;
            else if (right1 == null && right2 == null)
                rightLeft = null;
            else
            {
                Line r1 = (right1.Location as LocationCurve).Curve as Line;
                Line r2 = (right2.Location as LocationCurve).Curve as Line;
                double a = Math.Max(r1.GetEndPoint(0).X, r1.GetEndPoint(1).X);
                double b = Math.Max(r2.GetEndPoint(0).X, r2.GetEndPoint(1).X);

                if (a < b)
                {
                    rightLeft = right1;
                    rightRight = right2;
                }
                else
                {
                    rightLeft = right2;
                    rightRight = right1;
                }
            }

            if (rightLeft != null)
            {
                Line rl = (rightLeft.Location as LocationCurve).Curve as Line;
                double r = Math.Max(rl.GetEndPoint(0).X, rl.GetEndPoint(1).X);

                double w = r - position.X;
                if (w > 0)
                    ms.colWidth = w;
            }

            if (rightLeft != null && rightRight != null)
            {
                Line rl = (rightLeft.Location as LocationCurve).Curve as Line;
                double rlD = Math.Max(rl.GetEndPoint(0).X, rl.GetEndPoint(1).X);

                Line rr = (rightRight.Location as LocationCurve).Curve as Line;
                double rrD = Math.Max(rr.GetEndPoint(0).X, rl.GetEndPoint(1).X);

                ms.colSeparation = rrD - rlD;
            }

            using (Transaction tr = new Transaction(uidoc.Document, "Resizing box"))
            {
                tr.Start();

                group.SetEntity(ms);

                tr.Commit();
            }
        }

        /// <summary>
        /// Calculates the origin of the text note
        /// </summary>
        /// <returns></returns>
        public XYZ getOrigin()
        {
            MasterSchema ms = group.GetEntity<MasterSchema>();

            // Calc origin
            TextNote sampleElement = uidoc.Document.GetElement(ms.sampleElement) as TextNote;
            if (sampleElement == null)
            {
                return null;
            }
            XYZ position = sampleElement.Coord;
            XYZ relPosition = ms.sampleElementDeltaOrigin;
            position = position.Subtract(relPosition);

            return position;
        }
    }
}
