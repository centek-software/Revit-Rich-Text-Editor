//   Revit Rich Text Editor
//   Copyright (C) 2014 Centek Engineering

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using VCExtensibleStorageExtension;
using VCExtensibleStorageExtension.ElementExtensions;
using Microsoft.Win32;
using System.Diagnostics;

namespace CTEK_Rich_Text_Editor
{
    public partial class MainFormIE : System.Windows.Forms.Form
    {
        private UIApplication uiapp;
        private string content;

        private Timer aTimer;
        private System.Object lockThis = new System.Object();

        private UpdateHandler uh;

        private ActivationHandler activationHandler;

        public MainFormIE(UIApplication uiapp, string content, Element note, ActivationHandler activationHandler)
        {
            this.uiapp = uiapp;
            this.content = content;
            this.activationHandler = activationHandler;

            InitializeComponent();

            aTimer = new Timer();
            aTimer.Interval = 4000;
            //aTimer.Enabled = true;                                    // Uncomment this to enable auto-refresh
            aTimer.Tick += new System.EventHandler(OnTimerEvent);

            uh = new UpdateHandler(note as Group, uiapp);
            uh.UpdateManyThings();
            uh.Regenerate();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            tinyMceEditor.CreateEditor(content);
        }

        private void OnTimerEvent(object sender, EventArgs e)
        {
            runUpdate();
        }

        private void buttonLoad_Click(object sender, EventArgs e)
        {
            runUpdate();
        }

        private void runUpdate()
        {
            lock (lockThis)
            {
                uh.UpdateHTML(tinyMceEditor.HtmlContent);
                uh.Regenerate();
            }
        }

        private void buttonSave_Click(object sender, EventArgs e)
        {
            /*if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                File.WriteAllText(saveFileDialog.FileName, tinyMceEditor.HtmlContent);
            }*/
            TaskDialog.Show("Centek Rich Text Editor", "You just had to click it, didn't you?");
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            aTimer.Enabled = false;

            runUpdate();
        }

        private void spaceButton_Click(object sender, EventArgs e)
        {
            string html = tinyMceEditor.HtmlContent;

            html = AutoFormatTools.spaceAfterLi(html, "<br/><br/>");

            tinyMceEditor.HtmlContent = html;
        }

        private void spaceButtonSingle_Click(object sender, EventArgs e)
        {
            string html = tinyMceEditor.HtmlContent;

            html = AutoFormatTools.spaceAfterLi(html, "");

            tinyMceEditor.HtmlContent = html;
        }

        private void aboutBtn_Click(object sender, EventArgs e)
        {
            //if (!activationHandler.completedFirstActivation)
              //  return;

            new AboutForm(activationHandler).ShowDialog();
        }

    }
}
