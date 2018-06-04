using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace UtilityToolkit.UserCtls
{
    public partial class ProgressAxis : UserControl
    {
        private int axisItemNum = 10;
        private TableLayoutPanel tableLayoutPanel1 = null;
        public ProgressAxis()
        {

            InitializeComponent();
        }

        private void ProgressAxis_Load(object sender, EventArgs e)
        {
            this.tableLayoutPanel1 = new TableLayoutPanel();

            this.tableLayoutPanel1.ColumnCount = axisItemNum;
            
           // this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
          //  this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.Location = new System.Drawing.Point(39, 3);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 1;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
           // this.tableLayoutPanel1.Size = new System.Drawing.Size(200, 78);
            this.tableLayoutPanel1.TabIndex = 0;

            this.tableLayoutPanel1.Dock = DockStyle.Fill;

            for (int i = 0; i < axisItemNum; i++)
            {
                this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
                AxisItem axisItem = new AxisItem();
                axisItem.Margin = new Padding(0, 3, 0, 0);
                axisItem.CaptionTxt = string.Format("节点{0}", i + 1);
                axisItem.axisItemColor = Color.Green;
                axisItem.Dock = DockStyle.Fill;
                this.tableLayoutPanel1.Controls.Add(axisItem, i, 0);
            }
            this.Controls.Add(this.tableLayoutPanel1);

        }
    }
}
