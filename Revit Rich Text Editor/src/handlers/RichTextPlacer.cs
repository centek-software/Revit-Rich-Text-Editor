//   Revit Rich Text Editor
//   Copyright (C) 2014 Centek Engineering

using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Diagnostics;

namespace CTEK_Rich_Text_Editor
{
    /// <summary>
    /// Places rich text html, although it is in a single, very long column.
    /// Column Handler handles moving the elements to their correct column positions
    /// </summary>
    class RichTextPlacer
    {
        private const double scriptScale = 0.4;
        private UIDocument uidoc;
        int viewScale;

        private XmlNode root;
        private FundamentalProps tnp;
        private ColumnHandler ch;
        private TextTools tt;

        //Due to a revit restriction, you can't have both custom bullets and images or tables and images. So uhh... these variables prevent it
        private bool customBulletEncountered = false;
        private bool imageEncountered = false;
        private bool tableEncountered = false;

        private TextNoteType largestType = null;

        private string lastPlacedText = "";
        private string bulletId = "";
        private List<List<Cell>> table; //a matrix representation of a table. 
        private int tableRow;
        private int tableCol;

        private List<Cell> tableCells; //a one dimensional array holding all unique table cells
        
        //stacks for holding tables to allow tables inside tables inside tables... 
        private Stack<List<List<Cell>>> tableStack;
        private Stack<List<Cell>> tableCellStack;
        private bool renderingTable; //confirmation to see if a table is already being rendered

        private enum BulletStyle { KAPUSTA, NUMBER, LOWER_ALPHA, UPPER_ALPHA, LOWER_ROMAN, UPPER_ROMAN, WHITE_CIRCLE, BLACK_CIRCLE, SQUARE, CIRCLED_NUMBER };

        public RichTextPlacer(string html, UIDocument uidoc, FundamentalProps tnp, int viewScale, ColumnHandler ch, Element masterNote)
        {
            this.uidoc = uidoc;
            this.tnp = tnp;
            this.viewScale = viewScale;
            this.ch = ch;
            this.tt = new TextTools(uidoc, masterNote);

            tableStack = new Stack<List<List<Cell>>>();
            tableCellStack = new Stack<List<Cell>>();

            html = html.Replace("&nbsp;", " ");     // This simplification is necessary to handle underlining with mulitple consecutive spaces between 2 words

            html = "<root>\n" + html + "\n</root>";     // Throw everything inside a root element (as per XML spec)

            html = BigConsts.XML_ENTITIES + html;

            // Make a DOM of the HTML from TinyMCE
            XmlDocument htmlDocument = new XmlDocument();
            htmlDocument.Load(new StringReader(html));

            // Pull out the root element and start the recursive parsing
            root = htmlDocument.GetElementsByTagName("root").Item(0);
        }

        /// <summary>
        /// Do the heavy lifting - calculate where all the elements need to go
        /// </summary>
        public void DoParse()
        {
            TextNoteType tnt = tt.getType(TextTools.TextStyle.P);

            double X = 0;       // We handle relative coords in ColumnHandler now, so just make everything relative to 0,0 now
            double Y = 0;

            double colStartX = X;
            double colEndX = X + tnp.colWidth;
            double colStartY = Y;
            double relStartX = 0;
            double relStartY = 0;

            Parse(root, ref colStartX, ref colEndX, ref colStartY, ref relStartX, ref relStartY, false, false, false, false, tnt, BulletStyle.KAPUSTA, -1, 0, false, false, 0, 0);
        }

