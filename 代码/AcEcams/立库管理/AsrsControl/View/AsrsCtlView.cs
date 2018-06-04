using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Configuration;
using System.ServiceModel;
using System.ServiceModel.Description;
using ModuleCrossPnP;
using LogInterface;
using AsrsInterface;
namespace AsrsControl
{
    public partial class AsrsCtlView : BaseChildView,IAsrsCtlView
    {

        private AsrsCtlPresenter presenter = null;
        private CtlTaskView ctlTaskView = null;
        private PortBufferView portBufView = null;
        private AsrsControl.View.AsrsBatchSettingControl asrsBatchSettingCtl = null;
        private List<UserControl> asrsMonitors = new List<UserControl>();
        public List<UserControl> AsrsMonitors { get { return asrsMonitors; } }
        public AsrsControl.View.AsrsBatchSettingControl AsrsBatchSettingCtl { get { return asrsBatchSettingCtl; } }
        public AsrsCtlView(string captionText)
            : base(captionText)
        {
            InitializeComponent();
            this.Text = captionText;
            presenter = new AsrsCtlPresenter(this);
            this.ctlTaskView = new CtlTaskView("控制任务");
            portBufView = new PortBufferView("入库口缓存信息");
        }
        public void SetAsrsResManage(IAsrsManageToCtl asrsResManage)
        {
            this.presenter.SetAsrsResManage(asrsResManage);
            this.ctlTaskView.SetAsrsResManage(asrsResManage);
            this.asrsBatchSettingCtl.Init(asrsResManage);
        }
        public AsrsCtlPresenter GetPresenter()
        {
            return presenter;
        }
        public void Init()
        {
            //string dbSrc = ConfigurationManager.AppSettings["DBSource"];
           // CtlDBAccess.PubConstant.ConnectionString = string.Format(@"{0}Initial Catalog=AsrsCtlDB;User ID=sa;Password=123456;", dbSrc);
            presenter = new AsrsCtlPresenter(this);
           
            presenter.CtlInit();
            presenter.SetLogRecorder(logRecorder);
            foreach(AsrsCtlModel asrs in presenter.AsrsCtls)
            {
                View.AsrsMonitorUsercontrol ctl = new View.AsrsMonitorUsercontrol(asrs);
                asrsMonitors.Add(ctl);
            }

            //
            this.portBufView.AsrsPorts = presenter.AsrsPorts;
          
            this.asrsBatchSettingCtl = new View.AsrsBatchSettingControl();

            //宿主服务IAsrsCtlToManage
            Uri _baseAddress = new Uri("http://localhost:8733/ZZ/AsrsCtlSvc/AsrsCtl/");
            EndpointAddress _Address = new EndpointAddress(_baseAddress);
            BasicHttpBinding _Binding = new BasicHttpBinding();
            ContractDescription _Contract = ContractDescription.GetContract(typeof(AsrsInterface.IAsrsCtlToManage));
            ServiceEndpoint endpoint = new ServiceEndpoint(_Contract, _Binding, _Address);
            ServiceHost host = new ServiceHost(this.presenter, _baseAddress);
            //添加终结点ABC
            host.Description.Endpoints.Add(endpoint);
            //启用元数据交换
            ServiceMetadataBehavior meta = new ServiceMetadataBehavior();

            meta.HttpGetEnabled = true;
            host.Description.Behaviors.Add(meta);
            host.Open();
        }
       
        private void AsrsCtlView_Load(object sender, EventArgs e)
        {
            //presenter.CtlInit();
        }
        public override void ChangeRoleID(int roleID)
        {
            this.portBufView.ChangeRoleID(roleID);
            this.ctlTaskView.ChangeRoleID(roleID);
        }
        #region IAsrsCtlView接口实现
         public void StartMonitor()
        {
             foreach(UserControl ctl in asrsMonitors)
             {
                 (ctl as AsrsControl.View.AsrsMonitorUsercontrol).StartMonitor();
             }
        }
        public void StopMonitor()
        {
            foreach (UserControl ctl in asrsMonitors)
            {
                (ctl as AsrsControl.View.AsrsMonitorUsercontrol).StopMonitor();
            }
        }
        #endregion
        #region IModuleAttach接口实现
        public override List<string> GetLogsrcList()
        {
            return presenter.GetLogsrcList();
        }
         public override void RegisterMenus(MenuStrip parentMenu, string rootMenuText)
        {
           
            ToolStripMenuItem rootMenuItem = new ToolStripMenuItem(rootMenuText);//parentMenu.Items.Add("仓储管理");
            //rootMenuItem.Click += LoadMainform_MenuHandler;
            parentMenu.Items.Add(rootMenuItem);
            string[] menuItems = new string[] { "控制任务管理", "入库口缓存管理" };
            foreach(string menuStr in menuItems)
            {
                ToolStripItem menuItem = rootMenuItem.DropDownItems.Add(menuStr);
                menuItem.Click += LoadView_MenuHandler;
            }
          
        }
        public override void SetParent(/*Control parentContainer, Form parentForm, */IParentModule parentPnP)
        {
            this.parentPNP = parentPnP;
            ctlTaskView.SetParent(parentPnP);
            
        }
        public override void SetLoginterface(ILogRecorder logRecorder)
        {
            this.logRecorder = logRecorder;
         //   lineMonitorPresenter.SetLogRecorder(logRecorder);
            this.ctlTaskView.SetLoginterface(logRecorder);
           
        }
        #endregion
        private void LoadView_MenuHandler(object sender, EventArgs e)
        {
            ToolStripItem menuItem = sender as ToolStripItem;
            if (menuItem == null)
            {
                return;
            }
            switch (menuItem.Text)
            {
                case "控制任务管理":
                    {
                        this.parentPNP.AttachModuleView(this.ctlTaskView);
                        break;
                    }
                case "入库口缓存管理":
                    {
                        this.parentPNP.AttachModuleView(this.portBufView);
                        break;
                    }
                default:
                    break;
            }


        }
    }
}
