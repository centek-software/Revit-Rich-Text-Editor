//   Revit Rich Text Editor
//   Copyright (C) 2014 Centek Engineering

using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCExtensibleStorageExtension;
using VCExtensibleStorageExtension.ElementExtensions;

namespace CTEK_Rich_Text_Editor
{
    class TextTools
    {
        public enum TextStyle { P, H1, H2, H3, H4, H5 };

        public enum TextScriptType
        {
            REGULAR,
            SUBSCRIPT,
            SUPERSCRIPT
        };
        private const float REVIT_FONT_DPI = 72.0f;  // 72.0 NOT 96.0f;
       // private const float REVIT_2017_FONT_KLUDGE_PERCENT = 0.97f;     // The percent bigger fonts in 2017 is vs <= 2016

        private UIDocument uidoc;
        private Dictionary<string, TextNoteType> tntCache = new Dictionary<string, TextNoteType>();
        private Element note;
        private Dictionary<TextStyle, DefaultFontStyle> defaultFontStyles = new Dictionary<TextStyle, DefaultFontStyle>();

        static Schema defaultStyles;

        public TextTools(UIDocument uidoc, Element note)
        {
            this.uidoc = uidoc;
            this.note = note;

            string temp = "Rich Text Default ";
            defaultFontStyles.Add(TextStyle.P, new DefaultFontStyle(0.0078125, temp + "P"));
            defaultFontStyles.Add(TextStyle.H1, new DefaultFontStyle(0.0139973958, temp + "H1"));
            defaultFontStyles.Add(TextStyle.H2, new DefaultFontStyle(0.01276, temp + "H2"));
            defaultFontStyles.Add(TextStyle.H3, new DefaultFontStyle(0.01152, temp + "H3"));
            defaultFontStyles.Add(TextStyle.H4, new DefaultFontStyle(0.01028, temp + "H4"));
            defaultFontStyles.Add(TextStyle.H5, new DefaultFontStyle(0.00904, temp + "H5"));
        }

        public TextNoteType getType(TextStyle style)
        {
            if (note == null)
                return DefaultTextNoteType(style);

            MasterSchema ms = note.GetEntity<MasterSchema>();

            ElementId ttId = null;
            switch (style)
            {
                case TextStyle.P:
                    ttId = ms.fontP;
                    break;

                case TextStyle.H1:
                    ttId = ms.fontH1;
                    break;

                case TextStyle.H2:
                    ttId = ms.fontH2;
                    break;

                case TextStyle.H3:
                    ttId = ms.fontH3;
                    break;

                case TextStyle.H4:
                    ttId = ms.fontH4;
                    break;

                case TextStyle.H5:
                    ttId = ms.fontH5;
                    break;
            }

            if (ttId == null || ttId.IntegerValue == ElementId.InvalidElementId.IntegerValue)
                return DefaultTextNoteType(style);

            TextNoteType tnt = uidoc.Document.GetElement(ttId) as TextNoteType;

            if (tnt == null)
                return DefaultTextNoteType(style);

            return tnt;
        }

        /// <summary>
        /// MUST BE CALLED OUTSIDE TRANSACTION
        /// </summary>
        /// <param name="size"></param>
        /// <param name="styleName"></param>
        /// <param name="font"></param>
        /// <param name="widthFactor"></param>
        /// <returns></returns>
        public TextNoteType getType(double size, string styleName, string font = "Arial", double widthFactor = 1)
        {
            return getType(ArbitraryTextNoteType(uidoc), false, false, false, size, font, styleName, widthFactor);
        }

