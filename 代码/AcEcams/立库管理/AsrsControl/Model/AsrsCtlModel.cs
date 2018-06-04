using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using CtlDBAccess.BLL;
using CtlDBAccess.Model;
using AsrsInterface;
using AsrsModel;
using DevInterface;
using LogInterface;
using FlowCtlBaseModel;
namespace AsrsControl
{
    /// <summary>
    /// 立库控制模型，包括堆垛机、出入口等对象。
    /// 功能：实时监测出入口的状态，申请入库任务，调度出入库任务的执行。
    /// </summary>
    public class AsrsCtlModel : CtlNodeBaseModel
    {
        public delegate ControlTaskModel DelegateGetTaskTorun(AsrsControl.AsrsCtlModel asrsCtl,IAsrsManageToCtl asrsResManage, IList<ControlTaskModel> taskList, SysCfg.EnumAsrsTaskType taskType);
        #region 数据
        protected  Dictionary<int, string> mesStepLocalMap = new Dictionary<int, string>();
        public float defaultStoreTime = 10.0f;//默认存储时间(小时）
        private string houseName = "";
        private List<AsrsPortalModel> ports;
        private AsrsStackerCtlModel stacker;
        private List<ThreadRunModel> threadList = null;
        private IAsrsManageToCtl asrsResManage = null; //立库管理层接口对象
      //  private CtlDBAccess.BLL.ControlTaskBll ctlTaskBll = null;
       // private CtlDBAccess.BLL.BatteryModuleBll batModuleBll = null;
       
        private ThreadBaseModel asrsMonitorThread = null;
        private ThreadBaseModel PortMonitorThread = null;
        private ThreadBaseModel stackerPlcCommThread = null; //堆垛机PLC通信线程
       // private bool plcInitFlag = false;
       // private Int64 lastPortPlcStat = 0; //控制立库出入口的plc读写计数，异步通信用
        private IDictionary<SysCfg.EnumAsrsTaskType, DateTime> taskWaitBeginDic = null; //任务按类别等待计时开始
        private IDictionary<SysCfg.EnumAsrsTaskType, TimeSpan> taskWaitingDic = null;//任务按类别等待时间
        private EnumAsrsCheckoutMode asrsCheckoutMode = EnumAsrsCheckoutMode.计时出库; //出库模式
        private int asrsRow = 0;
        private int asrsCol = 0;
        private int asrsLayer = 0;
        #endregion
        #region 公共接口
        public AsrsStackerCtlModel StackDevice { get { return stacker; } set { stacker = value; } }
        public List<AsrsPortalModel> Ports { get { return ports; } set { ports = value; } }
        public IAsrsManageToCtl AsrsResManage { get { return asrsResManage; }}
        public string HouseName { get { return houseName; } set { houseName = value; } }
        public EnumAsrsCheckoutMode AsrsCheckoutMode { get { return asrsCheckoutMode; } set { asrsCheckoutMode = value; } }
        public DelegateGetTaskTorun dlgtGetTaskTorun = null;
      
        public AsrsCtlModel()
        {
            mesStepLocalMap[3] = "PS-20";
            mesStepLocalMap[5] = "PS-20";
            mesStepLocalMap[11] = "PS-50";
            mesStepLocalMap[12] = "PS-60";
            mesStepLocalMap[18] = "PS-100";
        }
        public bool Init()
        {
            ctlTaskBll = new ControlTaskBll();
           // batModuleBll = new CtlDBAccess.BLL.BatteryModuleBll();
         
            //1堆垛机控制线程
            threadList = new List<ThreadRunModel>();
            ThreadRunModel stackerThread = new ThreadRunModel(houseName + "堆垛机控制线程");
            stackerThread.AddNode(this.stacker);
            stackerThread.LogRecorder = this.logRecorder;
            stackerThread.LoopInterval = 100;
           // string reStr = "";
            if(!stackerThread.TaskInit())
            {
                logRecorder.AddLog(new LogInterface.LogModel(nodeName, "堆垛机控制线程创建失败", LogInterface.EnumLoglevel.错误));
                return false;
            }
            threadList.Add(stackerThread);

           //2出入口监控线程
            PortMonitorThread = new ThreadBaseModel(houseName + "堆垛机监控线程");
            PortMonitorThread.LogRecorder = this.logRecorder;
            PortMonitorThread.LoopInterval = 100;
            PortMonitorThread.SetThreadRoutine(PortBusinessLoop);
            if (!PortMonitorThread.TaskInit())
            {
                logRecorder.AddLog(new LogInterface.LogModel(nodeName, "监控线程创建失败", LogInterface.EnumLoglevel.错误));
                return false;
            }
            //3堆垛机通信线程

            if (!SysCfg.SysCfgModel.PlcCommSynMode)
            {
                stackerPlcCommThread = new ThreadBaseModel(houseName + "PLC通信线程");
                stackerPlcCommThread.LogRecorder = this.logRecorder;
                stackerPlcCommThread.LoopInterval = 10;
                stackerPlcCommThread.SetThreadRoutine(PlcCommLoop);
            }
            this.asrsMonitorThread = new ThreadBaseModel("立库货位状态监控线程");
            asrsMonitorThread.SetThreadRoutine(CellStatusMonitor);
            asrsMonitorThread.LoopInterval = 1000;
            asrsMonitorThread.TaskInit();
            this.stacker.dlgtTaskCompleted = TaskCompletedProcess;

            this.nodeID = this.stacker.NodeID;
            if(this.mesProcessStepID.Count()>0)
            {
                 MesDBAccess.BLL.ProcessStepBll processBll = new MesDBAccess.BLL.ProcessStepBll();
                MesDBAccess.Model.ProcessStepModel processModel = processBll.GetModel(this.mesProcessStepID.Last());
                if(processModel != null)
                {
                    this.defaultStoreTime = float.Parse(processModel.tag1);
                }
            }
           
            return true;
        }
        public void FillTaskTyps(List<SysCfg.EnumAsrsTaskType> taskTypes)
        {
            taskWaitBeginDic = new Dictionary<SysCfg.EnumAsrsTaskType, DateTime>();
            taskWaitingDic = new Dictionary<SysCfg.EnumAsrsTaskType, TimeSpan>();
            foreach (SysCfg.EnumAsrsTaskType taskType in taskTypes)
            {
                taskWaitBeginDic[taskType] = System.DateTime.Now;
                taskWaitingDic[taskType] = TimeSpan.Zero;
            }
            
        }
        public bool StartRun()
        {
          
            string reStr = "";
            if (!SysCfg.SysCfgModel.PlcCommSynMode)
            {
                if (stackerPlcCommThread.TaskStart(ref reStr))
                {
                    logRecorder.AddLog(new LogInterface.LogModel(nodeName, "PLC通信启动失败," + reStr, LogInterface.EnumLoglevel.错误));
                    return false;
                }
            }
           
            foreach(ThreadRunModel thread in threadList)
            {
                if(!thread.TaskStart(ref reStr))
                {
                    logRecorder.AddLog(new LogInterface.LogModel(nodeName, "启动失败," + reStr, LogInterface.EnumLoglevel.错误));
                    return false;
                }
            }
            if (!PortMonitorThread.TaskStart(ref reStr))
            {
                logRecorder.AddLog(new LogInterface.LogModel(nodeName, "启动失败," + reStr, LogInterface.EnumLoglevel.错误));
                return false;
            }
            if(!asrsMonitorThread.TaskStart(ref reStr))
            {
                logRecorder.AddLog(new LogInterface.LogModel(nodeName, "启动失败," + reStr, LogInterface.EnumLoglevel.错误));
                return false;
            }
            return true;
        }
        public bool PauseRun()
        {
            if (!SysCfg.SysCfgModel.PlcCommSynMode)
            {
                stackerPlcCommThread.TaskPause();
            }
           
            foreach (ThreadRunModel thread in threadList)
            {
                thread.TaskPause();
               
            }
            PortMonitorThread.TaskPause();
            asrsMonitorThread.TaskPause();
            return true;
        }
        public bool ExitRun()
        {
            string reStr = "";
            foreach (ThreadRunModel thread in threadList)
            {
                thread.TaskExit(ref reStr);

            }
            if (!SysCfg.SysCfgModel.PlcCommSynMode)
            {
                stackerPlcCommThread.TaskExit(ref reStr);
            }
            asrsMonitorThread.TaskExit(ref reStr);
            return true;
        }
        public void SetAsrsPortPlcRW(IPlcRW plcRW)
        {
            foreach(AsrsPortalModel port in ports)
            {
                port.PlcRW = plcRW;
            }
        }
        public void SetLogrecorder(ILogRecorder logRecorder)
        {
            this.logRecorder = logRecorder;
            this.stacker.LogRecorder = logRecorder;
            foreach(AsrsPortalModel port in ports)
            {
                port.LogRecorder = logRecorder;
            }
        }
        public void SetAsrsMangeInterafce(IAsrsManageToCtl asrsResManage)
        {
            this.asrsResManage = asrsResManage;
            this.stacker.AsrsResManage = asrsResManage;
            string reStr = "";
            if (!this.asrsResManage.GetCellCount(houseName, ref asrsRow, ref asrsCol, ref asrsLayer, ref reStr))
            {
                logRecorder.AddLog(new LogModel(nodeName, string.Format("获取货位数量信息失败,{0}", reStr), EnumLoglevel.错误));
                
            }
        }
        public override bool ExeBusiness(ref string reStr)
        {
            return true;
        }
        public bool AsrsCheckinTaskRequire(AsrsPortalModel port, EnumLogicArea logicArea,SysCfg.EnumAsrsTaskType taskType,string[] palletIDS,ref string reStr)
        {
            try
            {
                //if(port.BindedTaskInput != taskType)
                //{
                //    reStr = "未能匹配的入库任务类型 ";
                //    return false;
                //}
                CellCoordModel requireCell = null;
               
                if (asrsResManage.CellRequire(this.houseName, logicArea.ToString(), ref requireCell, ref reStr))
                {
                    //生成任务
                    ControlTaskModel asrsTask = new ControlTaskModel();
                    asrsTask.DeviceID = this.stacker.NodeID;
                    asrsTask.CreateMode = "自动";
                    asrsTask.CreateTime = System.DateTime.Now;
                    asrsTask.TaskID = System.Guid.NewGuid().ToString();
                    asrsTask.TaskStatus = SysCfg.EnumTaskStatus.待执行.ToString();
                    asrsTask.TaskType = (int)taskType;
                    AsrsTaskParamModel taskParam = new AsrsTaskParamModel();

                    taskParam.CellPos1 = requireCell;
                    taskParam.InputPort = port.PortSeq;
                    // if (taskType == EnumAsrsTaskType.产品入库)
                    // {
                    taskParam.InputCellGoods = palletIDS;
                    //  }
                    asrsTask.TaskParam = taskParam.ConvertoStr(taskType);


                    //申请完成后要锁定货位
                    if (!asrsResManage.UpdateCellStatus(houseName, requireCell, EnumCellStatus.空闲, EnumGSTaskStatus.锁定, ref reStr))
                    {
                        logRecorder.AddDebugLog(nodeName, "更新货位状态失败," + reStr);
                        return false;
                    }
                    if (!asrsResManage.UpdateGSOper(houseName, requireCell, EnumGSOperate.入库, ref reStr))
                    {
                        logRecorder.AddDebugLog(nodeName, "更新货位操作类行失败," + reStr);
                        return false;
                    }
                    else
                    {
                        asrsTask.tag1 = houseName;
                        asrsTask.tag2 = string.Format("{0}-{1}-{2}", requireCell.Row, requireCell.Col, requireCell.Layer);
                        asrsTask.Remark = taskType.ToString();
                        ctlTaskBll.Add(asrsTask);

                        string logInfo = string.Format("生成新的任务:{0},货位：{1}-{2}-{3}，任务参数：{4}", taskType.ToString(), requireCell.Row, requireCell.Col, requireCell.Layer,asrsTask.TaskParam);
                        logRecorder.AddDebugLog(nodeName, logInfo);
                        return true;
                    }
                }
                else
                {
                  
                    return false;
                }
            }
            catch (Exception ex)
            {
                reStr = ex.ToString();
                return false;
            }
        }
        /// <summary>
        /// 更新产品工艺状态信息，出库时更新
        /// </summary>
        /// <param name="containerID"></param>
        //public override void UpdateOnlineProductInfo(string containerID)
        //{
        //    string strSql = string.Format(@"palletID='{0}' and palletBinded=1 ",containerID);
        //    List<MesDBAccess.Model.ProductOnlineModel> products = productOnlineBll.GetModelList(strSql);
        //    if(products != null && products.Count()>0)
        //    {
        //        string nextStepID = "";

        //        int seq = SysCfg.SysCfgModel.stepSeqs.IndexOf(products[0].processStepID);
        //        if(seq<0)
        //        {
        //            Console.WriteLine("工艺路径错误,在UpdateOnlineProductInfo（）发生");
        //            return;
        //        }
        //        bool fndOK = false;
        //        for(int i=0;i<mesProcessStepID.Count();i++)
        //        {
        //            string processStep = mesProcessStepID[i];
        //            int stepSeq = SysCfg.SysCfgModel.stepSeqs.IndexOf(processStep);
        //            if(seq<stepSeq)
        //            {
        //                seq = stepSeq;
        //                fndOK = true;
        //                break;
        //            }

