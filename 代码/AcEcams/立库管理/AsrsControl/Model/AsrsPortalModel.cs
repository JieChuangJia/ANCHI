using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using FlowCtlBaseModel;
using DevInterface;
namespace AsrsControl
{
    /// <summary>
    /// 立库出入口控制模型
    /// </summary>
    public class AsrsPortalModel : CtlNodeBaseModel
    {
      //  private IrfidRW rfidRW = null;
      //  public IrfidRW RfidRW { get { return rfidRW; } set { rfidRW = value; } }
        private List<string> palletBuffer = new List<string>();
      //  private string palletWaitting = ""; //在读卡位置待处理的托盘号
        private int portCata = 1; //1：入口，2：出口，3：出入口共用
        private int portSeq = 1;//编号，从1开始
        private SysCfg.EnumAsrsTaskType bindedTaskInput = SysCfg.EnumAsrsTaskType.空;
        private SysCfg.EnumAsrsTaskType bindedTaskOutput = SysCfg.EnumAsrsTaskType.空;
     //   private bool inputPort = true;
    //    public bool InputPort { get { return inputPort; } set { inputPort = value; } }
        private AsrsCtlModel asrsCtlModel = null;
        public int PortCata { get { return portCata; } set { portCata = value; } }
        public int PortSeq { get { return portSeq; } }
        /// <summary>
        /// 入口缓存队列
        /// </summary>
        public List<string> PalletBuffer { get { return palletBuffer; } set { palletBuffer = value; } }
       // public string PalletWaiting { get { return palletWaitting; } set { palletWaitting = value; } }
        public SysCfg.EnumAsrsTaskType BindedTaskInput { get { return bindedTaskInput; } set { bindedTaskInput = value; } }
        public SysCfg.EnumAsrsTaskType BindedTaskOutput { get { return bindedTaskOutput; } set { bindedTaskOutput = value; } }
        public AsrsCtlModel AsrsCtl { get { return asrsCtlModel; } }
        /// <summary>
        /// 入口缓存托盘最大数量
        /// </summary>
        public int PortinBufCapacity { get; set; }
        public AsrsPortalModel(AsrsCtlModel asrsCtl)
        {
            PortinBufCapacity = 1; //默认最大容量是1
            this.asrsCtlModel = asrsCtl;
        }
        public override bool ExeBusiness(ref string reStr)
        {
            return true ;
        }
        public override bool DevStatusRestore()
        {
            //if(!base.DevStatusRestore())
            //{
            //    return false;
            //}
            MesDBAccess.BLL.AsrsPortBufferBll bufBll = new MesDBAccess.BLL.AsrsPortBufferBll();
            MesDBAccess.Model.AsrsPortBufferModel buf = bufBll.GetModel(this.nodeID);
            this.palletBuffer = new List<string>();
            if(buf != null)
            {
                if(!string.IsNullOrWhiteSpace(buf.palletBuffers))
                {
                    string[] strArray = buf.palletBuffers.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                    if(strArray != null && strArray.Count()>0)
                    {
                        this.palletBuffer.AddRange(strArray);
                    }
                }
                
            }
            return true;
        }
        public override bool BuildCfg(System.Xml.Linq.XElement xe, ref string reStr)
        {
            if(!base.BuildCfg(xe, ref reStr))
            {
                return false;
            }
            XElement selfDataXE = xe.Element("SelfDatainfo");
            this.portCata = int.Parse(selfDataXE.Element("PortType").Value.ToString());
            
            
            if (selfDataXE.Attribute("portSeq")!=null)
            {
                this.portSeq = int.Parse(selfDataXE.Attribute("portSeq").Value.ToString());
            }
            if (selfDataXE.Attribute("capacity")!= null)
            {
                this.PortinBufCapacity = int.Parse(selfDataXE.Attribute("capacity").Value.ToString());
            }
            if (selfDataXE.Attribute("bindedTask") !=null)
            {
                string[] strArray=selfDataXE.Attribute("bindedTask").Value.ToString().Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                if(strArray != null)
                {
                    if(strArray.Count()>0)
                    {
                        if(this.portCata == 1)
                        {
                            this.bindedTaskInput = (SysCfg.EnumAsrsTaskType)Enum.Parse(typeof(SysCfg.EnumAsrsTaskType), strArray[0]);
                        }
                        else if(this.portCata == 2)
                        {
                            this.bindedTaskOutput = (SysCfg.EnumAsrsTaskType)Enum.Parse(typeof(SysCfg.EnumAsrsTaskType), strArray[0]);
                        }
                        else if(this.portCata == 3)
                        {
                            this.bindedTaskInput = (SysCfg.EnumAsrsTaskType)Enum.Parse(typeof(SysCfg.EnumAsrsTaskType), strArray[0]);
                            if(strArray.Count()>1)
                            {
                                this.bindedTaskOutput = (SysCfg.EnumAsrsTaskType)Enum.Parse(typeof(SysCfg.EnumAsrsTaskType), strArray[1]);
                            }
                        }
                    }
                }
            }
            return true;
        }
       
        public bool ClearBufPallets(ref string reStr)
        {
            try
            {
                this.palletBuffer.Clear();
                MesDBAccess.BLL.AsrsPortBufferBll bufBll = new MesDBAccess.BLL.AsrsPortBufferBll();
                MesDBAccess.Model.AsrsPortBufferModel buf = bufBll.GetModel(this.nodeID);
                if(buf != null)
                {
                    buf.palletBuffers = "";
                    bufBll.Update(buf);
                }
                return true;
            }
            catch (Exception ex)
            {
                reStr = ex.ToString();
                return false;
                
            }
        }
        public void PushPalletID(string palletID)
        {
            if(this.palletBuffer.Contains(palletID))
            {
                return;
            }
            if(this.palletBuffer.Count()>=PortinBufCapacity)
            {
                this.palletBuffer.RemoveAt(0);
            }
            this.palletBuffer.Add(palletID);
            string strPallets = "";
            for (int i = 0; i < this.palletBuffer.Count(); i++)
            {
                strPallets += this.palletBuffer[i];
                if (palletBuffer.Count() > 1 && i < this.palletBuffer.Count() - 1)
                {
                    strPallets += ",";
                }
            }
            MesDBAccess.BLL.AsrsPortBufferBll bufBll = new MesDBAccess.BLL.AsrsPortBufferBll();
            MesDBAccess.Model.AsrsPortBufferModel buf = bufBll.GetModel(this.nodeID);
            if(buf == null)
            {
                buf = new MesDBAccess.Model.AsrsPortBufferModel();
                buf.houseName = this.AsrsCtl.HouseName;
                buf.nodeID = this.nodeID;
                buf.palletBuffers = strPallets;
                bufBll.Add(buf);
            }
            else
            {
                buf.palletBuffers = strPallets;
                bufBll.Update(buf);
            }
           
           
        }

    }
}
