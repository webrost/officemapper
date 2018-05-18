using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OfficeMapper.Models
{
    public class CompInvent
    {
        public string sAMAccountName { get; set; }
        public string[] ip { get; set; }
        public string osversion { get; set; }
        public string compname { get; set; }
        public string osarch { get; set; }
        public string RAM { get; set; }
        public string CPU_name { get; set; }
        public string CPU_freq { get; set; }
        public string GPU_NAME { get; set; }
        public string GPU_RAM { get; set; }
        public string GPU_HR { get; set; }
        public string GPU_VR { get; set; }
        public string errors { get; set; }
        public string dameware { get; set; }
        public List<HDD> HDDs { get; set; }
        public List<PrinterEntity> Printers { get; set; }
        public string userprofile { get; set; }
        public int userprofilesize { get; set; }
        public List<MailRecord> mail { get; set; }
        public List<ProgramEntry> programs { get; set; }
        public List<FileTypeEntry> allfiles { get; set; }
        public List<OdinCClass> bases { get; set; }
    }

    public class OdinCClass
    {
        public string Name { get; set; }
        public string Info { get; set; }
    }

    public class HDD
    {
        public string disc_name { get; set; }
        public string disc_interface { get; set; }
        public string disc_size { get; set; }
        public List<LogicalDisc> LogicalDiscs { get; set; }

    }

    public class MailRecord
    {
        public string lable { get; set; }
        public string eml { get; set; }
        public string dbx { get; set; }
    }

    public class LogicalDisc
    {
        public string Lable { get; set; }
        public string Size { get; set; }
        public string Free { get; set; }
    }

    public class PrinterEntity
    {
        public string Name { get; set; }
        public string Default { get; set; }
        public string Network { get; set; }
        public string Server { get; set; }
        public string ShareName { get; set; }
    }

    public class ProgramEntry
    {
        public string Name { get; set; }
    }

    public class FileTypeEntry
    {
        public string Lable { get; set; }
        public string Ext { get; set; }
        public int Size { get; set; }
    }
}