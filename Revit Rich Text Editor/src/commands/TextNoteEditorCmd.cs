//   Revit Rich Text Editor
//   Copyright (C) 2014 Centek Engineering

using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using CefSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using VCExtensibleStorageExtension;
using VCExtensibleStorageExtension.ElementExtensions;

namespace CTEK_Rich_Text_Editor
{
    [TransactionAttribute(TransactionMode.Manual)]
    [RegenerationAttribute(RegenerationOption.Manual)]
    class TextNoteEditorCmd : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiApp = commandData.Application;


            UIDocument uidoc = uiApp.ActiveUIDocument;

            // Load up the editor
            using (TransactionGroup transGroup = new TransactionGroup(uidoc.Document, "Edit rich text note"))
            {
                transGroup.Start();
                //===

                ElementId eid = SelectionTools.SelectNote(uiApp);
                if (eid == null)
                    return Result.Cancelled;

                Element e = uidoc.Document.GetElement(eid);
                MasterSchema ms = e.GetEntity<MasterSchema>();

                string html = ms.html;

                TestForm(uiApp, html, e, MainRevitProgram.activationHandler);

                //===
                transGroup.Assimilate();
            }

            return Result.Succeeded;
        }

        public void TestForm(UIApplication uiApp, string html, Element note, ActivationHandler activationHandler)
        {
            //Access a new instance of the Form1 created earlier and call it 'form'
            //using (var form = new MainFormIE(uiApp, html, note, activationHandler))
            //using (var form = new MainFormCEF(uiApp, html, note, activationHandler))
            //{
            //    //use ShowDialog to show the form as a modal dialog box. 
            //    form.ShowDialog();
            //}

            MainRevitProgram.mainForm = new MainFormCEF();
            MainRevitProgram.mainForm.PrepareForRelaunch(uiApp, html, note, activationHandler);
            MainRevitProgram.mainForm.ShowDialog();

            // TODO: Improve speed by using just Show() instead and then using the idling event
        }

    }
}
