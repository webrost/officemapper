using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace OfficeMapper.Controllers
{
    public class AppsController : Controller
    {
        // GET: Apps
        [Authorize]
        public ActionResult Index(string id)
        {
            Models.ApplicationView model = new Models.ApplicationView();
            model.User = new Models.AuthUser();

            if (string.IsNullOrEmpty(id)) { 
                model.User = Lib.ADAuth.GetUser(HttpContext.User.Identity.Name);
            }
            else
            {
                model.User = Lib.ADAuth.GetUserByAccessKey(id);
                if(model.User == null) throw new Exception("Invalid user acccess key: " + id);
            }

            ///---Получение приложений пользователя
            Lib.ApplicationHelper helper = new Lib.ApplicationHelper();
            model.Applications = helper.GetApplications(model.User);

            ///---Получение шар пользователя
            model.FileShares = Lib.FileShares.GetAllShares(model.User.UTNLogin);

            ///---Получаю режим работы приложения
            model.IsLearningMode = bool.Parse(System.Configuration.ConfigurationManager.AppSettings["learningMode"]);

            model.NewApplications = new List<Models.ApplicationEntry>();
            for(int i = 0; i < 10; i++)
            {
                model.NewApplications.Add(new Models.ApplicationEntry() {
                    Description = " "
                });
            }
            
            return View(model);
        }
       
        /// <summary>
        /// Сохранение приложений пользователя
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult Index(Models.ApplicationView data)
        {
            Models.ApplicationView model = new Models.ApplicationView();
            model.User = data.User;
            Lib.ApplicationHelper helper = new Lib.ApplicationHelper();
            helper.SaveApplications(data);

            model.Applications = helper.GetApplications(model.User);
           
            return View(model);
        }

        /// <summary>
        /// Импорт пользователей из АД в базу
        /// </summary>
        /// <returns></returns>
        [Authorize]
        public ActionResult ImportAD()
        {
            Lib.ADAuth.ImportAD();
            return View();
        }

        /// <summary>
        /// Возвращает все шары предприятия
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [HttpGet]
        public ActionResult Shares(Models.ApplicationView data)
        {
            return View(data);
        }
        
        /// <summary>
        /// Выводит пользователей, которые сегодня вошли в систему
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ActionResult OnlineUsers(string sortBy)
        {
            Models.OnlineUsersViewModel m = new Models.OnlineUsersViewModel();
            
            using (Models.PhonesDataContext model = new Models.PhonesDataContext())
            {
                
                m.users = 
                model.Logins.Where(x => x.Date >= DateTime.Today)
                .Select(x => new Models.OnlineUser()
                {
                    Login = x.sAMAccountName,
                    FIO = x.FIO,
                    Email = x.Email
                }).Distinct()
                .OrderBy(x => x.Login)
                .ToList();

                var phoneDic = model.Phones.ToList();

                foreach(var u in m.users)
                {
                    List<Models.LogonServers> servers = GetLogonServers(u.Login, DateTime.Today);

                    u.LastLogon = servers.Count > 0 ? servers.First().LogonDate: DateTime.MinValue;
                    u.LastLogonIP = servers.Count > 0 ? servers.First().ServerIP:"";
                    u.OSVersion = servers.Count > 0 ? servers.First().OSVersion:"";

                    u.osarch = servers.Count > 0 ? servers.First().osarch : "";
                    //u.RAM = servers.Count > 0 ? servers.First().RAM : "";
                    //u.CPU_name = servers.Count > 0 ? servers.First().CPU_name : "";
                    //u.CPU_freq = servers.Count > 0 ? servers.First().CPU_freq : "";
                    //u.GPU_NAME = servers.Count > 0 ? servers.First().GPU_NAME : "";
                    //u.GPU_RAM = servers.Count > 0 ? servers.First().GPU_RAM : "";
                    //u.GPU_HR = servers.Count > 0 ? servers.First().GPU_HR : "";
                    //u.GPU_VR = servers.Count > 0 ? servers.First().GPU_VR : "";
                    //u.HDDs = servers.Count > 0 ? servers.First().HDDs : "";

                    u.username = u.Login.Split('\\').Length > 1 ? u.Login.Split('\\')[1] : "";
                    u.domain = u.Login.Split('\\').Length > 1 ? u.Login.Split('\\')[0] : "";
                    u.DameWare = model.Logins.Count(x => x.sAMAccountName == u.Login && x.DameWare == true) > 0;  
                }


                m.users = m.users.GroupJoin(phoneDic, user => user.Email, 
                    ph => ph.Email, (user, ph) => new 
                {
                        user = user,
                        phones = ph
                })
                .SelectMany(x=>x.phones.DefaultIfEmpty(),
                (x,y)=>new Models.OnlineUser() {
                    Login = x.user.Login,
                    FIO = x.user.FIO,
                    Email = x.user.Email,
                    DameWare = x.user.DameWare,
                    LastLogon = x.user.LastLogon,
                    LastLogonIP = x.user.LastLogonIP,
                    OSVersion = x.user.OSVersion,
                    username = x.user.username,
                    domain = x.user.domain,                
                    Phone = x.phones.Count() > 0? x.phones.First().Phone1:"",

                    osarch = x.user.osarch
                    //RAM = x.user.RAM,
                    //CPU_name = x.user.CPU_name,
                    //CPU_freq = x.user.CPU_freq,
                    //GPU_NAME = x.user.GPU_NAME,
                    //GPU_RAM = x.user.GPU_RAM,
                    //GPU_HR = x.user.GPU_HR,
                    //GPU_VR = x.user.GPU_VR,
                    //HDDs = x.user.HDDs
            }
                )
                .ToList();

                switch (sortBy.ToLower())
                {
                    case "login":
                        m.users = m.users.OrderBy(x => x.Login).ToList();
                        break;
                    case "fio":
                        m.users = m.users.OrderBy(x => x.FIO).ToList();
                        break;
                    case "ip":
                        m.users = m.users.OrderBy(x => x.LastLogonIP).ToList();
                        break;
                    case "date":
                        m.users = m.users.OrderByDescending(x => x.LastLogon).ToList();
                        break;
                    case "email":
                        m.users = m.users.OrderBy(x => x.Email).ToList();
                        break;
                }
            }
            return View(m);
        }

        /// <summary>
        /// Отображает данные инвентаризации
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ActionResult Invent(string sortBy)
        {
            Models.InventViewModel m = new Models.InventViewModel();

            using (Models.PhonesDataContext model = new Models.PhonesDataContext())
            {

                m.computers =
                model.Logins.Where(x => x.Date >= DateTime.Today)
                .Select(x => new Models.OnlineComps()
                {
                    CompName = x.CompName
                }).Distinct()
                .ToList();

                m.TotalComps = m.computers.Count;

                foreach (var u in m.computers)
                {
                    var ls = GetLogonServerByName(u.CompName);
                    u.CPU_freq = ls.CPU_freq!=null?ls.CPU_freq.Value:0;
                    u.CPU_name = ls.CPU_name;
                    u.LastLogonIP = ls.IPAddress;
                    u.osarch = ls.osarch;
                    u.OSVersion = ls.OsVersion;
                    u.RAM = ls.RAM != null ? ls.RAM.Value : 0;
                    u.username = ls.sAMAccountName;
                    u.TotalDriveCount = ls.DriveCount!= null?ls.DriveCount.Value:0;
                    u.TotalDriveSize = ls.TotalHDDSize!=null?ls.TotalHDDSize.Value:0;
                    u.TotalFreeSize = ls.TotalFreeSize != null ? ls.TotalFreeSize.Value : 0;
                    u.DriveInterfaces = ls.DriveInterfaces;
                    u.LastLogon = ls.Date!=null?ls.Date.Value:DateTime.Now;
                    u.GPU_HR = ls.GPU_HR != null?ls.GPU_HR.Value:0;
                    u.GPU_VR = ls.GPU_VR != null ? ls.GPU_VR.Value : 0;
                    u.GPU_NAME = ls.GPU_NAME;
                    u.Depart = GetDepart(u.LastLogonIP);
                    u.Filial = GetFilial(u.LastLogonIP);
                    u.MailboxSize = ls.MailBoxSize != null? ls.MailBoxSize.Value:0;
                    u.ProfileSize = ls.ProfileSize != null ? ls.ProfileSize.Value : 0;
                    u.DefaultPrinterName = ls.DefaultPrinterName;
                }

                switch (sortBy.ToLower())
                {
                    case "cpu_name":
                        m.computers = m.computers.OrderBy(x => x.CPU_name).ToList();
                        break;
                    case "osversion":
                        m.computers = m.computers.OrderBy(x => x.OSVersion).ToList();
                        break;
                    case "compname":
                        m.computers = m.computers.OrderBy(x => x.CompName).ToList();
                        break;
                    case "lastlogonip":
                        m.computers = m.computers.OrderBy(x => x.LastLogonIP).ToList();
                        break;
                    case "date":
                        m.computers = m.computers.OrderByDescending(x => x.LastLogon).ToList();
                        break;
                    case "gpu_hr":
                        m.computers = m.computers.OrderBy(x => x.GPU_HR).ToList();
                        break;
                    case "gpu_vr":
                        m.computers = m.computers.OrderBy(x => x.GPU_VR).ToList();
                        break;
                    case "totaldrivecount":
                        m.computers = m.computers.OrderBy(x => x.TotalDriveCount).ToList();
                        break;
                    case "totaldrivesize":
                        m.computers = m.computers.OrderBy(x => x.TotalDriveSize).ToList();
                        break;
                    case "totalfreesize":
                        m.computers = m.computers.OrderBy(x => x.TotalFreeSize).ToList();
                        break;
                    case "depart":
                        m.computers = m.computers.OrderBy(x => x.Depart).ToList();
                        break;
                    case "filial":
                        m.computers = m.computers.OrderBy(x => x.Filial).ToList();
                        break;
                    case "ram":
                        m.computers = m.computers.OrderBy(x => x.RAM).ToList();
                        break;
                    case "defaultprintername":
                        m.computers = m.computers.OrderBy(x => x.DefaultPrinterName).ToList();
                        break;
                }
            }
            return View(m);
        }

        /// <summary>
        /// Возвращает последние данные компа по его имени
        /// </summary>
        /// <returns></returns>
        Models.Login GetLogonServerByName(string compName)
        {
            Models.Login ret = new Models.Login();
            using (Models.PhonesDataContext model = new Models.PhonesDataContext())
            {
                if(compName != null)
                { 
                ret = model.Logins
                    .Where(x => x.CompName == compName)
                    .OrderByDescending(x => x.Date)
                    .First();
                }
            }
            return ret;
        }

        /// <summary>
        /// Получаем подразделение по IP
        /// </summary>
        /// <param name="IP"></param>
        /// <returns></returns>
        string GetDepart(string IP)
        {
            using (Models.PhonesDataContext model = new Models.PhonesDataContext())
            {
                if(IP != null) { 
                List<string> octets = IP.Split('.').ToList();
                string network = "";
                if(octets.Count > 2)
                {
                    for(int i = 0; i < 3; i++)
                    {
                        network += octets[i] + ".";
                    }
                }
                if(model.Networks.Count(x=>x.network1.StartsWith(network)) > 0)
                {
                    return model.Networks.First(x => x.network1.StartsWith(network)).department;
                }
                }
            }
            return "";
        }

        /// Получаем ФИЛИАЛ по IP
        /// </summary>
        /// <param name="IP"></param>
        /// <returns></returns>
        string GetFilial(string IP)
        {
            using (Models.PhonesDataContext model = new Models.PhonesDataContext())
            {
                if (IP != null)
                {
                    List<string> octets = IP.Split('.').ToList();
                    string network = "";
                    if (octets.Count > 2)
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            network += octets[i] + ".";
                        }
                    }
                    if (model.Networks.Count(x => x.network1.StartsWith(network)) > 0)
                    {
                        return model.Networks.First(x => x.network1.StartsWith(network)).filial;
                    }
                }
            }
            return "";
        }

        public List<Models.LogonServers> GetLogonServers(string login, DateTime date)
        {            

            using (Models.PhonesDataContext model = new Models.PhonesDataContext())
            {
                List<Models.LogonServers> ret = new List<Models.LogonServers>();
                List<Models.LogonServers> r1 = new List<Models.LogonServers>();
                ret = model.Logins
                    .Where(x => x.sAMAccountName == login && x.Date.Value != null && x.Date.Value >= date)
                    .Select(x => new Models.LogonServers() {
                        LogonDate = x.Date.Value,
                        ServerIP = x.IPAddress,
                        OSVersion = x.OsVersion,
                        osarch = x.osarch
    })                
                    .OrderByDescending(x=>x.LogonDate)
                    .ToList();

                foreach(var x in ret)
                {
                    r1.Add(x);
                }

                //r1.RemoveAll(x => x.ServerIP.StartsWith("10.111."));
                //r1.AddRange(ret.Where(x => x.ServerIP.StartsWith("10.111.")).ToList());
                return r1;
            }
        }


    }
}