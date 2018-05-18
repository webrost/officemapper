using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OfficeMapper.Models
{
    public class UserLogin
    {
        public List<string> ip { get; set; }
        public string sAMAccountName { get; set; }
        public DateTime loginDate { get; set; }
        public string username { get; set; }
        public string domain { get; set; }
    }
}