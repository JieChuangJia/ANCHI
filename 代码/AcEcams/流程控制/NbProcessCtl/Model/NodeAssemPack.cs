using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PLProcessModel;
using DBAccess.Model;
using DBAccess.BLL;
namespace NbProcessCtl
{
    public class NodeAssemPack:CtlNodeBaseModel
    {
        private BatteryPackBll batPackBll = new BatteryPackBll();
        private BatteryModuleBll batModBll = new BatteryModuleBll();
        public override bool ExeBusiness(ref string reStr)
        {
            if (SysCfgModel.SimMode)
            {
                if (this.nodeID != "5001")
                {
                    return true;
                }
                string packID = "PACK00001";
                string packOPWorkerID = "W22345";
                string[] modIDs = new string[] { "MODT0001", "MODT0002" };
                if(batPackBll.Exists(packID))
                {
                    return true;
                }
                BatteryPackModel pack = new BatteryPackModel();
                pack.batPackID = packID;
                pack.opWorkerID = packOPWorkerID;
                pack.packAsmTime = DateTime.Parse(System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                batPackBll.Add(pack);
                foreach(string modID in modIDs)
                {
                    BatteryModuleModel batMod = batModBll.GetModel(modID);
                    if(batMod == null)
                    {
                        continue;
                    }
                    batMod.batPackID = packID;
                    batMod.curProcessStage = EnumModProcessStage.模组装配到PACK.ToString();
                    batModBll.Update(batMod);
                    AddProcessRecord(modID, EnumModProcessStage.模组装配到PACK.ToString());
                }
            }

            return true;
        }
    }
}
