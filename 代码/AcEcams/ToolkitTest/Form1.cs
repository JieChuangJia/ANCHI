using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ToolkitTest
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string str1 = "36ANCCB23140160N17K03C7K06010XXXXX";
            string str2 = str1.Insert(22, "1");
            MessageBox.Show(str1);
        }
    }
}