        /// <summary>
        /// MUST BE CALLED OUTSIDE TRANSACTION
        /// </summary>
        /// <param name="baseFont"></param>
        /// <param name="bold"></param>
        /// <param name="italic"></param>
        /// <param name="underline"></param>
        /// <param name="size"></param>
        /// <param name="font"></param>
        /// <param name="styleName"></param>
        /// <param name="widthFactor"></param>
        /// <returns></returns>
        public TextNoteType getType(TextNoteType baseFont, bool bold, bool italic, bool underline, double size = -1, string font = null, string styleName = null, double widthFactor = -1)
        {
            if (size < 0)
                size = baseFont.LookupParameterCTEK(BuiltInParameter.TEXT_SIZE).AsDouble();

            if (font == null)
                font = baseFont.LookupParameterCTEK(BuiltInParameter.TEXT_FONT).AsString();

            string title = "«" + font.ToUpperInvariant() + " " + Math.Round(size, 10) + (bold ? " BOLD" : "") + (italic ? " ITALIC" : "") + (underline ? " ULINE" : "") + "»";
            title = title.Replace(' ', '_');

            if (styleName != null)
                title = styleName;

            if (tntCache.ContainsKey(title))
                return tntCache[title];

            // We do not have this type cached for this session - recreate it

            Document doc = uidoc.Document;

            // Get access to all the TextNote Elements
            FilteredElementCollector collectorUsed = new FilteredElementCollector(doc);

            ICollection<ElementId> textNotes = collectorUsed.OfClass(typeof(TextNoteType)).ToElementIds();

            TextNoteType type = null;
            TextNoteType temp = null;
            foreach (ElementId textNoteid in textNotes)
            {
                TextNoteType tnt = doc.GetElement(textNoteid) as TextNoteType;

                if (tnt.Name.Equals(title))
                {
                    type = tnt;
                    break;
                }
                temp = tnt;
            }

            using (Transaction tr = new Transaction(uidoc.Document, "Creating type"))
            {
                tr.Start();

                if (type == null)
                    type = temp.Duplicate(title) as TextNoteType;

                if (type == null)
                    throw new Exception("FAILED TO FIND OR CREATE TNT: " + title);

                type.LookupParameterCTEK(BuiltInParameter.TEXT_COLOR).Set(0);     // 0 = black
                type.LookupParameterCTEK(BuiltInParameter.TEXT_BACKGROUND).Set(1);
                type.LookupParameterCTEK(BuiltInParameter.TEXT_BOX_VISIBILITY).Set(0);

                type.LookupParameterCTEK(BuiltInParameter.TEXT_FONT).Set(font);
                type.LookupParameterCTEK(BuiltInParameter.TEXT_SIZE).Set(size);

                type.LookupParameterCTEK(BuiltInParameter.TEXT_STYLE_BOLD).Set(bold ? 1 : 0);
                type.LookupParameterCTEK(BuiltInParameter.TEXT_STYLE_ITALIC).Set(italic ? 1 : 0);
                type.LookupParameterCTEK(BuiltInParameter.TEXT_STYLE_UNDERLINE).Set(underline ? 1 : 0);

                if (widthFactor > 0)
                    type.LookupParameterCTEK(BuiltInParameter.TEXT_WIDTH_SCALE).Set(widthFactor);
                else
                    type.LookupParameterCTEK(BuiltInParameter.TEXT_WIDTH_SCALE).Set(baseFont.LookupParameterCTEK(BuiltInParameter.TEXT_WIDTH_SCALE).AsDouble());

                tntCache.Add(title, type);

                tr.Commit();
            }
           

            return type;
        }

        public static double getSize(TextNoteType type)
        {
            return type.LookupParameterCTEK(BuiltInParameter.TEXT_SIZE).AsDouble();
        }

        /// <summary>
        /// Gets the string width in feet (view scale taken into consideration automatically here)
        /// </summary>
        /// <param name="textString"></param>
        /// <param name="textType"></param>
        /// <param name="includeBorder">This usually doesn't even matter</param>
        /// <param name="viewScale"></param>
        /// <returns></returns>
        public static double stringWidthApprox(UIDocument uidoc, string textString, TextNoteType textType, bool includeBorder, int viewScale)
        {
            double width = stringWidthInternal(textString, textType, includeBorder, viewScale);

           // double width = stringWidth2017(uidoc, textString, textType);

            //if (RevitVersionHandler.getRevitVersion() >= 2017)
              //  width *= REVIT_2017_FONT_KLUDGE_PERCENT;

            return width;
        }

        


        private static double stringWidthInternal(string textString, TextNoteType textType, bool includeBorder, int viewScale)
        {
            double stringWidthPx = systemStringWidth(textString, fontFrom(textType));
            double stringWidthIn = stringWidthPx / REVIT_FONT_DPI;
            double stringWidthFt = stringWidthIn / 12.0;

            Parameter paramTextWidthScale = textType.get_Parameter(BuiltInParameter.TEXT_WIDTH_SCALE);
            Parameter paramBorderSize = textType.get_Parameter(BuiltInParameter.LEADER_OFFSET_SHEET);
            double textBorder = paramBorderSize.AsDouble();
            double textWidthScale = paramTextWidthScale.AsDouble();

            if (!includeBorder)
                textBorder = 0;

            double lineWidth = ((stringWidthFt * textWidthScale) + (textBorder * 2.0)) * viewScale;

            return lineWidth;
        }


