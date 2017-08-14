//   Revit Rich Text Editor
//   Copyright (C) 2014 Centek Engineering

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CTEK_Rich_Text_Editor
{
    class DebugHandler
    {
        private const bool USE_DEBUG_FILE = false;

        public static string debug = "";

        private static volatile StreamWriter sw;

        public static void initialize()
        {
#pragma warning disable 0162
            if (USE_DEBUG_FILE)
            {
                string bb = new FileInfo(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath).Directory.FullName;
                string path = Path.Combine(bb, @"debug.txt");

                if (!File.Exists(path))
                {
                    using (File.CreateText(path))
                    {
                    }
                }

                sw = File.AppendText(path);

                sw.Flush();
            }
#pragma warning restore 0162
        }

        public static void println(string intro, string text)
        {
            print("[" + DateTime.Now.ToString("HH:mm:ss") + "][" + intro + "]: " + text + Environment.NewLine);
        }

        public static void print(string text)
        {
            debug += text;
            DebugCmd.Update(text);

#pragma warning disable 0162
            if (USE_DEBUG_FILE)
            {
                sw.WriteLine(text);
                sw.Flush();
            }
#pragma warning restore 0162
        }

        internal static void print(string intro, Exception e)
        {
            println(intro, e.Message);
            println(intro, e.StackTrace);
        }
    }
}