        //        }
        //        if(!fndOK)
        //        {
        //            nextStepID = mesProcessStepID[mesProcessStepID.Count() - 1];
        //        }
        //        else
        //        {
        //            nextStepID = SysCfg.SysCfgModel.stepSeqs[seq];
        //        }
                

        //        foreach(MesDBAccess.Model.ProductOnlineModel product in products)
        //        {
        //            product.processStepID = nextStepID;
        //            product.stationID =nodeID;
        //            productOnlineBll.Update(product);
        //        }
        //    }
        //}
        public override bool BuildCfg(System.Xml.Linq.XElement root, ref string reStr)
        {
            try
            {
                ports = new List<AsrsPortalModel>();
                this.nodeName=root.Attribute("name").Value.ToString();
                this.houseName = this.nodeName;
                IEnumerable<XElement> nodeXEList = root.Elements("Node");
                foreach (XElement el in nodeXEList)
                {
                    string className = (string)el.Attribute("className");
                   
                    if(className == "AsrsControl.AsrsStackerCtlModel")
                    {
                        this.stacker = new AsrsStackerCtlModel();
                        stacker.HouseName = this.houseName;
                        if(!this.stacker.BuildCfg(el,ref reStr))
                        {
                            return false;
                        }
                        this.nodeEnabled = this.stacker.NodeEnabled;
                        this.mesProcessStepID = this.stacker.MesProcessStepID;
                    }
                    else if(className == "AsrsPortalModel.AsrsPortalModel")
                    {
                        AsrsPortalModel port = new AsrsPortalModel(this);
                        if(!port.BuildCfg(el,ref reStr))
                        {
                            return false;
                        }
                        this.ports.Add(port);
                    }
                    else
                    {
                        continue;
                    }
                }
                this.currentStat = new CtlNodeStatus(nodeName);
                this.currentStat.Status = EnumNodeStatus.设备空闲;
                this.currentStat.StatDescribe = "空闲状态";
                return true;
            }
            catch (Exception ex)
            {
                reStr = ex.ToString();
                return false;
            }
        }
        public void GenerateAutoOutputTaskMulti(List<CellCoordModel> cells, SysCfg.EnumAsrsTaskType taskType)
        {
            if(cells == null)
            {
                return;
            }
            string reStr = "";
           
            //zwx,此处需要修改
            //checkOutBatch = SysCfg.SysCfgModel.CheckoutBatchDic[houseName].ToUpper().Trim();
           
            foreach (CellCoordModel cell in cells)
            {
                 string checkOutBatch = "";
                 EnumLogicArea logicArea= EnumLogicArea.常温区;
                 asrsResManage.GetLogicAreaName(houseName,cell,ref logicArea);
                 if (!asrsResManage.GetOutBatch(houseName, logicArea.ToString(), ref checkOutBatch, ref reStr))
                 {
                     continue;
                 }
                List<string> palletList = new List<string>();
                asrsResManage.GetStockDetail(this.houseName, cell, ref palletList);
                if(palletList.Count()>0)
                {
                    //zwx,此处需要更改
                    string palletID = palletList[0];
                   
                    string palletBatch = "";//productOnlineBll.GetBatchNameofPallet(palletList[0]).ToUpper().Trim();
                    
                    if(checkOutBatch == "所有")
                    {
                        GenerateOutputTask(cell, null, taskType, true);
                    }
                    else
                    {
                        if(SysCfg.SysCfgModel.SimMode)
                        {
                            palletBatch = productOnlineBll.GetBatchNameofPallet(palletList[0]).ToUpper().Trim();
                        }
                        else
                        {
                            VMResultLot batchRe = MesAcc.GetTrayCellLotNO(palletID);
                            
                            if (batchRe.ResultCode == 0)
                            {
                                palletBatch = batchRe.LotNO;
                            }
                            else
                            {
                                logRecorder.AddDebugLog(nodeName, string.Format("待生成托盘{0}的出库任务时，查询MES批次失败{1}", palletID, batchRe.ResultMsg));
                                continue;
                            }
                        }
                        if (checkOutBatch == "空" && string.IsNullOrWhiteSpace(palletBatch))
                        {
                            GenerateOutputTask(cell, null, taskType, true);
                        }
                        else if (palletBatch == checkOutBatch)
                        {
                            GenerateOutputTask(cell, null, taskType, true);
                        }
                        else
                        {
                            continue;
                        }
                    }
                   
                }
                else
                {
                    GenerateOutputTask(cell,null, taskType, true);
                }
            }
        }
        public bool GenerateOutputTask(CellCoordModel cell, CellCoordModel cell2, SysCfg.EnumAsrsTaskType taskType, bool autoTaskMode)
        {
           // throw new NotImplementedException();
          
            ControlTaskModel asrsTask = new ControlTaskModel();
            asrsTask.DeviceID = this.stacker.NodeID;
            if(autoTaskMode)
            {
                asrsTask.CreateMode = "自动";
            }
            else
            {
                asrsTask.CreateMode = "手动";
            }
            asrsTask.CreateTime = System.DateTime.Now;
            asrsTask.TaskID = System.Guid.NewGuid().ToString();
            asrsTask.TaskStatus = SysCfg.EnumTaskStatus.待执行.ToString();
            asrsTask.TaskType = (int)taskType;
            AsrsTaskParamModel taskParam = new AsrsTaskParamModel();
            taskParam.InputPort = 0;
           
            taskParam.CellPos1 = cell;
            taskParam.CellPos2 = cell2;
            List<string> storGoods = new List<string>();
            if (asrsResManage.GetStockDetail(houseName, cell, ref storGoods))
            {
                taskParam.InputCellGoods = storGoods.ToArray();
            }
            List<AsrsPortalModel> validPorts = GetOutPortsOfBindedtask(taskType);
            if(validPorts != null && validPorts.Count()>0)
            {
                taskParam.OutputPort = validPorts[0].PortSeq;
            }
            //if (taskType == EnumAsrsTaskType.空框出库)
            //{
            //    taskParam.OutputPort = 3;
            //}
            //else if (taskType == EnumAsrsTaskType.产品出库)
            //{
            //    taskParam.OutputPort = 2;//默认
               
            //}

            asrsTask.TaskParam = taskParam.ConvertoStr(taskType);
            //申请完成后要锁定货位
            string reStr = "";
            EnumCellStatus cellStoreStat = EnumCellStatus.空闲;
            EnumGSTaskStatus cellTaskStat = EnumGSTaskStatus.完成;
            this.asrsResManage.GetCellStatus(this.houseName, cell, ref cellStoreStat, ref cellTaskStat);
            if (!asrsResManage.UpdateCellStatus(houseName, cell, cellStoreStat, EnumGSTaskStatus.锁定, ref reStr))
            {
                logRecorder.AddDebugLog(nodeName, "更新货位状态失败," + reStr);
                return false;
            }
           
            if (!asrsResManage.UpdateGSOper(houseName, cell, EnumGSOperate.出库, ref reStr))
            {
                logRecorder.AddDebugLog(nodeName, "更新货位操作类行失败," + reStr);
                return false;
            }
            else
            {
                if (taskType == SysCfg.EnumAsrsTaskType.移库 && cell2 != null)
                {
                    List<string> cellStoreProducts = null;
                    if (!asrsResManage.GetStockDetail(houseName, cell, ref cellStoreProducts))
                    {
                        return false;
                    }
                    if (!asrsResManage.UpdateCellStatus(houseName, cell2, cellStoreStat, EnumGSTaskStatus.锁定, ref reStr))
                    {
                        logRecorder.AddDebugLog(nodeName, "更新货位状态失败," + reStr);
                        return false;
                    }
                    taskParam.InputCellGoods = cellStoreProducts.ToArray();
                    asrsTask.TaskParam = taskParam.ConvertoStr(taskType);
                    asrsTask.tag1 = houseName;
                    asrsTask.tag2 = string.Format("{0}-{1}-{2}", cell.Row, cell.Col, cell.Layer);
                    asrsTask.tag3 = string.Format("{0}-{1}-{2}", cell2.Row, cell2.Col, cell2.Layer);
                    asrsTask.Remark = taskType.ToString();

                    ctlTaskBll.Add(asrsTask);
                    string logInfo = string.Format("生成新的任务:{0},货位：{1}-{2}-{3}到 货位：{4}-{5}-{6},{7}", taskType.ToString(), cell.Row, cell.Col, cell.Layer, cell2.Row, cell2.Col, cell2.Layer,asrsTask.TaskParam);
                    logRecorder.AddDebugLog(nodeName, logInfo);
                }
                else
                {
                    asrsTask.tag1 = houseName;
                    asrsTask.tag2 = string.Format("{0}-{1}-{2}", cell.Row, cell.Col, cell.Layer);
                    asrsTask.Remark = taskType.ToString();
                    ctlTaskBll.Add(asrsTask);
                    string logInfo = string.Format("生成新的任务:{0},货位：{1}-{2}-{3},{4}", taskType.ToString(), cell.Row, cell.Col, cell.Layer,asrsTask.TaskParam);
                    logRecorder.AddDebugLog(nodeName, logInfo);
                }
            }
            return true;
        }
        public bool GenerateEmerOutputTask(CellCoordModel cell, SysCfg.EnumAsrsTaskType taskType, bool autoTaskMode, int EmerGrade, ref string reStr)
        {
            //zwx,此处需要修改
            //if(this.houseName != EnumStoreHouse.B1库房.ToString())
            //{
            //    reStr = "错误的库房选择";
            //    return false;
            //}
            ControlTaskModel asrsTask = new ControlTaskModel();
            asrsTask.DeviceID = this.stacker.NodeID;
            if (autoTaskMode)
            {
                asrsTask.CreateMode = "自动";
            }
            else
            {
                asrsTask.CreateMode = "手动";
            }
            asrsTask.CreateTime = System.DateTime.Now;
            asrsTask.TaskID = System.Guid.NewGuid().ToString();
            asrsTask.TaskStatus = SysCfg.EnumTaskStatus.待执行.ToString();
            asrsTask.TaskType = (int)taskType;
            AsrsTaskParamModel taskParam = new AsrsTaskParamModel();
            taskParam.InputPort = 0;

            taskParam.CellPos1 = cell;
            List<string> storGoods = new List<string>();
            if (asrsResManage.GetStockDetail(houseName, cell, ref storGoods))
            {
                taskParam.InputCellGoods = storGoods.ToArray();
            }
            if (taskType == SysCfg.EnumAsrsTaskType.空框出库)
            {
                taskParam.OutputPort = 3;
            }
            else if (taskType == SysCfg.EnumAsrsTaskType.产品出库)
            {
                taskParam.OutputPort = 3;
            }
            else
            {
                reStr = "不支持的任务类型，" + taskType.ToString();
                return false;
            }
            asrsTask.TaskParam = taskParam.ConvertoStr(taskType);
            //申请完成后要锁定货位
           
            EnumCellStatus cellStoreStat = EnumCellStatus.空闲;
            EnumGSTaskStatus cellTaskStat = EnumGSTaskStatus.完成;
            this.asrsResManage.GetCellStatus(this.houseName, cell, ref cellStoreStat, ref cellTaskStat);
            if (!asrsResManage.UpdateCellStatus(houseName, cell, cellStoreStat, EnumGSTaskStatus.锁定, ref reStr))
            {
                logRecorder.AddDebugLog(nodeName, "更新货位状态失败," + reStr);
                reStr = "更新货位状态失败," + reStr;
                return false;
            }
            if (!asrsResManage.UpdateGSOper(houseName, cell, EnumGSOperate.出库, ref reStr))
            {
                logRecorder.AddDebugLog(nodeName, "更新货位操作类行失败," + reStr);
                reStr = "更新货位操作类行失败," + reStr;
                return false;
            }
            else
            {
                asrsTask.tag1 = houseName;
                asrsTask.tag2 = string.Format("{0}-{1}-{2}", cell.Row, cell.Col, cell.Layer);
                asrsTask.Remark = taskType.ToString();
                asrsTask.tag5= EmerGrade.ToString();
                ctlTaskBll.Add(asrsTask);
                string logInfo = string.Format("生成新的任务:{0},货位：{1}-{2}-{3},{4}", taskType.ToString(), cell.Row, cell.Col, cell.Layer,asrsTask.TaskParam);
                logRecorder.AddDebugLog(nodeName, logInfo);
                return true;
            }
        }
        public AsrsPortalModel GetPortByDeviceID(string devID)
        {
            AsrsPortalModel port = null;
            foreach (AsrsPortalModel schPort in Ports)
            {
                if (schPort.NodeID == devID)
                {
                    return schPort;
                }
            }
            return port;
        }


        #endregion
        #region 私有

        /// <summary>
        /// 循环查询各入库口状态，申请入库任务
        /// </summary>
        //private void AsrsInputRequire()
        //{
        //     AsrsInputRequire(ports[0].NodeID);
               
