using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.IO;

namespace OfficeMapper.Controllers
{
    public class UsersController : ApiController
    {

        /// <summary>
        /// Возвращает коллекцию пользователей, имя которых начинается с...
        /// </summary>
        /// <param name="startFrom"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("api/users/{startFrom}")]
        public List<Models.AuthUser> findUsers(string startFrom)
        {
            List<Models.AuthUser> users = new List<Models.AuthUser>();
            using (Models.PhonesDataContext model = new Models.PhonesDataContext()) {
                users = model.Users.Where(x => x.UTNLogin.StartsWith(startFrom)
                || x.FIO.StartsWith(startFrom)).ToList().Select(x => new Models.AuthUser() {
                    FIO = x.FIO,
                    UTNLogin = x.UTNLogin,
                    email = x.Email,
                    AccessKey = x.AccessKey
                }).ToList();
            }
            return users;
        }

        /// <summary>
        /// Возвращает расширенную информацию по пользователю
        /// </summary>
        /// <param name="login"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("api/userinfo/{CodeId}")]
        public Models.StructureUnit GetUserInfo(string CodeId)
        {
            Models.StructureUnit user = new Models.StructureUnit();
            string Id = CodeId.Replace("-", ".").Trim();
            using (Models.PhonesDataContext model = new Models.PhonesDataContext())
            {
                if(model.Phones.Count(x=>x.Code == Id && x.PIB != null) > 0)
                {
                    var u = model.Phones.First(x => x.Code == Id);
                    user.Code = u.Code;
                    user.Name = u.PIB;
                    user.Post = u.Post;
                    user.Email = u.Email;
                    user.Phones = u.Phone1;
                    user.Mobile = u.Mobile;
                    user.PhotoFileName = DepController.PhotoName(u.Email);

                    string currDepId = "";
                    user.FullPost = "";
                    foreach (var depId in u.Code.Split('.').ToList())
                    {
                        currDepId += depId;
                        if(model.Phones.Count(x=>x.Dep != null && x.Code == currDepId) > 0)
                        {
                            user.FullPost += model.Phones.First(x => x.Dep != null && x.Code == currDepId).Dep + " ";
                        }
                        currDepId += ".";
                    }
                }
            }
            return user;
        }

