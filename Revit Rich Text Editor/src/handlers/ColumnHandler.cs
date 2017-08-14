//   Revit Rich Text Editor
//   Copyright (C) 2014 Centek Engineering

using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using VCExtensibleStorageExtension;
using VCExtensibleStorageExtension.ElementExtensions;

namespace CTEK_Rich_Text_Editor
{
    /// <summary>
    /// Handles columns
    /// </summary>
    class ColumnHandler
    {
        private ElementId masterNote;       // Master Note

        // These 2 lists contain the same elements, just stored differently. They should be parallel arrays.
        //private ICollection<IIInsertableItem> newMasterNoteIIs = new List<IIInsertableItem>();  // The to-do list of elements to make
        private ICollection<ElementId> newMasterNoteElements = new List<ElementId>();           // The Element IDs of all actually created elements

        private ICollection<ElementId> allExistingMasterNoteElements = new List<ElementId>();   // Every element inside the group when we started
        private ICollection<ElementId> knownMasterNoteElements = new List<ElementId>();         // Every element we expected to be in the group

        MasterSchema oldMs = null;      // The schema in the note when we started

        private View masterView;        // The view the master note was located on      
        private XYZ initialPos = null;  // The origin (ie upper left) of the note
        private ElementId sampleElement = null;         // The sample element used for determining the origin of the note
        private XYZ sampleElementDeltaOrigin = null;    // How far away from the origin that sample element would have been

        private UIDocument uidoc;
        private FundamentalProps tnt;   // Column sizing properties
        private string html;            // The html we are drawing (for saving)

        // ie 110% of the original width. Soft guarantee that text doesn't overflow in corner cases.
        private const float EXTRA_WIDTH_BUFFER_PC = 1.10f;
        // ie 1.5 feet extra
        private const float EXTRA_WIDTH_B = 1.50f;

        public const float TEXT_SPACING = 1.5f;

        /// <summary>
        /// Creates a new ColumnHandler where there is no existing master note
        /// </summary>
        /// <param name="uidoc"></param>
        /// <param name="tnt"></param>
        /// <param name="masterView"></param>
        /// <param name="initialPos"></param>
        /// <param name="html"></param>
        public ColumnHandler(UIDocument uidoc, FundamentalProps tnt, View masterView, XYZ initialPos, string html)
        {
            this.uidoc = uidoc;
            this.tnt = tnt;
            this.html = html;

            this.masterNote = null;
            this.masterView = masterView;
            this.initialPos = initialPos;
        }

        /// <summary>
        /// Creates a new ColumnHandler when there is an existing master note
        /// </summary>
        /// <param name="uidoc"></param>
        /// <param name="tnt"></param>
        /// <param name="masterNote"></param>
        /// <param name="html"></param>
        public ColumnHandler(UIDocument uidoc, FundamentalProps tnt, ElementId masterNote, string html)
        {
            this.uidoc = uidoc;
            this.tnt = tnt;
            this.html = html;

            this.masterNote = masterNote;

            // Just get some data
            Group masterGroup = uidoc.Document.GetElement(masterNote) as Group;
            this.oldMs = masterGroup.GetEntity<MasterSchema>();
            this.masterView = uidoc.Document.GetElement(masterGroup.OwnerViewId) as View;

            MasterSchema ms = masterGroup.GetEntity<MasterSchema>();
            TextNote sampleElement = uidoc.Document.GetElement(ms.sampleElement) as TextNote;

            XYZ position = null;

            allExistingMasterNoteElements = masterGroup.GetMemberIds();
            knownMasterNoteElements = ms.elementsWeMade;

            // Identify the origin
            if (sampleElement != null)
            {
                position = sampleElement.Coord;
                XYZ relPosition = ms.sampleElementDeltaOrigin;
                this.initialPos = position.Subtract(relPosition);
                DebugHandler.println("CH", "Using sample element pos (" + relPosition.X + "," + relPosition.Y + ")"
                    + " and subtracting (" + this.initialPos.X + "," + this.initialPos.Y + ")");
            }
            else
            {
                DebugHandler.println("CH", "ERROR: Sample element not available? Using fallback.");
                foreach (ElementId eid in allExistingMasterNoteElements)
                {
                    sampleElement = uidoc.Document.GetElement(eid) as TextNote;
                    if (sampleElement != null)
                    {
                        this.initialPos = sampleElement.Coord;
                        break;
                    }
                }
            }

            if (this.initialPos == null)
            {
                TaskDialog.Show("Rich Text Editor", "ERROR: The Rich Text Note was heavily corrupted and we were unable to determine its correct location. It will be rendered at the view origin.");
                this.initialPos = masterView.Origin;
            }

        }

