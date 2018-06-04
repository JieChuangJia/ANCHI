using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using AsrsInterface;
using FlowCtlBaseModel;
using ModuleCrossPnP;
using CtlDBAccess.BLL;
using CtlDBAccess.Model;
namespace ProcessCtl
{
    public delegate void DelgateCtlEnable(bool enabled);
    public partial class ProcessMonitorView:BaseChildView,IMonitorView
    {
        private delegate void DlgtRefreshPLCComm();
        private delegate void DelegateRefreshMonitor();
        private delegate void DelegateRefreshRgvStatus(int rgvIndex, bool conn, string mark);
        private delegate void DelegateRefreshCommDevStatus();
        #region 数据
        private delegate void DlgtPopupWelcome();
        private NbProcessPresenter presenter = null;
        private WelcomeForm welcomeDlg = new WelcomeForm();
        private ViewThemColor themColor = new ViewThemColor();
        private Dictionary<EnumNodeStatus, Color> nodeColorMap;
        private List<CtlNodeStatus> nodeStatusList = null;
        private Dictionary<string, UserControlCtlNode> userCtlNodesAsrsA = null;
        private Dictionary<string, UserControlCtlNode> userCtlNodesAsrsB = null;
        private Dictionary<string, UserControlCtlNode> userCtlNodesPack = null;
        private Dictionary<string, UserControlCtlNode> userCtlNodesWelding = null;
        private Dictionary<string, UserControlCtlNode> userCtlNodesAsmTop = null;
        private Dictionary<string, UserControlCtlNode> userCtlNodesAsmBottom = null;
        private Dictionary<string, UserControlCtlNode> userCtlNodesBinding = null;
        private Dictionary<string, UserControlCtlNode> userCtlNodes = null;
        #endregion
        #region 公有接口
        public NbProcessPresenter Presenter { get { return presenter; } }
        public ProcessMonitorView(string captionText)
            : base(captionText)
        {
            InitializeComponent();
            this.Text = captionText;
            presenter = new NbProcessPresenter(this);
        }
        public void SetAsrsPresener(AsrsControl.AsrsCtlPresenter asrsPresenter)
        {
            this.presenter.AsrsCtlPresenter = asrsPresenter;
        }
        public  bool Init()
        {
            
            presenter.SetLogRecorder(logRecorder);
            this.bt_StartSystem.Enabled = true;
            this.bt_StopSystem.Enabled = false;
            this.label12.Text = "系统待启动！";
            if(!presenter.ProcessInit())
            {
                Console.WriteLine("流程控制模块初始化失败");
                this.panel1.Enabled = false;
                this.bt_StartSystem.Enabled = false;
                return false;
            }
            RefreshCommDevStatus();
            this.comboBoxDevList.Items.AddRange(presenter.GetMonitorNodeNames().ToArray());
            this.comboBoxDevList.SelectedIndex = 0;
            return true;
        }
        
        private delegate void DlgtAbortApp();
        public void AbortApp()
        {
            if (this.bt_StartSystem.InvokeRequired)
            {
                DlgtAbortApp dlgt = new DlgtAbortApp(AbortApp);
                this.Invoke(dlgt, null);
            }
            else
            {
                OnStop();
                this.bt_StartSystem.Enabled = false;
                this.bt_StopSystem.Enabled = false;
            }

        }
        public void SetAsrsResManage(IAsrsManageToCtl asrsResManage)
        {
            this.presenter.SetAsrsResManage(asrsResManage) ;

        }
        public void SetAsrsMonitors(List<UserControl> asrsMonitors)
        {
            this.flowLayoutPanelAsrs.Controls.AddRange(asrsMonitors.ToArray());
        }
        public void SetAsrsBatchSetCtl(UserControl asrsBatchSet)
        {
            this.panel14.Controls.Clear();
            this.panel14.Controls.Add(asrsBatchSet);
            asrsBatchSet.Dock = DockStyle.Fill;
        }
        public override List<string> GetLogsrcList()
        {
            List<string> logNodes = presenter.GetMonitorNodeNames();
            return logNodes;
        }
        #endregion
        #region IMonitorView接口实现
        //public void InitNodeMonitorview()
        //{
           
