using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OfficeMapper.Models
{
    public class AuthUser
    {
        public int Id { get; set; }
        public string UTNLogin { get; set; }
        public string email { get; set; }
        public string post { get; set; }
        public string fullPost { get; set; }
        public string phone { get; set; }
        public string mobile { get; set; }
        public string domain { get; set; }
        public string FIO { get; set; }
        public string AccessKey { get; set; }
        public List<AuthRoles> roles {get;set;}
        public bool ITResponsible { get; set; }
        public string DepName { get; set; }
        public bool isDep { get; set; }
        public string Birthday { get; set; }
    }
}