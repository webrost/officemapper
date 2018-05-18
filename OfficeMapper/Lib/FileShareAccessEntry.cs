using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OfficeMapper.Lib
{
    public class FileShareAccessEntry
    {
        public bool AllowRead { get; set; }
        public bool AllowWrite { get; set; }
        public string AccountName { get; set; }
        public DateTime AcceptedOwner { get; set; }
        public string AcceeptedOwnerBy { get; set; }
    }
}