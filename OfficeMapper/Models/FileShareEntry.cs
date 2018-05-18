using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OfficeMapper.Models
{
    public class FileShareEntry
    {
        public string Path { get; set; }
        public bool AllowRead { get; set; }
        public bool AllowWrite { get; set; }
        public DateTime AcceptedOwner { get; set; }
        public string AcceeptedOwnerBy { get; set; }
        public string CurrentUserAccountName { get; set; }
        public List<string> Owners { get; set; }
        public List<Lib.FileShareAccessEntry> Rights { get; set; }
    }
}