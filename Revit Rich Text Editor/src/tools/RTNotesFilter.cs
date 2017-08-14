//   Revit Rich Text Editor
//   Copyright (C) 2014 Centek Engineering

using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VCExtensibleStorageExtension;
using VCExtensibleStorageExtension.ElementExtensions;

namespace CTEK_Rich_Text_Editor
{
    class RTNotesFilter : ISelectionFilter
    {
        Element except = null;

        public RTNotesFilter()
        {
        }

        public RTNotesFilter(Element except)
        {
            this.except = except;
        }

        public bool AllowElement(Element e)
        {
            if (except != null && e.Id.Equals(except.Id))
                return false;

            MasterSchema ms = e.GetEntity<MasterSchema>();
            return (ms != null);
        }

        public bool AllowReference(Reference r, XYZ p)
        {
            return false;
        }
    }
}
