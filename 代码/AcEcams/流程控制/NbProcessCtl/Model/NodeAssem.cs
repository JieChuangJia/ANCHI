using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PLProcessModel;
using DBAccess.Model;
using DBAccess.BLL;
namespace NbProcessCtl
{
    public class NodeAssem:CtlNodeBaseModel
    {
        BatteryBll batteryBll = new BatteryBll();
        BatteryModuleBll batModBll = new BatteryModuleBll();
     //   ModPsRecordBll modPsRecordBll = new ModPsRecordBll();
        Tb_CheckDataBll tbBatteryDataBll = new Tb_CheckDataBll(); //分选机电池数据
        private string workerID = ""; //人工码
        private string modID = ""; //模组码
        private string batteryID = "";//电池条码

        public override bool BuildCfg(System.Xml.Linq.XElement xe, ref string reStr)
        {
            if (!base.BuildCfg(xe, ref reStr))
            {
                return false;
            }
            this.dicCommuDataDB1[1].DataDescription = "1：待机状态/复位，2：开始扫码中，3：扫码完成（3秒）";
         
            currentTaskPhase = 0;

            return true;
        }
        public override bool ExeBusiness(ref string reStr)
        {
            if(SysCfgModel.SimMode)
            {
                if(this.nodeID != "5001")
                {
                    return true;
                }
                //test
                string[] testBatcodes = new string[] { "bat0001", "bat0004" };
                string[] testModcodes = new string[] { "MODT0001", "MODT0002" };
                string opWorkerID = "W12345";
                for (int i = 0; i < 2; i++)
                {
                    string modBarcode = testModcodes[i];
                    
                    string batBarcode = testBatcodes[i];
                    //检查模组是否已经存在
                    if(batModBll.Exists(modBarcode))
                    {
                        continue;
                    }
                    //绑定
                    //1 先检索所有电池
                    Tb_CheckDataModel tbBatModel = tbBatteryDataBll.GetModel(batBarcode);
                    string strWhere = string.Format("tf_Group='{0}'", tbBatModel.tf_Group);
                    List<Tb_CheckDataModel> batterys = tbBatteryDataBll.GetModelList(strWhere);

                    BatteryModuleModel batModule = new BatteryModuleModel();
                    batModule.asmTime = System.DateTime.Now;
                    batModule.batModuleID = modBarcode;
                    batModule.curProcessStage = EnumModProcessStage.模组装配下盖.ToString();
                    batModule.topcapOPWorkerID = opWorkerID;
                    batModule.palletBinded = false;
                    batModBll.Add(batModule);

                    //
                    foreach(Tb_CheckDataModel tbBattery in batterys)
                    {
                        BatteryModel batteryModel = new BatteryModel();
                        batteryModel.batteryID = tbBattery.BarCode;
                        batteryModel.batModuleID = modBarcode;
                        batteryModel.batModuleAsmTime = System.DateTime.Now;
                        batteryBll.Add(batteryModel);
                    }
                    //添加生产过程记录
                    AddProcessRecord(modBarcode, EnumModProcessStage.模组装配下盖.ToString());
                    //ModPsRecordModel modRecord = new ModPsRecordModel();
                    //modRecord.RecordID = System.Guid.NewGuid().ToString();
                    //modRecord.processRecord = EnumModProcessStage.模组装配.ToString();
                    //modRecord.batModuleID = modBarcode;
                    //modRecord.recordTime = System.DateTime.Now;
                    //modPsRecordBll.Add(modRecord);
                }
            }
            else
            {
               // string startFlagStr = "START";
                return ExeBusinessBottom(ref reStr);
            }
           
            return true;
        }
        private bool ExeBusinessUpper(ref string reStr)
        {
            List<string> recvBarcodesBuf = barcodeRW.GetBarcodesBuf();

            if (recvBarcodesBuf.Contains("START") || recvBarcodesBuf.Contains("start"))
            {
                this.currentTaskPhase = 1;
                logRecorder.AddDebugLog(nodeName, "流程开始");
                this.db1ValsToSnd[0] = 2; //开始扫码
            }

            if (this.currentTaskPhase < 1)
            {

                //待机状态
                this.db1ValsToSnd[0] = 1;
                return true;
            }
            if (this.currentTaskPhase == 3)
            {
                System.Threading.Thread.Sleep(3000);
                this.db1ValsToSnd[0] = 1;
                this.currentTaskPhase = 0;
            }
            switch (this.currentTaskPhase)
            {
                case 1:
                    {
                        //给PLC提示，开始扫码
                        barcodeRW.ClearBarcodesBuf();
                        this.workerID = string.Empty;
                        this.modID = string.Empty;
                        this.batteryID = string.Empty;

                        this.currentTaskPhase++;
                        currentTaskDescribe = "开始，等待扫码、模组绑定";
                        break;
                    }
                case 2:
                    {
                        recvBarcodesBuf = barcodeRW.GetBarcodesBuf();
                        for (int i = 0; i < recvBarcodesBuf.Count(); i++)
                        {
                            //Console.WriteLine(nodeName + "，扫码：" + recvBarcodesBuf[i]);
                            if (recvBarcodesBuf[i].Length <= 0)
                            {
                                continue;
                            }
                            if (recvBarcodesBuf[i].Substring(0, 1).ToUpper() == "M" && recvBarcodesBuf[i].Length > 15)
                            {
                                //模组码
                                if (string.IsNullOrWhiteSpace(modID))
                                {
                                    Console.WriteLine(string.Format("{0},扫到模组:{1}", nodeName, recvBarcodesBuf[i]));
                                }
                                this.modID = recvBarcodesBuf[i];
                            }
                            else if (recvBarcodesBuf[i].Substring(0, 2).ToUpper() == "NB")
                            {
                                if (string.IsNullOrWhiteSpace(workerID))
                                {
                                    Console.WriteLine(string.Format("{0},扫到员工码:{1}", nodeName, recvBarcodesBuf[i]));
                                }
                                this.workerID = recvBarcodesBuf[i];
                            }
                            else
                            {
                                Console.WriteLine(string.Format("{0},不可识别的条码:{1}", nodeName, recvBarcodesBuf[i]));
                            }
                        }
                        if (string.IsNullOrEmpty(this.workerID))
                        {
                            break;
                        }
                        if (string.IsNullOrEmpty(this.modID))
                        {
                            break;
                        }
                        //检查模组是否已经存在
                        BatteryModuleModel batModule = batModBll.GetModel(this.modID);
                        if (batModule == null)
                        {
                            currentTaskDescribe = string.Format("模组{0}，不存在，", this.modID);
                            break;
                        }
                        batModule.topcapOPWorkerID = this.workerID;
                        batModule.curProcessStage = EnumModProcessStage.模组焊接上盖.ToString();
                        batModBll.Update(batModule);
                        this.db1ValsToSnd[0] = 3; //绑定完成
                        currentTaskDescribe = string.Format("模组{0}员工号绑定完成", this.modID);
                        //添加生产过程记录
                        AddProcessRecord(this.modID, EnumModProcessStage.模组焊接上盖.ToString());
                        this.currentTaskPhase++;
                        break;
                    }
                case 3:
                    {
                        barcodeRW.ClearBarcodesBuf();
                        break;
                    }
            } 
            return true;
        }
        private bool ExeBusinessBottom(ref string reStr)
        {
            List<string> recvBarcodesBuf = barcodeRW.GetBarcodesBuf();
            
            if (recvBarcodesBuf.Contains("START") || recvBarcodesBuf.Contains("start"))
            {
                this.currentTaskPhase = 1;
                logRecorder.AddDebugLog(nodeName, "流程开始");
                this.db1ValsToSnd[0] = 2; //开始扫码
            }
            if (this.currentTaskPhase < 1)
            {
                //待机状态
                this.db1ValsToSnd[0] = 1;
                return true;
            }
            if (this.currentTaskPhase == 3)
            {
                System.Threading.Thread.Sleep(3000);
                this.db1ValsToSnd[0] = 1;
                this.currentTaskPhase = 0;
            }
            switch (this.currentTaskPhase)
            {
                case 1:
                    {
                        //给PLC提示，开始扫码
                        barcodeRW.ClearBarcodesBuf();
                        this.workerID = string.Empty;
                        this.modID = string.Empty;
                        this.batteryID = string.Empty;

                        this.currentTaskPhase++;
                        currentTaskDescribe = "开始，等待扫码、模组绑定";
                        break;
                    }
                case 2:
                    {
                        recvBarcodesBuf = barcodeRW.GetBarcodesBuf();

                        for (int i = 0; i < recvBarcodesBuf.Count(); i++)
                        {
                            //Console.WriteLine(nodeName + "，扫码：" + recvBarcodesBuf[i]);
                            if(recvBarcodesBuf[i].Length <= 0)
                            {
                                continue;
                            }
                            if (recvBarcodesBuf[i].Substring(0, 1).ToUpper() == "M" && recvBarcodesBuf[i].Length > 15)
                            {
                                //模组码
                                if (string.IsNullOrWhiteSpace(modID))
                                {
                                    Console.WriteLine(string.Format("{0},扫到模组:{1}", nodeName, recvBarcodesBuf[i]));
                                }

                                this.modID = recvBarcodesBuf[i];

                            }
                            else if (recvBarcodesBuf[i].Substring(0, 2).ToUpper() == "NB")
                            {
                                if (string.IsNullOrWhiteSpace(workerID))
                                {
                                    Console.WriteLine(string.Format("{0},扫到员工码:{1}", nodeName, recvBarcodesBuf[i]));
                                }
                                this.workerID = recvBarcodesBuf[i];
                            }
                            else
                            {
                                //电池条码
                                if (string.IsNullOrWhiteSpace(batteryID))
                                {
                                    Console.WriteLine(string.Format("{0},扫到电池码:{1}", nodeName, recvBarcodesBuf[i]));
                                }
                                this.batteryID = recvBarcodesBuf[i];
                            }
                        }
                        if (string.IsNullOrEmpty(this.workerID))
                        {
                            break;
                        }
                        if (string.IsNullOrEmpty(this.modID))
                        {
                            break;
                        }
                        if (string.IsNullOrEmpty(this.batteryID))
                        {
                            break;
                        }
                        //检查模组是否已经存在
                        if (batModBll.Exists(this.modID))
                        {
                            currentTaskDescribe = string.Format("已经存在模组{0}，绑定完成", this.modID);
                            this.db1ValsToSnd[0] = 3; //绑定完成
                            this.currentTaskPhase++;
                            break;
                        }
                        else
                        {
                            //1 先检索所有电池
                            Tb_CheckDataModel tbBatModel = tbBatteryDataBll.GetModel(this.batteryID);
                            if (tbBatModel == null)
                            {
                                Console.WriteLine(string.Format("不存在的电池条码：{0}", this.batteryID));
                                return false;
                            }
                            string strWhere = string.Format("tf_Group='{0}'", tbBatModel.tf_Group);
                            List<Tb_CheckDataModel> batterys = tbBatteryDataBll.GetModelList(strWhere);

                            BatteryModuleModel batModule = new BatteryModuleModel();
                            batModule.batchName = tbBatModel.FileName;//批次
                            batModule.asmTime = System.DateTime.Now;
                            batModule.batModuleID = this.modID;
                            batModule.curProcessStage = EnumModProcessStage.模组焊接下盖.ToString();
                            batModule.downcapOPWorkerID = this.workerID;
                            batModule.palletBinded = false;
                            batModBll.Add(batModule);

                            //
                            foreach (Tb_CheckDataModel tbBattery in batterys)
                            {
                                BatteryModel batteryModel = new BatteryModel();
                                batteryModel.batteryID = tbBattery.BarCode;
                                batteryModel.batModuleID = this.modID;
                                batteryModel.batModuleAsmTime = System.DateTime.Now;
                                batteryBll.Add(batteryModel);
                            }
                            //添加生产过程记录
                            AddProcessRecord(this.modID, EnumModProcessStage.模组焊接下盖.ToString());
                        }
                        this.db1ValsToSnd[0] = 3; //绑定完成
                        currentTaskDescribe = string.Format("模组{0},绑定完成", this.modID);
                        this.currentTaskPhase++;
                        break;
                    }
                case 3:
                    {
                        barcodeRW.ClearBarcodesBuf();

                        break;
                    }
                default:
                    break;
            }
            return true;
        }
    }
}
