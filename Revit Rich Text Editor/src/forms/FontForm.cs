//   Revit Rich Text Editor
//   Copyright (C) 2014 Centek Engineering

using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using VCExtensibleStorageExtension;
using VCExtensibleStorageExtension.ElementExtensions;

namespace CTEK_Rich_Text_Editor
{
    /// <summary>
    /// Form for editing fonts
    /// </summary>
    public partial class FontForm : System.Windows.Forms.Form
    {
        private List<TextNoteType> tnts;
        private List<AnnotationSymbolType> annsts;
        private ICollection<ElementId> collection;
        private UIApplication uiapp;
        private UIDocument uidoc;
        public static string startBulletComment = "<!-- BulletFamily: "; //if you change this variable you have to change it also in TextNoteCreatorCmd.cs (this can't be static)
        public static string endBulletComment = "-->";

        public FontForm(UIApplication uiapp, ICollection<ElementId> collection)
        {
            InitializeComponent();

            this.collection = collection;
            this.uiapp = uiapp;
            this.uidoc = uiapp.ActiveUIDocument;

            Element master = null;

            foreach (ElementId eid in collection)
            {
                master = uidoc.Document.GetElement(eid);
                break;
            }

            MasterSchema ms = null;

            if (master != null)
                ms = master.GetEntity<MasterSchema>();

            // Populate the combo boxes with all the text note types and last combo box with annotation symbol types
            tnts = getTextNoteTypes(uidoc);
            annsts = getAnnotationSymbolTypes(uidoc);

            TextNoteType fontP = uidoc.Document.GetElement(ms.fontP) as TextNoteType;

            int id = 0;
            foreach (TextNoteType tnt in tnts)
            {
                comboP.Items.Add(tnt.Name);
                comboH1.Items.Add(tnt.Name);
                comboH2.Items.Add(tnt.Name);
                comboH3.Items.Add(tnt.Name);
                comboH4.Items.Add(tnt.Name);
                comboH5.Items.Add(tnt.Name);

                // If this is the text note type for any of the boxes, set this as the selected index
                if (ms.fontP.Equals(tnt.Id)) 
                    comboP.SelectedIndex = id;                           
                if (ms.fontH1.Equals(tnt.Id))                            
                    comboH1.SelectedIndex = id;                          
                if (ms.fontH2.Equals(tnt.Id))                            
                    comboH2.SelectedIndex = id;                          
                if (ms.fontH3.Equals(tnt.Id))                            
                    comboH3.SelectedIndex = id;                          
                if (ms.fontH4.Equals(tnt.Id))                            
                    comboH4.SelectedIndex = id;                          
                if (ms.fontH5.Equals(tnt.Id))                            
                    comboH5.SelectedIndex = id;                          
                                                                         
                id++;                                                    
            }

            for (int i = 0; i < tnts.Count; i++)
            {
                switch (tnts[i].Name)
                {
                    case "Rich Text Default P":
                        if(comboP.SelectedIndex < 0)
                        {
                            comboP.SelectedIndex = i;
                        }
                        break;
                    case "Rich Text Default H1":
                        if (comboH1.SelectedIndex < 0)
                        {
                            comboH1.SelectedIndex = i;
                        }
                        break;
                    case "Rich Text Default H2":
                        if (comboH2.SelectedIndex < 0)
                        {
                            comboH2.SelectedIndex = i;
                        }
                        break;
                    case "Rich Text Default H3":
                        if (comboH3.SelectedIndex < 0)
                        {
                            comboH3.SelectedIndex = i;
                        }
                        break;
                    case "Rich Text Default H4":
                        if (comboH4.SelectedIndex < 0)
                        {
                            comboH4.SelectedIndex = i;
                        }
                        break;
                    case "Rich Text Default H5":
                        if (comboH5.SelectedIndex < 0)
                        {
                            comboH5.SelectedIndex = i;
                        }
                        break;

                }
            }
                                                
                                                                         
            if(comboP.SelectedIndex < 0)
                comboP.SelectedIndex = comboP.Items.Count - 1;                     
            
            if (comboH1.SelectedIndex < 0)
                comboH1.SelectedIndex = comboH1.Items.Count - 1;                    
            
            if (comboH2.SelectedIndex < 0)
                comboH2.SelectedIndex = comboH2.Items.Count - 1;                    
            
            if (comboH3.SelectedIndex < 0)
                comboH3.SelectedIndex = comboH3.Items.Count - 1;
            
            if (comboH4.SelectedIndex < 0)
                comboH4.SelectedIndex = comboH4.Items.Count - 1;
            
            if (comboH5.SelectedIndex < 0)
                comboH5.SelectedIndex = comboH5.Items.Count - 1;

            if (annsts != null)
            {
                id = 0;
                foreach (AnnotationSymbolType anns in annsts)
                {
                    if (anns != null)
                    {
                        comboBullet.Items.Add(anns.Name); //add it

                        //check if annotation symbol type is already selected, then set as selected index
                        if (getCustomBulletUniqueId(ms.html).Equals(anns.UniqueId))
                        {
                            comboBullet.SelectedIndex = id;
                        }
                        id++;
                    }
                }
            }

            comboBullet.Items.Add("Default");
            if (comboBullet.SelectedIndex < 0)
                comboBullet.SelectedIndex = comboBullet.Items.Count - 1;
        }