        //    nodeStatusList = new List<CtlNodeStatus>();
        //    userCtlNodes = new Dictionary<string, UserControlCtlNode>();
        //    userCtlNodesAsrsA = new Dictionary<string,UserControlCtlNode>();
        //    this.flowPanel1.Controls.Clear();
        //    string[] nodeIDS = new string[] { "1001", "2001", "2003", "2002"};
        //    Size boxSize = new Size(0, 0);
        //    boxSize.Width = this.flowPanel1.Width / (nodeIDS.Count() + 1);
        //    boxSize.Height = (int)(this.flowPanel1.Height);
        //    for(int i=0;i<nodeIDS.Count();i++)
        //    {
        //        CtlNodeBaseModel CtlNode = presenter.GetNodeByID(nodeIDS[i]);
        //        UserControlCtlNode monitorNode = new UserControlCtlNode();
        //        monitorNode.Size = boxSize;
        //        //监控控件属性赋值
        //        monitorNode.Title = CtlNode.NodeName;
        //        monitorNode.TimerInfo = "40";

        //        monitorNode.RefreshDisp();
        //        userCtlNodesAsrsA[CtlNode.NodeName] = monitorNode;
        //        userCtlNodes[CtlNode.NodeName] = monitorNode;
        //        this.flowPanel1.Controls.Add(monitorNode);
        //       // nodeStatusList.Add(new CtlNodeStatus(CtlNode.NodeName) { ProductBarcode = CtlNode.CurrentStat.ProductBarcode });
        //    }

        //    userCtlNodesAsrsB = new Dictionary<string, UserControlCtlNode>();
        //    this.flowPanel2.Controls.Clear();
        //    nodeIDS = new string[] { "1002", "2011", "2012", "2013"};
        //    boxSize = new Size(0, 0);
        //    boxSize.Width = this.flowPanel2.Width / (nodeIDS.Count() + 1);
        //    boxSize.Height = (int)(this.flowPanel2.Height);
        //    for (int i = 0; i < nodeIDS.Count(); i++)
        //    {
        //        CtlNodeBaseModel CtlNode = presenter.GetNodeByID(nodeIDS[i]);
        //        UserControlCtlNode monitorNode = new UserControlCtlNode();
        //        monitorNode.Size = boxSize;
        //        //监控控件属性赋值
        //        monitorNode.Title = CtlNode.NodeName;
        //        monitorNode.TimerInfo = "40";

        //        monitorNode.RefreshDisp();
        //        userCtlNodesAsrsB[CtlNode.NodeName] = monitorNode;
        //        userCtlNodes[CtlNode.NodeName] = monitorNode;
        //        this.flowPanel2.Controls.Add(monitorNode);
        //       // nodeStatusList.Add(new CtlNodeStatus(CtlNode.NodeName) { ProductBarcode = CtlNode.CurrentStat.ProductBarcode });
        //    }
        //    userCtlNodesWelding = new Dictionary<string, UserControlCtlNode>();
        //    this.flowPanel3.Controls.Clear();
        //    nodeIDS = new string[] { "4001", "4002", "4003", "4004","4005","4006","4007","4008" };
        //    boxSize = new Size(0, 0);
        //    boxSize.Width = this.flowPanel3.Width*2 / (nodeIDS.Count());
        //    boxSize.Height = (int)(this.flowPanel2.Height/2.0f);
        //    for (int i = 0; i < nodeIDS.Count(); i++)
        //    {
        //        CtlNodeBaseModel CtlNode = presenter.GetNodeByID(nodeIDS[i]);
        //        UserControlCtlNode monitorNode = new UserControlCtlNode();
        //        monitorNode.Size = boxSize;
        //        //监控控件属性赋值
        //        monitorNode.Title = "#"+(i+1).ToString();
        //        monitorNode.TimerInfo = "40";

        //        monitorNode.RefreshDisp();
        //        userCtlNodesWelding[CtlNode.NodeName] = monitorNode;
        //        userCtlNodes[CtlNode.NodeName] = monitorNode;
        //        this.flowPanel3.Controls.Add(monitorNode);
        //     //   nodeStatusList.Add(new CtlNodeStatus(CtlNode.NodeName) { ProductBarcode = CtlNode.CurrentStat.ProductBarcode });
        //    }
        //    userCtlNodesPack = new Dictionary<string, UserControlCtlNode>();
        //    this.flowPanel4.Controls.Clear();
        //    nodeIDS = new string[] { "5001", "5002"};
        //    boxSize = new Size(0, 0);
        //    boxSize.Width = this.flowPanel4.Width / (nodeIDS.Count())-20;
        //    boxSize.Height = (int)(this.flowPanel4.Height);
        //    for (int i = 0; i < nodeIDS.Count(); i++)
        //    {
        //        CtlNodeBaseModel CtlNode = presenter.GetNodeByID(nodeIDS[i]);
        //        UserControlCtlNode monitorNode = new UserControlCtlNode();
        //        monitorNode.Size = boxSize;
        //        //监控控件属性赋值
        //        monitorNode.Title = CtlNode.NodeName;
        //        monitorNode.TimerInfo = "40";

