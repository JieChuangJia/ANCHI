using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Threading;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.ServiceModel;
using System.ServiceModel.Description;
using LogInterface;
using FlowCtlBaseModel;
using AsrsInterface;
using AsrsControl;
using DevInterface;
using DevAccess;
using CtlDBAccess.Model;
//using EquipService;
namespace ProcessCtl
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class NbProcessPresenter : IRemoteMonitorSvc
    {
        #region 数据
        private IAsrsManageToCtl asrsResourceManage = null; //立库管理层接口对象
        private string objectName = "流程控制";
        private IMonitorView view = null;
        private List<IPlcRW> plcRWs = null; //plc读写对象列表
        public List<IrfidRW> rfidRWs = null;
        private List<IBarcodeRW> barcodeRWList = null;
        private List<IHKAccess> hkAccessList = null;
        private OcvAccess ocvAccess = null;
       // private List<CtlNodeBaseModel> nodeList = null; //控制节点
        private List<CtlNodeStatus> nodeStatusList = null;
        private List<CtlNodeBaseModel> monitorNodeList = null;
        private List<ThreadRunModel> threadList = null;
        private AsrsCtlPresenter asrsCtlPresenter = null;
        protected ILogRecorder logRecorder = null;
        private ThreadBaseModel historyDataClearThread = null; //历史数据清理线程，最多保持15天记录
       // private EquipService mesAcc = null;
        #endregion
        #region 公有
        public YHMesWrapper MesAcc { get; set; }
        public ILogRecorder LogRecorder { get { return logRecorder; } set { logRecorder = value; } }
        public AsrsCtlPresenter AsrsCtlPresenter { get { return asrsCtlPresenter; } set { asrsCtlPresenter = value; } }

        public List<IHKAccess> HkAccessList { get { return hkAccessList; } }
        public OcvAccess OcvAccessObj { get { return ocvAccess; } set { ocvAccess = value; } }
        public NbProcessPresenter(IMonitorView view)
        {
            this.view = view;
        }
        public void SetLogRecorder(ILogRecorder logRecorder)
        {
            this.logRecorder = logRecorder;
            
        }
        public void SetAsrsResManage(IAsrsManageToCtl asrsResManage)
        {
            this.asrsResourceManage = asrsResManage;
            NodeSwitch nodeSwitch = GetNodeByID("7001") as NodeSwitch;
            nodeSwitch.AsrsResManage = asrsResManage;
        }
       
        public bool ProcessInit()
        {
           // nodeList = new List<CtlNodeBaseModel>();
            MesAcc = new YHMesWrapper();

            monitorNodeList = new List<CtlNodeBaseModel>();
            this.nodeStatusList = new List<CtlNodeStatus>();
            //解析配置文件
            string reStr = "";
            //string xmlCfgFile = System.AppDomain.CurrentDomain.BaseDirectory + @"data/AcCfg.xml";
            XElement root = null;
            if (!SysCfg.SysCfgModel.LoadCfg(ref root,ref reStr))
            {
                Console.WriteLine("系统配置解析错误,{0}", reStr);
                return false;
            }
           
           
            //1 通信设备创建、初始化
            hkAccessList = new List<IHKAccess>();
            if (SysCfg.SysCfgModel.SimMode)
            {
                plcRWs = new List<IPlcRW>();
                for(int i=0;i<5;i++)
                {
                    IPlcRW plcRW = new PlcRWSim();
                    plcRW.PlcID = i + 1;
                    plcRWs.Add(plcRW);
                }
                rfidRWs = new List<IrfidRW>();
                for (int i = 0; i < 13; i++)
                {
                    int rfidID =  i + 1;
                    IrfidRW rfidRW = new rfidRWSim();
                    rfidRW.ReaderID = (byte)rfidID;
                    rfidRWs.Add(rfidRW);
                }
                HKAccess hk = new HKAccess(1,"127.0.0.1", 13535);
                hkAccessList.Add(hk);
                hk = new HKAccess(2, "127.0.0.1", 13535);
                hkAccessList.Add(hk);
                if(!hk.Conn(ref reStr))
                {
                    Console.WriteLine(reStr);
                }
                string ocvDBConn = "Data Source = 192.168.100.20;Initial Catalog=SRANCH;User ID=L_Guest;Password=Guest@123;"; ; // 
                ocvAccess = new OcvAccess(ocvDBConn, 36); 
            }
            else
            {
                
                XElement commDevXERoot = root.Element("CommDevCfg");
                if (!ParseCommDevCfg(commDevXERoot, ref reStr))
                {
                    //logRecorder.AddLog(new LogModel(objectName, "PLC初始化错误,"+reStr, EnumLoglevel.错误));
                    Console.WriteLine("通信设备错误，" + reStr);
                    return false;
                }
            }

            //2 控制节点初始化
            XElement CtlnodeRoot = root.Element("CtlNodes");
            if (!ParseCtlnodes(CtlnodeRoot, ref reStr))
            {
                logRecorder.AddLog(new LogModel(objectName, "节点初始化错误," + reStr, EnumLoglevel.错误));
                return false;
            }
            foreach (AsrsCtlModel asrsModel in asrsCtlPresenter.AsrsCtls)
            {
                monitorNodeList.Add(asrsModel.StackDevice);
                monitorNodeList.AddRange(asrsModel.Ports.ToArray());
            }

            //3 工位控制线程初始化
            this.threadList = new List<ThreadRunModel>();
            XElement ThreadnodeRoot = root.Element("ThreadAlloc");
            if (!ParseTheadNodes(ThreadnodeRoot, ref reStr))
            {
                logRecorder.AddLog(new LogModel(objectName, "任务分配错误," + reStr, EnumLoglevel.错误));
                return false;
            }
            NodeSwitch switchNode = GetNodeByID("7001") as NodeSwitch;
            List<AsrsControl.AsrsPortalModel> targetPorts = new List<AsrsPortalModel>();
            foreach(string portID in switchNode.TargetPortIDs)
            {
                AsrsPortalModel portObj = GetNodeByID(portID) as AsrsControl.AsrsPortalModel;
                targetPorts.Add(portObj);
            }
            switchNode.TargetPorts = targetPorts;

            foreach (ThreadRunModel threadObj in threadList)
            {
                threadObj.LogRecorder = logRecorder;
            }

            for (int i = 0; i < this.monitorNodeList.Count(); i++)
            {
                CtlNodeBaseModel node = this.monitorNodeList[i];
                node.LogRecorder = this.logRecorder;
                this.nodeStatusList.Add(node.CurrentStat);
            }
            
            //ThreadRunModel palletBindThread = new ThreadRunModel("工装板绑定");
            //palletBindThread.TaskInit();
            //CtlNodeBaseModel palletBindNode = GetNodeByID("6001");
            //if(palletBindNode != null && palletBindNode.NodeEnabled)
            //{
            //    palletBindThread.AddNode(palletBindNode);
            //}
            //this.threadList.Add(palletBindThread);

            //4 plc通信线程初始化

            //5 历史数据清理线程
            historyDataClearThread = new ThreadBaseModel("历史数据集清理");
            
            historyDataClearThread.SetThreadRoutine(ClearHistoryLoop);
            historyDataClearThread.LoopInterval = 60000;
            historyDataClearThread.TaskInit();


            //5 通信对象分配
            asrsCtlPresenter.PlcRWs = this.plcRWs;
            asrsCtlPresenter.RfidRWs = this.rfidRWs;
            asrsCtlPresenter.BarcodeRWs = this.barcodeRWList;
            foreach(AsrsCtlModel asrsCtl in asrsCtlPresenter.AsrsCtls)
            {
                asrsCtl.MesAcc = this.MesAcc;
                if(asrsCtl.HouseName== AsrsModel.EnumStoreHouse.A1库房.ToString())
                {
                    asrsCtl.dlgtGetTaskTorun = GetCheckoutOfA1;
                }
            }
            if(!asrsCtlPresenter.AsrsCommCfg())
            {
                return false;
            }
           
            //view.InitNodeMonitorview();
            bool re = asrsCtlPresenter.DevStatusRestore();
            if(!re)
            {
                return false;
            }
            return true;
        }
        public bool StartRun()
        {
            string reStr = "";
            this.historyDataClearThread.TaskStart(ref reStr);
            Thread.Sleep(200);
            foreach (ThreadRunModel threadObj in this.threadList)
            {

                if (!threadObj.TaskStart(ref reStr))
                {
                    Console.WriteLine(reStr);
                }
            }
            if (!asrsCtlPresenter.StartRun())
            {
                Console.WriteLine("立库控制系统启动失败");
            }
           
            if(!this.historyDataClearThread.TaskStart(ref reStr))
            {
                Console.WriteLine("历史数据清理线程启动失败," + reStr);
            }

            return true;
        }
        public bool PauseRun()
        {
           
            foreach (ThreadRunModel threadObj in this.threadList)
            {
                threadObj.TaskPause();
            }
            this.asrsCtlPresenter.PauseRun();
           
            this.historyDataClearThread.TaskPause();
            return true;
        }
        public void ExitSystem()
        {
            string reStr = "";
            foreach (ThreadRunModel threadObj in this.threadList)
            {

                if (!threadObj.TaskExit(ref reStr))
                {
                    Console.WriteLine(reStr);
                }
            }
            // this.mesUploadThread.TaskExit(ref reStr);

            this.historyDataClearThread.TaskExit(ref reStr);
            this.asrsCtlPresenter.ExitRun();
           

        }
        public CtlNodeBaseModel GetNodeByID(string nodeID)
        {
            foreach(CtlNodeBaseModel node in monitorNodeList)
            {
                if(node.NodeID == nodeID)
                {
                    return node;
                }
            }
            return null;
        }
        public List<CtlNodeStatus> GetNodeStatus()
        {
            return this.nodeStatusList;
        }
        #endregion
        #region 仿真模拟有关
        private CtlNodeBaseModel GetMonitorNode(string nodeName)
        {

            foreach (CtlNodeBaseModel node in monitorNodeList)
            {
                if (node.NodeName == nodeName)
                {
                    return node;
                }
            }
            return null;
        }
        public List<string> GetMonitorNodeNames()
        {
            List<string> names = new List<string>();
            foreach (CtlNodeBaseModel node in monitorNodeList)
            {
                if(node.NodeEnabled)
                {
                    names.Add(node.NodeName);
                }
               
            }
            return names;
        }
        public bool GetDevRunningInfo(string nodeName, ref DataTable db1Dt, ref DataTable db2Dt, ref string taskDetail)
        {
            CtlNodeBaseModel node = GetMonitorNode(nodeName);
            if (node == null)
            {
                return false;
            }
            //任务
            db1Dt = node.GetDB1DataDetail();
            db2Dt = node.GetDB2DataDetail();
            taskDetail = node.GetRunningTaskDetail();
            return true;
        }
      

        public void GetStackerStatus(int stackerIndex,ref int errCode,ref string[] status)
        {
            asrsCtlPresenter.AsrsCtls[stackerIndex - 1].StackDevice.GetRunningStatus(ref errCode,ref status);
        }

        public void StackerErrorReset(int stackerIndex)
        {
            string reStr="";
            AsrsStackerCtlModel stacker = asrsCtlPresenter.AsrsCtls[stackerIndex - 1].StackDevice;
            if(!asrsCtlPresenter.AsrsCtls[stackerIndex - 1].StackDevice.ErrorReset(ref reStr))
            {
                view.PopupMes(reStr);
            }
            else
            {
                logRecorder.AddLog(new LogInterface.LogModel(objectName, string.Format("{0}堆垛机报警复位完成",stacker.NodeName), LogInterface.EnumLoglevel.提示));
            }
        }
        public bool GetRgvSwitch(int rgvIndex,ref DataTable dtMainplc,ref DataTable dtRgvplc,ref string reStr)
        {
            try
            {
                IPlcRW plcMainPlc = null;
                IPlcRW plcRgv = null;
                dtMainplc = new DataTable();
                dtMainplc.Columns.Add("索引");
                dtMainplc.Columns.Add("地址");
                dtMainplc.Columns.Add("内容");
                dtRgvplc = new DataTable();
                dtRgvplc.Columns.Add("索引");
                dtRgvplc.Columns.Add("地址");
                dtRgvplc.Columns.Add("内容");
                if(rgvIndex == 1)
                {
                    plcMainPlc = plcRWs[2];
                    plcRgv = plcRWs[4];
                }
                else if(rgvIndex == 2)
                {
                    plcMainPlc = plcRWs[2];
                    plcRgv = plcRWs[4];
                }
                else
                {
                    reStr = "RGV序号参数错误";
                    return false;
                }
                string stAddr1 = "D2000";
                string stAddr2 = "D2500";
                int blockNum=10;
                short[] val1 = new short[blockNum];
                if (plcMainPlc.ReadMultiDB(stAddr2,blockNum,ref val1))
                {
                    for (int i = 0; i < 10; i++)
                    {
                        string addr = "D" + (int.Parse(stAddr1.Substring(1)) + i).ToString().PadLeft(4, '0');
                        dtMainplc.Rows.Add(i + 1, addr, val1[i]);
                    }
                }
                
                short[] val2 = new short[blockNum];
                if(plcRgv.ReadMultiDB(stAddr1,blockNum,ref val2))
                {
                    for (int i = 0; i < 10; i++)
                    {
                        string addr = "D" + (int.Parse(stAddr2.Substring(1)) + i).ToString().PadLeft(4, '0');
                        dtRgvplc.Rows.Add(i + 1, addr, val2[i]);
                    }
                }
                
                return true;
            }
            catch (Exception ex)
            {
                reStr = "获取RGV中转数据发生异常"+ex.ToString();
                return false;
            }
        }

        public bool SimSetDB2(string nodeName, int dbItemID, int val)
        {
            CtlNodeBaseModel node = GetMonitorNode(nodeName);
            if (node == null)
            {
                Console.WriteLine("工位：" + nodeName + " 不存在");
                return false;
            }
            node.DicCommuDataDB2[dbItemID].Val = val;
            return true;
        }
        public void SimSetRFID(string nodeName, string strUID)
        {
            //if(rfidID<1 || rfidID>rfidRWs.Count())
            //{
            //    Console.WriteLine("RFID ID错误");
            //    return;
            //}
            CtlNodeBaseModel node = GetMonitorNode(nodeName);
            if (node == null)
            {
                Console.WriteLine("工位：" + nodeName + " 不存在");
                return;
            }
            node.SimRfidUID = strUID;
           
        }
        public void SimSetBarcode(string nodeName,string barcode)
        {
            CtlNodeBaseModel node = GetMonitorNode(nodeName);
            if (node == null)
            {
                Console.WriteLine("工位：" + nodeName + " 不存在");
                return;
            }
            node.SimBarcode = barcode;
        }
        #endregion
        #region IRemoveSvc接口实现
         //获取设备连接状态
        public void GetCommDevStatus(ref bool[] conn)
        {
            int devNum = this.plcRWs.Count();
            conn = new bool[devNum];
            for (int i = 0; i < devNum; i++)
            {
                if (plcRWs[i].IsConnect)
                {
                    conn[i] = true;
                }
                else
                {
                    conn[i] = false;
                }
            }
        }

        //设置出入库批次
        public bool SetBatch(int inoutFlag, string houseName, string batch,ref string reStr)
        {
            if (inoutFlag == 1)
            {
                SysCfg.SysCfgModel.CheckinBatchDic[houseName] = batch;
            }
            else
            {
                SysCfg.SysCfgModel.CheckoutBatchDic[houseName] = batch;
            }


            if (!SysCfg.SysCfgModel.SaveCfg(ref reStr))
            {
               reStr="保存设置失败:" + reStr;
               return false;
            }
            return true;
        }

        //获取在库所有批次 
        public List<string> GetStoreBatchs(string houseName)
        {
            List<string> batchs = new List<string>();
            if (this.asrsResourceManage.GetStockProductBatch(houseName, ref batchs))
            {
                return batchs;
            }
            else
            {
                return null;
            }

        }

        //获取入库批次设置（多个库房）
        public List<string> GetCheckinBatchSet()
        {
            List<string> batchs = new List<string>();
            batchs.AddRange(new string[] { SysCfg.SysCfgModel.CheckinBatchDic["A1库房"], SysCfg.SysCfgModel.CheckinBatchDic["B1库房"] });
            return batchs;
        }

        //获取出库批次设置（多个库房）
        public List<string> GetCheckoutBatchSet()
        {
            List<string> batchs = new List<string>();
            batchs.AddRange(new string[] { SysCfg.SysCfgModel.CheckoutBatchDic["A1库房"], SysCfg.SysCfgModel.CheckoutBatchDic["B1库房"] });
            return batchs;
        }
        #endregion
        #region 私有
        private void ClearHistoryLoop()
        {
            //throw new NotImplementedException();
            try
            {
              // if(!SysCfg.SysCfgModel.SimMode)
               {
                   CtlDBAccess.BLL.SysLogBll logBll = new CtlDBAccess.BLL.SysLogBll();
                   logBll.ClearHistorydata();
                   CtlDBAccess.BLL.ControlTaskBll ctlTaskBll = new CtlDBAccess.BLL.ControlTaskBll();
                   ctlTaskBll.ClearHistorydata(new string[] { "已完成", "错误", "任务撤销" });
                   MesDBAccess.BLL.ProduceRecordBll recordBll = new MesDBAccess.BLL.ProduceRecordBll();
                   recordBll.ClearHistorydata(30);
                   this.asrsResourceManage.DeletePreviousData(30);
               }
               
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            
        }
     
        private bool ParseCtlnodes(XElement CtlnodeRoot, ref string reStr)
        {
            if (CtlnodeRoot == null)
            {
                reStr = "系统配置文件错误，不存在CtlNodes节点";
                return false;
            }
            try
            {
                IEnumerable<XElement> nodeXEList =
                from el in CtlnodeRoot.Elements()
                where el.Name == "Node"
                select el;
                foreach (XElement el in nodeXEList)
                {
                    string className = (string)el.Attribute("className");
                    CtlNodeBaseModel ctlNode = null;
                    switch (className)
                    {
                       
                        case "NbProcessCtl.NodePalletBind":
                            {
                                ctlNode = new NodePalletBind();
                             
                                break;
                            }
                        case "NbProcessCtl.NodeOcv":
                            {
                                ctlNode = new NodeOcv();
                                break;
                            }
                        case "NbProcessCtl.NodeGrasp":
                            {
                                NodeGrasp graspNode = new NodeGrasp();
                               // graspNode.MesAcc = this.mesAcc;
                                ctlNode = graspNode;
                                break;
                            }
                        case "NbProcessCtl.NodeVirStation":
                            {
                                ctlNode = new NodeVirStation();
                                break;
                            }
                        case "NbProcessCtl.NodeSwitch":
                            {
                                ctlNode = new NodeSwitch();
                                break;
                            }
                        default:
                            break;
                    }
                    if (ctlNode != null)
                    {
                        ctlNode.MesAcc = this.MesAcc;
                        if (!ctlNode.BuildCfg(el, ref reStr))
                        {
                            return false;
                        }
                        if (className == "NbProcessCtl.NodePalletBind")
                        {
                            NodePalletBind bndNode = ctlNode as NodePalletBind;
                            bndNode.hkAccess = GetHKAccessByID(bndNode.HkServerID);
                        }
                        if (className == "NbProcessCtl.NodeGrasp")
                        {
                            NodeGrasp graspNode = ctlNode as NodeGrasp;
                            graspNode.OcvAccess = ocvAccess;
                        }
                        if (className == "NbProcessCtl.NodeSwitch")
                        {
                            NodeSwitch switchNode = ctlNode as NodeSwitch;
                            switchNode.OcvAccess = ocvAccess;
                        }
                        ctlNode.PlcRW = GetPlcByID(ctlNode.PlcID); // this.plcRWs[2];
                        if(ctlNode.RfidID>0)
                        {
                            ctlNode.RfidRW = GetRfidByID(ctlNode.RfidID);
                        }
                        if (!SysCfg.SysCfgModel.SimMode)
                        {
                            //ctlNode.RfidRW = GetRfidByID((byte)ctlNode.RfidID);
                            if(ctlNode.BarcodeID>0)
                            {
                                ctlNode.BarcodeRW = GetBarcoderRWByID(ctlNode.BarcodeID);
                            }
                            
                        }
                        
                        //Console.WriteLine(ctlNode.NodeName + ",ID:" + ctlNode.NodeID + "创建成功！");
                        this.monitorNodeList.Add(ctlNode);
                    }

                }
            }
            catch (Exception ex)
            {
                reStr = ex.ToString();
                return false;
            }

            return true;
        }
        private bool ParseTheadNodes(XElement ThreadnodeRoot, ref string reStr)
        {
            if (ThreadnodeRoot == null)
            {
                reStr = "系统配置文件错误，不存在ThreadAlloc节点";
                return false;
            }
            try
            {
                IEnumerable<XElement> nodeXEList =
                from el in ThreadnodeRoot.Elements()
                where el.Name == "Thread"
                select el;
                foreach (XElement el in nodeXEList)
                {
                    string threadName = el.Attribute("name").Value;
                    int threadID = int.Parse(el.Attribute("id").Value);
                    int loopInterval = int.Parse(el.Attribute("loopInterval").Value);
                    ThreadRunModel threadObj = new ThreadRunModel(threadName);
                    //  ThreadBaseModel threadObj = new ThreadBaseModel(threadID, threadName);
                    threadObj.TaskInit();
                    threadObj.LoopInterval = loopInterval;
                    XElement nodeContainer = el.Element("NodeContainer");

                    IEnumerable<XElement> nodeListAlloc =
                    from nodeEL in nodeContainer.Elements()
                    where nodeEL.Name == "NodeID"
                    select nodeEL;
                    foreach (XElement nodeEL in nodeListAlloc)
                    {
                        string nodeID = nodeEL.Value;
                        CtlNodeBaseModel node = GetNodeByID(nodeID);
                        
                        if (node != null)
                        {
                            threadObj.AddNode(node);
                        }
                    }
                    this.threadList.Add(threadObj);
                }
                return true;
            }
            catch (Exception ex)
            {
                reStr = ex.ToString();
                return false;
            }
        }
        private bool ParseCommDevCfg(XElement commDevRoot, ref string reStr)
        {
            try
            {
                //1 PLC
                XElement plcRoot = commDevRoot.Element("PLCCfg");
              
                plcRWs = new List<IPlcRW>();
                IEnumerable<XElement> plcXES = plcRoot.Elements("PLC");
                foreach(XElement plcXE in plcXES)
                {
                    string connStr=plcXE.Value.ToString();
                    int db1Len=100,db2Len=100;
                    db1Len = int.Parse(plcXE.Attribute("db1Len").Value.ToString());
                    db2Len = int.Parse(plcXE.Attribute("db2Len").Value.ToString());
                    int plcID=int.Parse(plcXE.Attribute("id").Value.ToString());
                    EnumPlcCata plcCata = EnumPlcCata.FX5U;
                    if(plcXE.Attribute("cata") != null)
                    {
                        string strPlcCata=plcXE.Attribute("cata").Value.ToString();
                        plcCata = (EnumPlcCata)Enum.Parse(typeof(EnumPlcCata),strPlcCata);

                    }
                   
                    PLCRwMCPro plcRW = new PLCRwMCPro(plcCata, db1Len, db2Len);
                    // PLCRwMCPro2 plcRW = new PLCRwMCPro2(db1Len, db2Len);
                    plcRW.ConnStr = plcXE.Value.ToString();
                    plcRW.PlcID = plcID;
                    plcRWs.Add(plcRW);
                }
                //2 rfid
                XElement rfidRootXE = commDevRoot.Element("RfidCfg");
                IEnumerable<XElement> rfidXES = rfidRootXE.Elements("RFID");
                this.rfidRWs = new List<IrfidRW>();
                foreach (XElement rfidXE in rfidXES)
                {
                    byte id = byte.Parse(rfidXE.Attribute("id").Value.ToString());
                    string addr = rfidXE.Attribute("CommAddr").Value.ToString();
                    string[] strAddrArray = addr.Split(new string[]{":"},StringSplitOptions.RemoveEmptyEntries);
                    string ip = strAddrArray[0];
                    ushort port = ushort.Parse(strAddrArray[1]);
                    //WqwlRfidRW rfidRW = new WqwlRfidRW(EnumTag.TagEPCC1G2, id, ip, port);
                    WqRfidUdp rfidRW = new WqRfidUdp(EnumTag.TagEPCC1G2, id, ip, port,(uint)(9000+id));
                    
                    //rfidRW.HostIP = "192.168.1.251";
                   // rfidRW.HostPort = (uint)(9000+id); 
                    //SygoleHFReaderIF.HFReaderIF readerIF = new SygoleHFReaderIF.HFReaderIF();
                    //SgrfidRW rfidRW = new SgrfidRW(id);
                    //rfidRW.ReaderIF = readerIF;
                    //rfidRW.ReaderIF.ComPort = commPort;
                    rfidRWs.Add(rfidRW);
                }

                //3 条码枪
                XElement barcoderRootXE = commDevRoot.Element("BarScannerCfg");
                IEnumerable<XElement> barcodes = barcoderRootXE.Elements("BarScanner");
                barcodeRWList = new List<IBarcodeRW>();
                foreach (XElement barcodeXE in barcodes)
                {
                    byte id = byte.Parse(barcodeXE.Attribute("id").Value.ToString());
                    string commPort = barcodeXE.Attribute("CommAddr").Value.ToString();
                    BarcodeRWHonevor barcodeReader = new BarcodeRWHonevor(id);
                    barcodeReader.TriggerMode = EnumTriggerMode.程序命令触发;
                    //System.IO.Ports.SerialPort comPort = new System.IO.Ports.SerialPort(commPort);
                    //comPort.BaudRate = 115200;
                    //comPort.DataBits = 8;
                    //comPort.StopBits = System.IO.Ports.StopBits.One;
                    //comPort.Parity = System.IO.Ports.Parity.None;
                    barcodeReader.ComPortObj.PortName = commPort;
                    barcodeReader.ComPortObj.BaudRate = 115200;
                    barcodeReader.ComPortObj.DataBits = 8;
                    barcodeReader.ComPortObj.StopBits = System.IO.Ports.StopBits.One;
                    barcodeReader.ComPortObj.Parity = System.IO.Ports.Parity.None;
                    barcodeRWList.Add(barcodeReader);
                }
                //4 杭可装载服务器
                XElement hkFillSvcXECfg = commDevRoot.Element("HKFillServerCfg");
                string hkSvcIP = "127.0.0.1";
                int hkSvcPort = 13535;
                if(hkFillSvcXECfg != null)
                {
                    foreach (XElement hkXE in hkFillSvcXECfg.Elements("HkServer"))
                    {
                        int hkID = int.Parse(hkXE.Attribute("id").Value.ToString());
                        hkSvcIP = hkXE.Attribute("ip").Value.ToString();
                        hkSvcPort = int.Parse(hkXE.Attribute("port").Value.ToString());
                        HKAccess hk = new HKAccess(hkID,hkSvcIP, hkSvcPort);
                        hk.RecvTimeOut = 5000;
                        hkAccessList.Add(hk);
                    }
                  
                }

                string ocvDBConn = hkFillSvcXECfg.Element("HkOCVDB").Attribute("conn").Value.ToString(); // "Data Source = 192.168.100.20;Initial Catalog=SRANCH;User ID=L_Guest;Password=Guest@123;";
                ocvAccess = new OcvAccess(ocvDBConn, 36); 

                CommDevConnect();
                
               
                return true;
            }
            catch (Exception ex)
            {
                reStr = ex.ToString();
                return false;
            }

        }
        private void CommDevConnect()
        {
            try
            {
                view.WelcomePopup();
                //DelegateDevConn dlgt = new DelegateDevConn(AsyCommDevConnect);
                //IAsyncResult ar = dlgt.BeginInvoke(CallbackDevConnFinished,dlgt);
                AsyCommDevConnect();
                view.WelcomeClose();
            }
            catch (Exception ex)
            {
                view.PopupMes(ex.ToString());
            }

        }
        //异步，通信设备连接
        private void AsyCommDevConnect()
        {
            string reStr = "";
            //通信连接
            for (int i = 0; i < rfidRWs.Count(); i++)
            {
                string logStr = "";
                if (rfidRWs[i].Connect())
                {
                    logStr = string.Format("{0} 读卡器连接成功！", rfidRWs[i].ReaderID);
                }
                else
                {
                    logStr = string.Format("{0} 读卡器连接失败！", rfidRWs[i].ReaderID);
                }
                view.WelcomeAddStartinfo(logStr);
                Console.WriteLine(logStr);

            }
            
            for (int i = 0; i < this.barcodeRWList.Count(); i++)
            {
                string logStr = "";
                if (!this.barcodeRWList[i].StartMonitor(ref reStr))
                {
                    logStr = string.Format("{0} 号条码枪端口打开失败,{1}", this.barcodeRWList[i].ReaderID, reStr);
                    logRecorder.AddLog(new LogModel(objectName, logStr, EnumLoglevel.错误));
                }
                else
                {
                    logStr = string.Format("{0} 号条码枪端口打开成功！", this.barcodeRWList[i].ReaderID);
                    logRecorder.AddLog(new LogModel(objectName, logStr, EnumLoglevel.提示));
                }
                view.WelcomeAddStartinfo(logStr);
            }
            //view.WelcomeDispCurinfo("正在连接PLC...");
            for (int i = 0; i < plcRWs.Count(); i++)
            {
                
                string logStr = "";
                if (!plcRWs[i].ConnectPLC(ref reStr))
                {
                    logStr = string.Format("{0} 号PLC连接失败,{1}", plcRWs[i].PlcID, reStr);
                    Console.WriteLine(logStr);
                    view.WelcomeAddStartinfo(logStr);
                  //  logRecorder.AddLog(new LogModel(objectName, logStr, EnumLoglevel.错误));
                }
                else
                {
                    logStr = string.Format("{0} 号PLC连接成功", plcRWs[i].PlcID);
                    Console.WriteLine(logStr);
                    view.WelcomeAddStartinfo(logStr);
                   // logRecorder.AddLog(new LogModel(objectName, logStr, EnumLoglevel.提示));
                }
            }
            //for(int i=0;i<hkAccessList.Count();i++)
            //{
            //    string logStr = "";
            //    if(!hkAccessList[i].Conn(ref logStr))
            //    {
            //        logStr = "杭可装载服务器连接失败：" + reStr;
            //        Console.WriteLine(logStr);
            //        view.WelcomeAddStartinfo(logStr);
            //    }
            //}
           // view.RefreshNodeStatus();
        }

        public IrfidRW GetRfidByID(byte readerID)
        {
            foreach(IrfidRW rfid in rfidRWs)
            {
                if(rfid != null && rfid.ReaderID == readerID)
                {
                    return rfid;
                }
            }
            return null;
        }
        public IPlcRW GetPlcByID(int plcID)
        {
            foreach(IPlcRW plcRW in plcRWs)
            {
                if(plcID == plcRW.PlcID)
                {
                    return plcRW;
                }
            }
            return null;
        }
        public IrfidRW GetRfidByID(int rfidID)
        {
            foreach (IrfidRW rfidRW in rfidRWs)
            {
                if (rfidID == rfidRW.ReaderID)
                {
                    return rfidRW;
                }
            }
            return null;
        }
        public IBarcodeRW GetBarcoderRWByID(int barcodReaderID)
        {
            foreach(IBarcodeRW barcodeReader in barcodeRWList)
            {
                if(barcodeReader != null && barcodeReader.ReaderID == barcodReaderID)
                {
                    return barcodeReader;
                }
            }
            return null;
        }
        public IHKAccess GetHKAccessByID(int hkServerID)
        {
            foreach(IHKAccess hk in hkAccessList)
            {
                if(hk.HkAccessID == hkServerID)
                {
                    return hk;
                }
            }
            return null;
        }
        #endregion
        #region 立库任务调度委托
        //A1库出库任务委托
        public CtlDBAccess.Model.ControlTaskModel GetCheckoutOfA1(AsrsControl.AsrsCtlModel asrsCtl, IAsrsManageToCtl asrsResManage, IList<CtlDBAccess.Model.ControlTaskModel> taskList, SysCfg.EnumAsrsTaskType taskType)
        {
            if(asrsCtl.HouseName != AsrsModel.EnumStoreHouse.A1库房.ToString())
            {
                return null;
            }
            if (taskList == null)
            {
                return null;
            }
            NodeVirStation nodeStation = GetNodeByID("4001") as NodeVirStation;
            //AsrsModel.EnumLogicArea requireCheckoutArea = AsrsModel.EnumLogicArea.一次高温A区;
            List<AsrsModel.EnumLogicArea> areaList = new List<AsrsModel.EnumLogicArea>();
            if(nodeStation.Db2Vals[2] == 1)
            {
                //requireCheckoutArea = AsrsModel.EnumLogicArea.一次高温A区;
                areaList.Add(AsrsModel.EnumLogicArea.一次高温A区);
                areaList.Add(AsrsModel.EnumLogicArea.一次高温B区);
            }
            else if(nodeStation.Db2Vals[2] == 2)
            {
                areaList.Add(AsrsModel.EnumLogicArea.一次高温B区);
                areaList.Add(AsrsModel.EnumLogicArea.一次高温A区);
                
            }
            else
            {
                return null;
            }
            string houseName = asrsCtl.HouseName;
            ControlTaskModel task = null;
            foreach(AsrsModel.EnumLogicArea requireCheckoutArea in areaList)
            {
                task = GetCheckoutOfArea(asrsCtl, asrsResManage, taskType, taskList, requireCheckoutArea);
                if(task != null)
                {
                    break;
                }
            }
           
            return task;
        }
        private CtlDBAccess.Model.ControlTaskModel GetCheckoutOfArea(AsrsControl.AsrsCtlModel asrsCtl, IAsrsManageToCtl asrsResManage, SysCfg.EnumAsrsTaskType taskType,IList<CtlDBAccess.Model.ControlTaskModel> taskList,AsrsModel.EnumLogicArea checkOutArea)
        {
            string houseName = asrsCtl.HouseName;
            ControlTaskModel task = null;
            if (taskList == null)
            {
                return null;
            }
            foreach (ControlTaskModel t in taskList)
            {
                string reStr = "";
                AsrsTaskParamModel paramModel = new AsrsTaskParamModel();
                if (!paramModel.ParseParam(taskType, t.TaskParam, ref reStr))
                {
                    continue;
                }
                AsrsModel.EnumGSEnabledStatus cellEnabledStatus = AsrsModel.EnumGSEnabledStatus.启用;
                if (!asrsResManage.GetCellEnabledStatus(houseName, paramModel.CellPos1, ref cellEnabledStatus))
                {
                    // reStr = "获取货位启用状态失败";
                    continue;
                }
                if (cellEnabledStatus == AsrsModel.EnumGSEnabledStatus.禁用)
                {
                    continue;
                }
                AsrsModel.EnumLogicArea curLogicArea= AsrsModel.EnumLogicArea.一次高温A区;
                if(!asrsResManage.GetLogicAreaName(houseName, paramModel.CellPos1, ref curLogicArea))
                {
                    continue;
                }
                if(curLogicArea == checkOutArea)
                {
                    task = t;
                    break;
                }
            }
            return task;
        }
        #endregion
    }
}
