using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OfficeMapper.Models
{
    public class OnlineUsersViewModel
    {
        public List<OnlineUser> users { get; set; }
    }

    public class OnlineUser
    {
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
        public List<LogonServers> Servers { get; set; }
        public string errors { get; set; }
        public string osarch { get; set; }
        public string RAM { get; set; }
        public string CPU_name { get; set; }
        public string CPU_freq { get; set; }
        public string GPU_NAME { get; set; }
        public string GPU_RAM { get; set; }
        public string GPU_HR { get; set; }
        public string GPU_VR { get; set; }
        public string HDDs { get; set; }
    }

    public class LogonServers {
        public string ServerIP { get; set; }
        public DateTime LogonDate { get; set; }
        public string OSVersion { get; set; }
        public bool RemoteControlInstalled { get; set; }
        public bool RemoteControlAvailable { get; set; }
        public string osarch { get; set; }
        public int RAM { get; set; }
        public string CPU_name { get; set; }
        public int CPU_freq { get; set; }
        public string GPU_NAME { get; set; }
        public int GPU_RAM { get; set; }
        public int GPU_HR { get; set; }
        public int GPU_VR { get; set; }
    }
}