        //}
        private void AsrsInportBusiness()
        {
            foreach(AsrsPortalModel port in ports)
            {
                if(port.PortCata == 2)
                {
                    continue;
                }
                SysCfg.EnumAsrsTaskType taskType = port.BindedTaskInput;
                //若入口无RFID，则由外部节点控制入库申请
                if (taskType == SysCfg.EnumAsrsTaskType.产品入库 && port.RfidRW == null)
                {
                    continue;
                }
                if(port.PortinBufCapacity>1 && taskType == SysCfg.EnumAsrsTaskType.产品入库)
                {
                    AsrsInportBusiness2(port);
                    continue;
                }
                if (port.Db2Vals[0] < 2)
                {
                    port.Db1ValsToSnd[0] = 1;
                    port.CurrentStat.Status = EnumNodeStatus.设备空闲;
                    port.CurrentStat.StatDescribe = "空";
                    port.CurrentTaskDescribe = "无入库请求";
                }
              
                if (port.Db1ValsToSnd[0] == 2 || port.Db1ValsToSnd[0] == 4)
                {
                    continue;
                }
                
                if (2 != port.Db2Vals[0])
                {
                    continue;
                }
                port.CurrentStat.Status = EnumNodeStatus.设备使用中;
                port.CurrentStat.StatDescribe = "入库申请";
                if (ExistUnCompletedTask((int)taskType))
                {
                    continue;
                }
                string palletUID = string.Empty;
                //只有产品入库才读RFID
             //   bool unbindMode = SysCfg.SysCfgModel.UnbindMode;
                bool unBindMode = SysCfg.SysCfgModel.UnbindMode;
                //if (this.nodeName == EnumStoreHouse.A1库房.ToString())
                //{
                //    unBindMode = true;
                //}
                EnumLogicArea checkinArea = EnumLogicArea.常温区;
                string[] cellGoods = null;
                string reStr = "";
                #region 空筐入库口逻辑
                if (port.BindedTaskInput == SysCfg.EnumAsrsTaskType.空框入库)
                {
                    checkinArea = EnumLogicArea.空筐区;
                }
                #endregion
                #region 产品入库口逻辑
                else if(port.BindedTaskInput == SysCfg.EnumAsrsTaskType.产品入库)
                {
                    //SysCfg.SysCfgModel.UnbindMode
                    if (unBindMode)
                    {
                        rfidUID = System.Guid.NewGuid().ToString();
                    }
                    else
                    {
                        if (SysCfg.SysCfgModel.SimMode || SysCfg.SysCfgModel.RfidSimMode)
                        {
                            rfidUID = port.SimRfidUID;

                        }
                        else
                        {
                            if (port.BarcodeRW != null)
                            {
                                this.rfidUID = port.BarcodeRW.ReadBarcode();
                            }
                            else
                            {
                                this.rfidUID = port.RfidRW.ReadStrData();// port.RfidRW.ReadUID();
                            }
                        }
                        if (string.IsNullOrWhiteSpace(this.rfidUID))
                        {
                            port.CurrentStat.Status = EnumNodeStatus.无法识别;
                            port.CurrentStat.StatDescribe = "读卡失败";
                            port.CurrentTaskDescribe = "读RFID卡失败";
                            if (port.Db1ValsToSnd[0] != 3)
                            {
                                logRecorder.AddDebugLog(nodeName, "读RFID失败，长度不足9字符");
                            }
                            port.Db1ValsToSnd[0] = 3;
                            continue;
                        }
                        this.rfidUID = this.rfidUID.Trim(new char[] { '\0', '\r', '\n', '\t', ' ' });
                        string pattern = @"^[a-zA-Z0-9]*$"; //匹配所有字符都在字母和数字之间  
                        if (!System.Text.RegularExpressions.Regex.IsMatch(this.rfidUID, pattern))
                        {
                            if (this.db1ValsToSnd[1] != 3)
                            {
                                logRecorder.AddDebugLog(nodeName, "读料框ID错误，含有非法字符 ！" + this.rfidUID);
                            }
                            this.db1ValsToSnd[1] = 3;
                            continue;
                        }

                        if (this.rfidUID.Length > 9)
                        {
                            this.rfidUID = this.rfidUID.Substring(0, 9);
                        }
                    }
                    if (string.IsNullOrWhiteSpace(this.rfidUID))
                    {
                        port.CurrentStat.Status = EnumNodeStatus.无法识别;
                        port.CurrentStat.StatDescribe = "读卡失败";
                        port.CurrentTaskDescribe = "读RFID卡失败";
                        if (port.Db1ValsToSnd[0] != 3)
                        {
                            logRecorder.AddDebugLog(nodeName, "读RFID失败，长度不足9字符");
                        }
                        port.Db1ValsToSnd[0] = 3;
                        continue;
                    }

                    if (this.rfidUID.Length < 9)
                    {
                        if (port.Db1ValsToSnd[0] != 3)
                        {
                            logRecorder.AddDebugLog(nodeName, "读RFID失败，长度不足9字符");
                        }
                        port.Db1ValsToSnd[0] = 3;
                        continue;
                    }

                    palletUID = rfidUID;
                    port.LogRecorder.AddDebugLog(port.NodeName, "读到托盘号:" + palletUID);

                    int palletStat = MESPalletStat(palletUID, ref reStr);
                    if (palletStat < 0)
                    {
                        port.LogRecorder.AddDebugLog(port.NodeName, string.Format("查询MES托盘{0}状态失败,{1}", palletUID, reStr));
                        continue;
                    }
                    ANCStepResult stepRe = MesAcc.GetStep(palletUID);
                    if (stepRe.ResultCode != 0)
                    {
                        port.CurrentTaskDescribe = "查询MES托盘步次失败:" + stepRe.ResultMsg;
                        continue;
                    }
                    if (this.houseName == EnumStoreHouse.A1库房.ToString())
                    {
                        if(stepRe.Step==0 )
                        {
                            taskType = SysCfg.EnumAsrsTaskType.空框入库;
                            checkinArea = EnumLogicArea.空筐区;

                        }
                        else
                        {
                            port.PushPalletID(palletUID);
                            cellGoods = port.PalletBuffer.ToArray();
                            if (cellGoods == null || cellGoods.Count() < 1)
                            {
                                continue;
                            }
                             if(stepRe.Step < 4)
                             {
                                 checkinArea = EnumLogicArea.一次高温A区;
                             }
                             else
                             {
                                 checkinArea = EnumLogicArea.一次高温B区;
                             }
                        }
                       

                    }
                    else
                    {
                        port.PushPalletID(palletUID);
                        cellGoods = port.PalletBuffer.ToArray();
                        if (cellGoods == null || cellGoods.Count() < 1)
                        {
                            continue;
                        }
                     
                    }

                }
                #endregion
                //申请货位
                if (AsrsCheckinTaskRequire(port, checkinArea, taskType, cellGoods, ref reStr))
                {
                   // port.PalletBuffer.Clear(); //清空入口缓存
                    if(port.ClearBufPallets(ref reStr))
                    {
                        port.Db1ValsToSnd[0] = 2;
                    }
                    else
                    {
                        logRecorder.AddDebugLog(port.NodeName, "清理入口缓存数据失败" + reStr);
                    }
                    
                }
                else
                {
                    if (port.Db1ValsToSnd[0] != 5)
                    {
                        string logStr = string.Format("{0}申请失败,因为：{1}", taskType.ToString(), reStr);
                        logRecorder.AddDebugLog(nodeName, logStr);
                    }
                    port.Db1ValsToSnd[0] = 5;

                }

              
            }
        }
        private void AsrsInportBusiness2(AsrsPortalModel port)
        {
            if(port.PortCata == 2)
            {
                return;
            }
            SysCfg.EnumAsrsTaskType taskType = port.BindedTaskInput;
            port.CurrentTaskDescribe = "";
            //1 入库请求
            AsrsCheckinRequire2(port);
            if (port.Db2Vals[0] < 2)
            {
                port.Db1ValsToSnd[0] = 1;
                port.Db1ValsToSnd[1] = 0;//
                port.CurrentStat.Status = EnumNodeStatus.设备空闲;
                port.CurrentStat.StatDescribe = "空";
                port.CurrentTaskDescribe = "复位：无读卡请求";
            }
            //2 读卡
           
            if (port.Db2Vals[0] == 2 && port.Db1ValsToSnd[0] != 2)
            {
                //读卡
               
                if (SysCfg.SysCfgModel.UnbindMode)
                {
                    this.rfidUID = System.Guid.NewGuid().ToString();
                    port.Db1ValsToSnd[0] = 2;
                }
                else
                {
                    if (SysCfg.SysCfgModel.SimMode || SysCfg.SysCfgModel.RfidSimMode)
                    {
                        this.rfidUID = port.SimRfidUID;
                    }
                    else
                    {
                        if(port.BarcodeRW != null)
                        {
                            this.rfidUID = port.BarcodeRW.ReadBarcode();
                        }
                        else
                        {
                            this.rfidUID = port.RfidRW.ReadStrData();// port.RfidRW.ReadUID();
                        }
                    }
                    if (string.IsNullOrWhiteSpace(this.rfidUID))
                    {
                        port.CurrentStat.Status = EnumNodeStatus.无法识别;
                        port.CurrentStat.StatDescribe = "读卡失败";
                        port.CurrentTaskDescribe = "读RFID卡失败";
                        if(port.Db1ValsToSnd[0] != 3)
                        {
                           
                            logRecorder.AddDebugLog(nodeName, "读RFID失败");
                        }
                        port.Db1ValsToSnd[0] = 3;
                        return;

                    }
                    else
                    {
                        this.rfidUID = this.rfidUID.Trim(new char[] { '\0', '\r', '\n', '\t', ' ' });
                        string pattern = @"^[a-zA-Z0-9]*$"; //匹配所有字符都在字母和数字之间  
                        if (!System.Text.RegularExpressions.Regex.IsMatch(this.rfidUID, pattern))
                        {
                            if (this.db1ValsToSnd[1] != 3)
                            {
                                logRecorder.AddDebugLog(nodeName, "读料框ID错误，含有非法字符 ！" + this.rfidUID);
                            }
                            this.db1ValsToSnd[1] = 3;
                            return ;
                        }

                        if (this.rfidUID.Length > 9)
                        {
                            this.rfidUID = this.rfidUID.Substring(0, 9);
                        }
                        if (this.rfidUID.Length < 9)
                        {
                            if (port.Db1ValsToSnd[0] != 3)
                            {
                                logRecorder.AddDebugLog(nodeName, "读RFID失败，长度不足9字符");
                            }
                            port.Db1ValsToSnd[0] = 3;
                            return;
                        }
                        port.CurrentTaskDescribe = "RFID读卡完成";
                        //批次判断
                        port.Db1ValsToSnd[0] = 2;
                        port.LogRecorder.AddDebugLog(port.NodeName, "读到托盘号:" + this.rfidUID);
                        /*
                        List<MesDBAccess.Model.ProductOnlineModel> productList = this.productOnlineBll.GetModelList(string.Format("palletID='{0}' and palletBinded=1 ", this.rfidUID));
                        if (productList == null || productList.Count() < 1)
                        {
                            if (port.Db1ValsToSnd[0] != 4)
                            {
                                logRecorder.AddDebugLog(port.NodeName, "工装板绑定数据为空，" + this.rfidUID);
                            }
                            port.Db1ValsToSnd[0] = 4;
                            port.CurrentStat.Status = EnumNodeStatus.设备故障;
                            port.CurrentStat.StatDescribe = "工装板绑定数据为空";
                            port.CurrentTaskDescribe = "工装板绑定数据为空";
                            return;
                        }*/

                    }
                }
                
            }
            if (port.PalletBuffer.Count() < 1)
            {
                if (port.Db2Vals[1] == 2 || port.Db2Vals[2] == 2) //入口位置1为空
                {
                    port.CurrentTaskDescribe = string.Format("状态异常：入口缓存数据为空，实际入口位置有料框!");
                }
                
            }
            if (port.Db2Vals[1] != 2 && port.Db2Vals[2] != 2)
            {
                if(port.PalletBuffer.Count()>0)
                {
                    port.CurrentTaskDescribe = string.Format("状态异常：实际入口位置为空，入口缓存数据却不为空!");
                }
               
            }
         
          //  port.RfidUID = this.rfidUID;
            if(!string.IsNullOrWhiteSpace(this.rfidUID))
            {
                if (port.PalletBuffer.Count() < 1 ) //记录缓存为空，并且位置1未检测到料箱
                {
                    if(port.Db2Vals[1] != 2 && port.Db2Vals[2]!=2)
                    {
                        port.Db1ValsToSnd[1] = 2;
                        port.PushPalletID(this.rfidUID);
                        this.rfidUID = "";
                    }
                    
                   
                }
                else //if(port.PalletBuffer.Count()<port.PortinBufCapacity)
                {
                    string lastPalletID = port.PalletBuffer[port.PalletBuffer.Count() - 1];
                    VMResultLot reLast = MesAcc.GetTrayCellLotNO(lastPalletID);
                    VMResultLot reCur = MesAcc.GetTrayCellLotNO(this.rfidUID);
                    if (reLast.ResultCode != 0)
                    {
                        logRecorder.AddDebugLog(nodeName,string.Format("查询MES 托盘号{0}的批次失败,{1}", lastPalletID, reLast.ResultMsg));
                        return;
                    }
                    if (reCur.ResultCode != 0)
                    {
                        logRecorder.AddDebugLog(nodeName,string.Format("查询MES 托盘号{0}的批次失败,{1}", this.rfidUID, reCur.ResultMsg));
                        return;
                    }
                    string preBatch = reLast.LotNO;//productOnlineBll.GetBatchNameofPallet(lastPalletID);
                    string curBatch = reCur.LotNO;//productOnlineBll.GetBatchNameofPallet(this.rfidUID);


                    //string preBatch = productOnlineBll.GetBatchNameofPallet(port.PalletBuffer[port.PalletBuffer.Count() - 1]);
                    //string curBatch = productOnlineBll.GetBatchNameofPallet(this.rfidUID);
                    if (preBatch == curBatch)
                    {
                        port.Db1ValsToSnd[1] = 2;
                        port.PushPalletID(this.rfidUID);
                        this.rfidUID = "";
                    }
                    else
                    {
                        port.Db1ValsToSnd[1] = 1;
                    }
                }
            }
            

        }