        //        monitorNode.RefreshDisp();
        //        userCtlNodesPack[CtlNode.NodeName] = monitorNode;
        //        userCtlNodes[CtlNode.NodeName] = monitorNode;
        //        this.flowPanel4.Controls.Add(monitorNode);
        //       // nodeStatusList.Add(new CtlNodeStatus(CtlNode.NodeName) { ProductBarcode = CtlNode.CurrentStat.ProductBarcode });
        //    }
        //    userCtlNodesAsmTop = new Dictionary<string, UserControlCtlNode>();
        //    this.flowPanel5.Controls.Clear();
        //    boxSize = new Size(0, 0);
        //    boxSize.Width = this.flowPanel5.Width/ 16-4;
        //    boxSize.Height = (int)(this.flowPanel5.Height*2);
        //    for (int i = 0; i < 32;i++ )
        //    {
        //        string nodeID = (6001 + i).ToString();
        //        CtlNodeBaseModel CtlNode = presenter.GetNodeByID(nodeID);
        //        UserControlCtlNode monitorNode = new UserControlCtlNode();
        //        monitorNode.Margin = new Padding(1,1,1,1);
              
        //        monitorNode.Size = boxSize;
        //        //监控控件属性赋值
        //        monitorNode.Title = (i+1).ToString();
        //        monitorNode.TimerInfo = "40";

        //        monitorNode.RefreshDisp();
        //        userCtlNodesAsmTop[CtlNode.NodeName] = monitorNode;
        //        userCtlNodes[CtlNode.NodeName] = monitorNode;
        //        this.flowPanel5.Controls.Add(monitorNode);
        //        //nodeStatusList.Add(new CtlNodeStatus(CtlNode.NodeName) { ProductBarcode = CtlNode.CurrentStat.ProductBarcode });
        //    }

        //    userCtlNodesAsmBottom = new Dictionary<string, UserControlCtlNode>();
        //    this.flowPanel6.Controls.Clear();
        //    boxSize = new Size(0, 0);
        //    boxSize.Width = this.flowPanel6.Width/12-2;
        //    boxSize.Height = (int)(this.flowPanel6.Height * 2);
        //    for (int i = 0; i < 24; i++)
        //    {
        //        string nodeID = (7001 + i).ToString();
        //        CtlNodeBaseModel CtlNode = presenter.GetNodeByID(nodeID);
        //        UserControlCtlNode monitorNode = new UserControlCtlNode();
        //        monitorNode.Margin = new Padding(1);
        //        monitorNode.Size = boxSize;
        //        //监控控件属性赋值
        //        monitorNode.Title = (i + 1).ToString();
        //        monitorNode.TimerInfo = "40";

        //        monitorNode.RefreshDisp();
        //        userCtlNodesAsmBottom[CtlNode.NodeName] = monitorNode;
        //        userCtlNodes[CtlNode.NodeName] = monitorNode;
        //        this.flowPanel6.Controls.Add(monitorNode);
        //        //nodeStatusList.Add(new CtlNodeStatus(CtlNode.NodeName) { ProductBarcode = CtlNode.CurrentStat.ProductBarcode });
        //    }
        //    userCtlNodesBinding = new Dictionary<string, UserControlCtlNode>();
        //    this.flowPanel7.Controls.Clear();
        //    boxSize = new Size(0, 0);
        //    boxSize.Width = this.flowPanel7.Width-2;
        //    boxSize.Height = (int)(this.flowPanel7.Height-2);
        //    {
        //        string nodeID = "8001";
        //        CtlNodeBaseModel CtlNode = presenter.GetNodeByID(nodeID);
        //        UserControlCtlNode monitorNode = new UserControlCtlNode();
        //        monitorNode.Margin = new Padding(1);
        //        monitorNode.Size = boxSize;
        //        //监控控件属性赋值
        //        monitorNode.Title = CtlNode.NodeName;
        //        monitorNode.TimerInfo = "40";