        /// <summary>
        /// Requests that we draw the image at this relative position
        /// </summary>
        /// <param name="htmlData">img src in the form data:image/png;base64,iVBORw0KGgoAAAANSUhEU...</param>
        /// <param name="relX"></param>
        /// <param name="relY"></param>
        /// <param name="widthPixels">Width of the image (in pixels)</param>
        /// <param name="heightPixels">Height of the image (in pixels)</param>
        public void requestDrawImage(string htmlData, double relX, double relY, int widthPixels, int heightPixels)
        {
            int scale = masterView.Scale;
            //newMasterNoteIIs.Add(new IIImage(new XYZ(relX, relY, 0), ImageHandler.pixelsToFeet(width, scale), ImageHandler.pixelsToFeet(height, scale), htmlData, width, height));
            checkNeedsColumn(relY, ImageHandler.pixelsToFeet(heightPixels, scale));
            actuallyDrawImage(htmlData, relX + adjustX, relY + adjustY, widthPixels, heightPixels);
        }

        /// <summary>
        /// Requests that we draw text at this relative position
        /// </summary>
        /// <param name="textString">String to draw</param>
        /// <param name="relX"></param>
        /// <param name="relY"></param>
        /// <param name="textType">The text font</param>
        public double requestDrawText(string textString, double relX, double relY, TextNoteType textType, TextTools.TextScriptType textScript)
        {
            //DebugHandler.println("CH", "Text:[" + textString + "] RelY: [" + relY + "]");
            //double width = TextTools.stringWidth(uidoc, textString, textType, true, masterView.Scale);
            double height = TextTools.textHeight(textType) * masterView.Scale * TEXT_SPACING;

            checkNeedsColumn(relY, height);

            return actuallyDrawText(textString, relX + adjustX, relY + adjustY, textType, textScript);
            //newMasterNoteIIs.Add(new IITextNode(new XYZ(relX, relY, 0), height, textString, textType, textScript));
        }

        public void requestDrawAnnotation(string id, double relX, double relY, string bulletChar, double height, double relYforCol)
        {
            //DebugHandler.println("CH", "AnnotationChar:[" + bulletChar + "] RelY: [" + relY + "]");
            checkNeedsColumn(relYforCol, height);

            actuallyDrawAnnotationSymbol(id, relX + adjustX, relY + adjustY, bulletChar);
            //newMasterNoteIIs.Add(new IIAnnotationSymbol(new XYZ(X, Y, 0), 10, id, bulletChar));
        }

        /// <summary>
        /// Requests that we make a new column for all future elements
        /// </summary>
        public void requestNewColumn()
        {
            //newMasterNoteIIs.Add(new IIColumnBreak());
            //col++;
            //adjustX += tnt.colWidth + tnt.colSep;
            //adjustY = 0;
            colBreak = true;
        }

        //This method draws the large outline that extends around the whole table not just one cell (not used anymore because of table wrapping)
        public void requestNewTableBox(double relX, double endRelX, double relY, double endRelY) //this needs to be called from a started transaction
        {
            double startX = initialPos.X + relX + adjustX;
            double endX = initialPos.X + endRelX + adjustX;
            double startY = initialPos.Y - relY + adjustY;
            double endY = initialPos.Y - endRelY + adjustY;

            if (Math.Abs(relY - endRelY) > 0)
            {
                UIApplication uiapp = uidoc.Application;
                View view = uidoc.Document.ActiveView;
                DetailCurve lineTop = MakeLine(uiapp, view, startX, startY, endX, startY); //top line
                DetailCurve lineLeft = MakeLine(uiapp, view, startX, startY, startX, endY); //left line
                newMasterNoteElements.Add(lineTop.Id);
                newMasterNoteElements.Add(lineLeft.Id);
            }
        }

