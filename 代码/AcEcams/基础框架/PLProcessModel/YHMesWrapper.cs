using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlowCtlBaseModel
{
    public class YHMesWrapper
    {
        public JieChuangServices MesAcc { get; set; } //MES服务接口
        public YHMesWrapper()
        {
            MesAcc = new JieChuangServices();
        }
        public VMResult UpdateStep(int StepNow, string TrayNo)
        {
            try
            {
                return MesAcc.UpdateStep(StepNow, TrayNo);
            }
            catch (Exception ex)
            {
                VMResult re = new VMResult();
                re.ResultCode = -1;
                re.ResultMsg = "MES网络异常:"+ex.Message;
                return re;
            }
        }
        public ANCCellSeparation GetCellSeparation(int step, string trayNO)
        {
            try
            {
                return MesAcc.GetCellSeparation(step, trayNO);
            }
            catch (Exception ex)
            {
                ANCCellSeparation re = new ANCCellSeparation();
                re.ResultCode = -1;
                re.ResultMsg = "MES网络异常:" + ex.Message;
                return re;
            }
        }
        public ANCStepResult GetStep(string TrayNO)
        {
            try
            {
                return MesAcc.GetStep(TrayNO);
            }
            catch (Exception ex)
            {
                ANCStepResult re = new ANCStepResult();
                re.ResultCode = -1;
                re.ResultMsg = "MES网络异常:" + ex.Message;
                return re;
            }
        }
        public VMResultLot GetTrayCellLotNO(string trayNo)
        {
            try
            {
                return MesAcc.GetTrayCellLotNO(trayNo);
            }
            catch (Exception ex)
            {
                VMResultLot re = new VMResultLot();
                re.ResultCode = -1;
                re.ResultMsg = "MES网络异常:" + ex.Message;
                return re;
            }
        }
        public VMResultIntercept CellWhetherIntercept(string stepName, string barcode)
        {
            try
            {
                return MesAcc.CellWhetherIntercept(stepName, barcode);
            }
            catch (Exception ex)
            {
                VMResultIntercept re = new VMResultIntercept();
                re.ResultCode = -1;
                re.ResultMsg = "MES网络异常:" + ex.Message;
                return re;
            }
        }
        public VMResult UploadTrayCellInfo(string info)
        {
            try
            {
                return MesAcc.UploadTrayCellInfo(info);
            }
            catch (Exception ex)
            {
                VMResult re = new VMResult();
                re.ResultCode = -1;
                re.ResultMsg = "MES网络异常:" + ex.Message;
                return re;
            }
        }
        public VMResult UploadHighTempInfo(string info)
        {
            try
            {
                return MesAcc.UploadHighTempInfo(info);
            }
            catch (Exception ex)
            {
                VMResult re = new VMResult();
                re.ResultCode = -1;
                re.ResultMsg = "MES网络异常:" + ex.Message;
                return re;
            }
        }
        public VMResult UploadNormalTempInfo(string info)
        {
            try
            {
                return MesAcc.UploadNormalTempInfo(info);
            }
            catch (Exception ex)
            {
                VMResult re = new VMResult();
                re.ResultCode = -1;
                re.ResultMsg = "MES网络异常:" + ex.Message;
                return re;
            }
        }
        public VMResult UploadCoolingInfo(string info)
        {
            try
            {
                return MesAcc.UploadCoolingInfo(info);
            }
            catch (Exception ex)
            {
                VMResult re = new VMResult();
                re.ResultCode = -1;
                re.ResultMsg = "MES网络异常:" + ex.Message;
                return re;
            }
        }
        public VMResultCells GetTrayBindingCell(string TrayNo)
        {
            try
            {
                
                return MesAcc.GetTrayBindingCell(TrayNo);
            }
            catch (Exception ex)
            {
                VMResultCells re = new VMResultCells();
                re.ResultCode = -1;
                re.ResultMsg = "MES网络异常:" + ex.Message;
                return re;
            }
        }
        public bool UnbindTrayCell(string TrayNo)
        {
            try
            {
                return MesAcc.UnbindTrayCell(TrayNo);
            }
            catch (Exception)
            {
                return false;
               
            }
            
        }
       
    }
}
