using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASRSStorManage
{
    public class QueryStockParamModel
    {
        public string HouseName { get; set; }
        public string HouseArea { get; set; }
        public string Rowth { get; set; }
        public string Colth { get; set; }
        public string Layerth { get; set; }
        public string GsStatus { get; set; }
        public string GsTaskStatus { get; set; }
        public string Batch { get; set; }
    }
  public enum ExtendFormCate
  {
      内部,
      外部
  }
}
