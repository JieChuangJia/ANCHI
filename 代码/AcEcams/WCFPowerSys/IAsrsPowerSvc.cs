using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace WCFPowerSys
{
    // 注意: 使用“重构”菜单上的“重命名”命令，可以同时更改代码和配置文件中的接口名“IService1”。
    [ServiceContract]
    public interface IAsrsPowerSvc
    {
        [OperationContract]
        string GetData(int value);

        [OperationContract]
        CompositeType GetDataUsingDataContract(CompositeType composite);

        // TODO: 在此添加您的服务操作
        /// <summary>
        /// 查询接口库版本号
        /// </summary>
        /// <returns></returns>
        /// 
        [OperationContract]
        string GetInterfaceVersion();

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
        [OperationContract]
        bool IsAsrsCellReady(int row, int col, int layer, ref string[] barCodes, ref bool isReady, ref string reStr);

        /// <summary>
        /// 通知立库，充电完成
        /// </summary>
        /// <param name="row">立库排号（从1开始）</param>
        /// <param name="col">立库列（从1开始）</param>
        /// <param name="layer">立库层（从1开始）</param>
        /// <param name="reStr">(out)若接口调用失败返回失败信息</param>
        /// <returns>true：接口调用成功，false：接口调用失败</returns>
        [OperationContract]
        bool PowerFillOk(int row, int col, int layer, ref string reStr);

        /// <summary>
        /// 提交检测结果
        /// </summary>
        /// <param name="barcode">模组条码</param>
        /// <param name="checkRe">结果，1：合格，2：不合格,</param>
        /// <param name="reStr">(out)若接口调用失败返回失败信息</param>
        /// <returns>true：接口调用成功，false：接口调用失败</returns>
        [OperationContract]
        bool CommitCheckResult(string barcode, int checkRe, ref string reStr);

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
        [OperationContract]
        bool CellValidStatNotify(int row, int col, int layer, bool cellValid, string reason, ref string reStr);

        /// <summary>
        /// 货位紧急报警
        /// </summary>
        /// <param name="row">立库排号（从1开始）</param>
        /// <param name="col">立库列（从1开始）</param>
        /// <param name="layer">立库层（从1开始）</param>
        /// <param name="reason">紧急状态解释</param>
        /// <param name="reStr">(out)若接口调用失败返回失败信息</param>
        /// <returns>true：接口调用成功，false：接口调用失败</returns>
        [OperationContract]
        bool CellEmerDangerNotify(int row, int col, int layer, string reason, ref string reStr);
    }

    // 使用下面示例中说明的数据约定将复合类型添加到服务操作。
    [DataContract]
    public class CompositeType
    {
        bool boolValue = true;
        string stringValue = "Hello ";

        [DataMember]
        public bool BoolValue
        {
            get { return boolValue; }
            set { boolValue = value; }
        }

        [DataMember]
        public string StringValue
        {
            get { return stringValue; }
            set { stringValue = value; }
        }
    }
}