        // colstartX and colStartY NOT always == 0
        private void Parse(XmlNode node, ref double colStartX, ref double colEndX, ref double colStartY,
            ref double relStartX, ref double relStartY, bool bold, bool italic, bool underline, bool strike,
            TextNoteType textType, BulletStyle bulletStyle, int bulletCount, int bulletLevel, bool super, bool sub, double tableWidth, int tableheightInt)
        {
            textType = tt.getType(textType, bold, italic, underline);
            string type = node.Name;
            string value = node.Value;

            // Turn the CSS styles in the span tags into imaginary HTML tags instead.
            // This serves as a kludgy way to have a much less convoluted parser than HTML would ordinarily need
            // NOT DESIGNED TO WORK WITH arbitrary HTML, just the stuff that comes out of TinyMCE (after a little massaging)
            // == Don't delete this code in case future ==
            if (type.Equals("span") && node.Attributes != null)
            {
                foreach (XmlAttribute att in node.Attributes)
                {
                    if (att.Name.Equals("style"))
                    {
                        if (att.Value.Equals("text-decoration: underline;"))
                            type = "underline";
                        else if (att.Value.Equals("text-decoration: line-through;"))
                            type = "strike";
                    }
                }
            }

            double INDENT = TextTools.stringWidthApprox(uidoc, "999.", textType, false, viewScale);      // How wide to make the indentations
            int indentations = 0;   // How many indentations we should make

            // Handle indented paragraphs
            if (node.Attributes != null)
            {
                foreach (XmlAttribute att in node.Attributes)
                {
                    if (att.Name.Equals("style"))
                    {
                        if (att.Value.StartsWith("padding-left:"))
                        {
                            string padding = "";

                            foreach (char c in att.Value)
                            {
                                if (Char.IsDigit(c))
                                    padding += c;
                            }

                            indentations = Convert.ToInt32(padding) / 30;       // The magic number 30 is the default px of an indentation in TinyMCE
                        }
                    }
                }
            }

            bool newlineAfter = false;      // Set this to true if we need to make a newline after the current node

        switchStatement:
            switch (type)
            {
                case "root":
                    goto theParsing;

                case "p":
                    newlineAfter = true;
                    goto theParsing;

                case "strong":
                    bold = true;
                    goto theParsing;

                case "em":
                    italic = true;
                    goto theParsing;

                case "underline":
                //case "u":
                    underline = true;
                    goto theParsing;

                case "strike":
                    strike = true;
                    goto theParsing;

                case "sup":
                    super = true;
                    textType = tt.getType(textType, bold, italic, underline, TextTools.getSize(textType) * scriptScale, null, null, -1);
                    goto theParsing;

                case "sub":
                    sub = true;
                    // Kludge: Makes line height regular so subscripts don't get messed up
                    MakeTextNote("", colStartX + relStartX, colStartY - relStartY, textType, TextTools.TextScriptType.REGULAR);
                    
                    textType = tt.getType(textType, bold, italic, underline, TextTools.getSize(textType) * scriptScale, null, null, -1);
                    goto theParsing;

                case "#text":
                    string fullText = value;

                    fullText = fullText.Replace("\n", "");      // HTML ignores this shit, so we should too!
                    fullText = fullText.Replace("\r", "");
                    TextTools.TextScriptType textscript = TextTools.TextScriptType.REGULAR;
                    if (sub)
                        textscript = TextTools.TextScriptType.SUBSCRIPT;
                    else if (super)
                        textscript = TextTools.TextScriptType.SUPERSCRIPT;

                    // == Rewritten algorithm for handling multiple spaces correctly ==

                    // Split up all the words like so:
                    // String:   "       hello1           hello2             hello3             "
                    // Array:  { "       hello1", "           hello2", "             hello3", "             " }

                    List<string> words = new List<string>();

                    string curWord = "";
                    bool hitLetter = false;
                    foreach (char c in fullText)
                    {
                        if (Char.IsWhiteSpace(c) && hitLetter)
                        {
                            words.Add(curWord);
                            curWord = "";
                            hitLetter = false;
                        }

                        if (!Char.IsWhiteSpace(c))
                            hitLetter = true;

                        curWord += c;
                    }

                    if (!curWord.Equals(""))
                        words.Add(curWord);

                    // Go thru the words
                    string curLine = "";
                    foreach (string word in words)
                    {
                        string toBeOrNotToBe = curLine + word;

                        double proposedWidth = TextTools.stringWidthApprox(uidoc, toBeOrNotToBe, textType, false, viewScale);

                        //TaskDialog.Show("Rich Text Editor", "[" + toBeOrNotToBe + "] is [" + (colStartX + relStartX + proposedWidth) + "] colEndX[" + colEndX + "]");

                        if (colStartX + relStartX + proposedWidth > colEndX)
                        {
                            // Make the note using the text from CURLINE at (colStartX + relStartX, colStartY - relStartY)
                            bool allWhite = (curLine.Trim().Length == 0);

                            if (relStartX > 0)      // Handle kerning
                            {
                                relStartX -= KerningSubtract(lastPlacedText, curLine, textType);
                            }

                            if (!allWhite)
                                MakeTextNote(curLine, colStartX + relStartX, colStartY - relStartY, textType, textscript);


                            // Reset the position for the next line
                            Newline(ref colStartX, ref colEndX, ref colStartY, ref relStartX, ref relStartY, textType);

                            // Grab just the next word (without any preceding spaces) for the next line
                            curLine = word.TrimStart();
                        }
                        else
                            curLine += word;
                    }

                    // Handle dropping the last hunk of text
                    if (curLine != "")
                    {
                        // Make the note using the text from TOBEORNOTTOBE at (colStartX + relStartX, colStartY - relStartY)
                        bool allWhite = (curLine.Trim().Length == 0);

                        if (relStartX > 0)      // Handle kerning
                        {
                            relStartX -= KerningSubtract(lastPlacedText, curLine, textType);
                        }

                        double width = MakeTextNote(curLine, colStartX + relStartX, colStartY - relStartY, textType, textscript);

                        // Update the relative position by adding the text width
                        //double width = TextTools.stringWidthApprox(uidoc, curLine, textType, false, viewScale);
                        relStartX += width;
                    }

                    break;

                case "br":
                    Newline(ref colStartX, ref colEndX, ref colStartY, ref relStartX, ref relStartY, textType);
                    break;

                case "ul":
                case "ol":
                    BulletStyle bS = MyBulletStyle(node, bulletLevel + 1);

                    if (relStartX > 0)
                        Newline(ref colStartX, ref colEndX, ref colStartY, ref relStartX, ref relStartY, textType);

                    // Figure out what number we should start at
                    int cnt = 1;

                    if (node.Attributes != null)
                    {
                        foreach (XmlAttribute att in node.Attributes)
                        {
                            if (att.Name.ToLower().Equals("start"))
                            {
                                try
                                {
                                    cnt = Int32.Parse(att.Value);
                                }
                                catch (Exception)
                                {
                                }
                            }
                        }
                    }

                    // Draw each of the items
                    string prev = null;
                    foreach (XmlNode child in node.ChildNodes)
                    {
                        bool shouldCnt = true;

                        double liColStartX2 = colStartX;
                        if (child.Name.ToLower().Equals("ul") || child.Name.ToLower().Equals("ol"))
                        {
                            liColStartX2 += INDENT;

                            if (prev != null && prev.Equals("li"))
                                shouldCnt = false;
                        }

                        Parse(child, ref liColStartX2, ref colEndX, ref colStartY, ref relStartX, ref relStartY, bold, italic, underline, strike, textType, bS, (shouldCnt ? cnt++ : cnt), bulletLevel + 1, super, sub, 0, 0);
                        prev = child.Name.ToLower();
                    }
                    break;

                case "li":

                    if (bulletStyle != BulletStyle.CIRCLED_NUMBER) //check if it is a regular bullet
                    {
                        string bullet = GetBullet(bulletStyle, bulletCount);
                        MakeTextNote(bullet, colStartX + relStartX, colStartY - relStartY, textType, TextTools.TextScriptType.REGULAR);
                    }
                    else //if not then draw the annotation stuff
                    {
                        if (!imageEncountered)
                        {
                            customBulletEncountered = true;
                            string bullet = GetBullet(bulletStyle, bulletCount);
                            MakeAnnotationSymbol(bulletId, colStartX + relStartX, colStartY - relStartY - (ColumnHandler.TEXT_SPACING * TextTools.textHeight(textType) * viewScale) / 2, bullet, TextTools.textHeight(textType) * viewScale * ColumnHandler.TEXT_SPACING, colStartY - relStartY);
                        }
                        else if(!customBulletEncountered) //if there is an image drawn
                        {
                            MessageBox.Show("Due to a Revit restriction, custom bullets and images can not be used in the same text note.", "Error:");
                            customBulletEncountered = true;
                            
                        }
                    }

                    bool lastWasBr = false;
                    bool lastWasLi = false;
                    double liColStartX = colStartX + INDENT;
                    foreach (XmlNode child in node.ChildNodes)
                    {
                        Parse(child, ref liColStartX, ref colEndX, ref colStartY, ref relStartX, ref relStartY, bold, italic, underline, strike, textType, bulletStyle, bulletCount, bulletLevel, super, sub, 0, 0);

                        lastWasBr = (child.Name.Equals("br"));
                        lastWasLi = (child.Name.Equals("ul") || child.Name.Equals("ol"));
                    }

                    // If the last element in an <li> is <br>, it is seemingly ignored in HTML.
                    // If the last element was either an ul or and ol, then the cleanup newline has already been taken care of.
                    if (!lastWasBr && !lastWasLi)
                        Newline(ref colStartX, ref colEndX, ref colStartY, ref relStartX, ref relStartY, textType);
                    break;

                case "table":
                    if (!imageEncountered)
                    {
                        if (renderingTable) //if we are in the middle of rendering another table
                        {
                            //save the current table onto the stack
                            tableStack.Push(new List<List<Cell>>(table));
                            tableCellStack.Push(new List<Cell>(tableCells));
                        }
                        renderingTable = true;

                        tableEncountered = true;
                        tableCells = new List<Cell>();
                        table = new List<List<Cell>>(); //initialize a new 2d table list
                        tableRow = -1; //start at -1 so first row will be 0
                        tableWidth = -1;
                        tableheightInt = 0; //TODO this is never used, just remove it
                        double tableWidthInt = 0;

                        if (node.Attributes != null)
                        {
                            foreach (XmlAttribute att in node.Attributes)// getting the attributes of the <table> (width and height)
                            {
                                if (att.Name.Equals("style"))
                                {
                                    string styleAttr = att.Value;
                                    string[] individualTraits = styleAttr.Split(';');
                                    foreach (string trait in individualTraits)
                                    {
                                        if (trait.Contains("width:"))
                                        {
                                            tableWidthInt = Int32.Parse(Regex.Replace(trait, @"[^\d]", ""));//regex replace all but the numbers
                                        }
                                    }
                                }
                            }
                        }

                        double noteWidth = Math.Abs(colEndX - colStartX);
                        double tableWidthFt = ImageHandler.pixelsToFeet((int)tableWidthInt, viewScale);
                        if (tableWidthInt == 0 || tableWidthFt > noteWidth) //if there is no table width or the table is bigger than the note
                        {
                            tableWidthFt = noteWidth; //snap table to note size
                        }


                        double originalColStartX = colStartX;
                        double originalColEndX = colEndX;
                        double originalRelStartX = relStartX;
                        foreach (XmlNode child in node.ChildNodes)
                        {
                            relStartX = originalRelStartX;
                            Parse(child, ref colStartX, ref colEndX, ref colStartY, ref relStartX, ref relStartY, bold, italic, underline, strike, textType, bulletStyle, bulletCount, bulletLevel, super, sub, tableWidthFt, tableheightInt);
                        }

                        //fix all the widths because tinymce doesn't really calculate widths correctly on merged cells
                        Stopwatch sw = new Stopwatch();
                        sw.Start();
                        if (fixTableWidths(noteWidth)) //fixtablewidths will return false on failure, only continue if true
                        {
                            sw.Stop();
                            DebugHandler.println("RTP", "Elapsed time in table widths: " + sw.Elapsed);

                            int currentTableRow = 0;
                            double originalRelStartY = relStartY;
                            double tablePadding = TextTools.stringWidthApprox(uidoc, " ", textType, false, viewScale);
                            sw.Restart();
                            foreach (Cell cell in tableCells) //actually draw the table now
                            {
                                relStartY = originalRelStartY;
                                if (currentTableRow < cell.rowNumStart) //deal with new rows
                                {
                                    //find highest y value from previous row
                                    for (int i = 0; i < table[currentTableRow].Count; i++)
                                    {
                                        if (table[currentTableRow][i].rowNumEnd == currentTableRow)
                                        {
                                            originalRelStartY = Math.Max(originalRelStartY, table[currentTableRow][i].yEnd);
                                        }
                                    }
                                    //DebugHandler.println("RTP", "highest y value row " + currentTableRow + ": " + originalRelStartY);

                                    relStartY = originalRelStartY;

                                    for (int i = 0; i < tableCells.Count; i++)
                                    {
                                        if (tableCells[i].rowNumEnd == currentTableRow)
                                        {
                                            tableCells[i].yEnd = originalRelStartY;
                                        }
                                    }

                                    currentTableRow++;
                                }

                                //DebugHandler.println("RTP", cell.toString());
                                //DebugHandler.println("RTP", "start at y: " + relStartY);
                                XmlNodeList nodes = cell.nodes;
                                colStartX = cell.colStart + tablePadding;
                                colEndX = cell.colEnd - tablePadding;
                                cell.yStart = relStartY;
                                relStartX = 0;
                                foreach (XmlNode child in nodes)
                                {
                                    renderingTable = true; //other tables inside of this table may set this value to false. We keep it true.
                                    Parse(child, ref colStartX, ref colEndX, ref colStartY, ref relStartX, ref relStartY, bold, italic, underline, strike, textType, bulletStyle, bulletCount, bulletLevel, super, sub, 0, 0);
                                }
                                Newline(ref colStartX, ref colEndX, ref colStartY, ref relStartX, ref relStartY, textType);
                                cell.yEnd = relStartY;

                            }
                            sw.Stop();
                            DebugHandler.println("RTP", "Elapsed time in text notes: " + sw.Elapsed);

                            //printTable(1);
                            for (int j = currentTableRow; j < table.Count; j++) //set the y values for the remaining rows, there may be more than one if a row only consists of vertically merged cells
                            {
                                //j is tablerow and i is tablecol
                                //set the y values for the final row
                                for (int i = 0; i < table[j].Count; i++)
                                {
                                    originalRelStartY = Math.Max(originalRelStartY, table[j][i].yEnd);
                                }

                                relStartY = originalRelStartY;

                                for (int i = 0; i < tableCells.Count; i++) //TODO you can iterate through the 2d object array rather than the whole thing
                                {
                                    if (tableCells[i].rowNumEnd == j)
                                    {
                                        tableCells[i].yEnd = originalRelStartY;
                                    }
                                }
                            }

                            sw.Restart();

                            using (Transaction tr = new Transaction(uidoc.Document, "Drawing box/cell lines"))
                            {
                                tr.Start();
                                //draw the large table outline (left and top)
                                //ch.requestNewTableBox(table[0][0].colStart, table[0][table[0].Count - 1].colEnd, table[0][0].yStart, originalRelStartY);

                                for (int i = 0; i < tableCells.Count; i++) //draw all the table lines (bottom and right)
                                {
                                    Cell cell = tableCells[i];
                                    ch.requestNewCellBox(cell.colStart, cell.colEnd, cell.yStart, cell.yEnd);
                                }
                                tr.Commit();
                            }
                            sw.Stop();
                            DebugHandler.println("RTP", "Elapsed time in table lines: " + sw.Elapsed);
                        }
                        //reset everything
                        colStartX = originalColStartX;
                        colEndX = originalColEndX;
                        
                        //after rendering and everything is done, release the previous table from the stack
                        if (tableStack.Count > 0)
                        {
                            table = tableStack.Pop();
                            tableCells = tableCellStack.Pop();
                        }
                        renderingTable = false;
                    }
                    else if (!tableEncountered)
                    {
                        tableEncountered = true;
                        MessageBox.Show("Due to a Revit restriction, tables and images can not be used in the same text note.", "Error:");
                    }
                    
                    break;

                case "tr":
                    {
                        tableCol = 0; //reset table col
                        tableRow++; //increase row count

                        if (table.Count < tableRow + 1) //make a new table row if it doesn't exist from a previous rowspan
                        {
                            table.Add(new List<Cell>());
                        }
                        tableWidth = tableWidth / node.ChildNodes.Count; //set each tablewidth to be an average size
                        
                        double originalColStartX = colStartX;

                        foreach (XmlNode child in node.ChildNodes)
                        {
                            Parse(child, ref colStartX, ref colEndX, ref colStartY, ref relStartX, ref relStartY, bold, italic, underline, strike, textType, bulletStyle, bulletCount, bulletLevel, super, sub, tableWidth, tableheightInt);
                        }
                        colStartX = originalColStartX;
                    }
                    break;

                case "td":
                    double tempRelStartY = relStartY;
                    double cellWidth = tableWidth; //set each cell to the precalculated cell size
                    int rowspan = 1;
                    int colspan = 1;
                    foreach (XmlAttribute att in node.Attributes)// getting the attributes of the <tr> (width)
                    {
                        if (att.Name.Equals("style"))
                        {
                            string styleAttr = att.Value;
                            string[] individualTraits = styleAttr.Split(';');
                            foreach (string trait in individualTraits)
                            {
                                if (trait.Contains("width:"))
                                {
                                    cellWidth = Int32.Parse(Regex.Replace(trait, @"[^\d]", ""));//regex replace all but the numbers
                                    cellWidth = ImageHandler.pixelsToFeet((int)cellWidth, viewScale); //convert to feet
                                }
                            }
                        }
                        if (att.Name.Equals("rowspan"))
                        {
                            rowspan = int.Parse(att.Value); //assign rowspan to value to be used later
                        }
                        if (att.Name.Equals("colspan"))
                        {
                            colspan = int.Parse(att.Value); //assign colspan to value to be used later
                        }
                    }

                    Cell currentCell = new Cell(cellWidth);

                    double additionalWidth = 0;
                    int cellsFilled = 0;
                    while (cellsFilled < colspan) //calculate additional displacement needed according to vertically merged cells
                    {
                        if (table[tableRow].Count < tableCol + 1) //if the end of the list is reached, add a new cell
                        {
                            table[tableRow].Add(currentCell);
                            cellsFilled++;
                            tableCol++;
                        }
                        else if (table[tableRow][tableCol] == null) //if there is no cell associated (created by rowspan)
                        {
                            table[tableRow][tableCol] = currentCell;
                            cellsFilled++;
                            tableCol++;
                        }
                        else //if there is a width associated
                        {
                            additionalWidth += table[tableRow][tableCol].width;
                            tableCol = table[tableRow][tableCol].colNumEnd + 1;
                        }
                    }
                    colStartX += additionalWidth;
                    colEndX = colStartX + cellWidth;

                    for (int i = 1; i < rowspan; i++) //deal with vertically merged cells
                    {
                        int currentTableRow = i + tableRow;
                        if (table.Count < currentTableRow + 1)
                        {
                            table.Add(new List<Cell>());
                        }
                        
                        for (int j = 0; j < tableCol - 1; j++)
                        {
                            if (colspan > 1 && j >= tableCol - colspan && j <= tableCol - 1) //if the current table col falls under the table col of the merged cell
                            {
                                if (table[currentTableRow].Count < j + 1) //add another cell
                                {
                                    table[currentTableRow].Add(currentCell);
                                }
                                else if (table[currentTableRow][j] == null) //change value of the cell
                                {
                                    table[currentTableRow][j] = currentCell;
                                }
                            }
                            else
                            {
                                if (table[currentTableRow].Count < j + 1) //add another placeholder cell
                                {
                                    table[currentTableRow].Add(null);
                                }
                            }
                            
                        }

                        //give a reference to the vertically merged cell in the correct position at the end
                        if(table[currentTableRow].Count < tableCol - 1 + 1) //-1 + 1 instead of 0 because I want to show: tableCol - 1 is the column of the cell, add one to correctly compare with table count
                        {
                            table[currentTableRow].Add(currentCell);
                        }
                        else
                        {
                            table[currentTableRow][tableCol - 1] = currentCell;
                        }
                    }

                    currentCell.colNumStart = tableCol - colspan;
                    currentCell.colNumEnd = tableCol - 1;
                    currentCell.rowNumStart = tableRow;
                    currentCell.rowNumEnd = tableRow + rowspan - 1;

                    currentCell.colStart = colStartX;
                    currentCell.colEnd = colEndX;
                    currentCell.nodes = node.ChildNodes;
                    tableCells.Add(currentCell);


                    //relStartX = 0;
                    
                    colStartX = colEndX;
                    //printTable(0);
                    break;

                case "h1":
                    textType = tt.getType(TextTools.TextStyle.H1);
                    newlineAfter = true;
                    goto theParsing;
                case "h2":
                    textType = tt.getType(TextTools.TextStyle.H2);
                    newlineAfter = true;
                    goto theParsing;
                case "h3":
                    textType = tt.getType(TextTools.TextStyle.H3);
                    newlineAfter = true;
                    goto theParsing;
                case "h4":
                    textType = tt.getType(TextTools.TextStyle.H4);
                    newlineAfter = true;
                    goto theParsing;
                case "h5":
                    textType = tt.getType(TextTools.TextStyle.H5);
                    newlineAfter = true;
                    goto theParsing;

                case "span":
                    goto theParsing;

                //case "a":
                //    goto theParsing;

                case "img":
                    if (!customBulletEncountered && !tableEncountered)
                    {
                        imageEncountered = true;
                        string html = null;
                        int h = -1;
                        int w = -1;
                        foreach (XmlAttribute att in node.Attributes)
                        {
                            if (att.Name.Equals("src"))
                                html = att.Value;
                            else if (att.Name.Equals("width"))
                                w = Int32.Parse(att.Value);
                            else if (att.Name.Equals("height"))
                                h = Int32.Parse(att.Value);
                        }

                        if (h <= 0 || w <= 0)
                        {
                            Image img = ImageHandler.getBitmap(html);
                            w = img.Width;
                            h = img.Height;
                        }

                        ch.requestDrawImage(html, colStartX + relStartX, colStartY - relStartY, w, h);


                        relStartY += ImageHandler.pixelsToFeet(h, viewScale);
                        relStartY -= TextTools.textHeight(textType) * 1.5 * viewScale;
                        relStartX += ImageHandler.pixelsToFeet(w, viewScale);
                    }
                    else if(!imageEncountered)
                    {
                        imageEncountered = true;
                        MessageBox.Show("Due to a Revit restriction, tables or custom bullets can not be used in the same text note as images.", "Error:");
                    }
                    break;

                case "#comment":
                    if (node.Value.Contains("BulletFamily: "))
                    {
                        bulletId = node.Value.Substring("BulletFamily: ".Length + 1);
                        DebugHandler.println("RTP", bulletId);
                    }
                    else if (node.Value.Trim().Equals("pagebreak"))
                        ch.requestNewColumn();

                    break;

                default:
                    // Handle converting entities into their actual symbol
                    string entity = "&" + node.Name + ";";
                    string decode = HttpUtility.HtmlDecode(entity);

                    // If they're not equal, then the decode succeeded in making a new symbol (ie &copy; --> ©)
                    if (!entity.Equals(decode))
                    {
                        type = "#text";
                        value = decode;
                        //TaskDialog.Show("Revit", "Before: [" + entity + "] After: [" + decode + "]");
                        goto switchStatement;
                    }

                    // Otherwise they were equal, ergo the decode failed (e.g. &fuck; --> &fuck;), so it's an unhandled node tag.

                    TaskDialog.Show("WARN", "[WARN] Unhandled Node Tag <" + node.Name + "><" + node.Value + ">");
                    //Console.WriteLine("[WARN] Unhandled Node Tag <" + node.Name + ">");
                    break;
            }

            return;

        theParsing:
            double myColStartX = colStartX + indentations * INDENT;

            foreach (XmlNode child in node.ChildNodes)
            {
                Parse(child, ref myColStartX, ref colEndX, ref colStartY, ref relStartX, ref relStartY, bold, italic, underline, strike, textType, bulletStyle, bulletCount, bulletLevel, super, sub, 0, 0);
            }

            if (newlineAfter)
                Newline(ref colStartX, ref colEndX, ref colStartY, ref relStartX, ref relStartY, textType);

            return;

        }

