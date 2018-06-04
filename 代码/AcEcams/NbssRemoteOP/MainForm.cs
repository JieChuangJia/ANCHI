using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.ServiceModel;
using System.ServiceModel.Description;
using ModuleCrossPnP;
using AsrsInterface;
using LogManage;
using LogInterface;
using ProductRecordView;
using ASRSStorManage.View;
using CtlDBAccess.BLL;
using CtlDBAccess.Model;
using System.Configuration;
namespace NbssECAMS
{
    public partial class MainForm : Form, ILogDisp, IParentModule
    {
        #region 数据
        private string version = "1.0.1正式版 2017-1-5 ";
        private int roleID = 3;
        private string userName = "";
        const int CLOSE_SIZE = 10;
        int iconWidth = 16;
        int iconHeight = 16;
        // private CtlcorePresenter corePresenter;
        private AsrsCtlSvcProxy asrsCtlSvc = null;
        private AsrsInterface.IRemoteMonitorSvc remotMonitorSvc = null;
        private List<string> childList = null;
        //子模块
        //private LicenseMonitor licenseMonitor = null;
      
        private LogView logView = null;
        private RecordView recordView = null;
       // private AsrsCtlView asrsCtlView = null;
        private StorageMainView storageView = null;    //  private ASRSManage asrsManageView = null;
        #endregion
      
        public MainForm()
        {
            InitializeComponent();
            childList = new List<string>();
        }
        public MainForm(int roleID,string userName)
        {
            InitializeComponent();
            childList = new List<string>();
            this.roleID = roleID;
            this.userName = userName;
        }
      
        private void MainForm_Load(object sender, EventArgs e)
        {
            try
            {
                asrsCtlSvc = new AsrsCtlSvcProxy();
                string addr = ConfigurationManager.AppSettings["AsrsSvcAddr"];
                string svcAddr =  string.Format(@"http://{0}:8733/ZZ/AsrsCtlSvc/AsrsCtl/",addr);
                asrsCtlSvc.SetSvcAddr(svcAddr);
                svcAddr = string.Format(@"http://{0}:8733/ZZ/RemoveCtlSvc/RemoteMonitor/", addr);
                this.remotMonitorSvc = ChannelFactory<AsrsInterface.IRemoteMonitorSvc>.CreateChannel(new BasicHttpBinding(), new EndpointAddress(svcAddr));

                string dbSrc = ConfigurationManager.AppSettings["DBSource"];
                CtlDBAccess.DBUtility.PubConstant.ConnectionString = string.Format(@"{0}Initial Catalog=NbssECAMS;User ID=sa;Password=123456;", dbSrc);

                this.labelVersion.Text = this.version;
                this.MainTabControl.DrawMode = TabDrawMode.OwnerDrawFixed;
                this.MainTabControl.Padding = new System.Drawing.Point(CLOSE_SIZE, CLOSE_SIZE);
                this.MainTabControl.DrawItem += new DrawItemEventHandler(this.tabControlMain_DrawItem);
                this.MainTabControl.MouseDown += new System.Windows.Forms.MouseEventHandler(this.tabControlMain_MouseDown);
                Console.SetOut(new NbssECAMS.TextBoxWriter(this.richTextBoxLog));
                // corePresenter = new CtlcorePresenter();

                this.labelUser.Text = "当前用户：" + this.userName;

                ModuleAttach();//加载子模块

                //string licenseFile = AppDomain.CurrentDomain.BaseDirectory + @"\NBSSLicense.lic";
                //this.licenseMonitor = new LicenseMonitor(this, 2000, licenseFile, "zzkeyFT1");
                //if (!this.licenseMonitor.StartMonitor())
                //{
                //    throw new Exception("license 监控失败");
                //}
                //string reStr = "";
                //if (!this.licenseMonitor.IslicenseValid(ref reStr))
                //{
                //    MessageBox.Show(reStr);
                //    return;
                //}
                this.cb_HouseName.Items.AddRange(new string[] { "A库房" });
                this.cb_HouseName.SelectedIndex = 0;
                OnRefreshBatchList();
                DispBatchSet();
            }
            catch (Exception ex)
            {
                Console.WriteLine("初始化错误：" + ex.Message);
            }
           
           
        }

