//   Revit Rich Text Editor
//   Copyright (C) 2014 Centek Engineering

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CTEK_Rich_Text_Editor
{
    class FundamentalProps
    {
        public double colHeight { get; set; }
        public double colWidth { get; set; }
        public double colSep { get; set; }

        public FundamentalProps(double colHeight, double colWidth, double colSep)
        {
            this.colHeight = colHeight;
            this.colWidth = colWidth;
            this.colSep = colSep;
        }
    }
}
