using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
namespace AsrsInterface
{
    // 远程监控、设置服务接口
    [ServiceContract]
    public interface IRemoteMonitorSvc
    {
        //获取设备连接状态
        [OperationContract]
        void GetCommDevStatus(ref bool[] conn);

        //设置出入库批次
        [OperationContract]
        bool SetBatch(int inoutFlag, string houseName, string batch,ref string reStr);

        //获取在库所有批次 
        [OperationContract]
        List<string> GetStoreBatchs(string houseName);

        //获取入库批次设置（多个库房）
        [OperationContract]
        List<string> GetCheckinBatchSet();

        //获取出库批次设置（多个库房）
        [OperationContract]
        List<string> GetCheckoutBatchSet();
    }
}