        /// <summary>
        /// 入口请求处理规则2：入口多框一起分批次入库
        /// </summary>
        private void AsrsCheckinRequire2(AsrsPortalModel port)
        {
            SysCfg.EnumAsrsTaskType taskType = port.BindedTaskInput;
            if (taskType != SysCfg.EnumAsrsTaskType.产品入库)
            {
                return;
            }
            //入库申请
            bool asrsCheckinReq = false;
            if (port.Db1ValsToSnd[1] == 1 && port.PalletBuffer.Count() > 0 && port.Db2Vals[1] == 2)
            {
                //不同批，入口位置又信号，缓存有数据
                asrsCheckinReq = true;
            }
            else if ( port.PalletBuffer.Count() == port.PortinBufCapacity && port.Db2Vals[2] == 2)
            {
                asrsCheckinReq = true;
            }
            else if (port.Db2Vals[3] == 2 && port.PalletBuffer.Count() > 0) //手动入库按钮请求
            {
                asrsCheckinReq = true;
            }
            if(!asrsCheckinReq)
            {
                return;
            }
            string[] cellGoods = null;
            cellGoods = port.PalletBuffer.ToArray();
            if (cellGoods == null || cellGoods.Count() < 1)
            {
                return;
            }
            /*
            if (!SysCfg.SysCfgModel.UnbindMode)
            {
                List<MesDBAccess.Model.ProductOnlineModel> productList = this.productOnlineBll.GetModelList(string.Format("palletID='{0}' and palletBinded=1 ", cellGoods[0]));
                if (productList == null || productList.Count() < 1)
                {
                    if (port.Db1ValsToSnd[0] != 4)
                    {
                        logRecorder.AddDebugLog(port.NodeName, "工装板绑定数据为空:" + cellGoods[0]);
                    }
                    port.Db1ValsToSnd[0] = 4;
                    port.CurrentStat.Status = EnumNodeStatus.设备故障;
                    port.CurrentStat.StatDescribe = "工装板绑定数据为空";
                    port.CurrentTaskDescribe = "工装板绑定数据为空,入库U任务申请失败";
                    return;
                }
            }*/
            //cellGoods = new string[] { palletUID };
            string reStr = "";
            if(AsrsCheckinTaskRequire(port,EnumLogicArea.常温区,SysCfg.EnumAsrsTaskType.产品入库,cellGoods,ref reStr))
            {
                //port.PalletBuffer.Clear(); //清空入口缓存
                //if(port.ClearBufPallets(ref reStr))
                //{
                //    if (!string.IsNullOrWhiteSpace(this.rfidUID))
                //    {
                //        port.PushPalletID(this.rfidUID);
                //    }
                //}
                //else
                //{
                //    logRecorder.AddDebugLog(port.NodeName, "清理入口缓存数据失败,"+reStr);
                //}
              if(!port.ClearBufPallets(ref reStr))
              {
                  logRecorder.AddDebugLog(port.NodeName, "清理入口缓存数据失败," + reStr);
              }
            }
            else
            {
                if(port.Db1ValsToSnd[0] != 5)
                {
                    string logStr = string.Format("{0}申请失败,因为：{1}", taskType.ToString(), reStr);
                    logRecorder.AddDebugLog(nodeName, logStr);
                }
                port.Db1ValsToSnd[0] = 5;
              
            }
            //CellCoordModel requireCell = null;
            
            //if (asrsResManage.CellRequire(this.houseName, EnumLogicArea.常温区.ToString(), ref requireCell, ref reStr))
            //{
            //    //生成任务
            //    ControlTaskModel asrsTask = new ControlTaskModel();
            //    asrsTask.DeviceID = this.stacker.NodeID;
            //    asrsTask.CreateMode = "自动";
            //    asrsTask.CreateTime = System.DateTime.Now;
            //    asrsTask.TaskID = System.Guid.NewGuid().ToString();
            //    asrsTask.TaskStatus = SysCfg.EnumTaskStatus.待执行.ToString();
            //    asrsTask.TaskType = (int)taskType;
            //    AsrsTaskParamModel taskParam = new AsrsTaskParamModel();

            //    taskParam.CellPos1 = requireCell;
            //    taskParam.InputPort = port.PortSeq;
            //    // if (taskType == EnumAsrsTaskType.产品入库)
            //    // {
            //    taskParam.InputCellGoods = cellGoods;
            //    //  }
            //    asrsTask.TaskParam = taskParam.ConvertoStr(taskType);


            //    //申请完成后要锁定货位
            //    if (!asrsResManage.UpdateCellStatus(houseName, requireCell, EnumCellStatus.空闲, EnumGSTaskStatus.锁定, ref reStr))
            //    {
            //        logRecorder.AddDebugLog(nodeName, "更新货位状态失败," + reStr);
            //        return;
            //    }
            //    if (!asrsResManage.UpdateGSOper(houseName, requireCell, EnumGSOperate.入库, ref reStr))
            //    {
            //        logRecorder.AddDebugLog(nodeName, "更新货位操作类行失败," + reStr);
            //        return;
            //    }
            //    else
            //    {
            //        asrsTask.tag1 = houseName;
            //        asrsTask.tag2 = string.Format("{0}-{1}-{2}", requireCell.Row, requireCell.Col, requireCell.Layer);
            //        asrsTask.Remark = taskType.ToString();
            //        ctlTaskBll.Add(asrsTask);
                    
            //        string logInfo = string.Format("生成新的任务:{0},货位：{1}-{2}-{3}", taskType.ToString(), requireCell.Row, requireCell.Col, requireCell.Layer);
            //        logRecorder.AddDebugLog(nodeName, logInfo);
            //        port.PalletBuffer.Clear(); //清空入口缓存
            //        if(!string.IsNullOrWhiteSpace(this.rfidUID))
            //        {
            //            port.PushPalletID(this.rfidUID);
            //        }
            //    }
            //}
            //else
            //{
            //    string logStr = string.Format("{0}申请失败,因为：{1}", taskType.ToString(), reStr);
            //    logRecorder.AddDebugLog(nodeName, logStr);
            //    return;
            //}
           // port.PalletBuffer.Clear();
           
        }
        private void EmptyPalletOutputRequire(Dictionary<string, GSMemTempModel> asrsStatDic)
        {
            AsrsPortalModel port = null;
            if(this.houseName== EnumStoreHouse.A1库房.ToString())
            {
                port = ports[1];
            }
            else if(this.houseName== EnumStoreHouse.C1库房.ToString() || this.houseName== EnumStoreHouse.C2库房.ToString())
            {
                port = ports[2];
            }
            else
            {
                return;
            }
            if(this.houseName== EnumStoreHouse.A1库房.ToString())
            {
                if (port.Db2Vals[0] == 1)//出口有框，禁止出库
                {
                    return;
                }
                if (port.Db1ValsToSnd[0] == 2) //出库请求已经应答
                {
                    return;
                }
               if(port.Db2Vals[1] != 2) //无空框出库请求
               {
                   return;
               }
            }
            else
            {
                if (port.Db2Vals[1] == 1)//出口有框，禁止出库
                { 
                    return;
                }
                if (port.Db1ValsToSnd[0] == 2)//出库请求已经应答
                {
                    return;
                }
                if (port.Db2Vals[0] != 3) //无空框出库请求
                {
                    return;
                }
               
            }
            bool exitFlag = false;
            int r = 1, c = 1, L = 1;
            for (r = 1; r < asrsRow + 1; r++)
            {
                if (exitFlag)
                {
                    break;
                }
                for (c = 1; c < asrsCol + 1; c++)
                {
                    if (exitFlag)
                    {
                        break;
                    }
                    for (L = 1; L < asrsLayer + 1; L++)
                    {
                        CellCoordModel cell = new CellCoordModel(r, c, L);
                        string strKey = string.Format("{0}:{1}-{2}-{3}", houseName, r, c, L);
                        GSMemTempModel cellStat = null;
                        if (!asrsStatDic.Keys.Contains(strKey))
                        {
                            continue;
                        }
                        cellStat = asrsStatDic[strKey];

                        if (cellStat.GSStatus != EnumCellStatus.空料框.ToString())
                        {
                            continue;
                        }

                        if (cellStat.GSTaskStatus != EnumGSTaskStatus.锁定.ToString() && cellStat.GSEnabled)
                        {
                            if (GenerateOutputTask(cell, null, SysCfg.EnumAsrsTaskType.空框出库, true))
                            {
                                exitFlag = true;
                                port.Db1ValsToSnd[0] = 2;
                                string reStr = "";
                                if (!port.NodeCmdCommit(true, ref reStr))
                                {
                                    logRecorder.AddDebugLog(port.NodeName, "发送命令失败" + reStr);
                                }
                                else
                                {
                                    return;
                                }
                            }
                        }
                    }
                }
            }
           
        }
        //private void AsrsInputRequire(string portDevID)
        //{
        //    List<string> inputPorts = new List<string>();
        //    inputPorts.AddRange(new string[]{"2001","2003","2005","2007","2009","2011"});
        //    if(!inputPorts.Contains(portDevID))
        //    {
        //        return;
        //    }
        //    AsrsPortalModel port = GetPortByDeviceID(portDevID);
        //    if(!port.NodeEnabled)
        //    {
        //        return;
        //    }
        //    EnumAsrsTaskType taskType = EnumAsrsTaskType.产品入库;
        //    if (portDevID == "2007" || portDevID=="2011")  //zwx,此处需要修改
        //    {
        //        taskType = EnumAsrsTaskType.空框入库;
        //    }
        //    if(port.Db2Vals[0] < 2)
        //    {
        //        port.Db1ValsToSnd[0] = 1;
        //        port.CurrentStat.Status = EnumNodeStatus.设备空闲;
        //        port.CurrentStat.StatDescribe = "空";
        //        port.CurrentTaskDescribe = "无入库请求";
        //    }
        //    if(port.Db1ValsToSnd[0] == 2 || port.Db1ValsToSnd[0] == 4 || port.Db1ValsToSnd[0] == 5)
        //    {
        //        return;
        //    }
        //    if(2 != port.Db2Vals[0])
        //    {
        //        return;
        //    }
        //    port.CurrentStat.Status = EnumNodeStatus.设备使用中;
        //    port.CurrentStat.StatDescribe = "入库申请";
        //    if (ExistUnCompletedTask((int)taskType))
        //    {
        //        return;
        //    }
        //    //读卡
        //    string palletUID = string.Empty;
        //    if (taskType == EnumAsrsTaskType.产品入库)
        //    {
               
        //        //只有产品入库才读RFID
        //        if(SysCfg.SysCfgModel.UnbindMode)
        //        {
        //            palletUID = System.Guid.NewGuid().ToString();
        //        }
        //        else
        //        {
        //            if (!SysCfg.SysCfgModel.SimMode)
        //            {
        //                rfidUID = port.RfidRW.ReadUID();

        //            }
        //            else
        //            {
        //                rfidUID = port.SimRfidUID;

        //            }
        //            palletUID = rfidUID;
                  
        //        }
                
              
        //    }
        //    string[] cellGoods = null;
        //    if (taskType == EnumAsrsTaskType.产品入库)
        //    {
        //        if (string.IsNullOrWhiteSpace(palletUID))
        //        {
        //            port.CurrentStat.Status = EnumNodeStatus.无法识别;
        //            port.CurrentStat.StatDescribe = "读卡失败";
        //            port.CurrentTaskDescribe = "读RFID卡失败";
        //            port.Db1ValsToSnd[0] = 3;
        //            return;
        //        }
        //        if(!SysCfg.SysCfgModel.UnbindMode)
        //        {
        //            List<MesDBAccess.Model.ProductOnlineModel> productList = this.productOnlineBll.GetModelList(string.Format("palletID='{0}' and palletBinded=1 ", palletUID));
        //            if (productList == null || productList.Count() < 1)
        //            {
        //                if (port.Db1ValsToSnd[0] != 4)
        //                {
        //                    logRecorder.AddDebugLog(port.NodeName, "工装板绑定数据为空");
        //                }
        //                port.Db1ValsToSnd[0] = 4;
        //                port.CurrentStat.Status = EnumNodeStatus.设备故障;
        //                port.CurrentStat.StatDescribe = "工装板绑定数据为空";
        //                port.CurrentTaskDescribe = "工装板绑定数据为空";
        //                return;
        //            }
        //        }
               
