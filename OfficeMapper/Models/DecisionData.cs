using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OfficeMapper.Models
{
    public class DecisionData
    {
        public DecisionEnum Decision { get; set; }
        public Models.AuthUser Acceptor { get; set; }
    }
}