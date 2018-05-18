using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OfficeMapper.Models
{
    public class StructureUnit
    {
        public string Name { get; set; }
        public string FunctionDescription { get; set; }
        public string BirthDate { get; set; }
        public DateTime BirthDateDate { get; set; }
        public string Phones { get; set; }
        public string Mobile { get; set; }
        public string Email { get; set; }
        public string Post { get; set; }
        public string FullPost { get; set; }
        public int Level { get; set; }
        public string Code { get; set;}
        public List<StructureUnit> Children { get; set; }
        public bool Current { get; set; }
        public string Photo { get; set; }
        public string PhotoFileName { get; set; }

        /// <summary>
        /// type = "user" | "dep"
        /// </summary>
        public string Type { get; set; }

    }
}