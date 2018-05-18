using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OfficeMapper.Models
{
    public class LevelRange
    {
        public int startRow { get; set; }
        public int endRow { get; set; }
        public int level { get; set; }
        public string code { get; set; }
        public Models.AuthUser entry { get; set; }
        public string ObjectName { get; set; }

        public List<LevelRange> childLevels { get; set; }

        public bool HasChildren()
        {
            return endRow > (startRow + 1);
        }
    }
}