        //        monitorNode.RefreshDisp();
        //        userCtlNodesBinding[CtlNode.NodeName] = monitorNode;
        //        userCtlNodes[CtlNode.NodeName] = monitorNode;
        //        this.flowPanel7.Controls.Add(monitorNode);
        //    }
        //    nodeColorMap = new Dictionary<EnumNodeStatus, Color>();
        //    nodeColorMap[EnumNodeStatus.设备故障] = Color.Red;
        //    nodeColorMap[EnumNodeStatus.设备空闲] = Color.Green;
        //    nodeColorMap[EnumNodeStatus.设备使用中] = Color.Yellow;
            
        //    nodeColorMap[EnumNodeStatus.无法识别] = Color.PaleVioletRed;
        //    this.pictureBox1.BackColor = nodeColorMap[EnumNodeStatus.设备故障];
        //    this.pictureBox2.BackColor = nodeColorMap[EnumNodeStatus.设备空闲];
        //    this.pictureBox3.BackColor = nodeColorMap[EnumNodeStatus.设备使用中];
          
        //    this.pictureBox5.BackColor = nodeColorMap[EnumNodeStatus.无法识别];
        //}
        public void RefreshNodeStatus()
        {
            List<CtlNodeStatus> ns = this.presenter.GetNodeStatus();
            for (int i = 0; i < ns.Count(); i++)
            {
                if (!userCtlNodes.Keys.Contains(ns[i].NodeName))
                {
                    continue;
                }
                CtlNodeStatus nodeStatus = ns[i];
                
                //ns[i].Copy(ref nodeStatus);
                UserControlCtlNode monitorNode = userCtlNodes[nodeStatus.NodeName];
                monitorNode.BkgColor = nodeColorMap[nodeStatus.Status];
                monitorNode.DispDetail = nodeStatus.StatDescribe;
                monitorNode.RefreshDisp();
                monitorNode.DispPopupInfo = "主机条码：" + nodeStatus.ProductBarcode;

            }
        }
        public void PopupMes(string strMes)
        {
            MessageBox.Show(strMes);
        }
        public void WelcomeAddStartinfo(string info)
        {
            this.welcomeDlg.AddDispContent(info);
        }
        public void WelcomeDispCurinfo(string info)
        {

            this.welcomeDlg.DispCurrentInfo(info);
        }
        public void AsynWelcomePopup()
        {

            this.welcomeDlg.ShowDialog();
        }
        public void WelcomePopup()
        {

            DlgtPopupWelcome dlgt = new DlgtPopupWelcome(AsynWelcomePopup);
            dlgt.BeginInvoke(null, null);

        }
        public void WelcomeClose()
        {
            this.welcomeDlg.CloseDisp();
            this.welcomeDlg = null;
        }
       
        #endregion
        #region UI事件
        private void ProcessMonitorView_Load(object sender, EventArgs e)
        {
            //仿真模拟
            if (SysCfg.SysCfgModel.SimMode)
            {
                this.comboBoxDB2.Items.Clear();
                for (int i = 0; i < 5; i++)
                {
                    this.comboBoxDB2.Items.Add((i + 1).ToString());
                }
                this.comboBoxDB2.SelectedIndex = 0;

                this.comboBoxBarcodeGun.Items.AddRange(new string[] { "1", "2", "3" });
                this.comboBoxBarcodeGun.SelectedIndex = 0;
            }
            else 
            { 
                //this.groupBoxCtlSim.Visible = false;
                foreach(Control ctl in this.groupBoxCtlSim.Controls)
                {
                    ctl.Visible = false;
                }
            }
            if (SysCfg.SysCfgModel.RfidSimMode)
            {
                label7.Visible = true;
                this.textBoxRfidVal.Visible = true;
                this.buttonRfidSimWrite.Visible = true;
            }
            this.tabControl1.TabPages.RemoveAt(2);
            this.tabControl1.TabPages.RemoveAt(2);
            
        }
        private void bt_StartSystem_Click(object sender, EventArgs e)
        {
            if (OnStart())
            {
                this.bt_StartSystem.Enabled = false;
                this.bt_StopSystem.Enabled = true;
                this.label12.Text = "系统正在运行";
            }
           
        }

