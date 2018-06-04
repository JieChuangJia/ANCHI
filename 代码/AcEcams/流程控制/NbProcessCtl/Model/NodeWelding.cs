using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using PLProcessModel;
using DBAccess.Model;
using DBAccess.BLL;
namespace NbProcessCtl
{
    public class NodeWelding:CtlNodeBaseModel
    {
        private int machineSeq = 1;//焊机编号
        private BatteryModuleBll batModBll = new BatteryModuleBll();
        
        public override bool BuildCfg(System.Xml.Linq.XElement xe, ref string reStr)
        {
            if(!base.BuildCfg(xe, ref reStr))
            {
                return false;
            }
            XElement selfDataXE = xe.Element("SelfDatainfo");
            XElement machineXE = selfDataXE.Element("Mach");
            machineSeq=int.Parse(machineXE.Attribute("seq").Value.ToString());

            this.dicCommuDataDB1[1].DataDescription = "1：复位，2：记录完成,3:读卡失败,4:无绑定数据";
            this.dicCommuDataDB2[1].DataDescription = "1：无板，2：有板";
            for (int i = 0; i < 16; i++)
            {
                this.dicCommuDataDB2[2 + i].DataDescription = string.Format("RFID[{0}]", i + 1);
            }
            return true;
        }
        public override bool ExeBusiness(ref string reStr)
        {
            if(db2Vals[0]==1)
            {
                db1ValsToSnd[0] = 1;
                currentStat.Status = EnumNodeStatus.设备空闲;
            }
            else if(db2Vals[0] == 2)
            {
                currentStat.Status = EnumNodeStatus.设备使用中;
                currentStat.StatDescribe = "工作中";
                if(Db1ValsToSnd[0] !=2)
                {
                    string rfidUID = "";
                    
                    if(SysCfgModel.SimMode)
                    {
                        rfidUID = SimRfidUID;
                    }
                    else
                    {
                        byte[] rfidBytes = new byte[16];
                        for (int i = 0; i < 16; i++)
                        {
                            rfidBytes[i + 1] = (byte)db2Vals[i + 1];
                        }
                        rfidUID = System.Text.Encoding.UTF8.GetString(rfidBytes);
                    }
                    if(string.IsNullOrWhiteSpace(rfidUID))
                    {
                        db1ValsToSnd[0] = 3;
                        currentStat.Status = EnumNodeStatus.无法识别;
                        currentStat.StatDescribe = "RFID无效";
                        return true;
                    }
                    string strWhere = string.Format("palletID='{0}' ",rfidUID);
                    List<BatteryModuleModel> batModules = batModBll.GetModelList(strWhere);
                    if(batModules == null || batModules.Count()<1)
                    {
                        db1ValsToSnd[0] = 4;
                        currentStat.Status = EnumNodeStatus.设备故障;
                        currentStat.StatDescribe = "无绑定数据";
                        return true;
                    }
                    string modID = "";
                    bool weldOPTopCap = true;
                    if (machineSeq == 1 || machineSeq == 5)
                    {
                        modID = batModules[0].batModuleID;
                        weldOPTopCap = true;
                    }
                    else if(machineSeq == 2 || machineSeq == 6)
                    {
                        weldOPTopCap = true;
                        if(batModules.Count>1)
                        {
                            modID = batModules[1].batModuleID;
                        }
                    }
                    else if(machineSeq == 3 || machineSeq == 7)
                    {
                        modID = batModules[0].batModuleID;
                        weldOPTopCap = false;
                    }
                    else if(machineSeq == 4 || machineSeq== 8)
                    {
                        weldOPTopCap = false;
                        if (batModules.Count > 1)
                        {
                            modID = batModules[1].batModuleID;
                        }
                    }
                    else
                    {
                        db1ValsToSnd[0] = 3;
                        logRecorder.AddDebugLog(nodeName, "焊机ID错误，不可识别的ID，" + machineSeq.ToString());
                        return false;
                    }
                    string logInfo = string.Format("焊接模组，模组ID:{0},{1}", modID, weldOPTopCap? "上盖" : "下盖");
                    logRecorder.AddDebugLog(nodeName, logInfo);
                    BatteryModuleModel batMod = batModBll.GetModel(modID);
                    if (weldOPTopCap)
                    {
                        batMod.topcapWelderID = machineSeq;
                        AddProcessRecord(modID, EnumModProcessStage.模组焊接上盖.ToString());    
                    }
                    else
                    {
                        batMod.bottomcapWelderID = machineSeq;
                        AddProcessRecord(modID, EnumModProcessStage.模组焊接下盖.ToString());    
                    }
                    db1ValsToSnd[0] = 2;
                    logRecorder.AddDebugLog(nodeName, "焊接数据绑定完成");
                }
               
            }
            return true;
          
        }
    }
}