        /// <summary>
        /// Gets all the TextNoteTypes (ie fonts)
        /// </summary>
        public List<TextNoteType> getTextNoteTypes(UIDocument uidoc)
        {
            return new FilteredElementCollector(uidoc.Document).OfClass(typeof(TextNoteType)).Cast<TextNoteType>().ToList();
        }

        public List<AnnotationSymbolType> getAnnotationSymbolTypes(UIDocument uidoc)
        {
            Document doc = uidoc.Document;
            FilteredElementCollector collector = new FilteredElementCollector(doc);

            collector.OfClass(typeof(FamilySymbol));
            return (from e in collector.ToElements() where e is AnnotationSymbolType select e as AnnotationSymbolType).ToList<AnnotationSymbolType>();
            
        }

        private string getCustomBulletUniqueId(string html) //use the html from ms to find the comment with bullet id and return it
        {
            if (html.Substring(0, startBulletComment.Length).Equals(startBulletComment)) //check to see if comment exists
            {
                //find comment value
                string returnId = "";
                string trackEnd = html.Substring(startBulletComment.Length + 1, 3); //track next 3 characters to anticipate the end comment being reached
                for(int i = startBulletComment.Length; i < html.Length; i++)
                {
                    if(trackEnd.Equals(endBulletComment))
                    {
                        break;
                    }
                    returnId += html[i];
                    trackEnd = html.Substring(i + 1, 3);
                }
                return returnId;
            }
            else
            {
                return "";
            }
        }