        [HttpPost]
        [Route("api/userlogin")]
        public string SaveLogin()
        {
            using (Models.PhonesDataContext model = new Models.PhonesDataContext())
            {
                try {
                    var content = Request.Content.ReadAsByteArrayAsync().Result;
                    string res = System.Text.Encoding.UTF8.GetString(content);
                    res = res.Replace(@"\", @"\\");

                    var inv = JsonConvert.DeserializeObject<Models.CompInvent>(res);

                    model.EventLogs.InsertOnSubmit(new Models.EventLog()
                    {
                        Date = DateTime.Now,
                        Description = res,
                        Name = @"Json Request"
                    });
                    model.SubmitChanges();

                    foreach (string ip1 in inv.ip)
                    {
                        if(ip1.StartsWith("10."))
                        {                          
                            var DomainUser = Lib.ADAuth.GetUserFromAD(inv.sAMAccountName);
                            Models.Login l = new Models.Login()
                            {
                                sAMAccountName = inv.sAMAccountName,
                                IPAddress = ip1,
                                Date = DateTime.Now,
                                OsVersion = inv.osversion,
                                Email = DomainUser.email != null ? DomainUser.email : "",
                                FIO = DomainUser.FIO != null ? DomainUser.FIO : "",
                                DameWare = bool.Parse(inv.dameware),
                                CPU_freq = inv.CPU_freq != null ? int.Parse(inv.CPU_freq):0,
                                GPU_HR = inv.GPU_HR != null ? int.Parse(inv.GPU_HR):0,
                                GPU_NAME = inv.GPU_NAME,
                                CPU_name = inv.CPU_name,
                                GPU_RAM = inv.GPU_RAM != null ? int.Parse(inv.GPU_RAM):0,
                                CompName = inv.compname,
                                errors = inv.errors,
                                osarch = inv.osarch,
                                RAM = inv.RAM != null ? int.Parse(inv.RAM):0,
                                GPU_VR = inv.GPU_VR != null ? int.Parse(inv.GPU_VR):0,
                                ProfileSize = inv.userprofilesize,
                                ProfileName = inv.userprofile
                            };
                            model.Logins.InsertOnSubmit(l);
                            model.SubmitChanges();

                            int TotalDiskSize = 0;
                            int TotalFreeSize = 0;
                            string DriveInterfaces = "";
                            int DrivesCount = 0;

                            ///---Отбрабатываю данные по почте
                            if(inv.bases != null && inv.bases.Count > 0)
                            {
                                foreach(var b in inv.bases)
                                {
                                    Models.OdinC odinC = new Models.OdinC() {
                                        Info = b.Info,
                                        Name = b.Name
                                    };
                                    l.OdinCs.Add(odinC);
                                    model.SubmitChanges();
                                }
                            }

                            ///---Обрабатываю данные по почте
                            if(inv.mail != null && inv.mail.Count > 0)
                            {
                                int TotalMailSize = 0;
                                foreach(var m in inv.mail)
                                {
                                    var mail = new Models.Mail() {
                                        lable = m.lable,
                                        eml = string.IsNullOrEmpty(m.eml) ? 0: int.Parse(m.eml),
                                        dbx = string.IsNullOrEmpty(m.dbx) ? 0: int.Parse(m.dbx)
                                    };
                                    TotalMailSize += mail.eml.Value;
                                    TotalMailSize += mail.dbx.Value;

                                    l.Mails.Add(mail);
                                    model.SubmitChanges();
                                }
                                l.TotalMailSize = TotalMailSize;
                                model.SubmitChanges();
                            }

                            ///---Сохраняю физические диски
                            if(inv.HDDs != null && inv.HDDs.Count > 0)
                            {
                                foreach (var dd in inv.HDDs)
                                {
                                    var diskDrive = new Models.DiskDrive()
                                    {
                                        Interface = dd.disc_interface,
                                        Size = int.Parse(dd.disc_size),
                                        Name = dd.disc_name
                                    };
                                    l.DiskDrives.Add(diskDrive);
                                    model.SubmitChanges();

                                    ///---Сохраняю логические диски
                                    foreach (var ld in dd.LogicalDiscs)
                                    {
                                        TotalDiskSize += int.Parse(ld.Size);
                                        TotalFreeSize += int.Parse(ld.Free);

                                        var lDisk = new Models.LogicalDisk();
                                        lDisk.Free = int.Parse(ld.Free);
                                        lDisk.Size = int.Parse(ld.Size);
                                        lDisk.Label = ld.Lable;
                                        diskDrive.LogicalDisks.Add(lDisk);
                                        model.SubmitChanges();                                                                    
                                    }

                                    DrivesCount += 1;
                                    DriveInterfaces += dd.disc_interface + ",";
                                }

                                l.TotalFreeSize = TotalFreeSize;
                                l.TotalHDDSize = TotalDiskSize;
                                l.DriveCount = DrivesCount;
                                l.DriveInterfaces = DriveInterfaces;
                                model.SubmitChanges();
                            }

                            ///---Сохраняю принтеры
                            if(inv.Printers != null && inv.Printers.Count > 0)
                            {
                                foreach (var p in inv.Printers)
                                {
                                    l.Printers.Add(new Models.Printer() {
                                        Default = p.Default,
                                        Network = p.Network,
                                        Server = p.Server,
                                        ShareName = p.ShareName,
                                        Name = p.Name                              
                                    });

                                    if(p.Default.ToLower() == "true")
                                    {
                                        l.DefaultPrinterName = p.Name;
                                    }

                                    model.SubmitChanges();
                                }
                            }

                            ///---Сохраняю программы
                            if(inv.programs != null && inv.programs.Count > 0)
                            {
                                foreach (var pr in inv.programs)
                                {
                                    l.UserPrograms.Add(new Models.UserProgram() {
                                        Name = pr.Name
                                    });
                                }
                                model.SubmitChanges();
                            }

                            ///---Сохраняю файлы
                            if(inv.allfiles != null && inv.allfiles.Count > 0)
                            {
                                foreach (var f in inv.allfiles)
                                {
                                    l.UserFiles.Add(new Models.UserFile() {
                                        Ext = f.Ext,
                                        Lable = f.Lable,
                                        Size = f.Size
                                    });
                                }
                                model.SubmitChanges();
                            }

                        }
                    }
                    model.SubmitChanges();

                    //List<string> patval = res.Trim().Trim('}').Trim('{').Split(',').ToList();

                    //string sAMAccountName = "";
                    //List<string> ip = new List<string>();
                    //string osversion = "";
                    //string dameware = "";
                    //string osarch = "";
                    //string RAM = "";
                    //string CPU_name = "";
                    //string CPU_freq = "";
                    //string GPU_NAME = "";
                    //string GPU_RAM = "";
                    //string GPU_HR = "";
                    //string GPU_VR = "";
                    //string HDDS = "";
                    //string error = "";
                    //bool bdameware = false;




                    //foreach (string pv in patval)
                    //{
                    //    if (pv.Split(':')[0].Trim().ToLower().Trim('"').Trim('\'') == "samaccountname") sAMAccountName = pv.Split(':')[1].Trim().Trim('"').Trim('\'');
                    //    if (pv.Split(':')[0].Trim().ToLower().Trim('"').Trim('\'') == "osversion") osversion = pv.Split(':')[1].Trim().Trim('"').Trim('\'');

                    //    if (pv.Split(':')[0].Trim().ToLower().Trim('"').Trim('\'') == "osarch") osarch = pv.Split(':')[1].Trim().Trim('"').Trim('\'');
                    //    if (pv.Split(':')[0].Trim().ToLower().Trim('"').Trim('\'') == "ram") RAM = pv.Split(':')[1].Trim().Trim('"').Trim('\'');
                    //    if (pv.Split(':')[0].Trim().ToLower().Trim('"').Trim('\'') == "cpu_name") CPU_name = pv.Split(':')[1].Trim().Trim('"').Trim('\'');
                    //    if (pv.Split(':')[0].Trim().ToLower().Trim('"').Trim('\'') == "cpu_freq") CPU_freq = pv.Split(':')[1].Trim().Trim('"').Trim('\'');
                    //    if (pv.Split(':')[0].Trim().ToLower().Trim('"').Trim('\'') == "gpu_name") GPU_NAME = pv.Split(':')[1].Trim().Trim('"').Trim('\'');
                    //    if (pv.Split(':')[0].Trim().ToLower().Trim('"').Trim('\'') == "gpu_ram") GPU_RAM = pv.Split(':')[1].Trim().Trim('"').Trim('\'');
                    //    if (pv.Split(':')[0].Trim().ToLower().Trim('"').Trim('\'') == "gpu_hr") GPU_HR = pv.Split(':')[1].Trim().Trim('"').Trim('\'');
                    //    if (pv.Split(':')[0].Trim().ToLower().Trim('"').Trim('\'') == "gpu_vr") GPU_VR = pv.Split(':')[1].Trim().Trim('"').Trim('\'');
                    //    if (pv.Split(':')[0].Trim().ToLower().Trim('"').Trim('\'') == "error") error = pv.Split(':')[1].Trim().Trim('"').Trim('\'');


                    //    if (pv.Split(':')[0].Trim().ToLower().Trim('"').Trim('\'') == "hdds")
                    //    {
                    //        HDDS = "";
                    //        List<string> rips = pv.Split(':')[1].Trim().Trim('[').Trim(']').Split(',').ToList();
                    //        foreach (string rip in rips)
                    //        {
                    //            HDDS += rip.Trim('"').Trim('\'');
                    //        }
                    //    }

                    //    if (pv.Split(':')[0].Trim().ToLower().Trim('"').Trim('\'') == "ip")
                    //    {
                    //        List<string> rips = pv.Split(':')[1].Trim().Trim('[').Trim(']').Split(',').ToList();
                    //        foreach (string rip in rips)
                    //        {
                    //            ip.Add(rip.Trim('"').Trim('\''));
                    //        }
                    //    }

                    //    if (pv.Split(':')[0].Trim().ToLower().Trim('"').Trim('\'') == "dameware")
                    //    {
                    //        dameware = pv.Split(':')[1].Trim().Trim('"').Trim('\'');
                    //        bdameware = bool.Parse(dameware);
                    //    }


                    //}


                    //foreach (string ip1 in ip)
                    //{
                    //    var DomainUser = Lib.ADAuth.GetUserFromAD(sAMAccountName);
                    //    model.Logins.InsertOnSubmit(new Models.Login()
                    //    {
                    //        sAMAccountName = sAMAccountName,
                    //        IPAddress = ip1,
                    //        Date = DateTime.Now,
                    //        OsVersion = osversion,
                    //        Email = DomainUser.email != null? DomainUser.email:"",
                    //        FIO = DomainUser.FIO != null ? DomainUser.FIO : "",
                    //        DameWare = bdameware,
                    //        CPU_freq = CPU_freq,
                    //        GPU_HR = GPU_HR,
                    //        GPU_NAME = GPU_NAME,
                    //        CPU_name = CPU_name,
                    //        GPU_RAM = GPU_RAM,
                    //        errors = error,
                    //        HDDS = HDDS,
                    //        osarch = osarch,
                    //        RAM = RAM,
                    //        GPU_VR = GPU_VR                            
                    //    });
                    //}
                    //model.SubmitChanges();
                }
                catch (Exception ex)
                {
                    model.EventLogs.InsertOnSubmit(new Models.EventLog()
                    {
                        Date = DateTime.Now,
                        Description = ex.Message,
                        Name = @"Json Request exception"
                    });
                    model.SubmitChanges();
                }
            }            
            return "Ok";
        }

        [HttpPost]
        [Route("api/getUserServers")]
        public List<Models.LogonServers> getUserServers(Models.UserLogin user)
        {
            List<Models.LogonServers> servers = new List<Models.LogonServers>();

            string sAMAccountName = "";
            if (user.domain.StartsWith("KREMEN")) sAMAccountName = "KREMEN" + @"\" + user.username;
            if (user.domain.StartsWith("UKRTRANSNAFTA")) sAMAccountName = "UKRTRANSNAFTA" + @"\" + user.username;
            if (user.domain.StartsWith("ODESSA")) sAMAccountName = "ODESSA" + @"\" + user.username;
            if (user.domain.StartsWith("DRUZHBA_AD")) sAMAccountName = "DRUZHBA_AD" + @"\" + user.username;

            using (Models.PhonesDataContext model = new Models.PhonesDataContext())
            {
                servers = model.Logins.Where(x => x.Date >= DateTime.Today
                && x.sAMAccountName == sAMAccountName)
                    .OrderByDescending(x=>x.Date.Value)
                    .Select(x => new Models.LogonServers()
                    {
                        ServerIP = x.IPAddress,
                        LogonDate = x.Date.Value,
                        OSVersion = x.OsVersion
                    }).ToList();
            }
            return servers;
        }

        [HttpPost]
        [Route("api/remoteControlAvailable")]
        public bool remoteControlAvailable(Models.LogonServers server)
        {
            int port = int.Parse(System.Configuration.ConfigurationManager.AppSettings["RemoteControlPort"]);
            return Lib.TcpHelper.PortIsAvailable(server.ServerIP, port);
        }

        [HttpPost]
        [ActionName("BindPhone")]
        public HttpResponseMessage BindPhone(string xxxx)
        {
            HttpResponseMessage message = new HttpResponseMessage();
            return message;
        }

        [HttpGet]
        [Route("api/Resource/{imageName}")]
        public HttpResponseMessage GetImage(string imageName)
        {
            string imagePath = System.Configuration.ConfigurationManager.AppSettings["ResourcePath"].ToString();
            
            using (System.Drawing.Image img = System.Drawing.Image.FromFile(System.IO.Path.Combine(imagePath, "Photos","AU",imageName)))
            {
                using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
                {
                    img.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    HttpResponseMessage result = new HttpResponseMessage(HttpStatusCode.OK);
                    result.Content = new ByteArrayContent(ms.ToArray());
                    result.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpg");
                    return result;
                }
            }
        }

        [HttpGet]
        [Route("api/map/{mapName}")]
        public HttpResponseMessage GetMapAU(string mapName)
        {
            string imagePath = System.Configuration.ConfigurationManager.AppSettings["ResourcePath"].ToString();
            string mapPath = System.IO.Path.Combine(imagePath, "Maps","AU", mapName);

            HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StreamContent(new FileStream(mapPath, FileMode.Open, FileAccess.Read));
            response.Content.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("attachment");
            response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/svg+xml");
            return response;          
        }
    }   
}
