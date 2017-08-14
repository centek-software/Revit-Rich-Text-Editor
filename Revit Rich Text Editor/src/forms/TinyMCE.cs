//   Revit Rich Text Editor
//   Copyright (C) 2014 Centek Engineering

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Autodesk.Revit.UI;

namespace CTEK_Rich_Text_Editor
{

    public partial class TinyMCE : UserControl
    {
        public TinyMCE()
        {
            InitializeComponent();
        }

        public string HtmlContent
        {
            get
            {
                string content = string.Empty;
                if (webBrowserControl.Document != null)
                {
                    object html = webBrowserControl.Document.InvokeScript("GetContent");
                    content = html as string;
                }
                return content;
            }
            set
            {
                if (webBrowserControl.Document != null)
                {
                    webBrowserControl.Document.InvokeScript("SetContent", new object[] { value });
                }
            }
        }

        public void setDefaultContent(string content)
        {
            webBrowserControl.Document.InvokeScript("SetContent", new object[] { content });
        }

        public void CreateEditor(string content = "")
        {
            string bb = null;

            bb = new FileInfo(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath).Directory.FullName;


            // Check if the main script file exist being used by the HTML page
            String path = Path.Combine(bb, @"tinymce\js\tinymce\tinymce.min.js");

            if (File.Exists(path))
            {
                //string readText = File.ReadAllText(Path.Combine(bb, "tinymce.htm"));
                //readText = readText.Replace("[REPLACE_ME_WITH_HTML]", "<h1>THIS BE A TEST</h1>");

                webBrowserControl.ObjectForScripting = new ScriptManager(content, this);
                webBrowserControl.Url = new Uri(@"file:///" + Path.Combine(bb, "tinymce.htm").Replace('\\', '/'));
            }
            else
            {
                MessageBox.Show(path, "Not found", MessageBoxButtons.OK, MessageBoxIcon.Error);
                //MessageBox.Show("Could not find the tinyMCE script directory. Please ensure the directory is in the same location as tinymce.htm", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        [ComVisible(true)]
        public class ScriptManager
        {
            TinyMCE mce;
            string defText;
            static SpellCheck sc = new SpellCheck();

            // Constructor.
            public ScriptManager(string defText, TinyMCE mce)
            {
                this.defText = defText;
                this.mce = mce;
            }

            public void setDefaultContent()
            {
                mce.setDefaultContent(defText);
            }

            public string defaultSpellcheckLanguage()
            {
                string s = Properties.Settings.Default.language;

                if (s.Trim().Length == 0)
                    return "en_US";
                else
                    return s;
            }

            public string getSpellcheckResponse(string postData)
            {
                return sc.respond(postData);
            }

            public string spellcheckLanguages()
            {
                string result = "";

                foreach (KeyValuePair<string, SpellCheckDictionary> kvp in sc.getAvailabledDicts())
                    result += kvp.Value.displayName.Replace(",", "") + '=' + kvp.Key + ',';
                   
                
                if (result.Length == 0)
                    return "";
                else
                {
                    result = result.Substring(0, result.Length - 1);

                    DebugHandler.println("SPELL", result);

                    return result;
                }
            }
        }
    }
}