        /// <summary>
        /// 模块加载
        /// </summary>
        private void ModuleAttach()
        {
            logView = new LogView("日志");
            logView.SetParent(this);

           

            logView.RegisterMenus(this.menuStrip1, "日志查询");
            logView.SetLogDispInterface(this);

            recordView = new RecordView();
            recordView.SetParent(this);
            recordView.RegisterMenus(this.menuStrip1, "记录查询与管理");
            recordView.SetLoginterface(logView.GetLogrecorder());

            storageView = new StorageMainView();
            storageView.SetParent(this);
            storageView.RegisterMenus(this.menuStrip1, "库存管理");
            storageView.SetLoginterface(logView.GetLogrecorder());

            AsrsInterface.IAsrsManageToCtl asrsResManage = null;
           // AsrsInterface.IAsrsCtlToManage asrsCtl = asrsCtlView.GetPresenter();
            string reStr = "";
            if (!storageView.Init(asrsCtlSvc, ref asrsResManage, ref reStr))
            {
                logView.GetLogrecorder().AddLog(new LogModel("主模块", "立库管理层模块初始化错误," + reStr, EnumLoglevel.错误));
            }
           // asrsCtlView.SetAsrsResManage(asrsResManage);
            List<string> logSrcList = new List<string>();
            //List<string> logSrcs = asrsCtlView.GetLogsrcList();
            //if(logSrcs != null)
            //{
            //    logSrcList.AddRange(logSrcs);
            //}
            List<string>  logSrcs = storageView.GetLogsrcList();
            if (logSrcs != null)
            {
                logSrcList.AddRange(logSrcs);
            }
            logView.SetLogsrcList(logSrcList);
            Form frontForm = storageView.GetViewByCaptionTxt("货位看板");
            if(frontForm != null)
            {
                AttachModuleView(frontForm);
            }
           
        }
        #region 接口实现
        public string CurUsername { get { return this.userName; } }
        public int RoleID { get { return this.roleID; } }
        private delegate void DelegateDispLog(LogModel log);//委托，显示日志
        public void DispLog(LogModel log)
        {
            if(this.richTextBoxLog.InvokeRequired)
            {
                DelegateDispLog delegateLog = new DelegateDispLog(DispLog);
                this.Invoke(delegateLog, new object[] {log });
            }
            else
            {
                if (this.richTextBoxLog.Text.Count() > 10000)
                {
                    this.richTextBoxLog.Text = "";
                }
                this.richTextBoxLog.Text += (string.Format("[{0:yyyy-MM-dd HH:mm:ss.fff}]{1},{2}", log.LogTime, log.LogSource,log.LogContent) + "\r\n");
            }
            
        }
        public void AttachModuleView(System.Windows.Forms.Form childView)
        {
            TabPage tabPage = null;
           if(this.childList.Contains(childView.Text))
           {
               tabPage = this.MainTabControl.TabPages[childView.Text];
               this.MainTabControl.SelectedTab = tabPage;
               return;
           }
          
           this.MainTabControl.TabPages.Add(childView.Text, childView.Text);
           tabPage = this.MainTabControl.TabPages[childView.Text];
           tabPage.Controls.Clear();
           this.MainTabControl.SelectedTab = tabPage;
           childView.MdiParent = this;
           
           tabPage.Controls.Add(childView);
           this.childList.Add(childView.Text);
           childView.Dock = DockStyle.Fill;
           childView.Size = this.panelCenterview.Size;
           childView.Show();
           
        }
        #endregion
        #region ILicenseNotify接口实现
        public void ShowWarninfo(string info)
        {
            LogModel log = new LogModel("其它", info, EnumLoglevel.警告);
            logView.GetLogrecorder().AddLog(log);
        }
        public void LicenseInvalid(string warnInfo)
        {
           
            LogModel log = new LogModel("其它", warnInfo, EnumLoglevel.警告);
            logView.GetLogrecorder().AddLog(log);
        }
        public void LicenseReValid(string noteInfo)
        {
          
           
            LogModel log = new LogModel("其它", noteInfo, EnumLoglevel.提示);
            logView.GetLogrecorder().AddLog(log);
        }
        #endregion
        #region UI事件
        private void OnRefreshBatchList()
        {
           
            List<string> storeBatchs = this.remotMonitorSvc.GetStoreBatchs(this.cb_HouseName.Text);
            if (storeBatchs != null && storeBatchs.Count() > 0)
            {
                this.cb_CheckoutBatchs.Items.Clear();
                this.cb_CheckoutBatchs.Items.Add("空");
                foreach (string str in storeBatchs)
                {
                    if (string.IsNullOrWhiteSpace(str))
                    {
                        continue;
                    }
                    this.cb_CheckoutBatchs.Items.Add(str);
                }

                this.cb_CheckoutBatchs.SelectedIndex = 0;
            }
            //this.cb_CheckoutBatchs
            BatchBll batchBll = new BatchBll();
            List<BatchModel> batchList = batchBll.GetModelList(" ");

            List<string> batchNames = new List<string>();
            foreach (BatchModel batch in batchList)
            {
                batchNames.Add(batch.batchName);
            }
            this.cb_CheckinBatchList.Items.Clear();
            this.cb_CheckinBatchList.Items.Add("空");
            this.cb_CheckinBatchList.Items.AddRange(batchNames.ToArray());
            if (this.cb_CheckinBatchList.Items.Count > 0)
            {
                this.cb_CheckinBatchList.SelectedIndex = 0;
            }
            DispBatchSet();
        }
        private void DispBatchSet()
        {
            List<string> batchSets = this.remotMonitorSvc.GetCheckinBatchSet();

            if (batchSets != null && batchSets.Count() > 0)
            {
                this.lb_A1CheckInBatch.Text = batchSets[0];

            }

            batchSets = this.remotMonitorSvc.GetCheckoutBatchSet();
            if (batchSets != null && batchSets.Count() > 0)
            {
                this.lb_A1OutBatch.Text = batchSets[0];
            }
        }
        private void panelCenterview_SizeChanged(object sender, EventArgs e)
        {
            try
            {
                if(this.panelCenterview.Controls.Count<1)
                {
                    return;
                }
                Control child = this.panelCenterview.Controls[0];
                if (child != null)
                {
                    child.Size = this.panelCenterview.Size;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
               // throw;
            }
           
           
        }

        private void tabControlMain_DrawItem(object sender, DrawItemEventArgs e)
        {
            try
            {
               
                 Image icon = this.imageList1.Images[0];
                 Brush biaocolor = Brushes.Transparent; //选项卡的背景色
                Graphics g = e.Graphics;
                Rectangle r = MainTabControl.GetTabRect(e.Index);
                if (e.Index == this.MainTabControl.SelectedIndex)    //当前选中的Tab页，设置不同的样式以示选中
                {
                    Brush selected_color = Brushes.Wheat; //选中的项的背景色
                    g.FillRectangle(selected_color, r); //改变选项卡标签的背景色
                    string title = MainTabControl.TabPages[e.Index].Text + "  ";

                    g.DrawString(title, this.Font, new SolidBrush(Color.Black), new PointF(r.X , r.Y + 6));//PointF选项卡标题的位置

                    r.Offset(r.Width - iconWidth - 3, 2);
                    g.DrawImage(icon, new Point(r.X + 2, r.Y + 2));//选项卡上的图标的位置 fntTab = new System.Drawing.Font(e.Font, FontStyle.Bold);
                }
                else//非选中的
                {
                    g.FillRectangle(biaocolor, r); //改变选项卡标签的背景色
                    string title = this.MainTabControl.TabPages[e.Index].Text + "  ";

                    g.DrawString(title, this.Font, new SolidBrush(Color.Black), new PointF(r.X , r.Y + 6));//PointF选项卡标题的位置
                    r.Offset(r.Width - iconWidth - 3, 2);
                    g.DrawImage(icon, new Point(r.X + 2, r.Y + 2));//选项卡上的图标的位置
                }
                //Rectangle myTabRect = this.MainTabControl.GetTabRect(e.Index);

                ////先添加TabPage属性   
                //e.Graphics.DrawString(this.MainTabControl.TabPages[e.Index].Text
                //, this.Font, SystemBrushes.ControlText, myTabRect.X + 2, myTabRect.Y + 2);

                //myTabRect.Offset(myTabRect.Width - (CLOSE_SIZE + 3), 2);
                //myTabRect.Width = CLOSE_SIZE;
                //myTabRect.Height = CLOSE_SIZE;
                ////再画一个矩形框
                //using (Pen p = new Pen(Color.Red))
                //{

                //    //  e.Graphics.DrawRectangle(p, myTabRect);
                //}

          
                ////画关闭符号
                //using (Pen objpen = new Pen(Color.DarkGray, 1.0f))
                //{
                //    //"\"线
                //    Point p1 = new Point(myTabRect.X + 3, myTabRect.Y + 3);
                //    Point p2 = new Point(myTabRect.X + myTabRect.Width - 3, myTabRect.Y + myTabRect.Height - 3);
                //    e.Graphics.DrawLine(objpen, p1, p2);

                //    //"/"线
                //    Point p3 = new Point(myTabRect.X + 3, myTabRect.Y + myTabRect.Height - 3);
                //    Point p4 = new Point(myTabRect.X + myTabRect.Width - 3, myTabRect.Y + 3);
                //    e.Graphics.DrawLine(objpen, p3, p4);
                //}

                //e.Graphics.Dispose();
            }
            catch (Exception ex)
            {

                Console.WriteLine(ex.ToString());
            }
        }
        private void tabControlMain_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                Point p = e.Location;
                Rectangle r = MainTabControl.GetTabRect(this.MainTabControl.SelectedIndex);
                r.Offset(r.Width - iconWidth, 4);
                r.Width = iconWidth;
                r.Height = iconHeight;
                if (this.MainTabControl.SelectedTab.Text == "货位看板")
                {
                    return;
                }
                string tabText = this.MainTabControl.SelectedTab.Text;
         
                if (r.Contains(p))
                {
                    this.childList.Remove(tabText);
                    this.MainTabControl.TabPages.RemoveAt(this.MainTabControl.SelectedIndex);
                }
                    
                //int x = e.X, y = e.Y;

                ////计算关闭区域   
                //Rectangle myTabRect = this.MainTabControl.GetTabRect(this.MainTabControl.SelectedIndex);

                //myTabRect.Offset(myTabRect.Width - (CLOSE_SIZE + 3), 2);
                //myTabRect.Width = CLOSE_SIZE;
                //myTabRect.Height = CLOSE_SIZE;

                ////如果鼠标在区域内就关闭选项卡   
                //bool isClose = x > myTabRect.X && x < myTabRect.Right
                // && y > myTabRect.Y && y < myTabRect.Bottom;

                //if (isClose == true)
                //{
                //    if (this.MainTabControl.SelectedTab.Text == nodeMonitorView.Text)
                //    {
                //        return;
                //    }
                //    string tabText = this.MainTabControl.SelectedTab.Text;
                //    this.childList.Remove(tabText);
                //    this.MainTabControl.TabPages.Remove(this.MainTabControl.SelectedTab);
                  
                //}
            }
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            this.richTextBoxLog.Text = string.Empty;
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
           
        }