        //because table lines are drawn at the very end of the table rendering we have to do our own independent wrapping
        double tableAdjustX = 0;
        double tableAdjustY = 0;
        //this method draws the complete outline around one cell (duplicate lines have to be made to account for table wrapping)
        public void requestNewCellBox(double relX, double endRelX, double relY, double endRelY) //this needs to be called from a transaction
        {
            double startX;
            double endX;
            double startY;
            double endY;

            //DebugHandler.println("CH", "relY [" + relY + "], colHeight [" + tnt.colHeight + "], tableAdjustY[" + tableAdjustY + "]");
            if(relY + tableAdjustY >= tnt.colHeight)
            {
                tableAdjustX += tnt.colWidth + tnt.colSep;
                tableAdjustY = -relY;
            }
            startX = initialPos.X + relX + tableAdjustX;
            endX = initialPos.X + endRelX + tableAdjustX;
            startY = initialPos.Y - relY - tableAdjustY;
            endY = initialPos.Y - endRelY - tableAdjustY;

            if (Math.Abs(relY - endRelY) > 0)
            {
                UIApplication uiapp = uidoc.Application;
                View view = uidoc.Document.ActiveView;
                DetailCurve lineBottom = MakeLine(uiapp, view, startX, endY, endX, endY); //bottom line
                DetailCurve lineRight = MakeLine(uiapp, view, endX, startY, endX, endY); //right line
                newMasterNoteElements.Add(lineBottom.Id);
                newMasterNoteElements.Add(lineRight.Id);

                DetailCurve lineTop = MakeLine(uiapp, view, startX, startY, endX, startY); //top line
                DetailCurve lineLeft = MakeLine(uiapp, view, startX, startY, startX, endY); //left line
                newMasterNoteElements.Add(lineTop.Id);
                newMasterNoteElements.Add(lineLeft.Id);
            }
        }

        private void checkNeedsColumn(double upperLeftY, double height)
        {
            if (-upperLeftY - adjustY > tnt.colHeight - height || colBreak)       // Time to wrap!
            {
                col++;
                adjustX += tnt.colWidth + tnt.colSep;
                adjustY = -upperLeftY;
                colBreak = false;
            }
        }


        // == STUFF FROM actuallyDrawElements == //
        int col = 0;
        // How much to additionally adjust the note position
        double adjustX = 0;     // This is going to offset so we are in the correct X position for the column
        double adjustY = 0;     // This pulls the text up because otherwise it would still be drawn at the Y position directly under the last column
        bool colBreak = false;


        /// <summary>
        /// Actually draws the elements that we have requested to draw
        /// </summary>
        /// 
        //public void actuallyDrawElements()
        //{
            // TODO: NO LONGER USING THIS MOVING TO CLASSWIDE

            //int col = 0;

            //bool colBreak = false;

            //// How much to additionally adjust the note position
            //double adjustX = 0;     // This is going to offset so we are in the correct X position for the column
            //double adjustY = 0;     // This pulls the text up because otherwise it would still be drawn at the Y position directly under the last column

            //foreach (IIInsertableItem it in newMasterNoteIIs)
            //{
            //    // Handle column wrapping

            //    if (it is IIColumnBreak)    // If this is a column break, then the next element we draw should wrap whether it needs to or not
            //    {
            //        colBreak = true;
            //        continue;
            //    }

            //    if (-it.upperLeft.Y - adjustY > tnt.colHeight - it.height || colBreak)       // Time to wrap!
            //    {
            //        col++;
            //        adjustX += tnt.colWidth + tnt.colSep;
            //        adjustY = -it.upperLeft.Y;
            //        colBreak = false;
            //    }

            //    // Actually draw the item based on its type
            //    if (it is IITextNode)
            //    {
            //        IITextNode tn = it as IITextNode;
            //        actuallyDrawText(tn.textString, tn.upperLeft.X + adjustX, tn.upperLeft.Y + adjustY, tn.textType, tn.textScriptType);
            //    }
            //    else if (it is IIImage)
            //    {
            //        IIImage img = it as IIImage;
            //        actuallyDrawImage(img.htmlData, img.upperLeft.X + adjustX, img.upperLeft.Y + adjustY, img.widthPixels, img.heightPixels);
            //    }
            //    else if (it is IIAnnotationSymbol)
            //    {
            //        IIAnnotationSymbol ans = it as IIAnnotationSymbol;
            //        actuallyDrawAnnotationSymbol(ans.id, ans.upperLeft.X + adjustX, ans.upperLeft.Y + adjustY, ans.bulletText);
            //    }
            //}
        //}

        /// <summary>
        /// Actually draws the image in the view
        /// </summary>
        private void actuallyDrawImage(string htmlData, double relX, double relY, int width, int height)
        {
            using (Transaction tr = new Transaction(uidoc.Document, "Drawing image"))
            {
                tr.Start();

                XYZ pos = new XYZ(initialPos.X + relX, initialPos.Y + relY, initialPos.Z);
                Element e = ImageHandler.insertImage(htmlData, uidoc, pos, masterView, width, height);
                newMasterNoteElements.Add(e.Id);

                tr.Commit();
            }
           
        }