        private void btnApplySelected_Click(object sender, EventArgs e)
        {
            // For each note we had had selected
            foreach (ElementId el_id in collection)
            {
                Element el = uidoc.Document.GetElement(el_id);

                if (el == null)
                {
                    DebugHandler.println("FF", "Element " + el_id.IntegerValue + " did not actually exist? Skipping.");
                    continue;
                }
                    

                MasterSchema ms = el.GetEntity<MasterSchema>();     // Make sure it's actually a RTN

                if (ms == null)
                {
                    DebugHandler.println("FF", "Element " + el_id.IntegerValue + " is not actually a RTN? Skipping.");
                    continue;
                }

                // Set the font settings (if they were valid)

                if (comboP.SelectedIndex >= 0)
                    ms.fontP = tnts[comboP.SelectedIndex].Id;

                if (comboH1.SelectedIndex >= 0)
                    ms.fontH1 = tnts[comboH1.SelectedIndex].Id;

                if (comboH2.SelectedIndex >= 0)
                    ms.fontH2 = tnts[comboH2.SelectedIndex].Id;

                if (comboH3.SelectedIndex >= 0)
                    ms.fontH3 = tnts[comboH3.SelectedIndex].Id;

                if (comboH4.SelectedIndex >= 0) 
                    ms.fontH4 = tnts[comboH4.SelectedIndex].Id;

                if (comboH5.SelectedIndex >= 0)
                    ms.fontH5 = tnts[comboH5.SelectedIndex].Id;

                if (comboBullet.SelectedIndex >= 0)
                {
                    //delete current bulletfamily comment if it exists
                    string html = ms.html;
                    if(html.Substring(0, startBulletComment.Length).Equals(startBulletComment)) //check to see if comment exists
                    {
                        //delete it by finding first ocurrence of '-->' and substringing
                        html = html.Substring(html.IndexOf(endBulletComment) + endBulletComment.Length);
                    }
                    if(comboBullet.SelectedIndex == comboBullet.Items.Count - 1) //if the default option is selected
                    {
                        ms.html = startBulletComment + "DefaultBulletId" + endBulletComment + html;
                    }
                    else
                    {
                        ms.html = startBulletComment + annsts[comboBullet.SelectedIndex].UniqueId + endBulletComment + html; //add in new comment
                    }
                }

                // Apply the settings
                using (Transaction tr = new Transaction(uidoc.Document, "Setting text settings"))
                {
                    tr.Start();

                    el.SetEntity(ms);

                    tr.Commit();
                }

                // Regenerate the note
                UpdateHandler uh = new UpdateHandler(el as Group, uiapp);
                uh.updateManyThings();
                uh.regenerate();
            }
            this.Close();

        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void overrideTextNoteType(TextNoteType newType, TextNoteType oldType)
        {
            foreach (Parameter p in newType.Parameters)
            {
                if (!p.IsReadOnly)
                {
                    oldType.LookupParameter(p.Definition.Name).Set(p.AsDouble());
                }
            }
        }

        private void setDefaults_Click(object sender, EventArgs e)
        {
            if (comboBullet.SelectedIndex >= 0 && comboBullet.SelectedIndex < comboBullet.Items.Count - 1)
            {
                TextTools.setDefaults(uidoc.Document, annsts[comboBullet.SelectedIndex].UniqueId);
            }
            using (Transaction t = new Transaction(uidoc.Document, "Set default Text Styles"))
            {
                t.Start();

                for (int i = 0; i < tnts.Count; i++)
                {
                    switch(tnts[i].Name)
                    {
                        case "Rich Text Default P":
                            if (comboP.SelectedIndex >= 0 && comboP.SelectedIndex < comboP.Items.Count - 1)
                            {
                                TextNoteType newDefault = tnts[comboP.SelectedIndex];
                                TextNoteType currentDefault = tnts[i];
                                overrideTextNoteType(newDefault, currentDefault);
                            }
                        break;
                        case "Rich Text Default H1":
                            if (comboH1.SelectedIndex >= 0 && comboH1.SelectedIndex < comboH1.Items.Count - 1)
                            {
                                TextNoteType newDefault = tnts[comboH1.SelectedIndex];
                                TextNoteType currentDefault = tnts[i];
                                overrideTextNoteType(newDefault, currentDefault);
                            }
                        break;
                        case "Rich Text Default H2":
                            if (comboH2.SelectedIndex >= 0 && comboH2.SelectedIndex < comboH2.Items.Count - 1)
                            {
                                TextNoteType newDefault = tnts[comboH2.SelectedIndex];
                                TextNoteType currentDefault = tnts[i];
                                overrideTextNoteType(newDefault, currentDefault);
                            }
                        break;
                        case "Rich Text Default H3":
                            if (comboH3.SelectedIndex >= 0 && comboH3.SelectedIndex < comboH3.Items.Count - 1)
                            {
                                TextNoteType newDefault = tnts[comboH3.SelectedIndex];
                                TextNoteType currentDefault = tnts[i];
                                overrideTextNoteType(newDefault, currentDefault);
                            }
                        break;
                        case "Rich Text Default H4":
                            if (comboH4.SelectedIndex >= 0 && comboH4.SelectedIndex < comboH4.Items.Count - 1)
                            {
                                TextNoteType newDefault = tnts[comboH4.SelectedIndex];
                                TextNoteType currentDefault = tnts[i];
                                overrideTextNoteType(newDefault, currentDefault);
                            }
                        break;
                        case "Rich Text Default H5":
                            if (comboH5.SelectedIndex >= 0  && comboH5.SelectedIndex < comboH5.Items.Count - 1)
                            {
                                TextNoteType newDefault = tnts[comboH5.SelectedIndex];
                                TextNoteType currentDefault = tnts[i];
                                overrideTextNoteType(newDefault, currentDefault);
                            }
                        break;
                        
                    }
                }
                t.Commit();
            }
        }

    }
}
