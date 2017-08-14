//   Revit Rich Text Editor
//   Copyright (C) 2014 Centek Engineering

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CTEK_Rich_Text_Editor
{
    class SpellCheckDictionary
    {
        public string name { get; private set; }
        public string displayName { get; private set; }
        public string path { get; private set; }

        public SpellCheckDictionary(string name, string displayName, string path)
        {
            this.name = name;
            this.displayName = displayName;
            this.path = path;
        }
    }
}