        //        cellGoods = new string[]{palletUID};
               
                
        //    }
           
        //    //申请货位
        //    CellCoordModel requireCell = null;
        //    string reStr="";
        //    if(asrsResManage.CellRequire(this.houseName,EnumLogicArea.常温区.ToString(),ref requireCell,ref reStr))
        //    {
        //        //生成任务
        //        ControlTaskModel asrsTask = new ControlTaskModel();
        //        asrsTask.DeviceID = this.stacker.NodeID;
        //        asrsTask.CreateMode = "自动";
        //        asrsTask.CreateTime = System.DateTime.Now;
        //        asrsTask.TaskID = System.Guid.NewGuid().ToString();
        //        asrsTask.TaskStatus = EnumTaskStatus.待执行.ToString();
        //        asrsTask.TaskType = (int)taskType;
        //        AsrsTaskParamModel taskParam = new AsrsTaskParamModel();
                
        //        taskParam.CellPos1 = requireCell;
        //        if(taskType == EnumAsrsTaskType.产品入库)
        //        {
        //            taskParam.InputPort = 1;
        //            taskParam.InputCellGoods = cellGoods;// new string[] { "模组条码1", "条码2" };
        //        }
        //        else if(taskType== EnumAsrsTaskType.空框入库)
        //        {
        //            taskParam.InputPort = 3;
        //        }
        //        asrsTask.TaskParam = taskParam.ConvertoStr(taskType);
               
               
        //        //申请完成后要锁定货位
        //        if (!asrsResManage.UpdateCellStatus(houseName, requireCell, EnumCellStatus.空闲, EnumGSTaskStatus.锁定,ref reStr))
        //        {
        //            logRecorder.AddDebugLog(nodeName, "更新货位状态失败," + reStr);
        //            return;
        //        }
        //        if(!asrsResManage.UpdateGSOper(houseName,requireCell,EnumGSOperate.入库,ref reStr))
        //        {
        //            logRecorder.AddDebugLog(nodeName, "更新货位操作类行失败," + reStr);
        //            return;
        //        }
        //        else
        //        {
        //            asrsTask.tag1 = houseName;
        //            asrsTask.tag2 = string.Format("{0}-{1}-{2}", requireCell.Row, requireCell.Col, requireCell.Layer);
        //            asrsTask.Remark = taskType.ToString();
        //            ctlTaskBll.Add(asrsTask);
        //            port.Db1ValsToSnd[0] = 2;
        //            string logInfo = string.Format("生成新的任务:{0},货位：{1}-{2}-{3}", taskType.ToString(), requireCell.Row, requireCell.Col, requireCell.Layer);
        //            logRecorder.AddDebugLog(nodeName, logInfo);
        //        }
        //    }
        //    else
        //    {
        //        string logStr = string.Format("{0}申请失败,因为：{1}", taskType.ToString(), reStr);
        //        logRecorder.AddDebugLog(nodeName, logStr);
        //        return;
        //    }
        //    //申请成后，应答
        //    port.Db1ValsToSnd[0] = 2;
            
        //}
        //private void AsrsInputRequire_2001()
        //{
        //    AsrsPortalModel port = GetPortByDeviceID("2001");
          
        //    if (2 == port.Db2Vals[0])
        //    {
        //        if(ExistUnCompletedTask((int)EnumAsrsTaskType.产品入库))
        //        {
        //            return;
        //        }
        //        //读卡
        //        string palletUID = port.RfidRW.ReadUID(); 
        //        if(string.IsNullOrWhiteSpace(palletUID))
        //        {
        //            return;
        //        }
        //        //申请货位
        //        CellCoordModel requireCell = null;
        //        string reStr="";
        //        if(asrsResManage.CellRequire(this.houseName,ref requireCell,ref reStr))
        //        {
        //            //生成任务
        //            ControlTaskModel asrsTask = new ControlTaskModel();
        //            asrsTask.DeviceID = this.stacker.NodeID;
        //            asrsTask.CreateMode = "自动";
        //            asrsTask.TaskID = System.Guid.NewGuid().ToString();
        //            asrsTask.TaskStatus = EnumTaskStatus.待执行.ToString();
        //            asrsTask.TaskType = (int)(EnumAsrsTaskType.产品入库);
        //            AsrsTaskParamModel taskParam = new AsrsTaskParamModel();
        //            taskParam.CellPos1 = requireCell;
        //            //查询产品绑定数据库，根据RFID查询条码
        //            taskParam.InputCellGoods = new string[]{"模组条码1","条码2"};
        //            asrsTask.TaskParam = taskParam.ConvertoStr(EnumAsrsTaskType.产品入库);

        //            ctlTaskBll.Add(asrsTask);
        //        }
        //        else
        //        {
        //            logRecorder.AddDebugLog(nodeName, "申请产品入库失败" + reStr);
        //            return;
        //        }
        //        //申请成后，应答
        //        port.Db1ValsToSnd[0] = 2;
        //    }
        //}
        //private void AsrsInputRequire_2003()
        //{
        //    AsrsPortalModel port2003 = GetPortByDeviceID("2003");
        //    //空框入库
        //    AsrsPortalModel port = GetPortByDeviceID("2003");
        //    if (2 == port.Db2Vals[0])
        //    {
        //        if (ExistUnCompletedTask((int)EnumAsrsTaskType.空框入库))
        //        {
        //            return;
        //        }
        //        //申请货位
        //        CellCoordModel requireCell = null;
        //        string reStr="";
        //        if(asrsResManage.CellRequire(this.houseName,ref requireCell,ref reStr))
        //        {
        //            //生成任务
        //            ControlTaskModel asrsTask = new ControlTaskModel();
        //            asrsTask.DeviceID = this.stacker.NodeID;
        //            asrsTask.CreateMode = "自动";
        //            asrsTask.TaskID = System.Guid.NewGuid().ToString();
        //            asrsTask.TaskStatus = EnumTaskStatus.待执行.ToString();
        //            asrsTask.TaskType = (int)(EnumAsrsTaskType.空框入库);
        //            AsrsTaskParamModel taskParam = new AsrsTaskParamModel();
        //            taskParam.CellPos1 = requireCell;
        //            asrsTask.TaskParam = taskParam.ConvertoStr(EnumAsrsTaskType.空框入库);