        private bool fixTableWidths(double noteWidth)
        {
            printTable(0);

            //confirm that the table is valid (some html tables get really wonky and difficult to render, Ex: row of completely vertically merged cells)
            double numColumns = table[0].Count;

            for(int i = 0; i < table.Count; i++)
            {
                if(table[i].Count != numColumns) //if we have a jagged table
                {
                    //ABORT
                    MainRevitProgram.ShowDialog("Error", "The table is constructed invalidly. This may be due to a whole row consisting only of merged cells.");
                    return false;
                }
            }

            double[] colWidth = new double[table[0].Count];
            DebugHandler.println("RTP", "viewscale: " + viewScale);
            double defaultWidth = viewScale/100.0; //rough approximation for a default width
            for(int i = 0; i < colWidth.Length; i++)
            {
                colWidth[i] = defaultWidth;
            }

            List<Cell> cellsByColSpan = new List<Cell>(); //create a new list of cells that is a copy of the original
            for (int i = 0; i < tableCells.Count; i++)
            {
                cellsByColSpan.Add(tableCells[i]);
            }
            cellsByColSpan = cellsByColSpan.OrderBy(x => (x.colNumEnd - x.colNumStart)).ToList(); //order it by colspan (ascending)

            foreach (Cell cell in cellsByColSpan) //start setting values for the widths of each column
            {
                if (cell.colNumStart == cell.colNumEnd) //deal with single colspanned cells
                {
                    colWidth[cell.colNumStart] = Math.Max(cell.colEnd - cell.colStart, colWidth[cell.colNumStart]);
                }
                else //deal with all the other merged crap
                {
                    double remainingWidth = cell.colEnd - cell.colStart;
                    int indexOfUnknown = cell.colNumStart; //a variable that tracks the last index where there was no width found from colWidth array
                    for(int i = cell.colNumStart; i <= cell.colNumEnd; i++) //sequentially deduce the possible values of each column width
                    {
                        if(colWidth[i] == defaultWidth) //if no width was explicitly set for this column
                        {
                            indexOfUnknown = i;
                        }
                        else
                        {
                            remainingWidth -= colWidth[i];
                        }
                    }
                    if(remainingWidth > colWidth[indexOfUnknown]) //biggest width will win out
                    {
                        colWidth[indexOfUnknown] = remainingWidth;
                    }
                }
            }

            string build = "";
            for(int i = 0; i < colWidth.Length; i++)
            {
                build += colWidth[i] + " ";
            }

            //DebugHandler.println("RTP", build);

            double totalWidth = 0;
            for (int i = 0; i < colWidth.Length; i++)
            {
                totalWidth += colWidth[i];
            }
            double scaleValue = noteWidth / totalWidth;
            if(scaleValue < 1) //if totalwidth is bigger than the width of the note, scale columns down
            {
                for(int i = 0; i < colWidth.Length; i++)
                {
                    colWidth[i] *= scaleValue;
                }
            }

            //reposition every table cell
            double startX = tableCells[0].colStart;
            foreach(Cell cell in tableCells)
            {
                double width = 0;
                double xDisplacement = 0;
                for (int i = 0; i < cell.colNumStart; i++ )
                {
                    xDisplacement += colWidth[i];
                }
                for (int i = cell.colNumStart; i <= cell.colNumEnd; i++)
                {
                    width += colWidth[i];
                }

                cell.colStart = startX + xDisplacement;
                cell.colEnd = cell.colStart + width;
            }

            return true;

        }

