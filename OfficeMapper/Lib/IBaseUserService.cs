using Ninject;
using Ninject.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;

namespace OfficeMapper.Lib
{
    
    public interface IBaseUserService
    {
        string Id { get; set; }
        string ServiceName { get; set; }
        string Description { get; set; }
        string DeprecationText { get; set;}
        string secret { get; set; }

        void Save(AcceptRequest ar);
        void Save(List<AcceptRequest> ar);
        StageEnum PushNextStage(string secret, Models.DecisionData data = null);
    }
}