using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OfficeMapper.Models
{
    public class SiteStatistic
    {
        public int TodayHitCount { get; set; }
        public int TodayVisitors { get; set; }
        public int TotalHitCount { get; set; }
        public int TotalVisitors { get; set; }
    }
}