        private const double KERNING_SUBTRACT_MULTIPLIER = 0.15;     // Total hack; this should in theory be 1

        /// <summary>
        /// Figures out how much space to subtract from the end of the last placed text in order to have roughly correct kerning
        /// when placing the next line
        /// </summary>
        /// <param name="lastPlacedText"></param>
        /// <param name="curLine"></param>
        /// <param name="textType"></param>
        /// <returns></returns>
        private double KerningSubtract(string lastPlacedText, string curLine, TextNoteType textType)
        {
            //double summedLength = TextTools.stringWidth(lastPlacedText, textType, false, viewScale)
            //                        + TextTools.stringWidth(curLine, textType, false, viewScale);
            //double actualLength = TextTools.stringWidth(lastPlacedText + curLine, textType, false, viewScale);
            //double subtract = summedLength - actualLength;
            ////TaskDialog.Show("Rich Text Editor", "Subtracting [" + subtract + "] Based on ["+lastPlacedText+"] ["+curLine+"]");
            //return subtract * KERNING_SUBTRACT_MULTIPLIER;
            return 0;
        }

        // Gets alpha bullets
        // ie a,b,c,...,y,z,aa,ab,...,zy,zz,aaa,...
        private static String GetAlpha(int num)
        {
            String result = "";
            while (num > 0)
            {
                num--; // 1 => a, not 0 => a
                int remainder = num % 26;
                char digit = (char)(remainder + 97);
                result = digit + result;
                num = (num - remainder) / 26;
            }

            return result;
        }