        private static double systemStringWidth(string text, Font font)
        {
            using (Graphics g = Graphics.FromHwnd(IntPtr.Zero))
            //using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

                StringFormat format = new System.Drawing.StringFormat();
                format.FormatFlags = StringFormatFlags.MeasureTrailingSpaces;

                RectangleF rect = new RectangleF(0, 0, float.MaxValue / 2.0f, float.MaxValue / 2.0f);
                CharacterRange[] ranges = { new CharacterRange(0, text.Length) };
                Region[] regions = new Region[1];

                format.SetMeasurableCharacterRanges(ranges);

                regions = g.MeasureCharacterRanges(text, font, rect, format);
                if (regions.Length < 1)
                    return 0;
                rect = regions[0].GetBounds(g);

                // The final DPI ratio multiply fixes problems with different DPI screens (ie if in the screen resolution thing you change to 125%)
                float dpiX = g.DpiX;
                return rect.Width * (96.0 / dpiX);
            }
        }


        /// <summary>
        /// Gets the text height as a double.
        /// NOTE: More often than not, this needs to be multiplied by the view scale and 1.5
        /// </summary>
        /// <param name="textType"></param>
        /// <returns></returns>
        public static double textHeight(TextNoteType textType)
        {
            Parameter paramTextSize = textType.get_Parameter(BuiltInParameter.TEXT_SIZE);

            double textHeight = paramTextSize.AsDouble();

            return textHeight;
        }

        public static Font fontFrom(TextNoteType textType)
        {
            Parameter paramTextFont = textType.get_Parameter(BuiltInParameter.TEXT_FONT);
            Parameter paramTextSize = textType.get_Parameter(BuiltInParameter.TEXT_SIZE);
            Parameter paramBorderSize = textType.get_Parameter(BuiltInParameter.LEADER_OFFSET_SHEET);
            Parameter paramTextBold = textType.get_Parameter(BuiltInParameter.TEXT_STYLE_BOLD);
            Parameter paramTextItalic = textType.get_Parameter(BuiltInParameter.TEXT_STYLE_ITALIC);
            Parameter paramTextUnderline = textType.get_Parameter(BuiltInParameter.TEXT_STYLE_UNDERLINE);
            Parameter paramTextWidthScale = textType.get_Parameter(BuiltInParameter.TEXT_WIDTH_SCALE);

            string fontName = paramTextFont.AsString();
            double textHeight = paramTextSize.AsDouble();
            bool textBold = (paramTextBold.AsInteger() == 1 ? true : false);
            bool textItalic = (paramTextItalic.AsInteger() == 1 ? true : false);
            bool textUnderline = (paramTextUnderline.AsInteger() == 1 ? true : false);
            double textBorder = paramBorderSize.AsDouble();
            double textWidthScale = paramTextWidthScale.AsDouble();

            FontStyle textStyle = FontStyle.Regular;

            if (textBold)
                textStyle |= FontStyle.Bold;

            if (textItalic)
                textStyle |= FontStyle.Italic;

            if (textUnderline)
                textStyle |= FontStyle.Underline;

            float fontHeightInch = (float) textHeight * 12.0f;

            float pointSize;
            if(RevitVersionHandler.getRevitVersion() >= 2017)
            {
                // Witchcraft
                pointSize = (float) ((int) ((textHeight * 12.0 * REVIT_FONT_DPI) + 1.1f) * 2.0) / 2.0f;
            }
            else
            {
                pointSize = (float) (textHeight * 12.0 * REVIT_FONT_DPI);
            }

            return new Font(fontName, pointSize, textStyle);
        }

        /// <summary>
        /// MUST BE CALLED OUTSIDE TRANSACTION
        /// </summary>
        /// <param name="textStyle"></param>
        /// <returns></returns>
        public TextNoteType DefaultTextNoteType(TextStyle textStyle)
        {
            DefaultFontStyle dfs = defaultFontStyles[textStyle];

            if (dfs.textNoteType == null)
            {
                // See if TextNoteType exists but is not cached
                List<TextNoteType> noteTypeList = new FilteredElementCollector(uidoc.Document).OfClass(typeof(TextNoteType)).Cast<TextNoteType>().ToList();

                foreach (TextNoteType tnt in noteTypeList)
                {
                    if (tnt.Name.Equals(dfs.name))
                    {
                        dfs.textNoteType = tnt;
                        return tnt;
                    }
                }

                // Create new TextNoteType
                dfs.textNoteType = getType(dfs.size, dfs.name);
            }

            return dfs.textNoteType;
        }

        public static TextNoteType ArbitraryTextNoteType(UIDocument uidoc)
        {
            List<TextNoteType> noteTypeList = new FilteredElementCollector(uidoc.Document).OfClass(typeof(TextNoteType)).Cast<TextNoteType>().ToList();

            return noteTypeList.ElementAt(0);
        }

