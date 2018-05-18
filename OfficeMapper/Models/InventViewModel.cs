using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace OfficeMapper.Models
{
    public class InventViewModel
    {
        [Required]
        public List<OnlineComps> computers { get; set; }
        public int TotalComps { get; set; }
    }

    public class OnlineComps
    {
        [Key]
        public string CompName { get; set; }
        public string Depart { get; set; }
        public string Filial { get; set; }
        public string Login { get; set; }
        public string username { get; set; }
        public string domain { get; set; }
        public string FIO { get; set; }
        public string Post { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public DateTime LastLogon { get; set; }
        public string LastLogonIP { get; set; }
        public string OSVersion { get; set; }
        public bool DameWare { get; set; }
        public string errors { get; set; }
        public string osarch { get; set; }
        public int RAM { get; set; }
        public string CPU_name { get; set; }
        public int CPU_freq { get; set; }
        public string GPU_NAME { get; set; }
        public int GPU_RAM { get; set; }
        public int GPU_HR { get; set; }
        public int GPU_VR { get; set; }
        public int TotalDriveCount { get; set; }
        public int TotalDriveSize { get; set; }
        public int TotalFreeSize { get; set; }
        public string DriveInterfaces { get; set; }
        public string DefaultPrinterName { get; set; }
        public int MailboxSize { get; set; }
        public int ProfileSize { get; set; }

    }

}