        // Gets roman numeral bullets
        private static string GetRoman(int number)
        {
            var romanNumerals = new string[][]
        {
            new string[]{"", "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX"}, // ones
            new string[]{"", "X", "XX", "XXX", "XL", "L", "LX", "LXX", "LXXX", "XC"}, // tens
            new string[]{"", "C", "CC", "CCC", "CD", "D", "DC", "DCC", "DCCC", "CM"}, // hundreds
            new string[]{"", "M", "MM", "MMM"} // thousands
        };

            // split integer string into array and reverse array
            var intArr = number.ToString().Reverse().ToArray();
            var len = intArr.Length;
            var romanNumeral = "";
            var i = len;

            // starting with the highest place (for 3046, it would be the thousands
            // place, or 3), get the roman numeral representation for that place
            // and add it to the final roman numeral string
            while (i-- > 0)
            {
                romanNumeral += romanNumerals[i][Int32.Parse(intArr[i].ToString())];
            }

            return romanNumeral;
        }

        // Gets the appropriate bullet symbol for a style and number
        private string GetBullet(BulletStyle bulletStyle, int bulletCount)
        {
            string SYMBOL_PREFIX = " ";
            string HACK = "";      // TODO MAKE THIS HACK BETTER (I don't think this is used anymore with new subscript, superscript handling)
            switch (bulletStyle)
            {
                case BulletStyle.NUMBER:
                    return bulletCount + ".";
                
                case BulletStyle.CIRCLED_NUMBER:
					// Just spit out the number
					// Actually circling it is handled by the custom bullet type
                    return bulletCount + "";

                case BulletStyle.LOWER_ALPHA:
                    return GetAlpha(bulletCount).ToLower() + ".";

                case BulletStyle.UPPER_ALPHA:
                    return GetAlpha(bulletCount).ToUpper() + ".";

                case BulletStyle.LOWER_ROMAN:
                    return GetRoman(bulletCount).ToLower() + ".";

                case BulletStyle.UPPER_ROMAN:
                    return GetRoman(bulletCount).ToUpper() + ".";

                case BulletStyle.BLACK_CIRCLE:
                    return SYMBOL_PREFIX + "●";

                case BulletStyle.WHITE_CIRCLE:
                    return SYMBOL_PREFIX + "○";

                case BulletStyle.SQUARE:
                    return SYMBOL_PREFIX + "■";
            }

            throw new Exception("INVALID BULLET STYLE " + bulletStyle.ToString() + " " + bulletCount);
        }

