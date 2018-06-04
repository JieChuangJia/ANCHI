using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlowCtlBaseModel;
namespace ProcessCtl
{
    public interface IMonitorView
    {
        void WelcomeAddStartinfo(string info); //增加欢迎信息
        void WelcomeDispCurinfo(string info);//显示当前信息
        void WelcomePopup(); //弹出启动界面
        void WelcomeClose(); //关闭欢迎界面
        void PopupMes(string strMes);
       // void InitNodeMonitorview();
        void RefreshNodeStatus();

        /// <summary>
        /// rgv通信中转状态更新
        /// </summary>
        /// <param name="rgvIndex"></param>
        /// <param name="conn"></param>
        /// <param name="mark"></param>
       // void RgvCommStatusRefresh(int rgvIndex, bool conn, string mark);
       
    }
}
