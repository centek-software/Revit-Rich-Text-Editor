//   Revit Rich Text Editor
//   Copyright (C) 2014 Centek Engineering

using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.UI.Selection;

namespace CTEK_Rich_Text_Editor
{
    [TransactionAttribute(TransactionMode.Manual)]
    [RegenerationAttribute(RegenerationOption.Manual)]
    class TextNoteCreatorCmd : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiApp = commandData.Application;

            UIDocument uidoc = uiApp.ActiveUIDocument;

            // Get selection
            PickedBox pb = null;
            try
            {
                pb = uidoc.Selection.PickBox(PickBoxStyle.Enclosing, "Draw the rectangle that will be your text note");
            }
            catch (Exception)
            {
                return Result.Succeeded;
            }

            double Xlft = Math.Min(pb.Min.X, pb.Max.X);
            double Xrgt = Math.Max(pb.Min.X, pb.Max.X);
            double Ytop = Math.Max(pb.Min.Y, pb.Max.Y);
            double Ybot = Math.Min(pb.Min.Y, pb.Max.Y);

            // Automatic values that need a way to be adjusted
            int cols = 1;
            double colSepPercent = .20;

            // Fundamental vals
            double colHeight = Ytop - Ybot;
            double width = (Xrgt - Xlft);

            DebugHandler.println("TNCreator", "Making Note [Height: " + colHeight + ", Width: " + width + "]");

            if (colHeight == 0 || width == 0)
            {
                MainRevitProgram.ShowDialog("Click and Drag", "You need to click and drag to make a rich text note!");
                return Result.Succeeded;
            }

            View view = uidoc.ActiveGraphicalView;

            TextTools textTools = new TextTools(uidoc, null);

            using (TransactionGroup transGroup = new TransactionGroup(uidoc.Document, "Create rich text note"))
            {
                transGroup.Start();
                //===

                // Warn user if making a tiny note.
                // This is because we got lots of questions when people did this and it spit out garbage.
                // Uses magic numbers and is pretty arbitrary.

                // Forces the creation of these styles
                textTools.DefaultTextNoteType(TextTools.TextStyle.H1);
                textTools.DefaultTextNoteType(TextTools.TextStyle.H2);
                textTools.DefaultTextNoteType(TextTools.TextStyle.H3);
                textTools.DefaultTextNoteType(TextTools.TextStyle.H4);
                textTools.DefaultTextNoteType(TextTools.TextStyle.H5);

                double minWidth = TextTools.stringWidthApprox(uidoc, "Welcome to the Rich Text", textTools.DefaultTextNoteType(TextTools.TextStyle.P), false, view.Scale);

                if (colHeight < minWidth * 2 || width < minWidth)
                {
                    MainRevitProgram.ShowDialog("Click and Drag", "You are making a very tiny text note! If this was not your intention, undo and try dragging a larger box.");
                }

                // Default column separation
                colHeight = Math.Max(0.001, colHeight);
                width = Math.Max(0.001, width);

                double colSep = width * colSepPercent;
                double colWidth = (width - (cols - 1) * colSep) / cols;

                FundamentalProps fundamentalProperties = new FundamentalProps(colHeight, colWidth, colSep);

                string example = "<p><strong>Welcome to the Rich Text Editor!</strong></p>\n<p></p>\n<p>Quick Start:</p>\n<p></p>\n<ul>\n<li>Editing Content:</li>\n<ul>\n<li><strong>DO NOT double click on this note to edit it.</strong></li>\n<li>Use the&nbsp;<strong>Edit Note</strong> button<br /><br /></li>\n</ul>\n<li>Resizing:\n<ul>\n<li>Use&nbsp;<strong>Toggle Box</strong> to show the resize lines</li>\n<li>Drag the resize lines to the desired position</li>\n<li>Use <strong>Apply Resize</strong> to reform the note</li>\n<li>Use <strong>Toggle Box</strong> again<br /><br /></li>\n</ul>\n</li>\n<li>Setting Fonts\n<ul>\n<li>Use <strong>Set Fonts</strong></li>\n<li>For any unset fonts, we pick one arbitrarily.</li>\n</ul>\n</li>\n</ul>";
                
                example = "<!-- BulletFamily: " + "DefaultBulletId" + "-->" + example;
                ColumnHandler ch = new ColumnHandler(uidoc, fundamentalProperties, view, new XYZ(Xlft, Ytop, pb.Min.Z), example);
                RichTextPlacer rtp = new RichTextPlacer(example, uidoc, fundamentalProperties, view.Scale, ch, null);
                
                rtp.DoParse();

                using (Transaction tr = new Transaction(uidoc.Document, "Grouping text nodes together"))
                {
                    tr.Start();

                    ch.GroupColumns();

                    tr.Commit();
                }

                //===
                transGroup.Assimilate();        // Merges the individual transactions into a single one for the undo menu
            }

            if (Properties.Settings.Default.doubleClickWarning)
                new DoubleClickWarning().ShowDialog();

            return Result.Succeeded;
        }

    }
}
