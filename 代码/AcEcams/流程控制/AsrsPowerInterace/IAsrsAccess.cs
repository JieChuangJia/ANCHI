using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AsrsPowerInterace
{
    /// <summary>
    /// 电源系统访问立库的接口类
    /// </summary>
    public interface IAsrsAccess
    {
        /// <summary>
        /// 查询接口库版本号
        /// </summary>
        /// <returns></returns>
        string GetInterfaceVersion();

        /// <summary>
        /// 连接立库调度管理系统
        /// </summary>
        /// <param name="addr">连接地址</param>
        /// <param name="reStr">连接结果信息反馈，尤其是失败情况下的信息</param>
        /// <returns>成功连接返回true，否则返回false</returns>
        bool ConnectAsrs(string svcAddr,ref string reStr);

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
        bool IsAsrsCellReady(int row, int col, int layer, ref string[] barCodes,ref bool isReady, ref string reStr);

        /// <summary>
        /// 通知立库，充电完成
        /// </summary>
        /// <param name="row">立库排号（从1开始）</param>
        /// <param name="col">立库列（从1开始）</param>
        /// <param name="layer">立库层（从1开始）</param>
        /// <param name="reStr">(out)若接口调用失败返回失败信息</param>
        /// <returns>true：接口调用成功，false：接口调用失败</returns>
        bool PowerFillOk(int row, int col, int layer, ref string reStr);

        /// <summary>
        /// 提交检测结果
        /// </summary>
        /// <param name="barcode">模组条码</param>
        /// <param name="checkRe">结果，1：合格，2：不合格,</param>
        /// <param name="reStr">(out)若接口调用失败返回失败信息</param>
        /// <returns>true：接口调用成功，false：接口调用失败</returns>
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
        bool CellValidStatNotify(int row, int col, int layer, bool cellValid, string reason,ref string reStr);

        /// <summary>
        /// 货位紧急报警
        /// </summary>
        /// <param name="row">立库排号（从1开始）</param>
        /// <param name="col">立库列（从1开始）</param>
        /// <param name="layer">立库层（从1开始）</param>
        /// <param name="reason">紧急状态解释</param>
        /// <param name="reStr">(out)若接口调用失败返回失败信息</param>
        /// <returns>true：接口调用成功，false：接口调用失败</returns>
        bool CellEmerDangerNotify(int row, int col, int layer, string reason, ref string reStr);

    }
}