        private void bt_StopSystem_Click(object sender, EventArgs e)
        {
            if (OnStop())
            {
                this.bt_StartSystem.Enabled = true;
                this.bt_StopSystem.Enabled = false;
                this.label12.Text = "系统已经停止";
            }
        }

        private void bt_ExitSys_Click(object sender, EventArgs e)
        {
            OnExit();
            //presenter.ExitSystem();
        }
        private bool OnStart()
        {
            this.presenter.StartRun();
            this.timerNodeStatus.Start();
            return true;
        }
        private bool OnStop()
        {
            this.timerNodeStatus.Stop();
            this.presenter.PauseRun();
            return true;
        }
        public bool OnExit()
        {
            if (0 == PoupAskmes("确定要退出系统？"))
            {
                return false;
            }
           // if (presenter.NeedSafeClosing())
            {
                ClosingWaitDlg dlg = new ClosingWaitDlg();
                if (DialogResult.Cancel == dlg.ShowDialog())
                {
                    return false;
                }
            }
            OnStop();
            this.presenter.ExitSystem();
            System.Environment.Exit(0);
            return true;
        }

        private void timerNodeStatus_Tick(object sender, EventArgs e)
        {
            DelegateRefreshMonitor dlgtRefreshMonitor = new DelegateRefreshMonitor(AsynRefreshMonitorView);
            dlgtRefreshMonitor.BeginInvoke(null, dlgtRefreshMonitor);

            
            // RefreshNodeStatus();

            //if (this.checkBoxAutorefresh.Checked)
            //{
            //    RefreshPLCComm();
            //}
        }
        private void AsynRefreshMonitorView()
        {
            // RefreshNodeStatus();

            if (this.checkBoxAutorefresh.Checked)
            {
                RefreshPLCComm();
            }
            RefreshCommDevStatus();
           // RefreshStackerStatus();
           // RefreshRgvSwitch();
        }
        private void ProcessMonitorView_SizeChanged(object sender, EventArgs e)
        {
            if (this.userCtlNodesAsrsA != null)
            {
                Size monitorBoxSize = new Size(this.flowPanel1.Width / (this.userCtlNodesAsrsA.Count() + 1), (int)(this.flowPanel1.Height));
                foreach (string nodeKey in this.userCtlNodesAsrsA.Keys)
                {
                    UserControl monitorNode = this.userCtlNodesAsrsA[nodeKey];
                    monitorNode.Size = monitorBoxSize;
                }
            }
            if (this.userCtlNodesAsrsB != null)
            {
                Size monitorBoxSize = new Size(this.flowPanel2.Width / (this.userCtlNodesAsrsB.Count() + 1), (int)(this.flowPanel2.Height));
                foreach (string nodeKey in this.userCtlNodesAsrsB.Keys)
                {
                    UserControl monitorNode = this.userCtlNodesAsrsB[nodeKey];
                    monitorNode.Size = monitorBoxSize;
                }
            }
            if(this.userCtlNodesWelding != null)
            {
                Size monitorBoxSize = new Size(this.flowPanel3.Width * 2 / (this.userCtlNodesWelding.Count()+1), (int)(this.flowPanel3.Height/2.0f));
                foreach (string nodeKey in this.userCtlNodesWelding.Keys)
                {
                    UserControl monitorNode = this.userCtlNodesWelding[nodeKey];
                    monitorNode.Size = monitorBoxSize;
                }
            }
            if(this.userCtlNodesPack != null)
            {
                Size monitorBoxSize = new Size(this.flowPanel4.Width/ (this.userCtlNodesPack.Count())-20, (int)(this.flowPanel4.Height));
                foreach (string nodeKey in this.userCtlNodesPack.Keys)
                {
                    UserControl monitorNode = this.userCtlNodesPack[nodeKey];
                    monitorNode.Size = monitorBoxSize;
                }
            }
            if (this.userCtlNodesAsmTop!= null)
            {
                Size monitorBoxSize = new Size(this.flowPanel5.Width*2 / (this.userCtlNodesAsmTop.Count())-2, (int)(this.flowPanel5.Height/2.0));
                foreach (string nodeKey in this.userCtlNodesAsmTop.Keys)
                {
                    UserControl monitorNode = this.userCtlNodesAsmTop[nodeKey];
                    monitorNode.Size = monitorBoxSize;
                }
            }
            if(this.userCtlNodesAsmBottom != null)
            {
                Size monitorBoxSize = new Size(this.flowPanel6.Width * 2 / (this.userCtlNodesAsmBottom.Count())-2, (int)(this.flowPanel6.Height / 2.0));
                foreach (string nodeKey in this.userCtlNodesAsmBottom.Keys)
                {
                    UserControl monitorNode = this.userCtlNodesAsmBottom[nodeKey];
                    monitorNode.Size = monitorBoxSize;
                }
            }
            if(this.userCtlNodesBinding != null)
            {
                Size monitorBoxSize = new Size(this.flowPanel7.Width - 2, (int)(this.flowPanel7.Height- 2));
                foreach (string nodeKey in this.userCtlNodesBinding.Keys)
                {
                    UserControl monitorNode = this.userCtlNodesBinding[nodeKey];
                    monitorNode.Size = monitorBoxSize;
                }
            }
        }