        // NOTE: Some (but not all) have a FILE SEPARATOR character before them, as a *REALLY* hacky way of making them work in REVIT
        public static string attemptSuperscript(string value)
        {
            string result = "";
            foreach (char c in value)
            {
                switch (c)
                {
                    case '0':
                        result += "⁰";
                        break;

                    case '1':
                        result += "¹";
                        break;

                    case '2':
                        result += "²";
                        break;

                    case '3':
                        result += "³";
                        break;

                    case '4':
                        result += "⁴";
                        break;

                    case '5':
                        result += "⁵";
                        break;

                    case '6':
                        result += "⁶";
                        break;

                    case '7':
                        result += "⁷";
                        break;

                    case '8':
                        result += "⁸";
                        break;

                    case '9':
                        result += "⁹";
                        break;

                    case '+':
                        result += "⁺";
                        break;

                    case '-':
                        result += "⁻";
                        break;

                    case '=':
                        result += "⁼";
                        break;

                    case '(':
                        result += "⁽";
                        break;

                    case ')':
                        result += "⁾";
                        break;

                    default:
                        result += c;
                        break;
                }
            }
            return result;
        }

        public static string attemptSubscript(string value)
        {
            string result = "";
            foreach (char c in value)
            {
                switch (c)
                {
                    case '0':
                        result += "₀";
                        break;

                    case '1':
                        result += "₁";
                        break;

                    case '2':
                        result += "₂";
                        break;

                    case '3':
                        result += "₃";
                        break;

                    case '4':
                        result += "₄";
                        break;

                    case '5':
                        result += "₅";
                        break;

                    case '6':
                        result += "₆";
                        break;

                    case '7':
                        result += "₇";
                        break;

                    case '8':
                        result += "₈";
                        break;

                    case '9':
                        result += "₉";
                        break;

                    case '+':
                        result += "₊";
                        break;

                    case '-':
                        result += "₋";
                        break;

                    case '=':
                        result += "₌";
                        break;

                    case '(':
                        result += "₍";
                        break;

                    case ')':
                        result += "₎";
                        break;

                    default:
                        result += c;
                        break;
                }
            }
            return result;
        }

        public static void createSchema()
        {
            SchemaBuilder schemaBuilder = new SchemaBuilder(new Guid("b56f4483-ad79-48a3-92db-8923b0b58db5"));
            schemaBuilder.SetReadAccessLevel(AccessLevel.Public);
            schemaBuilder.SetWriteAccessLevel(AccessLevel.Vendor);
            schemaBuilder.SetVendorId("CTEK");
            schemaBuilder.AddSimpleField("Bullet", typeof(string));
            schemaBuilder.SetSchemaName("DefaultTextStyles");
            defaultStyles = schemaBuilder.Finish();
        }

        public static void setDefaults(Document doc, /*ElementId P, ElementId H1, ElementId H2, ElementId H3, ElementId H4, ElementId H5, */string bullet)
        {
            if(defaultStyles == null)
            {
                createSchema();
            }
            using (Transaction t = new Transaction(doc, "Set defaults"))
            {
                t.Start();
                DataStorage createdInfoStorage = DataStorage.Create(doc);

                Entity entity = new Entity(defaultStyles);

                entity.Set("Bullet", bullet);
                doc.ProjectInformation.SetEntity(entity);

                t.Commit();
            }

        }

        public static string readDefaults(Document doc)
        {
            if (defaultStyles == null)
            {
                createSchema();
            }
            Entity retrievedEntity = doc.ProjectInformation.GetEntity(defaultStyles);
            if(retrievedEntity != null && retrievedEntity.IsValid())
            {
                string retrievedData = retrievedEntity.Get<string>(defaultStyles.GetField("Bullet"));
                return retrievedData;
            }
            else
            {
                return "";
            }
            //FilteredElementCollector collector = new FilteredElementCollector(doc);

            //Element dataStorage = collector.OfClass(typeof(DataStorage)).Last();


            //if (dataStorage == null)
            //{
            //    DebugHandler.println("TT", "No Data Storage Found");

            //    return;
            //}

            //// Retrieve entity from the data storage

            //if (defaultStyles != null)
            //{
            //    Entity createdInfoEntity = dataStorage.GetEntity(defaultStyles);

            //    if (!createdInfoEntity.IsValid())
            //    {
            //        DebugHandler.println("TT", "Data storage doesn't have DefaultStylesSchema");

            //        return;
            //    }

            //    string bulletId = createdInfoEntity.Get<string>("Bullet");

            //    DebugHandler.println("TT", "Bullet ID: " + bulletId);
            //}
        }
    }

    class DefaultFontStyle
    {
        public double size;
        public string name;
        public string fontName;
        public TextNoteType textNoteType;

        public DefaultFontStyle(double size, string name, string fontName = "Arial")
        {
            this.size = size;
            this.name = name;
            this.fontName = fontName;
        }
    }
}