        /// <summary>
        /// Actually draws the annotation symbol in the view
        /// </summary>
        private void actuallyDrawAnnotationSymbol(string id, double relX, double relY, string bulletText)
        {
            if(id.Equals("DefaultBulletId"))
            {
                string tempId = TextTools.readDefaults(uidoc.Document); //see if default bullet id is set
                if(!tempId.Equals(""))
                {
                    id = tempId;
                }
                else //if no default bullet id was found
                {
                    return;
                }
            }
            AnnotationSymbolType annotationsymbol = uidoc.Document.GetElement(id) as AnnotationSymbolType;
            if (annotationsymbol == null)
            {
                return;
            }

            Element e;

            using (Transaction tr = new Transaction(uidoc.Document, "Making element"))
            {
                tr.Start();

                e = uidoc.Document.Create.NewFamilyInstance(new XYZ(initialPos.X + relX, initialPos.Y + relY, initialPos.Z), annotationsymbol, uidoc.Document.ActiveView);

                foreach (Parameter p in e.Parameters)
                {
                    if (!p.IsReadOnly)
                    {
                        p.Set(bulletText);
                        p.Set(int.Parse(bulletText));
                        //have to set it twice to get all integer and string parameters
                    }
                }
                newMasterNoteElements.Add(e.Id);

                tr.Commit();
            }
          
        }

        /// <summary>
        /// Actually draws the text in the view. Do not pass in empty strings.
        /// </summary>
        private double actuallyDrawText(string textString, double relX, double relY, TextNoteType textType, TextTools.TextScriptType textScript)
        {
            if (textString.Trim().Equals(""))
                throw new Exception("WE CANNOT DRAW EMPTY STRINGS");

            double height = TextTools.textHeight(textType);
            double subscriptOffsetY = 2.3 * masterView.Scale * height; //2.3 is how far down to draw the text (kind of arbitrary but looks fine)
            if (textScript == TextTools.TextScriptType.SUBSCRIPT)
                relY -= subscriptOffsetY;

            // Draw the note in the master
            XYZ pLoc = new XYZ(initialPos.X + relX, initialPos.Y + relY, initialPos.Z);

            // Add 'potato' here to make the text measurement here a tad longer
            double oversizeWidth = TextTools.stringWidthApprox(uidoc, textString + "potato", textType, true, masterView.Scale);
            
            TextNote textNote = null;
            using (Transaction tr = new Transaction(uidoc.Document, "Making the next node"))
            {
                tr.Start();

                //2e6dff5e-d602-4fe8-a229-7e6bcf78aed0

                switch (RevitVersionHandler.getRevitVersion())
                {
                    case 2015:
                        //textNote = uidoc.Document.Create.NewTextNote(masterView, pLoc, XYZ.BasisX, XYZ.BasisY, width, TextAlignFlags.TEF_ALIGN_LEFT | TextAlignFlags.TEF_ALIGN_TOP, textString);
                        textNote = CreateTextNoteWrapper2015(masterView, pLoc, oversizeWidth, textString);
                        textNote.TextNoteType = textType;
                        textNote.Width = oversizeWidth;
                        break;

                    case 2016:
                    case 2017:
                    default:
                        // Temporary Reflection hack
                        textNote = (TextNote) RevitVersionHandler.createTextNote2016.Invoke(null, new object[] { uidoc.Document, masterView.Id, pLoc, textString, textType.Id });
                        // What it should actually be
                        //textNote = TextNote.Create(uidoc.Document, masterView.Id, pLoc, textString, textType.Id);
                        break;
                }

                tr.Commit();
                
            }

            // If we don't have a sample element yet, make this the sample element!
            if (sampleElement == null)
            {
                sampleElement = textNote.Id;

                sampleElementDeltaOrigin = new XYZ(relX, relY, 0);
                //DebugHandler.println("CH", "Setting delta origin X:" + sampleElementDeltaOrigin.X + " Y:" + sampleElementDeltaOrigin.Y + " addY:" + addY + " relY:" + relY + " subOffY:" + subscriptOffsetY);
            }

            newMasterNoteElements.Add(textNote.Id);

            // Use approx width if we don't have API for non-approx width yet
            double contentWidth;

            switch (RevitVersionHandler.getRevitVersion())
            {
                case 2015:
                case 2016:
                    // 2015-2016: Very close to correct
                    // >=2017 Somewhat close, but no longer close enough for exact placement.
                    double approxWidth = TextTools.stringWidthApprox(uidoc, textString, textType, false, masterView.Scale);
                    contentWidth = approxWidth;
                    break;

                case 2017:
                default:
                    // 2015: This is the entire width of the note; Not what we want
                    // 2016: Exact width of the text but spaces are trimmed
                    // 2017: Exact width of the text including preceding and trailing spaces
                    contentWidth = textNote.Width * uidoc.ActiveGraphicalView.Scale;
                    break;
            }

            return contentWidth;
        }

