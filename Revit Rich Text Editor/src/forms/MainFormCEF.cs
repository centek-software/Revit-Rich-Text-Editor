using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CefSharp.WinForms;
using CefSharp;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using System.IO;
using System.Threading;
using System.Web;

namespace CTEK_Rich_Text_Editor
{
    public partial class MainFormCEF : System.Windows.Forms.Form
    {
        public readonly ChromiumWebBrowser browser = null;

        private UIApplication uiapp;
        public string defaultContent;

        private System.Windows.Forms.Timer aTimer;
        private System.Object lockThis = new System.Object();

        private UpdateHandler uh;

        private ActivationHandler activationHandler;

        public string cachedHtml = null;

        private string path;

        public MainFormCEF()
        {
            InitializeComponent();

            string bb = PathTools.executingAssembly;


            path = new Uri(@"file:///" + Path.Combine(bb, "tinymce.htm").Replace('\\', '/')).AbsoluteUri;

            browser = new ChromiumWebBrowser("abount:blank")
            {
                Dock = DockStyle.Fill,
            };

            browser.BrowserSettings.FileAccessFromFileUrls = CefState.Enabled;
            browser.BrowserSettings.UniversalAccessFromFileUrls = CefState.Enabled;


            browser.RegisterJsObject("bound", new ScriptManager(this));

            toolStripContainer1.ContentPanel.Controls.Add(browser);
        }

        public void PrepareForRelaunch(UIApplication uiapp, string content, Element note, ActivationHandler activationHandler)
        {
            this.uiapp = uiapp;
            this.defaultContent = content;
            this.activationHandler = activationHandler;

            status.Text = "Loading... give us a moment!";

            uh = new UpdateHandler(note as Group, uiapp);
            uh.updateManyThings();
            uh.regenerate();

            browser.Load(path);
        }

        private void OnTimerEvent(object sender, EventArgs e)
        {
            RunUpdate();
        }

        private void RunUpdate()
        {
            string html = GetHtmlContent();

            try
            {
                lock (lockThis)
                {
                    if (html != null)
                    {
                        uh.updateHTML(html);
                        uh.regenerate();
                    }
                }
            }
            catch (Exception e)
            {
                MainRevitProgram.ShowDialog("Error", "Something went wrong while trying to update your text");
                DebugHandler.print("MAINFORM", e);
                DebugHandler.println("MAINFORM", html);
            }
        }

        public string GetHtmlContent()
        {
            return cachedHtml;
        }

        public void SetHtmlContent(string html)
        {
            browser.ExecuteScriptAsync("SetContent(\"" + HttpUtility.JavaScriptStringEncode(html) + "\")");
        }

        private void LoadUrl(string url)
        {
            if (Uri.IsWellFormedUriString(url, UriKind.RelativeOrAbsolute))
            {
                browser.Load(url);
            }
        }

        private void UpdateButton_Click(object sender, EventArgs e)
        {
            RunUpdate();
        }

        private void SpaceDblButton_Click(object sender, EventArgs e)
        {
            string html = GetHtmlContent();

            html = AutoFormatTools.spaceAfterLi(html, "<br/><br/>");

            SetHtmlContent(html);
        }

        private void SpaceSnglButton_Click(object sender, EventArgs e)
        {
            string html = GetHtmlContent();

            html = AutoFormatTools.spaceAfterLi(html, "");

            SetHtmlContent(html);
        }

        private void ToUppercaseButton_Click(object sender, EventArgs e)
        {
            string html = GetHtmlContent();

            html = AutoFormatTools.changeCase(html, "", true);

            SetHtmlContent(html);
        }

        private void ToLowercaseButton_Click(object sender, EventArgs e)
        {
            string html = GetHtmlContent();

            html = AutoFormatTools.changeCase(html, "", false);

            SetHtmlContent(html);
        }

        private void AboutButton_Click(object sender, EventArgs e)
        {
            new AboutForm(activationHandler).ShowDialog();
        }

        private void MainFormCEF_FormClosing(object sender, FormClosingEventArgs e)
        {
            RunUpdate();

            Task.Factory.StartNew(() => browser.Dispose());

            //e.Cancel = true;
            //this.Visible = false;
        }

        private void MainFormCEF_FormClosed(object sender, FormClosedEventArgs e)
        {

        }

        public void ClearStatusBar()
        {
            status.Text = "";
        }

        public string statusText
        {
            get
            {
                return status.Text;
            }

            set
            {
                status.Text = value;
            }
        }

        

       
    }

    public class ScriptManager
    {
        private static SpellCheck sc = new SpellCheck();

        private MainFormCEF mainForm;

        private int currentChange = -1;

        // Constructor.
        public ScriptManager(MainFormCEF mainForm)
        {
            this.mainForm = mainForm;
        }

        public void TinyOnChange(int changeId, string content)
        {
            if (changeId <= currentChange)
                return;

            mainForm.cachedHtml = content;
        }

        public string GetDefaultContent()
        {

            mainForm.ClearStatusBar();
            return mainForm.defaultContent;
        }

        public string DefaultSpellcheckLanguage()
        {
            string s = Properties.Settings.Default.language;

            if (s.Trim().Length == 0)
                return "en_US";
            else
                return s;
        }

        public string GetSpellcheckResponse(string postData)
        {
            return sc.respond(postData);
        }

        public string SpellcheckLanguages()
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
