using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OfficeMapper.Models
{
    public class ClamedServices
    {
        public AuthUser user { get; set; }
        public string secret { get; set; }
    }
}