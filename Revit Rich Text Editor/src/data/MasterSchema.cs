//   Revit Rich Text Editor
//   Copyright (C) 2014 Centek Engineering

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using VCExtensibleStorageExtension;
using VCExtensibleStorageExtension.Attributes;
using Autodesk.Revit.DB;

namespace CTEK_Rich_Text_Editor
{
    // == DO NOT CHANGE THIS FILE AT ALL OR WE BREAK BACKWARD COMPATIBILITY ==

    [Schema("cffd436d-f953-4133-8232-c2bf79fee533", "MasterSchema")]
    public class MasterSchema : IRevitEntity
    {
        // The HTML from TinyMCE, which is used to generate the note
        [Field]
        public string html { get; set; }

        // Fonts for p (ie default), h1, h2, h3, ...
        [Field]
        public ElementId fontP { get; set; }

        [Field]
        public ElementId fontH1 { get; set; }

        [Field]
        public ElementId fontH2 { get; set; }

        [Field]
        public ElementId fontH3 { get; set; }

        [Field]
        public ElementId fontH4 { get; set; }

        [Field]
        public ElementId fontH5 { get; set; }

        // Properties for the columns
        [Field(UnitType = UnitType.UT_Custom)]
        public double colHeight { get; set; }

        [Field(UnitType = UnitType.UT_Custom)]
        public double colWidth { get; set; }

        [Field(UnitType = UnitType.UT_Custom)]
        public double colSeparation { get; set; }

        // Lists of elements in the master group
        [Field]
        public List<ElementId> elementsWeMade { get; set; }         // Elements made from text in the editor

        // Info for deducing what the origin should be
        [Field]
        public ElementId sampleElement { get; set; }

        [Field(UnitType=UnitType.UT_Length)]
        public XYZ sampleElementDeltaOrigin { get; set; }

        // Info for the resize box
        [Field]
        public bool resizeBoxActivated { get; set; }

        [Field]
        public ElementId rbRight1 { get; set; }

        [Field]
        public ElementId rbRight2 { get; set; }

        [Field]
        public ElementId rbBottom { get; set; }
    }
}