        //private void btnWarnReset_Click(object sender, EventArgs e)
        //{
        //    if(sender == this.btnWarnReset)
        //    {
        //        //logRecorder.AddLog(new LogInterface.LogModel("流程监控", "A库堆垛机报警复位", LogInterface.EnumLoglevel.提示));
        //        presenter.StackerErrorReset(1);
        //    }
        //    else if(sender == this.btnWarnReset2)
        //    {
        //        //logRecorder.AddLog(new LogInterface.LogModel("流程监控", "B库堆垛机报警复位", LogInterface.EnumLoglevel.提示));
        //        presenter.StackerErrorReset(2);
        //    }
        //}
        #endregion
        #region 通信数据监控与流程仿真
        private void buttonRefreshDevStatus_Click(object sender, EventArgs e)
        {
            RefreshPLCComm();

        }
        private void buttonDB2SimSet_Click(object sender, EventArgs e)
        {
            if (!SysCfg.SysCfgModel.SimMode)
            {
                MessageBox.Show("当前不处于仿真模式");
                return;
            }
            try
            {
                string devID = this.comboBoxDevList.Text;
                int itemID = int.Parse(comboBoxDB2.Text);
                presenter.SimSetDB2(devID, itemID, int.Parse(this.textBoxDB2ItemVal.Text));

                RefreshPLCComm();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }

        }
        private void comboBoxDevList_SelectedIndexChanged(object sender, EventArgs e)
        {
            RefreshPLCComm();
        }
    
