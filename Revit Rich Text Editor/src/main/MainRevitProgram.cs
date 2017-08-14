//   Revit Rich Text Editor
//   Copyright (C) 2014 Centek Engineering

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.Attributes;
using System.Reflection;
using System.IO;
using Autodesk.Revit.UI.Selection;
using System.Windows.Media.Imaging;
using System.Runtime.InteropServices;
using Autodesk.Revit.UI.Events;
using CefSharp;

/**
 * 1.0 First release
 * 1.2 Different DPI screen fix, Fixed image files not deleting
 * 1.4 Spell check, ordered list numbering bug fix, unpurged styles bug fix
 * 1.5 Introduced activation, removed 'p' from valid children for 'li'
 * 1.6 = Local Deploy =
 * 1.7 Don't allow absurdly small notes, Switch to chromium engine, Worksharing support
 * ...
 * 2.0 Fix localization
 * 3.0 Tables, Custom bullets, 2017 support
 * 3.1 Fixes custom bullet wrapping
 * 3.2 ?
 * 3.3 2018 support, merge cells
 */
namespace CTEK_Rich_Text_Editor
{
    public class MainRevitProgram : IExternalApplication
    {
        public const int VERSION = 33;

        public static ActivationHandler activationHandler;
        public static MainFormCEF mainForm;

