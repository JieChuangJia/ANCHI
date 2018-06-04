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
   
    public partial class AxisItem : UserControl
    {
        private string captionTxt = "";
        public Color axisItemColor{get;set;}
        public string CaptionTxt { get { return captionTxt; } set { captionTxt = value; this.label1.Text = value; } }
        public AxisItem()
        {
            InitializeComponent();
            captionTxt = "状态描述";
        }

        private void AxisItem_Load(object sender, EventArgs e)
        {
           
           // this.label1.DataBindings.Add(new Binding("Text",this,captionTxt));
        }

        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Size rectSize= this.pictureBox1.Size;
            Pen linePen = new Pen(this.axisItemColor, 5);
            linePen.EndCap = System.Drawing.Drawing2D.LineCap.Triangle;
           
           // linePen.DashStyle = System.Drawing.Drawing2D.DashStyle.DashDotDot;
            Point stPt = new Point(0, rectSize.Height / 2);
            Point endPt = new Point(rectSize.Width, rectSize.Height / 2);
            g.DrawLine(linePen, stPt, endPt);
            SolidBrush cirBrush = new SolidBrush(Color.Blue);
            int cirRadis = 10;
            g.FillEllipse(cirBrush,new Rectangle(new Point(rectSize.Width/2-cirRadis,rectSize.Height/2-cirRadis),new Size(cirRadis*2,cirRadis*2)));


        }
    }
}