        private void richTextBoxLog_TextChanged(object sender, EventArgs e)
        {
            this.richTextBoxLog.SelectionStart = this.richTextBoxLog.Text.Length; //Set the current caret position at the end
            this.richTextBoxLog.ScrollToCaret();
        }

        private void 切换用户ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                LoginView2 logView2 = new LoginView2();
                if (DialogResult.OK == logView2.ShowDialog())
                {
                    string tempUserName = "";
                    int tempRoleID = logView2.GetLoginRole(ref tempUserName);
                    if (tempRoleID < 1)
                    {
                        return;
                    }
                    this.roleID = tempRoleID;
                    this.userName = tempUserName;
                    this.labelUser.Text = "当前用户：" + this.userName;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void bt_BatchSet_Click(object sender, EventArgs e)
        {
            string reStr = "";
            int flag = 1;
            string houseName = this.cb_HouseName.Text;
            string batchName = "";
            if (this.radioButtonCheckIn.Checked)
            {
                flag = 1;
                batchName = this.cb_CheckinBatchList.Text;

            }
            else
            {
                flag = 2;
                batchName = this.cb_CheckoutBatchs.Text;
            }
            
            if (!this.remotMonitorSvc.SetBatch(flag, houseName, batchName, ref reStr))
            {
                MessageBox.Show("错误：" + reStr);
                return;
            }
            DispBatchSet();
        }

        private void bt_RefreshBatch_Click(object sender, EventArgs e)
        {
            OnRefreshBatchList();
        }
        #endregion
        private delegate void DelegateRefreshMonitor();
        private void timer1_Tick(object sender, EventArgs e)
        {
            DelegateRefreshMonitor dlgtRefreshMonitor = new DelegateRefreshMonitor(AsynRefreshMonitorView);
            dlgtRefreshMonitor.BeginInvoke(null, dlgtRefreshMonitor);
        }

        private void AsynRefreshMonitorView()
        {
            try
            {
                bool[] conns = null;
                remotMonitorSvc.GetCommDevStatus(ref conns);
                if (conns == null)
                {
                    return;
                }
                for (int i = 0; i < conns.Count(); i++)
                {
                    string picboxName = "pictureBox" + (8 + i).ToString();
                    if (conns[i])
                    {
                        this.tableLayoutPanel7.Controls[picboxName].BackColor = Color.Green;
                    }
                    else
                    {
                        this.tableLayoutPanel7.Controls[picboxName].BackColor = Color.Red;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("发生错误，可能主控系统未开启," + ex.Message);
            }
          
        }
    }

}
