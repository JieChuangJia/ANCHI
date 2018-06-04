using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.ServiceModel;
using System.ServiceModel.Description;
using AsrsModel;
using LogInterface;
using AsrsInterface;
using DevInterface;
using DevAccess;
using FlowCtlBaseModel;
using CtlDBAccess.BLL;
using CtlDBAccess.Model;
namespace AsrsControl
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class AsrsCtlPresenter:IAsrsCtlToManage
    {
        #region 数据
        public static bool SimMode = true;
        private string objectName = "立库控制";
       // private ThreadBaseModel asrsMonitorThread = null;
        protected ILogRecorder logRecorder = null;
        private IAsrsCtlView view = null;
        private List<AsrsCtlModel> asrsCtls = null;
        private List<AsrsPortalModel> asrsPorts = null;
        private IAsrsManageToCtl asrsResourceManage = null; //立库管理层接口对象
        private List<IPlcRW> plcRWs = null; //plc读写对象列表
        private List<IrfidRW> rfidRWs = null;//rfid读写对象列表
        private List<IBarcodeRW> barcodeRWList = null;
       // private BatteryModuleBll batModbll = null;
        #endregion  
        #region 公共接口
        public ILogRecorder LogRecorder { get { return logRecorder; } set { logRecorder = value; } }
        public List<AsrsCtlModel> AsrsCtls { get { return asrsCtls; } }
        public List<AsrsPortalModel> AsrsPorts { get { return asrsPorts; } }
        public List<IPlcRW> PlcRWs { get { return plcRWs; } set { plcRWs = value; } }
        public List<IrfidRW> RfidRWs { get { return rfidRWs; } set { rfidRWs = value; } }
        public List<IBarcodeRW> BarcodeRWs { get { return barcodeRWList; } set { barcodeRWList = value; } }
        public AsrsCtlPresenter(IAsrsCtlView view)
        {
            this.view = view;

        }
        public IPlcRW GetPlcByID(int plcID)
        {
            foreach (IPlcRW plcRW in plcRWs)
            {
                if (plcID == plcRW.PlcID)
                {
                    return plcRW;
                }
            }
            return null;
        }

        public IrfidRW GetRfidByID(int rfidID)
        {
            foreach(IrfidRW rfidRW in rfidRWs)
            {
                if(rfidID == rfidRW.ReaderID)
                {
                    return rfidRW;
                }
            }
            return null;
        }
        public IBarcodeRW GetBarcoderRWByID(int barcodReaderID)
        {
            foreach (IBarcodeRW barcodeReader in barcodeRWList)
            {
                if (barcodeReader != null && barcodeReader.ReaderID == barcodReaderID)
                {
                    return barcodeReader;
                }
            }
            return null;
        }
        public bool AsrsCommCfg()
        {
            if(this.plcRWs==null)
            {
                Console.WriteLine("立库通信配置失败，PLC通信列表对象为空");
                return false;
            }
            foreach(AsrsCtlModel asrsCtl in asrsCtls)
            {
                foreach(AsrsPortalModel port in asrsCtl.Ports)
                {
                    port.PlcRW = GetPlcByID(port.PlcID);
                    if(port.PortCata==1)
                    {
                        if(port.RfidID>0)
                        {
                            port.RfidRW = GetRfidByID(port.RfidID);
                        }
                        if (!SysCfg.SysCfgModel.SimMode)
                        {
                            //ctlNode.RfidRW = GetRfidByID((byte)ctlNode.RfidID);
                            if (port.BarcodeID > 0)
                            {
                                port.BarcodeRW = GetBarcoderRWByID(port.BarcodeID);
                            }

                        }
                    }
                }
                asrsCtl.StackDevice.PlcRW = GetPlcByID(asrsCtl.StackDevice.PlcID);
               
            }
            return true;
        }
        //public void SetAsrsPortPlcRW(int asrsIndex, IPlcRW plcRW)
        //{
        //    asrsCtls[asrsIndex].SetAsrsPortPlcRW(plcRW);

        //}
        //public void SetAsrsStackerPlcRW(int asrsIndex,IPlcRW plcRW)
        //{
        //    asrsCtls[asrsIndex].StackDevice.PlcRW = plcRW;
        //}
        public void SetLogRecorder(ILogRecorder logRecorder)
        {
            this.logRecorder = logRecorder;
            foreach(AsrsCtlModel asrs in asrsCtls)
            {
                asrs.SetLogrecorder(logRecorder);
            }
        }
        public void SetAsrsResManage(IAsrsManageToCtl asrsResManage)
        {
            this.asrsResourceManage = asrsResManage;
            foreach(AsrsCtlModel asrsModel in asrsCtls)
            {
                asrsModel.SetAsrsMangeInterafce(asrsResManage);
            }
        }
        public bool DevStatusRestore()
        {
            try
            {
                foreach(AsrsCtlModel asrs in asrsCtls)
                {
                    asrs.StackDevice.DevStatusRestore();
                    
                    foreach(AsrsPortalModel port in asrs.Ports)
                    {
                        port.DevStatusRestore();
                    }
                }
               
                return true;
            }
            catch (Exception ex)
            {
                //logRecorder.AddDebugLog(objectName, ex.ToString());
                Console.WriteLine(ex.ToString());
                return false;
            }
        }
        public bool CtlInit()
        {
          //  batModbll = new BatteryModuleBll();
            //1 通信设备初始化PLC,
         
            asrsCtls = new List<AsrsCtlModel>();
            this.asrsPorts = new List<AsrsPortalModel>();

            //2 控制节点初始化
            //解析配置文件
            string reStr = "";
            //string xmlCfgFile = System.AppDomain.CurrentDomain.BaseDirectory + @"data/AcCfg.xml";
            //if (!File.Exists(xmlCfgFile))
            //{
            //    reStr = "系统配置文件：" + xmlCfgFile + " 不存在!";
            //    logRecorder.AddLog(new LogModel(objectName, reStr, EnumLoglevel.错误));
            //    return false;
            //}
            XElement root = null;
            if (!SysCfg.SysCfgModel.LoadCfg(ref root, ref reStr))
            {
                Console.WriteLine("系统配置解析错误,{0}", reStr);
                return false;
            }

          //  XElement root = XElement.Load(xmlCfgFile);
            XElement CtlnodeRoot = root.Element("AsrsNodes");
            if (!ParseCtlnodes(CtlnodeRoot, ref reStr))
            {
                Console.WriteLine(reStr);
                return false;
            }
            //3 分配通信对象
            foreach(AsrsCtlModel asrs in asrsCtls)
            {
                if(!asrs.Init())
                {
                    logRecorder.AddLog(new LogModel(objectName, asrs.NodeName + "初始化错误", EnumLoglevel.错误));
                    return false;
                }

                this.asrsPorts.AddRange(asrs.Ports.ToArray());
                asrs.SetLogrecorder(logRecorder);
                if (asrs.HouseName == EnumStoreHouse.B1库房.ToString())
                {
                    asrs.FillTaskTyps(new List<SysCfg.EnumAsrsTaskType> { SysCfg.EnumAsrsTaskType.产品入库,
                        SysCfg.EnumAsrsTaskType.产品出库, 
                        SysCfg.EnumAsrsTaskType.移库 });
                }
                else
                {
                    asrs.FillTaskTyps(new List<SysCfg.EnumAsrsTaskType> { SysCfg.EnumAsrsTaskType.产品入库, 
                        SysCfg.EnumAsrsTaskType.产品出库,
                        SysCfg.EnumAsrsTaskType.移库,
                        SysCfg.EnumAsrsTaskType.空框入库,
                        SysCfg.EnumAsrsTaskType.空框出库});
                   
                }
            }

            return true;
        }
        public bool StartRun()
        {
           // string reStr="";
            foreach (AsrsCtlModel asrsCtlObj in asrsCtls)
            {
                if (!asrsCtlObj.StartRun())
                {
                    return false;
                }
            }
          //  asrsMonitorThread.TaskStart(ref reStr);
            view.StartMonitor();
            return true;
        }
        public bool PauseRun()
        {
            foreach (AsrsCtlModel asrsCtlObj in asrsCtls)
            {
                asrsCtlObj.PauseRun();
            }
       //     asrsMonitorThread.TaskPause();
            view.StopMonitor();
            return true;

        }
        public bool ExitRun()
        {
            //string reStr = "";
            foreach (AsrsCtlModel asrsCtlObj in asrsCtls)
            {
                asrsCtlObj.ExitRun();
            }
         //   asrsMonitorThread.TaskExit(ref reStr);
            return true;
        }
        public List<string> GetLogsrcList()
        {
             List<string> logSrcList = new List<string>();
             foreach(AsrsCtlModel asrsCtl in this.asrsCtls)
             {
                 logSrcList.Add(asrsCtl.NodeName);
                 logSrcList.Add(asrsCtl.StackDevice.NodeName);

             }
             logSrcList.Add(objectName);
             return logSrcList;
        }
        #endregion
        #region IAsrsCtlToManage接口实现
        public bool CreateManualOutputTask(string houseName, CellCoordModel cell, ref string reStr)
        {
          
            AsrsCtlModel asrsModel = null;
            foreach(AsrsCtlModel m in asrsCtls)
            {
                if(m.HouseName == houseName)
                {
                    asrsModel = m;
                    break;
                }
            }
            if(asrsModel == null)
            {
                reStr = "未识别的： " + houseName;
                return false;
            }

            SysCfg.EnumAsrsTaskType taskType = SysCfg.EnumAsrsTaskType.产品出库;
            EnumCellStatus cellStoreStats= EnumCellStatus.空闲;
            EnumGSTaskStatus cellTaskStatus = EnumGSTaskStatus.完成;
            asrsResourceManage.GetCellStatus(houseName,cell,ref cellStoreStats,ref cellTaskStatus);
            EnumGSEnabledStatus cellEnabledStatus = EnumGSEnabledStatus.启用;
            if(!asrsResourceManage.GetCellEnabledStatus(houseName, cell, ref cellEnabledStatus))
            {
                reStr = string.Format("获取货位启用状态失败{0}-{1}-{2}", cell.Row, cell.Col, cell.Layer); 
                return false;
            }
            if(cellEnabledStatus== EnumGSEnabledStatus.禁用)
            {
                reStr = string.Format("货位{0}-{1}-{2}禁用,无法生成出库任务",cell.Row,cell.Col,cell.Layer);
                return false;
            }
            if(cellTaskStatus == EnumGSTaskStatus.锁定)
            {
                reStr="货位处于锁定状态";
                return false;
            }
            
            if(cellStoreStats == EnumCellStatus.空料框)
            {
                taskType = SysCfg.EnumAsrsTaskType.空框出库;
            }

            asrsModel.GenerateOutputTask(cell, null,taskType, false);

            return true;
        }
        public bool CreateManualMoveGSTask(string startHouseName, CellCoordModel startCell, string endHouseName, CellCoordModel endCell, ref string reStr)
        {
            //throw new NotImplementedException();
            string houseName = startHouseName;
            AsrsCtlModel asrsModel = null;
            foreach (AsrsCtlModel m in asrsCtls)
            {
                if (m.HouseName == houseName)
                {
                    asrsModel = m;
                    break;
                }
            }
            if (asrsModel == null)
            {
                reStr = "未识别的： " + houseName;
                return false;
            }
            EnumGSEnabledStatus cellEnabledStatus = EnumGSEnabledStatus.启用;
            if (!asrsResourceManage.GetCellEnabledStatus(houseName, startCell, ref cellEnabledStatus))
            {
                reStr = string.Format("获取货位启用状态失败{0}-{1}-{2}", startCell.Row, startCell.Col, startCell.Layer); 
                return false;
            }
            if (cellEnabledStatus == EnumGSEnabledStatus.禁用)
            {
                reStr = string.Format("货位{0}-{1}-{2}禁用,无法生成移库任务", startCell.Row, startCell.Col, startCell.Layer);
                return false;
            }

            if (!asrsResourceManage.GetCellEnabledStatus(houseName,endCell, ref cellEnabledStatus))
            {
                reStr = string.Format("获取货位启用状态失败{0}-{1}-{2}", endCell.Row, endCell.Col, endCell.Layer);
                return false;
            }
            if (cellEnabledStatus == EnumGSEnabledStatus.禁用)
            {
                reStr = string.Format("货位{0}-{1}-{2}禁用,无法生成移库任务", endCell.Row, endCell.Col, endCell.Layer);
                return false;
            }
            EnumCellStatus cellStoreStats = EnumCellStatus.空闲;
            EnumGSTaskStatus cellTaskStatus = EnumGSTaskStatus.完成;
            asrsResourceManage.GetCellStatus(houseName, startCell, ref cellStoreStats, ref cellTaskStatus);
            if (cellTaskStatus == EnumGSTaskStatus.锁定)
            {
                reStr = string.Format("货位{0}-{1}-{2}处于锁定状态,无法生成移库任务", startCell.Row, startCell.Col, startCell.Layer); ;
                return false;
            }
            asrsResourceManage.GetCellStatus(houseName, endCell, ref cellStoreStats, ref cellTaskStatus);
            if (cellTaskStatus == EnumGSTaskStatus.锁定)
            {
                reStr = string.Format("目标货位{0}-{1}-{2}处于锁定状态,无法生成移库任务", endCell.Row, endCell.Col, endCell.Layer); ;
                return false;
            }
            if(cellStoreStats != EnumCellStatus.空闲)
            {
                reStr = string.Format("目标货位{0}-{1}-{2}不为空,无法生成移库任务", endCell.Row, endCell.Col, endCell.Layer); ;
                return false;
            }
            SysCfg.EnumAsrsTaskType taskType = SysCfg.EnumAsrsTaskType.移库;
            asrsModel.GenerateOutputTask(startCell,endCell,taskType, false);
            return true;
        }
        #endregion
        #region 私有接口
        private bool ParseCtlnodes(XElement CtlnodeRoot, ref string reStr)
        {
            if (CtlnodeRoot == null)
            {
                reStr = "系统配置文件错误，不存在CtlNodes节点";
                return false;
            }
            try
            {
                
                IEnumerable<XElement> nodeXEList =CtlnodeRoot.Elements("Asrs");
                //from el in CtlnodeRoot.Elements()
                //where el.Name == "Asrs"
                //select el;
                foreach (XElement el in nodeXEList)
                {
                    AsrsCtlModel asrsModel = new AsrsCtlModel();
                    if(!asrsModel.BuildCfg(el, ref reStr))
                    {
                       // logRecorder.AddLog(new LogModel(objectName, reStr, EnumLoglevel.错误));
                        Console.WriteLine(reStr);
                        return false;
                    }
                    asrsModel.AsrsCheckoutMode = EnumAsrsCheckoutMode.计时出库;
                    this.asrsCtls.Add(asrsModel);
                }
                return true;
            }
            catch (Exception ex)
            {
                reStr = ex.ToString();
                return false;
            }
        }
       
       
        /// <summary>
        /// 空框出库请求处理
        /// </summary>
        private void EmptyPalletOutputRequire()
        {
            AsrsCtlModel asrsHouse = asrsCtls[0];
            AsrsPortalModel emptyPalletOutport = asrsHouse.GetPortByDeviceID("2003");
            if (emptyPalletOutport.Db2Vals[1] == 1)
            {
                //出口有框，禁止出库
                return;
            }
            if(emptyPalletOutport.Db1ValsToSnd[0]==2)
            {
                return;
            }
            if(emptyPalletOutport.Db2Vals[0] != 3)
            {
                return;
            }
            bool exitFlag = false;
            int row = 2, col = 21, layer = 6; //要查询得到

            int r = 1, c = 1, L = 1;
            for (r = 1; r < row + 1; r++)
            {
                if (exitFlag)
                {
                    break;
                }
                for (c = 1; c < col + 1; c++)
                {
                    if (exitFlag)
                    {
                        break;
                    }
                    for (L = 1; L < layer + 1; L++)
                    {
                        CellCoordModel cell = new CellCoordModel(r, c, L);
                        EnumCellStatus cellStoreStat = EnumCellStatus.空闲;
                        EnumGSTaskStatus cellTaskStat = EnumGSTaskStatus.完成;
                        if(!this.asrsResourceManage.GetCellStatus(asrsHouse.HouseName, cell, ref cellStoreStat, ref cellTaskStat))
                        {
                            continue;
                        }
                        if (cellStoreStat == EnumCellStatus.空料框 && cellTaskStat != EnumGSTaskStatus.锁定)
                        {
                            if(asrsHouse.GenerateOutputTask(cell, null,SysCfg.EnumAsrsTaskType.空框出库, true))
                            {
                                exitFlag = true;
                                emptyPalletOutport.Db1ValsToSnd[0] = 2;
                                string reStr = "";
                                if(!emptyPalletOutport.NodeCmdCommit(true, ref reStr))
                                {
                                    logRecorder.AddDebugLog(emptyPalletOutport.NodeName, "发送命令失败" + reStr);
                                }
                                else
                                {
                                    return;
                                }
                                break;
                            }
                            
                        }
                    }
                }
            }
        }
        #endregion
        #region 充电系统远程服务委托方法
        private bool IsAsrsCellReady(int row, int col, int layer, ref string[] barCodes, ref bool isReady, ref string reStr)
        {

           // Console.WriteLine("收到远程调用,IsAsrsCellReady");
            try
            {
                CellCoordModel cell = new CellCoordModel(row, col, layer);
                EnumCellStatus cellStoreStat = EnumCellStatus.空闲;
                EnumGSTaskStatus cellTaskStat = EnumGSTaskStatus.完成;
                if (!this.asrsResourceManage.GetCellStatus(asrsCtls[1].HouseName, cell, ref cellStoreStat, ref cellTaskStat))
                {
                    reStr = string.Format("货位不存在：{0}-{1}-{2}", row, col, layer);
                    return false;
                }
                if (cellStoreStat == EnumCellStatus.满位 && cellTaskStat == EnumGSTaskStatus.完成)
                {
                    List<string> storBarcodes=new List<string>();
                    this.asrsResourceManage.GetStockDetail(asrsCtls[1].HouseName, cell, ref storBarcodes);
                    if(storBarcodes.Count()<1)
                    {
                        isReady = false;
                        reStr = "货位没有产品";

                    }
                    else
                    {
                        if (storBarcodes.Count() < 2)
                        {
                            storBarcodes.Add("");
                        }
                        //storBarcodes[1] = string.Empty;
                        barCodes = storBarcodes.ToArray();
                        isReady = true;
                    }
                    
                }
                else
                {
                    reStr = "货位没有产品";
                    isReady = false;
                }
                return true;
            }
            catch (Exception ex)
            {
                reStr = ex.ToString();
                return false; 
            }
        }
        private bool PowerFillOk(int row, int col, int layer, ref string reStr)
        {
            try
            {
                CellCoordModel cell = new CellCoordModel(row, col, layer);
                EnumCellStatus cellStoreStat = EnumCellStatus.空闲;
                EnumGSTaskStatus cellTaskStat = EnumGSTaskStatus.完成;
                if (!this.asrsResourceManage.GetCellStatus(asrsCtls[1].HouseName, cell, ref cellStoreStat, ref cellTaskStat))
                {
                    reStr = string.Format("货位不存在：{0},{1}-{2}-{3}", asrsCtls[1].HouseName,row, col, layer);
                  //  logRecorder.AddDebugLog(objectName, "充电完成事件错误,"+reStr);
                    return false;
                }
                EnumGSEnabledStatus cellEnabledStatus = EnumGSEnabledStatus.禁用;
                if (!this.asrsResourceManage.GetCellEnabledStatus(asrsCtls[1].HouseName, cell, ref cellEnabledStatus))
                {
                    
                    reStr = string.Format("货位禁用：{0},{1}-{2}-{3}", asrsCtls[1].HouseName, row, col, layer);
                    //logRecorder.AddDebugLog(objectName, "充电完成事件错误," + reStr);
                    return false;
                }
                if (cellEnabledStatus == EnumGSEnabledStatus.禁用)
                {
                    reStr = string.Format("货位禁用：{0},{1}-{2}-{3}", asrsCtls[1].HouseName, row, col, layer);
                    //logRecorder.AddDebugLog(objectName, "充电完成事件错误," + reStr);
                    return false;
                }
                if (cellStoreStat != EnumCellStatus.满位)
                {
                    reStr = string.Format("货位为空：{0},{1}-{2}-{3},", asrsCtls[1].HouseName, row, col, layer) + reStr;
                    //logRecorder.AddDebugLog(objectName, "充电完成事件错误," + reStr);
                    return false;
                }
                 if (cellTaskStat == EnumGSTaskStatus.出库允许)
                 {
                     return true;
                 }
                if (cellTaskStat == EnumGSTaskStatus.锁定)
                {
                    reStr = string.Format("货位任务锁定：{0},{1}-{2}-{3},", asrsCtls[1].HouseName, row, col, layer) + reStr;
                    logRecorder.AddDebugLog(objectName, "充电完成事件错误," + reStr);
                    return false;
                }

                cellTaskStat = EnumGSTaskStatus.出库允许;
                if(!this.asrsResourceManage.UpdateCellStatus(asrsCtls[1].HouseName, cell, cellStoreStat, cellTaskStat, ref reStr))
                {
                    reStr=string.Format("更新货位状态失败：{0},{1}-{2}-{3},", asrsCtls[1].HouseName, row, col, layer)+reStr;
                    logRecorder.AddDebugLog(objectName, "充电完成事件错误," + reStr);
                    return false;
                }
               
                return true;

            }
            catch (Exception ex)
            {
                reStr = ex.ToString();
                return false;

            }
        }
        private bool CommitCheckResult(string barcode, int checkRe, ref string reStr)
        {
            throw new NotImplementedException();
            //BatteryModuleModel batMod =  batModbll.GetModel(barcode);
            //if(batMod == null)
            //{
            //    reStr = "没有该模组信息：" + barcode;
            //    return false;
            //}
            //batMod.checkResult = checkRe;
            //batModbll.Update(batMod);
            return true;
        }
        private bool CellValidStatNotify(int row, int col, int layer, bool cellValid, string reason, ref string reStr)
        {
            
            CellCoordModel cell = new CellCoordModel(row, col, layer);
            EnumGSEnabledStatus enableStatus = EnumGSEnabledStatus.禁用;
            if(cellValid)
            {
                enableStatus = EnumGSEnabledStatus.启用;
            }
            //zwx,此处需要修改
            return asrsResourceManage.UpdateGsEnabledStatus(EnumStoreHouse.B1库房.ToString(), cell, enableStatus, ref reStr);
            
            
        }
        private  bool CellEmerDangerNotify(int row, int col, int layer, string reason, ref string reStr)
        {
           logRecorder.AddDebugLog(this.objectName,string.Format("B库房货位：{0}-{1}-{2}充电故障，需要紧急出库",row,col,layer));
           if(!asrsCtls[1].GenerateEmerOutputTask(new CellCoordModel(row,col,layer),SysCfg.EnumAsrsTaskType.产品出库,true,1,ref reStr))
           {
               return false;
           }
            return true;
        }
        #endregion
    }
}