        //            ctlTaskBll.Add(asrsTask);
        //        }
        //        else
        //        {
        //            logRecorder.AddDebugLog(nodeName, "申请空框入库失败" + reStr);
        //            return;
        //        }
        //        //申请成后，应答
        //        port.Db1ValsToSnd[0] = 2;
        //    }
        //}
        private void CellStatusMonitor()
        {
            if(!this.nodeEnabled)
            {
                return;
            }
           // Console.WriteLine(string.Format("{0},P1",houseName));
           // int row = 2, col = 24, layer = 6; //要查询得到
            try
            {
                int r = 1, c = 1, L = 1;
                EnumCellStatus cellStoreStat = EnumCellStatus.空闲;
                EnumGSTaskStatus cellTaskStat = EnumGSTaskStatus.完成;
                //  EnumGSEnabledStatus cellEnabledStatus = EnumGSEnabledStatus.启用;
                List<CellCoordModel> outputEnabledCells = null;
                string reStr = "";
                MesDBAccess.BLL.ViewProduct_PSBll productPSViewBll = new MesDBAccess.BLL.ViewProduct_PSBll();
                //Console.WriteLine("{0} P1", houseName);
                Dictionary<string, GSMemTempModel> asrsStatDic = new Dictionary<string, GSMemTempModel>();
                if (!this.asrsResManage.GetAllGsModel(ref asrsStatDic, ref reStr))
                {
                    Console.WriteLine(string.Format("{0} 获取货位状态失败", houseName));
                    return;
                }
              //  Console.WriteLine("{0} P2", houseName);
                bool emptyPalletExist = false; //A库房空框是否存在(非锁定/禁用，可以生成新的出库任务的）
                if (asrsCheckoutMode == EnumAsrsCheckoutMode.计时出库)
                {
                    for (r = 1; r < asrsRow + 1; r++)
                    {
                        for (c = 1; c < asrsCol + 1; c++)
                        {
                            for (L = 1; L < asrsLayer + 1; L++)
                            {
                                string strKey = string.Format("{0}:{1}-{2}-{3}", houseName, r, c, L);
                                GSMemTempModel cellStat = null;
                                if (!asrsStatDic.Keys.Contains(strKey))
                                {
                                    continue;
                                }
                                cellStat = asrsStatDic[strKey];

                                CellCoordModel cell = new CellCoordModel(r, c, L);
                                if (cellStat.GSStatus == EnumCellStatus.空料框.ToString())
                                {
                                    emptyPalletExist = true;
                                    continue;
                                }
                                if ((!cellStat.GSEnabled) || (cellStat.GSTaskStatus == EnumGSTaskStatus.锁定.ToString()) || (cellStat.GSStatus != EnumCellStatus.满位.ToString()))
                                {
                                    // reStr = string.Format("货位{0}-{1}-{2}禁用,无法生成出库任务", cell.Row, cell.Col, cell.Layer);
                                    continue;
                                }

                                DateTime inputTime = System.DateTime.Now;
                                if (!this.asrsResManage.GetCellInputTime(this.houseName, cell, ref inputTime))
                                {
                                    continue;
                                }
                                DateTime curTime = System.DateTime.Now;
                                TimeSpan ts = curTime - inputTime;
                                //zwx,此处修改，根据产品查找当前工艺，获取理论存储时间
                                List<string> pallets = new List<string>();
                                if (!this.asrsResManage.GetStockDetail(houseName, cell, ref pallets))
                                {
                                    continue;
                                }
                                if (pallets.Count() < 1)
                                {
                                    continue;
                                }
                                float storeTimeMax = 0;
                                int mesStep = 3;
                                if(this.houseName== EnumStoreHouse.A1库房.ToString())
                                {
                                    mesStep = 3;
                                }
                                else if (this.houseName == EnumStoreHouse.B1库房.ToString())
                                {
                                    mesStep = 11;
                                }
                                else
                                {
                                    /*System.Threading.Thread.Sleep(5000);
                                    ANCStepResult mesStepRe = MesAcc.GetStep(pallets[0]);
                                    if (mesStepRe.ResultCode != 0)
                                    {
                                        Console.WriteLine("库房{0}出库计时过程中查询MES工步失败,货位：{1}-{2}-{3}，托盘：{4}", houseName, r, c, L, pallets[0]);
                                        continue;
                                    }
                                    mesStep = mesStepRe.Step;*/
                                    if(cellStat.StoreAreaName == EnumLogicArea.冷却区.ToString())
                                    {
                                        mesStep = 12;
                                    }
                                    else
                                    {
                                        mesStep = 18;
                                    }
                                }
                              
                                string localProcessStep = mesStepLocalMap[mesStep];
                                MesDBAccess.BLL.ProcessStepBll processStepBll = new MesDBAccess.BLL.ProcessStepBll();
                                MesDBAccess.Model.ProcessStepModel processStep = processStepBll.GetModel(localProcessStep);
                                if (processStep == null)
                                {
                                    Console.WriteLine("库房{0}出库计时过程中发生内部错误,查询出库计时参数失败", houseName);
                                    continue;
                                }
                                if (!float.TryParse(processStep.tag1, out storeTimeMax))
                                {
                                    Console.WriteLine("库房{0}出库计时过程中发生内部错误,解析出库计时参数失败", houseName);
                                    continue;
                                }
                                storeTimeMax *= 60.0f;
                                
                                if (ts.TotalMinutes > storeTimeMax)
                                {
                                    //静置时间到，可以出 
                                    this.asrsResManage.GetCellStatus(this.houseName, cell, ref cellStoreStat, ref cellTaskStat);
                                   // if (cellTaskStat != EnumGSTaskStatus.出库允许)
                                    if(cellTaskStat == EnumGSTaskStatus.完成)
                                    {
                                        if (!this.asrsResManage.UpdateCellStatus(this.houseName, cell, cellStoreStat, EnumGSTaskStatus.出库允许, ref reStr))
                                        {
                                            Console.WriteLine(string.Format("{0}更新货位状态失败", this.houseName));
                                        }
                                    }

                                }
                            }
                        }
                    }
                }
                //  Console.WriteLine(string.Format("{0},P2", houseName));
                //Console.WriteLine("{0} P3", houseName);
                //是否存在可以空框出库的任务
                if (emptyPalletExist)
                {
                    if(houseName== EnumStoreHouse.A1库房.ToString())
                    {
                        ports[1].Db1ValsToSnd[1] = 2;
                    }
                    else if(houseName== EnumStoreHouse.C1库房.ToString() || houseName== EnumStoreHouse.C2库房.ToString())
                    {
                        ports[2].Db1ValsToSnd[1] = 2;
                    }
                }
                else
                {
                    if (houseName == EnumStoreHouse.A1库房.ToString())
                    {
                        ports[1].Db1ValsToSnd[1] = 1;
                    }
                    else if (houseName == EnumStoreHouse.C1库房.ToString() || houseName == EnumStoreHouse.C2库房.ToString())
                    {
                        ports[2].Db1ValsToSnd[1] = 1;
                    }
                }

                //统一生成出库任务
                outputEnabledCells = new List<CellCoordModel>();
                if (asrsResManage.GetAllowLeftHouseGs(this.houseName, ref outputEnabledCells, ref reStr))
                {
                    if (outputEnabledCells != null && outputEnabledCells.Count() > 0)
                    {
                        GenerateAutoOutputTaskMulti(outputEnabledCells, SysCfg.EnumAsrsTaskType.产品出库);
                    }
                }
                EmptyPalletOutputRequire(asrsStatDic);
                //Console.WriteLine("{0} P4", houseName);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
         

        }
        private void PortBusinessLoop()
        {
            try
            {
                //zwx,此处需要修改
                if(!this.nodeEnabled)
                {
                    return;
                }
                
               // IPlcRW plcRW = ports[0].PlcRW;
                //if (!SysCfgModel.SimMode)
                //{
                //    if (lastPortPlcStat == plcRW.PlcStatCounter)
                //    {
                //        return;
                //    }
                //}

                string reStr = "";
                for (int i = 0; i < ports.Count(); i++)
                {
                    AsrsPortalModel port = ports[i];
                    if (!port.ReadDB2(ref reStr))
                    {
                        logRecorder.AddDebugLog(port.NodeName, "读DB2数据错误");
                        continue;
                    }
                    if (!port.NodeCmdCommit(false, ref reStr))
                    {
                        logRecorder.AddDebugLog(port.NodeName, "数据提交错误");
                        continue;
                    }
                }
               //出口状态
                //string[] outPorts = new string[] { "2002", "2004", "2012","2013" };
                foreach (AsrsPortalModel port in ports)
                {
                    if(port.PortCata ==1)
                    {
                        continue;
                    }
                    if(port.Db2Vals[0] ==2)
                    {
                        port.CurrentStat.StatDescribe = "允许出库";
                        port.CurrentStat.Status = EnumNodeStatus.设备空闲;
                    }
                    else
                    {
                        port.CurrentStat.StatDescribe = "禁止出库";
                        port.CurrentStat.Status = EnumNodeStatus.设备使用中;
                    }
                }
                
                //1 查询各入口是否有入库申请，注意：申请过的就不要申请，防止重复申请。
                AsrsInportBusiness();

                //2 若堆垛机处于空闲状态，根据调度规则取任务
                AsrsTaskAllocate();// 堆垛机作业调度
                //lastPortPlcStat = plcRW.PlcStatCounter;
            }
            catch (Exception ex)
            {
                ThrowErrorStat("异常发生:" + ex.ToString(), EnumNodeStatus.设备故障);
            }
           
        }
        private void PlcCommLoop()
        {
            //if (!plcInitFlag)
            //{
            //    //创建PLC通信对象，连接PLC

            //}
          //  plcRW = this.plcRWs[0] as PLCRWMx;
            //short[] tempDb1Vals = new short[800];
            //if (!plcRW.ReadMultiDB("D2000", 800, ref PLCRWMx.db1Vals))
            //{
            //    this.PauseRun();
            //    logRecorder.AddLog(new LogModel(objectName, "PLC通信失败,系统将停止!", EnumLoglevel.错误));
            //    return;
            //}
            //Array.Copy(tempDb1Vals, PLCRWMx.db1Vals, tempDb1Vals.Count());
            IPlcRW plcRW = stacker.PlcRW;
            DateTime commSt = System.DateTime.Now;
            //if (!plcRW.WriteDB("D2700", 1))
            //{
            //    Console.WriteLine("PLC通信失败!");
            //    //logRecorder.AddLog(new LogModel(objectName, "PLC通信失败!", EnumLoglevel.错误));
            //    string reStr = "";
            //    plcRW.CloseConnect();
            //    if (!plcRW.ConnectPLC(ref reStr))
            //    {
            //        //logRecorder.AddLog(new LogModel(objectName, "PLC重新连接失败!", EnumLoglevel.错误));
            //        Console.WriteLine("PLC重新连接失败!");

            //        return;
            //    }
            //    else
            //    {
            //        logRecorder.AddLog(new LogModel(stacker.NodeName, "PLC重新连接成功!", EnumLoglevel.错误));
            //        return;
            //    }
            //}
            short[] tempDb2Vals = new short[stacker.Db2Vals.Count()];
            if (!plcRW.ReadMultiDB(stacker.Db2StartAddr, stacker.Db2Vals.Count(), ref tempDb2Vals))
            {

                // logRecorder.AddLog(new LogModel(objectName, "PLC通信失败!", EnumLoglevel.错误));
                Console.WriteLine("PLC通信失败!");
                string reStr = "";
                plcRW.CloseConnect();
                if (!plcRW.ConnectPLC(ref reStr))
                {
                    // logRecorder.AddLog(new LogModel(objectName, "PLC重新连接失败!", EnumLoglevel.错误));
                    Console.WriteLine("PLC重新连接失败!");
                   
                    return;
                }
                else
                {
                    logRecorder.AddLog(new LogModel(stacker.NodeName, "PLC重新连接成功!", EnumLoglevel.错误));
                    return;
                }

            }
            Array.Copy(tempDb2Vals, plcRW.Db2Vals, tempDb2Vals.Count());

            short[] tempDB1ValsSnd = new short[stacker.Db1ValsToSnd.Count()];
            Array.Copy(plcRW.Db1Vals, tempDB1ValsSnd, tempDB1ValsSnd.Count());
            if (!plcRW.WriteMultiDB(stacker.Db1StartAddr, stacker.Db1ValsToSnd.Count(), plcRW.Db1Vals))
            {

                //logRecorder.AddLog(new LogModel(objectName, "PLC通信失败!", EnumLoglevel.错误));
                Console.WriteLine("PLC重新连接失败!");
                string reStr = "";
                plcRW.CloseConnect();
                if (!plcRW.ConnectPLC(ref reStr))
                {
                    //logRecorder.AddLog(new LogModel(objectName, "PLC重新连接失败!", EnumLoglevel.错误));
                    Console.WriteLine("PLC重新连接失败!");
                    return;
                }
                else
                {
                    logRecorder.AddLog(new LogModel(stacker.NodeName, "PLC重新连接成功!", EnumLoglevel.错误));
                    return;
                }

            }
            plcRW.PlcRWStatUpdate();
            DateTime commEd = System.DateTime.Now;
            TimeSpan ts = commEd - commSt;
            string dispCommInfo = string.Format("PLC通信周期:{0}毫秒", (int)ts.TotalMilliseconds);
            if (ts.TotalMilliseconds > 500)
            {
                logRecorder.AddDebugLog(stacker.NodeName, dispCommInfo);
            }
           // view.DispCommInfo(dispCommInfo);
        }
        
        /// <summary>
        /// 任务调度
        /// </summary>
        private void AsrsTaskAllocate()
        {
            //zwx,此处需要修改
          //  if(this.houseName == EnumStoreHouse.B1库房.ToString())
            {
                
                //先查询有无紧急任务，
                List<ControlTaskModel> emerTaskList = ctlTaskBll.GetEmerTaskToRunList(SysCfg.EnumTaskStatus.待执行.ToString(), stacker.NodeID);
                if (emerTaskList != null && emerTaskList.Count > 0)
                {
                    List<AsrsPortalModel> validPorts = GetOutPortsOfBindedtask(SysCfg.EnumAsrsTaskType.产品出库);
                    AsrsPortalModel port = null;
                    if (validPorts != null && validPorts.Count() > 0)
                    {
                       port=validPorts[0];
                    }
                    
                    if (port.Db2Vals[0] != 2)
                    {
                        return;
                    }
                    ControlTaskModel task = emerTaskList[0];
                    if (stacker.CurrentTask == null && stacker.Db2Vals[1] == 1)
                    {
                        string reStr = "";
                        if (stacker.FillTask(task, ref reStr))
                        {
                            return;
                        }
                        else
                        {
                            logRecorder.AddDebugLog(nodeName, "分配任务失败," + "," + reStr);
                        }
                    }

                    return;
                }
            }
           
            //1 先计时，如果当前类型任务正在执行，则不计时
            foreach (SysCfg.EnumAsrsTaskType taskType in taskWaitBeginDic.Keys.ToArray())
            //for (int i = 0; i < taskWaitBeginDic.Keys.Count();i++ ) 
            {
              //  EnumAsrsTaskType taskType = taskWaitBeginDic.Keys[i] as EnumAsrsTaskType;
                if (this.stacker.CurrentTask != null && stacker.CurrentTask.TaskType == (int)taskType)
                {
                    taskWaitBeginDic[taskType] = System.DateTime.Now;
                    taskWaitingDic[taskType] = TimeSpan.Zero;
                }
                else
                {
                    taskWaitingDic[taskType] = System.DateTime.Now - taskWaitBeginDic[taskType];
                }
            }
            //2排序
          
            Dictionary<SysCfg.EnumAsrsTaskType, TimeSpan> dicSortDesc = taskWaitingDic.OrderByDescending(o => o.Value).ToDictionary(o => o.Key, p => p.Value);
         
            //foreach (KeyValuePair<EnumAsrsTaskType, TimeSpan> kvp in dicSortDesc)
            //{
            //    Console.WriteLine("{0} 等待时间{1} 毫秒", kvp.Key, (int)kvp.Value.TotalMilliseconds);
            //}


            //3 按照顺序取任务，若当前条件不满足，取下一种任务类型
            if(stacker.CurrentTask == null && stacker.Db2Vals[1] == 1)
            {
                //设备当前任务为空，并且空闲，取新的任务

                foreach (SysCfg.EnumAsrsTaskType taskType in dicSortDesc.Keys.ToArray())
                {
                    //ControlTaskModel task = ctlTaskBll.GetTaskToRun((int)taskType, EnumTaskStatus.待执行.ToString(),stacker.NodeID);
                   

                    //遍历所有可执行任务，找到第一个可用的
                    List<ControlTaskModel> taskList = ctlTaskBll.GetTaskToRunList((int)taskType, SysCfg.EnumTaskStatus.待执行.ToString(), stacker.NodeID);
                    ControlTaskModel task = GetTaskTorun(taskList, (SysCfg.EnumAsrsTaskType)taskType);
                    /*
                    if(taskList != null)
                    {
                        foreach(ControlTaskModel t in taskList)
                        {
                            string reStr = "";
                            AsrsTaskParamModel paramModel = new AsrsTaskParamModel();

                            if (!paramModel.ParseParam((SysCfg.EnumAsrsTaskType)taskType, t.TaskParam, ref reStr))
                            {
                                continue;
                            }
                            EnumGSEnabledStatus cellEnabledStatus = EnumGSEnabledStatus.启用;
                            if (!this.asrsResManage.GetCellEnabledStatus(houseName, paramModel.CellPos1, ref cellEnabledStatus))
                            {
                                // reStr = "获取货位启用状态失败";
                                continue;
                            }
                            if (cellEnabledStatus == EnumGSEnabledStatus.禁用)
                            {
                                continue;
                            }
                            else
                            {
                                task = t;
                                break;
                            }
                        }
                    }*/
                    if(task != null)
                    {
                        string reStr = "";
                        AsrsTaskParamModel paramModel = new AsrsTaskParamModel();

                        if (!paramModel.ParseParam((SysCfg.EnumAsrsTaskType)taskType, task.TaskParam, ref reStr))
                        {
                            continue;
                        }
                        EnumGSEnabledStatus cellEnabledStatus = EnumGSEnabledStatus.启用;
                        if (!this.asrsResManage.GetCellEnabledStatus(houseName, paramModel.CellPos1, ref cellEnabledStatus))
                        {
                           // reStr = "获取货位启用状态失败";
                            continue;
                        }
                        if(cellEnabledStatus == EnumGSEnabledStatus.禁用)
                        {
                            continue;
                        }
                        if (taskType == SysCfg.EnumAsrsTaskType.产品出库 || taskType == SysCfg.EnumAsrsTaskType.空框出库)
                        {
                            List<AsrsPortalModel> validPorts = GetOutPortsOfBindedtask(taskType);
                            AsrsPortalModel port = null;
                            if (validPorts != null && validPorts.Count() > 0)
                            {
                                port = validPorts[0];
                                if(port.PortCata == 3)
                                {
                                    //出入库共用一个口
                                    if (port.Db2Vals[1] != 2)
                                    {
                                        continue;
                                    }
                                }
                                else
                                {
                                    //仅限出口
                                    if (port.Db2Vals[0] != 2)
                                    {
                                        continue;
                                    }
                                }
                                
                            }
                            else
                            {
                                continue;
                            }
                        }
                       
                        if(stacker.FillTask(task,ref reStr))
                        {
                            break;
                        }
                        else
                        {
                            logRecorder.AddDebugLog(nodeName, "分配任务失败," + taskType.ToString() + "," + reStr);
                        }
                    }
                }
               
            }
           
        }
        private ControlTaskModel GetTaskTorun(IList<ControlTaskModel> taskList,SysCfg.EnumAsrsTaskType taskType)
        {
            
            if(taskType == SysCfg.EnumAsrsTaskType.产品出库 && this.houseName == EnumStoreHouse.A1库房.ToString() && dlgtGetTaskTorun != null && SysCfg.SysCfgModel.ZhuyeMode == 2)
            {
                return dlgtGetTaskTorun(this,asrsResManage, taskList, taskType);
            }
            ControlTaskModel task = null;
            if (taskList != null)
            {
                foreach (ControlTaskModel t in taskList)
                {
                    string reStr = "";
                    AsrsTaskParamModel paramModel = new AsrsTaskParamModel();
                    if (!paramModel.ParseParam(taskType, t.TaskParam, ref reStr))
                    {
                        continue;
                    }
                    EnumGSEnabledStatus cellEnabledStatus = EnumGSEnabledStatus.启用;
                    if (!this.asrsResManage.GetCellEnabledStatus(houseName, paramModel.CellPos1, ref cellEnabledStatus))
                    {
                        // reStr = "获取货位启用状态失败";
                        continue;
                    }
                    if (cellEnabledStatus == EnumGSEnabledStatus.禁用)
                    {
                        continue;
                    }
                    else
                    {
                        task = t;
                        break;
                    }
                }
            }
            return task;
        }
        ///// <summary>
        ///// 查询是否存在未完成的任务，包括待执行的
        ///// </summary>
        ///// <param name="taskType"></param>
        ///// <returns></returns>
        //private bool ExistUnCompletedTask(int taskType)
        //{
        //    string strWhere = string.Format("TaskType={0} and DeviceID='{1}' and TaskStatus<>'{2}' and TaskStatus<>'{3}'",
        //        taskType, this.stacker.NodeID, SysCfg.EnumTaskStatus.已完成.ToString(), SysCfg.EnumTaskStatus.任务撤销.ToString());
        //    DataSet ds = ctlTaskBll.GetList(strWhere);
        //    if(ds !=null && ds.Tables.Count>0&& ds.Tables[0].Rows.Count>0)
        //    {
        //        return true;
        //    }
        //    else
        //    {
        //        return false;
        //    }
        //}
        //private string SimModuleGenerate()
        //{
        //    string batchName = SysCfg.SysCfgModel.CheckinBatchHouseA;

        //    //zwx,此处需要修改
        //    //if (this.houseName == EnumStoreHouse.B1库房.ToString())
        //    //{
        //    //    batchName = SysCfg.SysCfgModel.CheckinBatchHouseB;
        //    //}
        //    //if (batchName == "空")
        //    //{
        //    //    batchName = string.Empty;
        //    //}
        //    string palletID = System.Guid.NewGuid().ToString();
        //    //for(int i=0;i<2;i++)
        //    //{
        //    //    string modID = System.Guid.NewGuid().ToString();
        //    //    CtlDBAccess.Model.BatteryModuleModel batModule = new  CtlDBAccess.Model.BatteryModuleModel();
        //    //    batModule.asmTime = System.DateTime.Now;
        //    //    batModule.batModuleID = modID;
        //    //    batModule.curProcessStage = SysCfg.EnumModProcessStage.模组焊接下盖.ToString();
        //    //    batModule.topcapOPWorkerID = "W0001";
        //    //    batModule.palletBinded = true;
        //    //    batModule.palletID = palletID;
        //    //    batModule.batchName = batchName;
        //    //    batModuleBll.Add(batModule);
        //    //}
        //    return palletID;
        //}
        /// <summary>
        /// 处理任务完成信息，更新货位状态
        /// </summary>
        /// <param name="ctlTask"></param>
        /// <returns></returns>
        private bool TaskCompletedProcess(AsrsTaskParamModel taskParamModel, ControlTaskModel ctlTask)
        {
            try
            {
                string reStr = "";

                switch (ctlTask.TaskType)
                {
                    case (int)SysCfg.EnumAsrsTaskType.产品入库:
                        {
                            //1 先更新货位存储状态
                            if (!this.asrsResManage.UpdateCellStatus(this.houseName, taskParamModel.CellPos1,
                                EnumCellStatus.满位,
                                EnumGSTaskStatus.完成,
                                ref reStr))
                            {
                                logRecorder.AddLog(new LogInterface.LogModel(nodeName, "更新货位状态失败：" + reStr, LogInterface.EnumLoglevel.错误));

                                return false;
                            }
                            //2 更新库存状态
                            //获取入库批次，临时调试用
                            //string batchName = SysCfgModel.CheckinBatchHouseA;
                            //if(this.houseName == EnumStoreHouse.B库房.ToString())
                            //{
                            //    batchName = SysCfgModel.CheckinBatchHouseB;
                            //}
                            string batchName = string.Empty;

                            int stepUpdate = 3;
                            if(SysCfg.SysCfgModel.UnbindMode)
                            {
                                batchName = SysCfg.SysCfgModel.CheckinBatchDic[houseName];
                            }
                            else
                            {
                                if (taskParamModel.InputCellGoods != null && taskParamModel.InputCellGoods.Count() > 0)
                                {
                                    string palletID = taskParamModel.InputCellGoods[0];
                                    ANCStepResult stepRe = MesAcc.GetStep(palletID);
                                    if (stepRe.ResultCode != 0)
                                    {
                                        logRecorder.AddDebugLog(nodeName, string.Format("查询托盘MES{0}步次失败", palletID));
                                        return false;
                                    }
									
									//查询MES 托盘批次
                                    if(stepRe.Step<4)
                                    {
                                        batchName = "空";
                                    }
                                    else
                                    {
                                        VMResultLot batchRe = MesAcc.GetTrayCellLotNO(palletID);
                                        if (batchRe.ResultCode == 0)
                                        {
                                            batchName = batchRe.LotNO;
                                            logRecorder.AddDebugLog(nodeName, string.Format("{0}入库完成查询MES批次{1}:", palletID, batchName));
                                        }
                                        else
                                        {
                                            //batchName = productOnlineBll.GetBatchNameofPallet(palletID);
                                            logRecorder.AddLog(new LogInterface.LogModel(nodeName, string.Format("查询托盘{0}的MES批次失败:{1}", palletID, batchRe.ResultMsg), LogInterface.EnumLoglevel.错误));
                                            return false;
                                        }
                                    }
                                    
                                    #region 上传MES 出入库时间，库位，托盘号
                                   
                                  //  if (this.houseName != EnumStoreHouse.A1库房.ToString())
                                    //{
                                        DateTime checkInTime = DateTime.Now;
                                       // DateTime checkOutTime = DateTime.Now;
                                        //this.asrsResManage.GetCellInputTime(this.houseName, taskParamModel.CellPos1, ref checkInTime);
                                        JObject jsonObj = new JObject(new JProperty("Step", "3"), new JProperty("LOC", ""), new JProperty("TrayNO", ""), new JProperty("StartTime", ""), new JProperty("EndTime", ""));
                                        jsonObj["StartTime"] = checkInTime.ToString("yyyy/MM/dd HH:mm:ss");//checkInTime.ToString("yyyy/MM/dd HH:mm:ss");
                                        jsonObj["EndTime"] = " ";// checkOutTime.ToString("yyyy/MM/dd HH:mm:ss");
                                        jsonObj["LOC"] = string.Format("{0}-{1}-{2}", taskParamModel.CellPos1.Row, taskParamModel.CellPos1.Col, taskParamModel.CellPos1.Layer);
                                        if (this.houseName == EnumStoreHouse.A1库房.ToString())
                                        {
                                            jsonObj.Add(new JProperty("Type", "one"));
                                            for (int i = 0; taskParamModel.InputCellGoods != null && i < taskParamModel.InputCellGoods.Count(); i++)
                                            {
                                                palletID = taskParamModel.InputCellGoods[i];
                                                jsonObj["TrayNO"] = palletID;
                                              
                                                stepRe = MesAcc.GetStep(palletID);
                                                if (stepRe.ResultCode != 0)
                                                {
                                                    continue;
                                                }
                                                if (stepRe.Step < 4)
                                                {
                                                    jsonObj["Step"] = "3";
                                                    stepUpdate = 3;
                                                }
                                                else
                                                {
                                                    jsonObj["Step"] = "5";
                                                    stepUpdate = 5;
                                                }
                                                VMResult re = null;
                                                try
                                                {
                                                    re = this.MesAcc.UploadHighTempInfo(jsonObj.ToString());
                                                }
                                                catch (Exception ex)
                                                {
                                                    logRecorder.AddDebugLog(nodeName, "MES网络异常:" + ex.Message) ;
                                                }
                                                
                                                if (re.ResultCode != 0)
                                                {
                                                    reStr = string.Format("{0} 上传MES失败,{1}", palletID, re.ResultMsg);
                                                    logRecorder.AddDebugLog(nodeName, reStr);
                                                    // return false;    //zwx ,11-16
                                                }
                                                this.MesAcc.UpdateStep(stepUpdate, palletID);
                                            }

                                            
                                        }
                                        else if (this.houseName == EnumStoreHouse.B1库房.ToString())
                                        {
                                            jsonObj.Add(new JProperty("Type", "two"));
                                            for (int i = 0; taskParamModel.InputCellGoods != null && i < taskParamModel.InputCellGoods.Count(); i++)
                                            {
                                                palletID = taskParamModel.InputCellGoods[i];
                                                jsonObj["TrayNO"] = palletID;
                                                jsonObj["Step"] = "11";
                                                stepUpdate = 11;
                                                VMResult re = null;
                                                try
                                                {
                                                    re = this.MesAcc.UploadHighTempInfo(jsonObj.ToString());
                                                }
                                                catch (Exception ex)
                                                {
                                                    logRecorder.AddDebugLog(nodeName, "MES网络异常:" + ex.Message);
                                                }
                                                if (re.ResultCode != 0)
                                                {
                                                    reStr = string.Format("{0} 上传MES失败,{1}", palletID, re.ResultMsg);
                                                    logRecorder.AddDebugLog(nodeName, reStr);
                                                    // return false; //zwx ,11-16
                                                }
                                                this.MesAcc.UpdateStep(stepUpdate, palletID);
                                            }
                                        }
                                        else
                                        {
                                            for (int i = 0; taskParamModel.InputCellGoods != null && i < taskParamModel.InputCellGoods.Count(); i++)
                                            {
                                                palletID = taskParamModel.InputCellGoods[i];
                                                jsonObj["TrayNO"] = palletID;
                                                stepRe = MesAcc.GetStep(palletID);
                                                if (stepRe.ResultCode != 0)
                                                {
                                                    continue;
                                                }
                                                if (stepRe.Step <=12)
                                                {
                                                    jsonObj["Step"] = "12";
                                                    stepUpdate = 12;
                                                }
                                                else
                                                {
                                                    jsonObj["Step"] = "18";
                                                    stepUpdate = 18;
                                                }
                                                VMResult re = null;
                                                if (stepRe.Step < 12)
                                                {
                                                    re = this.MesAcc.UploadCoolingInfo(jsonObj.ToString());
                                                }
                                                else
                                                {
                                                    re = this.MesAcc.UploadNormalTempInfo(jsonObj.ToString());
                                                }

                                                if (re.ResultCode != 0)
                                                {
                                                    reStr = string.Format("{0} 上传MES失败,{1}", palletID, re.ResultMsg);
                                                    logRecorder.AddDebugLog(nodeName, reStr);
                                                    // return false; //zwx ,11-16
                                                }
                                                this.MesAcc.UpdateStep(stepUpdate, palletID);
                                            }
                                        }
                                  //  }

                                    #endregion
                                    // CtlDBAccess.Model.BatteryModuleModel batModule = batModuleBll.GetModel(taskParamModel.InputCellGoods[0]);
                                    // batchName = batModule.batchName;
                                }
                            }
                           
                            this.asrsResManage.AddStack(this.houseName, taskParamModel.CellPos1, batchName, taskParamModel.InputCellGoods, ref reStr);

                            //3 更新出入库操作状态
                            this.asrsResManage.UpdateGSOper(this.houseName, taskParamModel.CellPos1, EnumGSOperate.无, ref reStr);

                            //4 增加出入库操作记录
                            this.asrsResManage.AddGSOperRecord(this.houseName, taskParamModel.CellPos1, EnumGSOperateType.入库, "", ref reStr);
                            if (taskParamModel.InputCellGoods != null && taskParamModel.InputCellGoods.Count()>0)
                            {
                                for (int i = 0; i < taskParamModel.InputCellGoods.Count(); i++)
                                {
                                    UpdateOnlineProductInfo(taskParamModel.InputCellGoods[i]);
                                    string logStr = string.Format("产品入库:{0},货位：{1}-{2}-{3},更新MES步次{4}", houseName, taskParamModel.CellPos1.Row, taskParamModel.CellPos1.Col, taskParamModel.CellPos1.Layer,stepUpdate);
                                    AddProduceRecord(taskParamModel.InputCellGoods[i], logStr);

                                }
                            }
                           

                            break;
                        }
                    case (int)SysCfg.EnumAsrsTaskType.空框入库:
                        {
                            //1 先更新货位存储状态
                            if (!this.asrsResManage.UpdateCellStatus(this.houseName, taskParamModel.CellPos1,
                                EnumCellStatus.空料框,
                                EnumGSTaskStatus.完成,
                                ref reStr))
                            {
                                logRecorder.AddLog(new LogInterface.LogModel(nodeName, "更新货位状态失败：" + reStr, LogInterface.EnumLoglevel.错误));

                                return false;
                            }

                            //2 更新库存状态
                            this.asrsResManage.AddEmptyMeterialBox(this.houseName, taskParamModel.CellPos1, ref reStr);

                            //3 更新出入库操作状态
                            this.asrsResManage.UpdateGSOper(this.houseName, taskParamModel.CellPos1, EnumGSOperate.无, ref reStr);

                            //4 增加出入库操作记录
                            this.asrsResManage.AddGSOperRecord(this.houseName, taskParamModel.CellPos1, EnumGSOperateType.入库, "", ref reStr);
                            break;
                        }
                    case (int)SysCfg.EnumAsrsTaskType.空框出库:
                    case (int)SysCfg.EnumAsrsTaskType.产品出库:
                        {
                            int stepUpdate = 3;
                            #region 上传MES 出入库时间，库位，托盘号
                          //  if(this.houseName != EnumStoreHouse.A1库房.ToString())
                           // {
                                DateTime checkInTime = DateTime.Now;
                                DateTime checkOutTime = DateTime.Now;
                                this.asrsResManage.GetCellInputTime(this.houseName, taskParamModel.CellPos1, ref checkInTime);
                                JObject jsonObj = new JObject(new JProperty("Step", "3"), new JProperty("LOC", ""), new JProperty("TrayNO", ""), new JProperty("StartTime", ""), new JProperty("EndTime", ""));
                                jsonObj["StartTime"] = checkInTime.ToString("yyyy/MM/dd HH:mm:ss");
                                jsonObj["EndTime"] = checkOutTime.ToString("yyyy/MM/dd HH:mm:ss");
                                jsonObj["LOC"] = string.Format("{0}-{1}-{2}", taskParamModel.CellPos1.Row, taskParamModel.CellPos1.Col, taskParamModel.CellPos1.Layer);
                              
                                if (this.houseName == EnumStoreHouse.A1库房.ToString())
                                {
                                    jsonObj.Add(new JProperty("Type", "one"));
                                    for (int i = 0; taskParamModel.InputCellGoods != null && i < taskParamModel.InputCellGoods.Count(); i++)
                                    {
                                        string palletID = taskParamModel.InputCellGoods[i];
                                        jsonObj["TrayNO"] = palletID;
                                        ANCStepResult stepRe = MesAcc.GetStep(palletID);
                                        if (stepRe.ResultCode != 0)
                                        {
                                            continue;
                                        }
                                       
                                        VMResult re = this.MesAcc.UploadHighTempInfo(jsonObj.ToString());
                                        if (re.ResultCode != 0)
                                        {
                                            reStr = string.Format("{0} 上传MES失败,{1}", palletID, re.ResultMsg);
                                            logRecorder.AddDebugLog(nodeName, reStr);
                                            // return false; //zwx ,11-16
                                        }
                                        if (stepRe.Step < 4)
                                        {
                                            jsonObj["Step"] = "3";
                                            stepUpdate = 3;
                                        }
                                        else
                                        {
                                            jsonObj["Step"] = "5";
                                            stepUpdate = 5;
                                        }
                                        this.MesAcc.UpdateStep(stepUpdate, palletID);
                                    }
                                   
                                    //this.MesAcc.UploadHighTempInfo()
                                }
                                else if (this.houseName == EnumStoreHouse.B1库房.ToString())
                                {
                                    jsonObj.Add(new JProperty("Type", "two"));
                                    for (int i = 0; taskParamModel.InputCellGoods != null && i < taskParamModel.InputCellGoods.Count(); i++)
                                    {
                                        string palletID = taskParamModel.InputCellGoods[i];
                                        jsonObj["TrayNO"] = palletID;
                                        jsonObj["Step"] = "11";
                                       
                                        VMResult re = this.MesAcc.UploadHighTempInfo(jsonObj.ToString());
                                        if (re.ResultCode != 0)
                                        {
                                            reStr = string.Format("{0} 上传MES失败,{1}", palletID, re.ResultMsg);
                                            logRecorder.AddDebugLog(nodeName, reStr);
                                            //  return false; //zwx ,11-16
                                        }
                                        stepUpdate = 11;
                                        this.MesAcc.UpdateStep(stepUpdate, palletID);
                                    }
                                }
                                else
                                {
                                    for (int i = 0; taskParamModel.InputCellGoods != null && i < taskParamModel.InputCellGoods.Count(); i++)
                                    {
                                        string palletID = taskParamModel.InputCellGoods[i];
                                        jsonObj["TrayNO"] = palletID;
                                        ANCStepResult stepRe = MesAcc.GetStep(palletID);
                                        if (stepRe.ResultCode != 0)
                                        {
                                            continue;
                                        }
                                        if (stepRe.Step <=12)
                                        {
                                            jsonObj["Step"] = "12";
                                            stepUpdate = 12;
                                           
                                        }
                                        else
                                        {
                                            jsonObj["Step"] = "18";
                                            stepUpdate = 18;
                                           
                                        }
                                        VMResult re = null;
                                        if (stepRe.Step < 12)
                                        {
                                            re = this.MesAcc.UploadCoolingInfo(jsonObj.ToString());
                                        }
                                        else
                                        {
                                            re = this.MesAcc.UploadNormalTempInfo(jsonObj.ToString());
                                        }

                                        if (re.ResultCode != 0)
                                        {
                                            reStr = string.Format("{0} 上传MES失败,{1}", palletID, re.ResultMsg);
                                            logRecorder.AddDebugLog(nodeName, reStr);
                                            // return false; //zwx ,11-16
                                        }
                                        this.MesAcc.UpdateStep(stepUpdate, palletID);
                                    }
                                }
                           // }
                               
                            #endregion
                           
                            if (!this.asrsResManage.UpdateCellStatus(this.houseName, taskParamModel.CellPos1,
                                EnumCellStatus.空闲,
                                EnumGSTaskStatus.完成,
                                ref reStr))
                            {
                                logRecorder.AddLog(new LogInterface.LogModel(nodeName, "更新货位状态失败：" + reStr, LogInterface.EnumLoglevel.错误));

                                return false;
                            }
                            //2 移除库存
                            this.asrsResManage.RemoveStack(this.houseName, taskParamModel.CellPos1, ref reStr);

                            //3 更新出入库操作状态
                            this.asrsResManage.UpdateGSOper(this.houseName, taskParamModel.CellPos1, EnumGSOperate.无, ref reStr);

                            //4 增加出入库操作记录
                            EnumGSOperateType gsOPType = EnumGSOperateType.系统自动出库;
                            if (ctlTask.CreateMode == "手动")
                            {
                                gsOPType = EnumGSOperateType.手动出库;
                            }
                            this.asrsResManage.AddGSOperRecord(this.houseName, taskParamModel.CellPos1, gsOPType, "", ref reStr);
                            for (int i = 0; taskParamModel.InputCellGoods != null && i < taskParamModel.InputCellGoods.Count(); i++)
                            {
                                 string logStr = string.Format("产品出库:{0},货位：{1}-{2}-{3}", houseName,taskParamModel.CellPos1.Row,taskParamModel.CellPos1.Col,taskParamModel.CellPos1.Layer);

                                AddProduceRecord(taskParamModel.InputCellGoods[i], logStr);
                                
                            }
                            break;
                        }
                    case (int)SysCfg.EnumAsrsTaskType.移库:
                        {
                            //1 货位1的处理
                            if (!this.asrsResManage.UpdateCellStatus(this.houseName, taskParamModel.CellPos1,
                                EnumCellStatus.空闲,
                                EnumGSTaskStatus.完成,
                                ref reStr))
                            {
                                logRecorder.AddLog(new LogInterface.LogModel(nodeName, "更新货位状态失败：" + reStr, LogInterface.EnumLoglevel.错误));

                                return false;
                            }
                            //this.asrsResManage.RemoveStack(this.houseName, taskParamModel.CellPos1, ref reStr);
                            this.asrsResManage.UpdateGSOper(this.houseName, taskParamModel.CellPos1, EnumGSOperate.无, ref reStr);

                            //增加出入库操作记录
                            EnumGSOperateType gsOPType = EnumGSOperateType.系统自动出库;
                            if (ctlTask.CreateMode == "手动")
                            {
                                gsOPType = EnumGSOperateType.手动出库;
                            }

                            this.asrsResManage.AddGSOperRecord(this.houseName, taskParamModel.CellPos1, gsOPType, "", ref reStr);

                            //货位2的处理
                            if (!this.asrsResManage.UpdateCellStatus(this.houseName, taskParamModel.CellPos2,
                               EnumCellStatus.满位,
                               EnumGSTaskStatus.完成,
                               ref reStr))
                            {
                                logRecorder.AddLog(new LogInterface.LogModel(nodeName, "更新货位状态失败：" + reStr, LogInterface.EnumLoglevel.错误));
                                return false;
                            }
                            this.asrsResManage.UpdateGSOper(this.houseName, taskParamModel.CellPos2, EnumGSOperate.无, ref reStr);
                            //增加出入库操作记录
                            this.asrsResManage.AddGSOperRecord(this.houseName, taskParamModel.CellPos2, EnumGSOperateType.入库, "", ref reStr);

                            string batchName = string.Empty;
                            //zwx,此处需要修改
                            //  CtlDBAccess.BLL.BatteryModuleBll batModuleBll = new CtlDBAccess.BLL.BatteryModuleBll();
                            if (taskParamModel.InputCellGoods != null && taskParamModel.InputCellGoods.Count() > 0)
                            {
                                string palletID = taskParamModel.InputCellGoods[0];
                                batchName = productOnlineBll.GetBatchNameofPallet(palletID);
                                // CtlDBAccess.Model.BatteryModuleModel batModule = batModuleBll.GetModel(taskParamModel.InputCellGoods[0]);
                                // batchName = batModule.batchName;
                            }

                            this.asrsResManage.RemoveStack(houseName, taskParamModel.CellPos1, ref reStr);
                            if (taskParamModel.InputCellGoods != null && taskParamModel.InputCellGoods.Count() > 0)
                            {
                                for (int i = 0; i < taskParamModel.InputCellGoods.Count();i++ )
                                {
                                     string logStr = string.Format("产品入库:{0},货位：{1}-{2}-{3}", houseName,taskParamModel.CellPos1.Row,taskParamModel.CellPos1.Col,taskParamModel.CellPos1.Layer);
                                    AddProduceRecord(taskParamModel.InputCellGoods[i], logStr);
                                }
                                if (!this.asrsResManage.AddStack(houseName, taskParamModel.CellPos2, batchName, taskParamModel.InputCellGoods, ref reStr))
                                {
                                    logRecorder.AddDebugLog(nodeName, string.Format("货位:{0}-{1}-{2}增加库存信息失败，{3}", taskParamModel.CellPos2.Row, taskParamModel.CellPos2.Col, taskParamModel.CellPos2.Layer, reStr));

                                }
                            }
                           
                            break;
                        }
                    default:
                        break;
                }
                ctlTask.FinishTime = System.DateTime.Now;
                ctlTask.TaskStatus = SysCfg.EnumTaskStatus.已完成.ToString();
                return ctlTaskBll.Update(ctlTask);
            }
            catch (Exception ex)
            {
                logRecorder.AddLog(new LogInterface.LogModel(nodeName, "任务完成处理异常，" + ex.ToString(), LogInterface.EnumLoglevel.错误));

                return false;
            }
        }

        /// <summary>
        /// 根据任务，获取绑定的出口
        /// </summary>
        /// <param name="taskType"></param>
        /// <returns></returns>
        private List<AsrsPortalModel> GetOutPortsOfBindedtask(SysCfg.EnumAsrsTaskType taskType)
        {
            List<AsrsPortalModel> validPorts = new List<AsrsPortalModel>();
            foreach(AsrsPortalModel port in ports)
            {
                if(port.BindedTaskOutput == taskType)
                {
                    validPorts.Add(port);
                }
            }
            if(this.houseName== EnumStoreHouse.A1库房.ToString()) //特殊处理，A1库产品空框混流出库
            {
                validPorts.Add(ports[1]);
            }
            return validPorts;
        }
        
        #endregion
    }
}
