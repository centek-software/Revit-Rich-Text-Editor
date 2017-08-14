using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCExtensibleStorageExtension;
using VCExtensibleStorageExtension.ElementExtensions;

namespace CTEK_Rich_Text_Editor
{
    class SelectionTools
    {
        /// <summary>
        /// Gets a Rich Text Note. If the user has already selected 1 note, it uses that. Otherwise, prompt.
        /// If the user cancels the prompt, return null.
        /// </summary>
        /// <param name="uiApp"></param>
        /// <param name="checkout">Should we check the note out? You absolutely must if you are going to edit/delete the note.</param>
        /// <param name="message">The message to put in the status bar</param>
        /// <param name="acceptSelection">Should we just return the current selection if one exists?</param>
        /// <returns></returns>
        public static ElementId SelectNote(UIApplication uiApp, bool checkout = true, string message = "Select the note", bool acceptSelection = true)
        {
            UIDocument uidoc = uiApp.ActiveUIDocument;
            Document doc = uidoc.Document;

            Selection selection = uidoc.Selection;
            ICollection<ElementId> collection = new List<ElementId>();

            foreach (ElementId eid in selection.GetElementIds())
            {
                Element e = uidoc.Document.GetElement(eid);

                MasterSchema ms = e.GetEntity<MasterSchema>();

                if (ms != null)
                    collection.Add(eid);
            }

            if (acceptSelection && collection.Count == 1)
            {
                // There will only be one ElementId in the collection
                foreach (ElementId eid in collection)
                    return Checkout(doc, eid);
            }
            else
            {
                // Select a rich text note

                Reference r2 = null;
                try
                {
                    r2 = uidoc.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Element, new RTNotesFilter(), message);
                }
                catch (Exception)
                {
                    // User canceled operation
                    return null;
                }

                Element e = uidoc.Document.GetElement(r2.ElementId);

                MasterSchema ms = e.GetEntity<MasterSchema>();

                return fixIfNotExisting(uidoc, Checkout(doc, e.Id));
            }

            // This will never happen
            return null;
        }

        /// <summary>
        /// If the user has already selected note(s), return all of them.
        /// If the user has not selected any yet, prompt them to select 1
        /// If the user cancels, return null
        /// </summary>
        /// <param name="uiApp"></param>
        /// <param name="checkout">Should we check the notes out? You absolutely must if you are going to edit/delete the notes.</param>
        /// <param name="message">The message to put in the status bar</param>
        /// <returns></returns>
        public static ICollection<ElementId> SelectNotes(UIApplication uiApp, bool checkout = true, string message = "Select the note")
        {
            UIDocument uidoc = uiApp.ActiveUIDocument;
            Document doc = uidoc.Document;
            Selection selection = uidoc.Selection;

            ICollection<ElementId> collection = new List<ElementId>();

            foreach (ElementId eid in selection.GetElementIds())
            {
                Element e = uidoc.Document.GetElement(eid);

                MasterSchema ms = e.GetEntity<MasterSchema>();

                if (ms != null)
                {
                    if (checkout)
                    {
                        if (Checkout(doc, eid) != null)
                            collection.Add(eid);
                    }
                    else
                        collection.Add(eid);
                }

            }

            if (collection.Count == 0)
            {
                Reference r2 = null;
                try
                {
                    r2 = uidoc.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Element, new RTNotesFilter(), "Pick the note");
                }
                catch (Exception)
                {
                    return null;
                }

                Element e = uidoc.Document.GetElement(r2.ElementId);

                if (Checkout(doc, r2.ElementId) != null)
                    collection.Add(r2.ElementId);
                else
                    return null;
            }

