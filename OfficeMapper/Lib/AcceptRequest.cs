using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OfficeMapper.Lib
{
    public class AcceptRequest
    {
        public string ServiceId { get; set; }
        public ServiceTypeEnum ServiceType { get; set; }
        public DateTime RequestDate { get; set; }
        public string UTNLogin { get; set; }
        public string Secret { get; set; }
        public StageEnum RequestStage { get; set; }
    }
}