        // Returns the corresponding bullet style for a UL or OL node of level 'level'
        private BulletStyle MyBulletStyle(XmlNode node, int level)
        {
            if (node.Name.Equals("ul"))
            {
                foreach (XmlAttribute att in node.Attributes)
                {
                    if (att.Name.Equals("style"))
                    {
                        if (att.Value.Equals("list-style-type: circle;"))
                            return BulletStyle.WHITE_CIRCLE;
                        else if (att.Value.Equals("list-style-type: disc;"))
                            return BulletStyle.BLACK_CIRCLE;
                        else if (att.Value.Equals("list-style-type: square;"))
                            return BulletStyle.SQUARE;
                    }
                }

                switch (level)
                {
                    case 1:
                        return BulletStyle.BLACK_CIRCLE;
                    case 2:
                        return BulletStyle.WHITE_CIRCLE;
                    case 3:
                    default:
                        return BulletStyle.SQUARE;
                }
            }
            else if (node.Name.Equals("ol"))
            {
                BulletStyle bulletReturn = BulletStyle.NUMBER;
                foreach (XmlAttribute att in node.Attributes)
                {
                    if (att.Name.Equals("style"))
                    {
                        if (att.Value.Equals("list-style-type: lower-alpha;"))
                            bulletReturn = BulletStyle.LOWER_ALPHA;
                        else if (att.Value.Equals("list-style-type: lower-greek;"))
                            bulletReturn = BulletStyle.CIRCLED_NUMBER;
                        else if (att.Value.Equals("list-style-type: lower-roman;"))
                            bulletReturn = BulletStyle.LOWER_ROMAN;
                        else if (att.Value.Equals("list-style-type: lower-roman;"))
                            bulletReturn = BulletStyle.LOWER_ROMAN;
                        else if (att.Value.Equals("list-style-type: upper-alpha;"))
                            bulletReturn = BulletStyle.UPPER_ALPHA;
                        else if (att.Value.Equals("list-style-type: upper-roman;"))
                            bulletReturn = BulletStyle.UPPER_ROMAN;
                    }
                    if(att.Name.Equals("class"))
                    {
                        if (att.Value.Equals("customBulletCircle"))
                            bulletReturn = BulletStyle.CIRCLED_NUMBER;
                    }
                }
                return bulletReturn;
            }
            else
                throw new Exception(node.Name + " IS NOT A LIST STYLE");
        }

