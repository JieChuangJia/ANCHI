using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AsrsInterface;
using AsrsModel;
using System.ServiceModel;
using System.ServiceModel.Description;
namespace NbssECAMS
{
    public class AsrsCtlSvcProxy:IAsrsCtlToManage
    {
        IAsrsCtlToManage asrsCtl = null;
        public AsrsCtlSvcProxy()
        {

        }
        public void SetSvcAddr(string svcAddr)
        {
            asrsCtl = ChannelFactory<AsrsInterface.IAsrsCtlToManage>.CreateChannel(new BasicHttpBinding(), new EndpointAddress(svcAddr)); //new AsrsSvc.AsrsPowerSvcClient();;
            
        }
        public bool CreateManualOutputTask(string houseName, CellCoordModel cell, ref string reStr)
        {
            try
            {
                return asrsCtl.CreateManualOutputTask(houseName, cell, ref reStr);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }
        }

        public bool CreateManualMoveGSTask(string startHouseName, CellCoordModel startCell, string endHouseName, CellCoordModel endCell, ref string reStr)
        {
            try
            {
                return asrsCtl.CreateManualMoveGSTask(startHouseName, startCell, endHouseName,endCell, ref reStr);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }
        }
    }
}
