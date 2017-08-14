using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CTEK_Rich_Text_Editor
{
    [TransactionAttribute(TransactionMode.Manual)]
    [RegenerationAttribute(RegenerationOption.Manual)]
    class TestCmd : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
             UIApplication uiApp = commandData.Application;

            UIDocument uidoc = uiApp.ActiveUIDocument;
            Document doc = uidoc.Document;

            FilteredElementCollector collector = new FilteredElementCollector(doc);

            collector.OfClass(typeof(FamilySymbol));

            List<AnnotationSymbolType> potato = (from e in collector.ToElements() where e is AnnotationSymbolType select e as AnnotationSymbolType).ToList<AnnotationSymbolType>();

            string s = "{";

            foreach (AnnotationSymbolType a in potato)
            {
                s += a.FamilyName + ", ";
            }
            s += "}";

            DebugHandler.println("Test", s);
            return Result.Succeeded;
        }
    }
}
