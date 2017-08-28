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

        private List<ColumnMarker> columnStartPositions = new List<ColumnMarker>();

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

            this.columnStartPositions.Add(new ColumnMarker());
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

            this.columnStartPositions.Add(new ColumnMarker());
        }

        /// <summary>
        /// Requests that we draw the image at this relative position
        /// </summary>
        /// <param name="htmlData">img src in the form data:image/png;base64,iVBORw0KGgoAAAANSUhEU...</param>
        /// <param name="relX"></param>
        /// <param name="relY"></param>
        /// <param name="widthPixels">Width of the image (in pixels)</param>
        /// <param name="heightPixels">Height of the image (in pixels)</param>
        public void RequestDrawImage(string htmlData, double relX, double relY, int widthPixels, int heightPixels)
        {
            int scale = masterView.Scale;
            //CheckNeedsColumn(relY, ImageHandler.pixelsToFeet(heightPixels, scale));
            var height = ImageHandler.pixelsToFeet(heightPixels, scale);

            var adjust = GetActualPosition(relY, height);
            var adjustX = adjust.X;
            var adjustY = adjust.Y;

            ActuallyDrawImage(htmlData, relX + adjustX, relY + adjustY, widthPixels, heightPixels);
        }

        /// <summary>
        /// Requests that we draw text at this relative position
        /// </summary>
        /// <param name="textString">String to draw</param>
        /// <param name="relX"></param>
        /// <param name="relY"></param>
        /// <param name="textType">The text font</param>
        public double RequestDrawText(string textString, double relX, double relY, TextNoteType textType, TextTools.TextScriptType textScript)
        {
            double height = TextTools.textHeight(textType) * masterView.Scale * TEXT_SPACING;

            // CheckNeedsColumn(relY, height);
            var adjust = GetActualPosition(relY, height);
            var adjustX = adjust.X;
            var adjustY = adjust.Y;

            return ActuallyDrawText(textString, relX + adjustX, relY + adjustY, textType, textScript);
        }

        public void RequestDrawAnnotation(string id, double relX, double relY, string bulletChar, double height, double relYforCol)
        {
            // CheckNeedsColumn(relYforCol, height);
            var adjust = GetActualPosition(relYforCol, height);
            var adjustX = adjust.X;
            var adjustY = adjust.Y;

            ActuallyDrawAnnotationSymbol(id, relX + adjustX, relY + adjustY, bulletChar);
        }

        // this method draws the complete outline around one cell (duplicate lines have to be made to account for table wrapping)
        // TODO: Don't create line which already exists exactly the same
        public void RequestNewCellBox(double relX, double endRelX, double relY, double endRelY) //this needs to be called from a transaction
        {
            // TODO: This gets called with bad values so let's add line drawing to a future update
            // var adjust = GetActualPosition(endRelY, 0);
            // ActuallyDrawNewCellBox(relX + adjust.X, endRelX + adjust.X, relY + adjust.Y, endRelY + adjust.Y);
        }

        /// <summary>
        /// Requests that we make a new column for elements below this position
        /// </summary>
        private void RequestNewColumn(double relY)
        {
            var lastColumn = columnStartPositions.First();
            columnStartPositions.Insert(0, new ColumnMarker()
            {
                Column = lastColumn.Column + 1,
                PositionY = relY
            });
        }

        public void RequestNewColumn()
        {
            needColumn++;
        }

        // How many more empty columns need to be created now
        private int needColumn = 0;
        
        /// <summary>
        /// Gets the actual position where we have to draw the element after taking wrapping into consideration.
        /// Assumes note position is (0,0). Origin handling is elsewhere.
        /// </summary>
        /// <param name="requestedY">The starting height of the item</param>
        /// <param name="height">Used to wrap if necessary</param>
        /// <returns>(adjustX, adjustY, 0)</returns>
        private XYZ GetActualPosition(double requestedY, double height)
        {
            //foreach (var marker in columnStartPositions)
            for (int i = 0; i < columnStartPositions.Count; ++i)
            {
                var marker = columnStartPositions[i];

                DebugHandler.println("CH", String.Format("Marker {0} requested {1}", marker.PositionY, requestedY));
                if (requestedY - height <= marker.PositionY || marker == columnStartPositions.Last())
                {
                    int baseColumn = marker.Column;
                    double baseY = marker.PositionY;
                    double actualY = requestedY - baseY;
                    int actualColumn = baseColumn + (int) ((-actualY + height) / tnt.colHeight);
                    double columnX = actualColumn * (tnt.colWidth + tnt.colSep);
                    double adjustY = -baseY + (actualColumn - baseColumn) * tnt.colHeight;

                    if (actualColumn > baseColumn)
                        needColumn++;

                    // If we need a new column, request it and jump back to it to redo all the calculations
                    if (needColumn > 0)
                    {
                        RequestNewColumn(requestedY);
                        i--;
                        needColumn--;
                        continue;
                    }

                    return new XYZ(columnX, adjustY, 0);
                }
            }

            // Impossible, in principal
            return null;
        }

        private void ActuallyDrawNewCellBox(double relX, double endRelX, double relY, double endRelY) //this needs to be called from a transaction
        {
            // TODO: This gets called with bad values so let's not use it yet

            //double startX = initialPos.X + relX;
            //double endX = initialPos.X + endRelX;
            //double startY = initialPos.Y - relY;
            //double endY = initialPos.Y - endRelY;

            //if (Math.Abs(relY - endRelY) > 0)
            //{
            //    UIApplication uiapp = uidoc.Application;
            //    View view = uidoc.Document.ActiveView;
            //    DetailCurve lineBottom = MakeLine(uiapp, view, startX, endY, endX, endY); // bottom line
            //    DetailCurve lineRight = MakeLine(uiapp, view, endX, startY, endX, endY); // right line
            //    newMasterNoteElements.Add(lineBottom.Id);
            //    newMasterNoteElements.Add(lineRight.Id);

            //    DetailCurve lineTop = MakeLine(uiapp, view, startX, startY, endX, startY); // top line
            //    DetailCurve lineLeft = MakeLine(uiapp, view, startX, startY, startX, endY); // left line
            //    newMasterNoteElements.Add(lineTop.Id);
            //    newMasterNoteElements.Add(lineLeft.Id);
            //}
        }

        /// <summary>
        /// Actually draws the image in the view
        /// </summary>
        private void ActuallyDrawImage(string htmlData, double relX, double relY, int width, int height)
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
        private void ActuallyDrawAnnotationSymbol(string id, double relX, double relY, string bulletText)
        {
            if (id.Equals("DefaultBulletId"))
            {
                string tempId = TextTools.readDefaults(uidoc.Document);  // see if default bullet id is set
                if (!tempId.Equals(""))
                {
                    id = tempId;
                }
                else  // if no default bullet id was found
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
                        // have to set it twice to get all integer and string parameters
                    }
                }
                newMasterNoteElements.Add(e.Id);

                tr.Commit();
            }

        }

        /// <summary>
        /// Actually draws the text in the view. Do not pass in empty strings.
        /// </summary>
        private double ActuallyDrawText(string textString, double relX, double relY, TextNoteType textType, TextTools.TextScriptType textScript)
        {
            if (textString.Trim().Equals(""))
                throw new Exception("WE CANNOT DRAW EMPTY STRINGS");

            double height = TextTools.textHeight(textType);
            double subscriptOffsetY = 2.3 * masterView.Scale * height; // 2.3 is how far down to draw the text (kind of arbitrary but looks fine)
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

                switch (RevitVersionHandler.GetRevitVersion())
                {
                    case 2015:
                        textNote = CreateTextNoteWrapper2015(masterView, pLoc, oversizeWidth, textString);
                        textNote.TextNoteType = textType;
                        textNote.Width = oversizeWidth;
                        break;

                    case 2016:
                    case 2017:
                    default:
                        // Temporary Reflection hack:
                        textNote = (TextNote)RevitVersionHandler.CreateTextNote2016.Invoke(null, new object[] { uidoc.Document, masterView.Id, pLoc, textString, textType.Id });
                        // What it should actually be:
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
            }

            newMasterNoteElements.Add(textNote.Id);

            // Use approx width if we don't have API for non-approx width yet
            double contentWidth;

            switch (RevitVersionHandler.GetRevitVersion())
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
                    // I hate Revit:
                    // 2015: This is the entire width of the note, not the text
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
        // #kludge
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
        public bool HasElements()
        {
            return newMasterNoteElements.Count > 0;
        }

        /// <summary>
        /// Deletes all the KNOWN elements in the master. In other words, we intentionally do not delete extra items that have been added to the note.
        /// </summary>
        public void DeleteMaster()
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
            PurgeIfNecessary(masterGroup);
        }

        private void PurgeIfNecessary(Group masterGroup)
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
        public Group GroupColumns()
        {
            ICollection<ElementId> newCollection = new List<ElementId>();
            foreach (ElementId eid in newMasterNoteElements.Union(allExistingMasterNoteElements))
            {
                newCollection.Add(eid);
            }
            Group group = uidoc.Document.Create.NewGroup(newCollection);
            AddMasterData(group);

            return group;
        }

        /// <summary>
        /// Adds our master data (ex html, sizing) to the note
        /// </summary>
        /// <param name="group"></param>
        private void AddMasterData(Group group)
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

    class ColumnMarker
    {
        public ColumnMarker()
        {
            this.Column = 0;
            this.PositionY = 0;
        }

        public int Column { get; set; }
        public double PositionY { get; set; }
    }
}
