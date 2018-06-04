using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlowCtlBaseModel;
namespace ProcessCtl
{
    //OCV测试
    public class NodeOcv : CtlNodeBaseModel
    {
        public override bool BuildCfg(System.Xml.Linq.XElement xe, ref string reStr)
        {
            if (!base.BuildCfg(xe, ref reStr))
            {
                return false;
            }
            //this.dicCommuDataDB1[1].DataDescription = "1:rfid复位，2：RFID成功，3：读RFID失败";
            //this.dicCommuDataDB1[2].DataDescription = "1:人工扫码复位，2：扫码绑定中，3:扫码结束，放行，4：模组数据未绑定";
            //this.dicCommuDataDB2[1].DataDescription = "1：无请求，2：RFID读取/扫码开始,3：只有一个模组";
            //this.dicCommuDataDB2[2].DataDescription = "1：无请求，2：只有一个模组";
            currentTaskPhase = 0;

            return true;
        }
        public override bool ExeBusiness(ref string reStr)
        {


            return true;
        }
    }
}
