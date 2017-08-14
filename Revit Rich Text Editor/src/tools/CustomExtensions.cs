using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CTEK_Rich_Text_Editor
{
    public static class CustomExtensions
    {
        /**
         * This is a replacement for get_Parameter and LookupParameter so we can use them in 2014-2016
         */
        public static Parameter LookupParameterCTEK(this Element el, BuiltInParameter builtInParam)//string paramName)
        {
            string paramName = LabelUtils.GetLabelFor(builtInParam);

            ParameterSet set = el.Parameters;
            foreach (Parameter p in set)
            {
                if (p.Definition.Name.Equals(paramName))
                    return p;
            }

            return null;
        }

    }
}