        private void printTable(int value) //prints out the table for debugging (value: corresponds with 0: width or 1: height)
        {
            string print = "";
            print += System.Environment.NewLine;
            for(int i = 0; i < table.Count; i++)
            {
                for(int j = 0; j < table[i].Count; j++)
                {
                    if(value == 0)
                    {
                        if(table[i][j] != null)
                            print += ((int)table[i][j].width).ToString(" 00;-00") + " ";
                        else
                            print += " Na ";   
                    }
                    else if(value == 1)
                    {
                        if (table[i][j] != null)
                            print += ((int)table[i][j].yEnd).ToString(" 00;-00") + " ";
                        else
                            print += " Na ";   

                    }
                    
                }
                print += System.Environment.NewLine;
            }
            
            DebugHandler.println("RTP", print);
        }

        private double MakeTextNote(string textString, double X, double Y, TextNoteType textType, TextTools.TextScriptType textScript)
        {
            double width = 0;
            if (!textString.Trim().Equals(""))
                width = ch.requestDrawText(textString, X, Y, textType, textScript);

            lastPlacedText = textString;

            if (largestType == null)
                largestType = textType;
            else if (TextTools.textHeight(textType) > TextTools.textHeight(largestType))
                largestType = textType;

            return width;
        }

        private void MakeAnnotationSymbol(string id, double relX, double relY, string bulletChar, double height, double relYforCol)
        {
            ch.requestDrawAnnotation(id, relX, relY, bulletChar, height, relYforCol);
        }

