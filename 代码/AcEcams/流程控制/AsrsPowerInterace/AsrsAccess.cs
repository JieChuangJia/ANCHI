using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.ServiceModel.Description;
namespace AsrsPowerInterace
{
    public class AsrsAccess:IAsrsAccess
    {
        private string svcAddr = "";
        //private IRbtAccess rbtAccSvc = null;
        AsrsSvc.IAsrsPowerSvc asrsPowerSvc = null;
        public AsrsAccess()
        {

        }
        public string GetInterfaceVersion()
        {
            return "1.0.1 2016-10-16";
        }
        /// <summary>
        /// 连接立库调度管理系统
        /// </summary>
        /// <param name="addr">连接地址</param>
        /// <param name="reStr">连接结果信息反馈，尤其是失败情况下的信息</param>
        /// <returns>成功连接返回true，否则返回false</returns>
        public bool ConnectAsrs(string svcAddr,ref string reStr)
        {
            try
            {
                this.svcAddr = svcAddr;
                asrsPowerSvc = ChannelFactory<AsrsSvc.IAsrsPowerSvc>.CreateChannel(new BasicHttpBinding(), new EndpointAddress(svcAddr)); ;//new AsrsSvc.AsrsPowerSvcClient();
                string[] barcodes=null;
                bool isReady=false;
                asrsPowerSvc.IsAsrsCellReady(1, 1, 1, ref barcodes, ref isReady, ref reStr);
                return true;
            }
            catch (Exception ex)
            {
                reStr = "服务连接失败，发生异常，" + ex.Message;
                return false;
            }
            
        }

        /// <summary>
        /// 查询货位是否就绪（可以充电）
        /// </summary>
        /// <param name="row">立库排号（从1开始）</param>
        /// <param name="col">立库列（从1开始）</param>
        /// <param name="layer">立库层（从1开始）</param>
        /// <param name="barCodes">模组条码，每个工装板带两个模组，按照约定摆放顺序</param>
        /// <param name="isReady">（out) 是否就绪</param>
        /// <param name="reStr">(out)若接口调用失败返回失败信息</param>
        /// <returns>true：接口调用成功，false：接口调用失败</returns>
        public bool IsAsrsCellReady(int row, int col, int layer, ref string[] barCodes, ref bool isReady, ref string reStr)
        {
            return asrsPowerSvc.IsAsrsCellReady(row, col, layer, ref barCodes, ref isReady, ref reStr);
        }

        /// <summary>
        /// 通知立库，充电完成
        /// </summary>
        /// <param name="row">立库排号（从1开始）</param>
        /// <param name="col">立库列（从1开始）</param>
        /// <param name="layer">立库层（从1开始）</param>
        /// <param name="reStr">(out)若接口调用失败返回失败信息</param>
        /// <returns>true：接口调用成功，false：接口调用失败</returns>
        public bool PowerFillOk(int row, int col, int layer, ref string reStr)
        {
            return asrsPowerSvc.PowerFillOk(row, col, layer, ref reStr);
           // return true;
        }

        /// <summary>
        /// 提交检测结果
        /// </summary>
        /// <param name="barcode">模组条码</param>
        /// <param name="checkRe">结果，1：合格，2：不合格,</param>
        /// <param name="reStr">(out)若接口调用失败返回失败信息</param>
        /// <returns>true：接口调用成功，false：接口调用失败</returns>
        public bool CommitCheckResult(string barcode, int checkRe, ref string reStr)
        {
            return asrsPowerSvc.CommitCheckResult(barcode, checkRe, ref reStr);
          
        }

        /// <summary>
        /// 通知立库系统，货位是否可用。
        /// </summary>
        /// <param name="row">立库排号（从1开始）</param>
        /// <param name="col">立库列（从1开始）</param>
        /// <param name="layer">立库层（从1开始）</param>
        /// <param name="cellValid">货位是否可用</param>
        /// <param name="reason">货位不可用的解释</param>
        /// <param name="reStr">(out)若接口调用失败返回失败信息</param>
        /// <returns>true：接口调用成功，false：接口调用失败</returns>
        public bool CellValidStatNotify(int row, int col, int layer, bool cellValid, string reason, ref string reStr)
        {
            return asrsPowerSvc.CellValidStatNotify(row,col,layer,cellValid,reason,ref reStr);
        }

        /// <summary>
        /// 货位紧急报警
        /// </summary>
        /// <param name="row">立库排号（从1开始）</param>
        /// <param name="col">立库列（从1开始）</param>
        /// <param name="layer">立库层（从1开始）</param>
        /// <param name="reason">紧急状态解释</param>
        /// <param name="reStr">(out)若接口调用失败返回失败信息</param>
        /// <returns>true：接口调用成功，false：接口调用失败</returns>
        public bool CellEmerDangerNotify(int row, int col, int layer, string reason, ref string reStr)
        {
            return asrsPowerSvc.CellEmerDangerNotify(row, col, layer, reason, ref reStr);
           
        }
    }
}
