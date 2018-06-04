using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PLProcessModel;
using DBAccess.Model;
using DBAccess.BLL;
namespace NbProcessCtl
{
    public class NodeAssemBottom:CtlNodeBaseModel
    {
        private string workerID = ""; //人工码
        private string modID = ""; //模组码
        BatteryModuleBll batModBll = new BatteryModuleBll();
        public override bool ExeBusiness(ref string reStr)
        {
            if (SysCfgModel.SimMode)
            {
                if (this.nodeID != "7001")
                {
                    return true;
                }
                string opWorkerID = "W12346";
                string testModID = "MODT0001";
                BatteryModuleModel batMod = batModBll.GetModel(testModID);
                batMod.downcapOPWorkerID = opWorkerID;
                batModBll.Update(batMod);
            }
            return true;
        }
    }
}
