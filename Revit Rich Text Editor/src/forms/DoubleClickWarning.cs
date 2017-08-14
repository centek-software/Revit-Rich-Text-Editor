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
    public partial class DoubleClickWarning : Form
    {
        public DoubleClickWarning()
        {
            InitializeComponent();
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
            {
                Properties.Settings.Default.doubleClickWarning = false;
                Properties.Settings.Default.Save();
            }

            this.Close();
        }
    }
}