        private void RefreshPLCComm()
        {
            
            
            if(this.dataGridViewDevDB1.InvokeRequired)
            {
                DlgtRefreshPLCComm dlgtRefresh = new DlgtRefreshPLCComm(RefreshPLCComm);
                this.Invoke(dlgtRefresh);
            }
            else
            {
                string nodeName = this.comboBoxDevList.Text;

                DataTable dt1 = null;
                DataTable dt2 = null;
                //DataTable dtTask = null;
                string taskDetail = "";
                if (!presenter.GetDevRunningInfo(nodeName, ref dt1, ref dt2, ref taskDetail))
                {
                    Console.WriteLine("刷新工位信息失败");
                    return;
                }
                this.dataGridViewDevDB1.DataSource = dt1;
                for (int i = 0; i < this.dataGridViewDevDB1.Columns.Count; i++)
                {
                    this.dataGridViewDevDB1.Columns[i].SortMode = DataGridViewColumnSortMode.NotSortable;
                    this.dataGridViewDevDB1.Columns[i].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                }
                this.dataGridViewDevDB1.Columns[this.dataGridViewDevDB1.Columns.Count - 1].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                this.dataGridViewDevDB2.DataSource = dt2;
                for (int i = 0; i < this.dataGridViewDevDB2.Columns.Count; i++)
                {
                    this.dataGridViewDevDB2.Columns[i].SortMode = DataGridViewColumnSortMode.NotSortable;
                    this.dataGridViewDevDB2.Columns[i].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                }
                this.dataGridViewDevDB2.Columns[this.dataGridViewDevDB2.Columns.Count - 1].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                this.richTextBoxTaskInfo.Text = taskDetail;

               
            }
            
        }
        private void RefreshRgvSwitch()
        {
             if(this.dataGridView1.InvokeRequired)
             {
                 DlgtRefreshPLCComm dlgtRefreshRgv = new DlgtRefreshPLCComm(RefreshRgvSwitch);
                 this.Invoke(dlgtRefreshRgv);
             }
             else
             {
                 DataTable dtMainPlc1 = new DataTable();
                 DataTable dtRgvPlc1 = new DataTable();
                 string reStr = "";
                 presenter.GetRgvSwitch(1, ref dtMainPlc1, ref dtRgvPlc1, ref reStr);
                 this.dataGridView1.DataSource = dtMainPlc1;
                 this.dataGridView2.DataSource = dtRgvPlc1;

                 presenter.GetRgvSwitch(2, ref dtMainPlc1, ref dtRgvPlc1, ref reStr);
                 this.dataGridView3.DataSource = dtMainPlc1;
                 this.dataGridView4.DataSource = dtRgvPlc1;
             }
        }
        private void RefreshCommDevStatus()
        {
            if(this.tableLayoutPanel7.InvokeRequired)
            {
                DelegateRefreshCommDevStatus dlgt = new DelegateRefreshCommDevStatus(RefreshCommDevStatus);
                this.Invoke(dlgt, null);
            }
            else
            {
                if(SysCfg.SysCfgModel.ZhuyeMode == 1)
                {
                    this.label31.Text = "一次注液模式：一步完成";
                }
                else if (SysCfg.SysCfgModel.ZhuyeMode==2)
                {
                    this.label31.Text = "一次注液模式：两步完成";
                }
                else
                {
                    this.label31.Text = "一次注液模式：无效模式";
                }
                bool[] conn = null;
                presenter.GetCommDevStatus(ref conn);
                for (int i = 0; i < conn.Count(); i++)
                {
                    string picboxName = "pictureBox" + (8 + i).ToString();
                    if (conn[i])
                    {
                        this.tableLayoutPanel7.Controls[picboxName].BackColor = Color.Green;
                    }
                    else
                    {
                        this.tableLayoutPanel7.Controls[picboxName].BackColor = Color.Red;
                    }
                }
                
                   
            }
        }
        //private void RefreshStackerStatus()
        //{
        //    if(this.groupBox4.InvokeRequired)
        //    {
        //        DelegateRefreshCommDevStatus dlgt = new DelegateRefreshCommDevStatus(RefreshStackerStatus);
        //        this.Invoke(dlgt, null);
        //    }
        //    else
        //    {
        //        string[] status=null;
        //        int errCode1 = 0;
        //        presenter.GetStackerStatus(1, ref errCode1,ref status);
        //        if(errCode1 == 0)
        //        {
        //            this.label31.BackColor = Color.Transparent;
        //        }
        //        else
        //        {
        //            this.label31.BackColor = Color.Red;
        //        }
        //        this.label31.Text = status[0];
        //        this.label32.Text = status[1];
        //        this.label33.Text = status[2];

        //        int errCode2 = 0;
        //        presenter.GetStackerStatus(2, ref errCode2,ref status);
        //        if (errCode2 == 0)
        //        {
        //            this.label34.BackColor = Color.Transparent;
        //        }
        //        else
        //        {
        //            this.label34.BackColor = Color.Red;
        //        }
        //        this.label34.Text = status[0];
        //        this.label35.Text = status[1];
        //        this.label36.Text = status[2];
        //    }
        //}
        private void buttonRfidSimWrite_Click(object sender, EventArgs e)
        {
            try
            {
                string nodeName = this.comboBoxDevList.Text;
                string rfidVal = this.textBoxRfidVal.Text;
                presenter.SimSetRFID(nodeName, rfidVal);
                string barcode = this.textBoxBarcode.Text;
                presenter.SimSetBarcode(nodeName, barcode);
                
            }
            catch (System.Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
        private void flowLayoutPanelAsrs_SizeChanged(object sender, EventArgs e)
        {
            foreach(Control ctl in this.flowLayoutPanelAsrs.Controls)
            {
                ctl.Width = this.flowLayoutPanelAsrs.Width / 2 - 20;
                ctl.Height = this.flowLayoutPanelAsrs.Height / 2 - 20;
            }
        }
        #endregion     

        private void btnRfidReconn_Click(object sender, EventArgs e)
        {
            foreach (DevInterface.IrfidRW rfid in presenter.rfidRWs)
            {
                rfid.Disconnect();
                if (rfid.Connect())
                {
                    string log = string.Format("{0} 号RFID重连成功", rfid.ReaderID);
                    Console.WriteLine(log);
                }
                else
                {
                    string log = string.Format("{0} 号RFID重连失败", rfid.ReaderID);
                    Console.WriteLine(log);
                }
            }
        }

      
    }
}