        public Result OnStartup(Autodesk.Revit.UI.UIControlledApplication app)
        {
            DebugHandler.initialize();
            DebugHandler.println("LOAD", "New Instance");

            DebugHandler.println("LOAD", "Revit Version: [" + app.ControlledApplication.VersionBuild + "]");
            DebugHandler.println("LOAD", "Revit Year: [" + RevitVersionHandler.GetRevitVersion() + "]");
            DebugHandler.println("LOAD", "Create note function: [" + (RevitVersionHandler.CreateTextNote2016 != null) + "]");

            if (RevitVersionHandler.GetRevitVersion() <= 2014)
            {
                DebugHandler.println("LOAD", "This year is not supported. Not loading.");
                return Result.Failed;
            }

            if (RevitVersionHandler.NeedsUpdate(app.ControlledApplication.VersionBuild))
            {
                ShowDialog("Needs Update", "Due to a bug in this particular build of Autodesk Revit, you need to update to \"Autodesk Revit 2015 Update Release 3.\" The update is free and painless. The Rich Text Editor add on will be disabled until you do so.");
                return Result.Failed;
            }

            CefSettings settings = new CefSettings();

            settings.CefCommandLineArgs.Add("no-proxy-server", "no-proxy-server");

            string bb = PathTools.executingAssembly;

            settings.BrowserSubprocessPath = Path.Combine(bb, settings.BrowserSubprocessPath);
            settings.RemoteDebuggingPort = 8088;

            DebugHandler.println("EDITOR_CMD", "BrowserSubprocessPath: " + settings.BrowserSubprocessPath);

            bool result = Cef.Initialize(settings, true, true);

            DebugHandler.println("EDITOR_CMD", "CEFSharp Initialization " + (result ? "succeeded" : "FAILED!"));


            activationHandler = new ActivationHandler(app);
            //activationHandler.attemptActivation();
            activationHandler.ActivateMc();


            string tabName = "Centek";
            try
            {
                app.CreateRibbonTab(tabName);
            }
            catch (Exception)
            {
                // Happens if the ribbon tab already exists
            }


            // Setup panel
            RibbonPanel panel = app.CreateRibbonPanel(tabName, "Rich Text Editor v" + GetAppVersion());

            PushButton createButton = panel.AddItem(new PushButtonData("CreateRTN", "Create Note", AssemblyFullName, "CTEK_Rich_Text_Editor.TextNoteCreatorCmd")) as PushButton;
            createButton.LargeImage = new BitmapImage(new Uri(Path.Combine(bb, @"img\new.png")));
            createButton.ToolTip = "Create a new text note with some sample text.";
            createButton.SetContextualHelp(new ContextualHelp(ContextualHelpType.Url, "https://software.centekeng.com/index.php/richtexteditor/help#create"));


            PushButton editButton = panel.AddItem(new PushButtonData("editRTN", "Edit Note", AssemblyFullName, "CTEK_Rich_Text_Editor.TextNoteEditorCmd")) as PushButton;
            editButton.LargeImage = new BitmapImage(new Uri(Path.Combine(bb, @"img\edit.png")));
            editButton.ToolTip = "Edit an existing note.";
            editButton.SetContextualHelp(new ContextualHelp(ContextualHelpType.Url, "https://software.centekeng.com/index.php/richtexteditor/help#edit"));

            PushButton toggleResizeButton = panel.AddItem(new PushButtonData("resizeBoxRTN", "Toggle Box", AssemblyFullName, "CTEK_Rich_Text_Editor.TextNoteToggleResizeCmd")) as PushButton;
            toggleResizeButton.LargeImage = new BitmapImage(new Uri(Path.Combine(bb, @"img\toggle.png")));
            toggleResizeButton.ToolTip = "Toggle the visibility of the resize box.";
            toggleResizeButton.SetContextualHelp(new ContextualHelp(ContextualHelpType.Url, "https://software.centekeng.com/index.php/richtexteditor/help#resize"));

            PushButton resizeButton = panel.AddItem(new PushButtonData("resizeRTN", "Apply Resize", AssemblyFullName, "CTEK_Rich_Text_Editor.TextNoteResizeCmd")) as PushButton;
            resizeButton.LargeImage = new BitmapImage(new Uri(Path.Combine(bb, @"img\resize.png")));
            resizeButton.ToolTip = "Resize the note to the bounds defined by the resize box.";
            resizeButton.SetContextualHelp(new ContextualHelp(ContextualHelpType.Url, "https://software.centekeng.com/index.php/richtexteditor/help#resize"));

            PushButton setFonts = panel.AddItem(new PushButtonData("setFonts", "Set Fonts", AssemblyFullName, "CTEK_Rich_Text_Editor.SetFontsCmd")) as PushButton;
            setFonts.LargeImage = new BitmapImage(new Uri(Path.Combine(bb, @"img\font.png")));
            setFonts.ToolTip = "Set the fonts for paragraph and headings for this note.";
            setFonts.SetContextualHelp(new ContextualHelp(ContextualHelpType.Url, "https://software.centekeng.com/index.php/richtexteditor/help#fonts"));

            PushButton formatPainter = panel.AddItem(new PushButtonData("formatPainter", "Format Painter", AssemblyFullName, "CTEK_Rich_Text_Editor.FormatPainterCmd")) as PushButton;
            formatPainter.LargeImage = new BitmapImage(new Uri(Path.Combine(bb, @"img\painter.png")));
            formatPainter.ToolTip = "Copy the font and size settings from one note to others.";
            formatPainter.SetContextualHelp(new ContextualHelp(ContextualHelpType.Url, "https://software.centekeng.com/index.php/richtexteditor/help#painter"));

            panel.AddSlideOut();

            PushButton about = panel.AddItem(new PushButtonData("about", "About", AssemblyFullName, "CTEK_Rich_Text_Editor.AboutCmd")) as PushButton;
            about.LargeImage = new BitmapImage(new Uri(Path.Combine(bb, @"img\about.png")));
            about.ToolTip = "Find out about this application";
            about.SetContextualHelp(new ContextualHelp(ContextualHelpType.Url, "https://software.centekeng.com/index.php/richtexteditor/help#activation"));

            PushButton debug = panel.AddItem(new PushButtonData("debug", "Debug", AssemblyFullName, "CTEK_Rich_Text_Editor.DebugCmd")) as PushButton;
            debug.LargeImage = new BitmapImage(new Uri(Path.Combine(bb, @"img\debug.png")));
            debug.ToolTip = "View the terminal with debug information";
            debug.SetContextualHelp(new ContextualHelp(ContextualHelpType.Url, "https://software.centekeng.com/index.php/richtexteditor/help#debug"));


            return Result.Succeeded;
        }

        public static string GetAppVersion()
        {
            string s = VERSION.ToString();
            return s.Substring(0, s.Length - 1) + '.' + s.Substring(s.Length - 1);
        }

        private string AssemblyFullName
        {
            get
            {
                return Assembly.GetExecutingAssembly().Location;
            }
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            activationHandler.EndSession();

            //mainForm.browser.Dispose();
            //mainForm.Dispose();
            Cef.Shutdown();

            return Result.Succeeded;
        }

        /// <summary>
        /// Show a task dialog with a message and title.
        /// </summary>
        /// <param name="title">The title.</param>
        /// <param name="message">The message.</param>
        public static void ShowDialog(string title, string message)
        {
            // Show the user a message.
            TaskDialog td = new TaskDialog(title)
            {
                MainInstruction = message,
                TitleAutoPrefix = false
            };
            td.Show();
        }
    }
}
