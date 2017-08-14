using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CTEK_Rich_Text_Editor
{
    class PathTools
    {

        public static string executingAssembly
        {
            get
            {
                return new FileInfo(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath).Directory.FullName;
            }

            private set
            {
            }
        }
    }
}
