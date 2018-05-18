using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace OfficeMapper.Models
{
    public class Pool
    {
        public string Id { get; set; }
        public string Name { get; set; }

        [Display(Name="Ответственный")]
        public AuthUser Responsible { get; set; }
    }
}