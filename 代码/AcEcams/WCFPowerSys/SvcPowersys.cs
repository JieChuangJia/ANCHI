using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
namespace WCFPowerSys
{
    // 注意: 使用“重构”菜单上的“重命名”命令，可以同时更改代码和配置文件中的类名“Service1”。
    public delegate bool DlgtIsAsrsCellReady(int row, int col, int layer, ref string[] barCodes, ref bool isReady, ref string reStr);
    public delegate bool DlgtPowerFillOk(int row, int col, int layer, ref string reStr);
    public delegate bool DlgtCommitCheckResult(string barcode, int checkRe, ref string reStr);
    public delegate bool DlgtCellValidStatNotify(int row, int col, int layer, bool cellValid, string reason, ref string reStr);
    public delegate bool DlgtCellEmerDangerNotify(int row, int col, int layer, string reason, ref string reStr);

    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class SvcPowersys : IAsrsPowerSvc
    {
        public DlgtIsAsrsCellReady dlgtIsAsrsCellReady = null;
        public DlgtPowerFillOk dlgtPowerFillOk = null;
        public DlgtCommitCheckResult dlgtCommitCheckRe = null;
        public DlgtCellValidStatNotify dlgtCellValidNotify = null;
        public DlgtCellEmerDangerNotify dlgtCellDangerNotify = null;

        public string GetData(int value)
        {
            return string.Format("You entered: {0}", value);
        }

        public CompositeType GetDataUsingDataContract(CompositeType composite)
        {
            if (composite == null)
            {
                throw new ArgumentNullException("composite");
            }
            if (composite.BoolValue)
            {
                composite.StringValue += "Suffix";
            }
            return composite;
        }

        public string GetInterfaceVersion()
        {
            return "1.0.0 2016-07-21";
        }
        public bool IsAsrsCellReady(int row, int col, int layer, ref string[] barCodes, ref bool isReady, ref string reStr)
        {
            if(dlgtIsAsrsCellReady != null)
            {
                return dlgtIsAsrsCellReady(row, col, layer, ref barCodes, ref isReady, ref reStr);
               // Console.WriteLine("收到远程调用,IsAsrsCellReady");
               
            }
            else
            {
                reStr = "服务不可调用";
                return false;
            }
            
        }
        public bool PowerFillOk(int row, int col, int layer, ref string reStr)
        {
          
            if(dlgtPowerFillOk != null)
            {
                return dlgtPowerFillOk(row, col, layer, ref reStr);
            }
            else
            {
                reStr = "服务不可调用";
                return false;
            }
           
        }
        public bool CommitCheckResult(string barcode, int checkRe, ref string reStr)
        {
           // Console.WriteLine("收到远程调用,CommitCheckResult");
            if (dlgtCommitCheckRe != null)
            {
                return dlgtCommitCheckRe(barcode, checkRe, ref reStr);
            }
            else
            {
                reStr = "服务不可调用";
                return false;
            }
        }
        public bool CellValidStatNotify(int row, int col, int layer, bool cellValid, string reason, ref string reStr)
        {
            if(dlgtCellValidNotify != null)
            {
                return dlgtCellValidNotify(row, col, layer, cellValid, reason, ref reStr);
            }
            else
            {
                reStr = "服务不可调用";
                return false;
            }
           
        }
        public bool CellEmerDangerNotify(int row, int col, int layer, string reason, ref string reStr)
        {
            if(dlgtCellDangerNotify != null)
            {
                return dlgtCellDangerNotify(row, col, layer, reason, ref reStr);
            }
            else
            {
                reStr = "服务不可调用";
                return false;
            }
            return true;
        }
    }
}
