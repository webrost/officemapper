using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace OfficeMapper.Controllers
{
    public class DataController : ApiController
    {
        [HttpGet]
        [Route("api/data/userbydomain")]
        public List<Models.Data.DomainData> UsersByDomain()
        {
            string[] domains = { "DRUZHBA","DRUZHBA_AD", "UKRTRANSNAFTA", "KREMEN", "ODESSA"  };
            List<Models.Data.DomainData> ret = new List<Models.Data.DomainData>();
            using (Models.PhonesDataContext model = new Models.PhonesDataContext())
            {
                foreach(var d in domains)
                {
                    ret.Add(new Models.Data.DomainData()
                    {
                        count = model.Logins
                        .Where(x => x.Date >= DateTime.Today)
                        .Select(x => new Models.OnlineUser()
                        {
                            Login = x.sAMAccountName,
                            FIO = x.FIO,
                            Email = x.Email
                        }).Distinct()
                        .Count(x=>x.Login.StartsWith(d)),
                        domain = d
                    });
                }
            }
            return ret;
        }

        [HttpGet]
        [Route("api/data/newdomain")]
        public List<Models.Data.DomainData> newdomain()
        {
            string newDomainName = System.Configuration.ConfigurationManager.AppSettings["NewDomainName"].ToString();
            List<Models.Data.DomainData> ret = new List<Models.Data.DomainData>();
            using (Models.PhonesDataContext model = new Models.PhonesDataContext())
            {
                    ret.Add(new Models.Data.DomainData()
                    {
                        count = model.Logins
                        .Where(x => x.Date >= DateTime.Today)
                        .Select(x => new Models.OnlineUser()
                        {
                            Login = x.sAMAccountName,
                            FIO = x.FIO,
                            Email = x.Email
                        }).Distinct()
                        .Count(x => x.Login.StartsWith(newDomainName)),
                        domain = "НОВЫЙ ДОМЕН"
                    });

                ret.Add(new Models.Data.DomainData()
                {
                        count = model.Logins
                        .Where(x => x.Date >= DateTime.Today)
                        .Select(x => new Models.OnlineUser()
                        {
                            Login = x.sAMAccountName,
                            FIO = x.FIO,
                            Email = x.Email
                        }).Distinct()
                        .Count(x => !x.Login.StartsWith(newDomainName)),
                    domain = "СТАРЫЕ ДОМЕНЫ"
                });
            }
            return ret;
        }

        [HttpGet]
        [Route("api/data/os")]
        public List<Models.Data.DomainData> os()
        {
            string[] domains = { "Windows XP", "Windows 7", "Windows 8", "Windows 10", "Windows 2000" };
            List<Models.Data.DomainData> ret = new List<Models.Data.DomainData>();
            using (Models.PhonesDataContext model = new Models.PhonesDataContext())
            {
                foreach (var d in domains)
                {
                    ret.Add(new Models.Data.DomainData()
                    {
                        count = model.Logins
                        .Count(x => x.Date >= DateTime.Today 
                        && x.OsVersion.ToLower().Contains(d.ToLower())),
                        domain = d
                    });
                }
            }
            return ret;
        }

        [HttpGet]
        [Route("api/data/uniqueUsersPerMonth")]
        public List<Models.Data.DomainData> uniqueUsersPerMonth()
        {
            string[] domains = { "DRUZHBA", "DRUZHBA_AD", "UKRTRANSNAFTA", "KREMEN", "ODESSA" };
            List<Models.Data.DomainData> ret = new List<Models.Data.DomainData>();
            using (Models.PhonesDataContext model = new Models.PhonesDataContext())
            {
                foreach (var d in domains)
                {
                    ret.Add(new Models.Data.DomainData()
                    {
                        count = model.Logins
                        .Where(x => x.Date >= DateTime.Today.AddMonths(-2))
                        .Select(x => new Models.OnlineUser()
                        {
                            Login = x.sAMAccountName
                        }).Distinct()
                        .Count(x => x.Login.StartsWith(d)),
                        domain = d
                    });
                }
            }
            return ret;
        }
    }
}