            return fixIfNotExisting(uidoc, collection);
            
        }

        private static ICollection<ElementId> fixIfNotExisting(UIDocument uidoc, ICollection<ElementId> collection)
        {
            foreach (ElementId eid in collection)
                fixIfNotExisting(uidoc, eid);

            return collection;
        }

        /// <summary>
        /// Adds default bullet content if it wasn't already there
        /// </summary>
        /// <param name="uidoc"></param>
        /// <param name="eid"></param>
        /// <returns></returns>
        private static ElementId fixIfNotExisting(UIDocument uidoc, ElementId eid)
        {
            Element e = uidoc.Document.GetElement(eid);
            MasterSchema ms = e.GetEntity<MasterSchema>();

            if (!bulletIdExists(ms.html)) //add bullet id if it doesn't exist
            {
                ms.html = sanitizeBulletComments(ms.html);
                ms.html = "<!-- BulletFamily: " + "DefaultBulletId" + "-->" + ms.html;

                using (Transaction tr = new Transaction(uidoc.Document, "Set entity"))
                {
                    tr.Start();

                    e.SetEntity(ms);

                    tr.Commit();
                }
            }
            else //dont mess with bullet id, just clean up other comments if they exist
            {
                string html = ms.html;
                int index = html.IndexOf(FontForm.endBulletComment) + FontForm.endBulletComment.Length; //index of the end of the ending comment
                ms.html = html.Substring(0, index) + sanitizeBulletComments(html.Substring(index));

                using (Transaction tr = new Transaction(uidoc.Document, "Set entity"))
                {
                    tr.Start();

                    e.SetEntity(ms);

                    tr.Commit();
                }
            }
            return eid;
        }

        //check if there is a bullet id in the top of the text note
        private static bool bulletIdExists(string html)
        {
            if(html.Substring(0, "<!-- BulletFamily: ".Length).Equals("<!-- BulletFamily: "))
            {
                return true;
            }
            return false;
        }

        private static string sanitizeBulletComments(string html) //obliterates other bullet id comments
        {
            string sanitized = html;
            int index;
            index = html.IndexOf(FontForm.startBulletComment);
            while(index >= 0)
            {
                //delete comment
                sanitized = html.Substring(0, index);
                int indexEnd = html.IndexOf(FontForm.endBulletComment) + FontForm.endBulletComment.Length;
                sanitized += html.Substring(indexEnd);
                html = sanitized;
                index = sanitized.IndexOf(FontForm.startBulletComment);
            }
            return sanitized;
        }

        /// <summary>
        /// Checks out an element
        /// </summary>
        /// <param name="eid">The ElementId of the element to check out</param>
        /// <returns>eid if there was a success, and null if not</returns>
        private static ElementId Checkout(Document doc, ElementId eid)
        {
            if (eid == null)
                return null;

            // No need to checkout if not workshared
            if (!doc.IsWorkshared)
                return eid;

            ICollection<ElementId> checkedOutIds = WorksharingUtils.CheckoutElements(doc, new ElementId[] { eid });

            bool checkedOutSuccessfully = checkedOutIds.Contains(eid);

            if (!checkedOutSuccessfully)
            {
                string owner;
                WorksharingUtils.GetCheckoutStatus(doc, eid, out owner);

                if (owner.Equals(String.Empty))
                {
                    TaskDialog.Show("Element is not up to date", "Cannot edit the text note until you Reload Latest.");
                }
                else
                {
                    TaskDialog.Show("Element is not owned by you", "Cannot edit the text note until '"
                        + owner + "' resaves the element to central and relinquishes it and you Reload Latest.");
                }

                return null;
            }

            // If element is updated in central or deleted in central, it is not editable
            ModelUpdatesStatus updatesStatus = WorksharingUtils.GetModelUpdatesStatus(doc, eid);
            if (updatesStatus == ModelUpdatesStatus.DeletedInCentral || updatesStatus == ModelUpdatesStatus.UpdatedInCentral)
            {
                //FailureMessage fm = new FailureMessage(BuiltInFailures.EditingFailures.OwnElementsOutOfDate);
                TaskDialog.Show("Element is not up to date", "Cannot edit the text note until you Reload Latest.");
                return null;
            }

            return eid;
        }
    }
}
