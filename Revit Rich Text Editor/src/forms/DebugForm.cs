using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CTEK_Rich_Text_Editor
{
    public partial class DebugForm : Form
    {
        public DebugForm()
        {
            InitializeComponent();

            textBox1.AppendText(DebugHandler.debug);

            textBox1.VisibleChanged += (sender, e) =>
            {
                if (textBox1.Visible)
                {
                    textBox1.SelectionStart = textBox1.TextLength;
                    textBox1.ScrollToCaret();
                }
            };

            textBox1.SelectionStart = textBox1.TextLength;
            textBox1.ScrollToCaret();
        }

        public void Update(string update)
        {
            textBox1.AppendText(update);
        }
    }
}