        private void Newline(ref double colStartX, ref double colEndX, ref double colStartY, ref double relStartX, ref double relStartY, TextNoteType textType)
        {
            relStartX = 0;

            double height = TextTools.textHeight(textType);
            if (largestType != null)
                height = TextTools.textHeight(largestType);

            relStartY += height * 1.5 * viewScale;      // The 1.5 is a magic number which gives us very close to Revit default line spacing

            largestType = null;
        }


    }

    class Cell
    {
        public double width;
        public double colStart; //the x position of the start of the cell
        public double colEnd; //the x position of the end of the cell
        public double yStart; //the y position of the start of the cell
        public double yEnd; //the y position of the end of the cell
        public int colNumStart; //the index of the start of the cell for colspan (where it lies in the 2d array called table)
        public int colNumEnd; //the index of the end of the cell for colspan
        public int rowNumStart;
        public int rowNumEnd;
        public XmlNodeList nodes;

        public Cell(double width)
        {
            this.width = width;
        }

        public Cell(int colNumStart, int colNumEnd, int rowNumStart, int rowNumEnd)
        {
            this.colNumStart = colNumStart;
            this.colNumEnd = colNumEnd;
            this.rowNumStart = rowNumStart;
            this.rowNumEnd = rowNumEnd;
        }

        public Cell()
        {
            this.colStart = 0;
            this.colEnd = 0;
            this.yStart = 0;
            this.yEnd = 0;
        }

        public void setValues(double colStart, double colEnd, double yStart)
        {
            this.colStart = colStart;
            this.colEnd = colEnd;
            this.yStart = yStart;
        }

        public void setYEnd(double yEnd)
        {
            this.yEnd = yEnd;
        }

        public string toString()
        {
            return " Column Start: " + colNumStart + " Column End: " + colNumEnd + " Row Start: " + rowNumStart + " Row End: " + rowNumEnd
                    + " relx: " + colStart + " relxEnd: " + colEnd + " rely: " + yStart + " relyEnd: " + yEnd;
        }
    }
}
