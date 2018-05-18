using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OfficeMapper.Lib
{
    public class FileShare : IBaseUserService
    {
        public string Id { get; set; }
        public string ServiceName { get; set; }
        public string Description { get; set; }
        public string DeprecationText { get; set; }
        public string secret { get; set; }
        public void Save(AcceptRequest ar)
        {
            Console.WriteLine("fs");
        }

        public void Save(List<AcceptRequest> ars)
        {
            Console.WriteLine("fs");
        }


        public StageEnum PushNextStage(string secret, Models.DecisionData data) {
            return StageEnum.NewRequest;
        }
    }
}