        private double TextNoteLineWidthWrapper2015(TextNote textNote)
        {
            return textNote.LineWidth;
        }

        // The .NET VM realizes that the function inside here doesn't exist when used in Revit 2017,
        // but it doesn't notice it if we encapsulate it in another function.
        private TextNote CreateTextNoteWrapper2015(View masterView, XYZ pLoc, double width, string textString)
        {
            return uidoc.Document.Create.NewTextNote(masterView, pLoc, XYZ.BasisX, XYZ.BasisY, width, TextAlignFlags.TEF_ALIGN_LEFT | TextAlignFlags.TEF_ALIGN_TOP, textString);
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

        /// <summary>
        /// Checks if we have any elements waiting to be drawn
        /// </summary>
        /// <returns></returns>
        public bool hasElements()
        {
            return newMasterNoteElements.Count > 0;
            //return newMasterNoteIIs.Count > 0;
        }

        /// <summary>
        /// Deletes all the KNOWN elements in the master. In other words, we intentionally do not delete extra items that have been added to the note.
        /// </summary>
        public void deleteMaster()
        {
            Group masterGroup = uidoc.Document.GetElement(masterNote) as Group;
            masterGroup.UngroupMembers();
            foreach (ElementId eid in knownMasterNoteElements)
            {
                try
                {
                    uidoc.Document.Delete(eid);
                }
                catch (Exception)
                {
                }

                allExistingMasterNoteElements.Remove(eid);
            }

            // Delete the group type, so we don't clutter the model
            //uidoc.Document.Delete(masterGroup.GetTypeId());
            purgeIfNecessary(masterGroup);
        }

        private void purgeIfNecessary(Group masterGroup)
        {
            FilteredElementCollector collectorUsed = new FilteredElementCollector(uidoc.Document);

            ICollection<ElementId> groups = collectorUsed.OfClass(typeof(Group)).ToElementIds();

            ElementId compare = masterGroup.GetTypeId();
            ElementId masterId = masterGroup.Id;

            foreach (ElementId groupId in groups)
            {
                if (groupId.Equals(masterId))
                    continue;

                if (uidoc.Document.GetElement(groupId).GetTypeId().Equals(compare))
                {
                    // Another group exists with the same type id, so we don't want to delete the type
                    return;
                }
            }

            // Home free - delete the old type!
            uidoc.Document.Delete(masterGroup.GetTypeId());
        }

        /// <summary>
        /// Groups all the elements we have created
        /// </summary>
        /// <returns>The Group that gets made</returns>
        public Group groupColumns()
        {
            ICollection<ElementId> newCollection = new List<ElementId>();
            foreach (ElementId eid in newMasterNoteElements.Union(allExistingMasterNoteElements))
            {
                newCollection.Add(eid);
            }
            Group group = uidoc.Document.Create.NewGroup(newCollection);
            addMasterData(group);

            return group;
        }

        /// <summary>
        /// Adds our master data (ex html, sizing) to the note
        /// </summary>
        /// <param name="group"></param>
        private void addMasterData(Group group)
        {
            MasterSchema ms = new MasterSchema();

            ms.html = html;

            ms.colHeight = tnt.colHeight;
            ms.colWidth = tnt.colWidth;
            ms.colSeparation = tnt.colSep;

            ms.elementsWeMade = new List<ElementId>();
            foreach (ElementId ei in newMasterNoteElements)
                ms.elementsWeMade.Add(ei);

            ms.sampleElement = sampleElement;
            ms.sampleElementDeltaOrigin = sampleElementDeltaOrigin;

            if (oldMs != null)
            {
                ms.resizeBoxActivated = oldMs.resizeBoxActivated;
                ms.rbRight1 = oldMs.rbRight1;
                ms.rbRight2 = oldMs.rbRight2;
                ms.rbBottom = oldMs.rbBottom;

                ms.fontP = oldMs.fontP;
                ms.fontH1 = oldMs.fontH1;
                ms.fontH2 = oldMs.fontH2;
                ms.fontH3 = oldMs.fontH3;
                ms.fontH4 = oldMs.fontH4;
                ms.fontH5 = oldMs.fontH5;
            }
            else
                ms.resizeBoxActivated = false;

            group.SetEntity(ms);

        }

        
    }
}
