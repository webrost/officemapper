using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.DirectoryServices;
using System.DirectoryServices.Protocols;
using System.Net;
using System.DirectoryServices.AccountManagement;

namespace OfficeMapper.Lib
{
    public class ADAuth
    {       
        public static Models.AuthUser Authenticate(string username, string password, string domain)
        {
            Models.AuthUser user = null;
            string controller = "";
            switch(domain.ToLower())
            {
                case "kremen":
                    controller = System.Configuration.ConfigurationManager.AppSettings["kremenDC"];
                    break;
                case "odessa":
                    controller = System.Configuration.ConfigurationManager.AppSettings["odessaDC"];
                    break;
                case "lviv":
                    controller = System.Configuration.ConfigurationManager.AppSettings["lvivDC"];
                    break;
                case "kyiv":
                    controller = System.Configuration.ConfigurationManager.AppSettings["kyivDC"];
                    break;
            }


            try
            {
                LdapConnection lcon = new LdapConnection(controller);
                NetworkCredential nc = new NetworkCredential(username, password, domain);
                lcon.Credential = nc;
                lcon.AuthType = AuthType.Negotiate;
                // user has authenticated at this point,
                // as the credentials were used to login to the dc.
                lcon.Bind(nc);

                ///---TODO Вычитываем данные из домена для сохранения в базе
                user = new Models.AuthUser();

                DirectoryEntry de = new DirectoryEntry(@"LDAP://"+controller,username,password);
                DirectorySearcher ds = new DirectorySearcher(de);
                ds.SearchScope = System.DirectoryServices.SearchScope.Subtree;
                ds.Filter = "(&(objectCategory=User) (samAccountName=" + username + "))";
                ds.PropertiesToLoad.Add("mail");
                ds.PropertiesToLoad.Add("phone");
                SearchResult result = ds.FindOne();

                user.UTNLogin = username + @"@" + domain;
                user.domain = domain;
                user.email = result.Properties["mail"].Count > 0? result.Properties["mail"][0].ToString():"";
                user.FIO = result.Path.Split(',').Count() > 0
                    ? result.Path.Split(',')[0].Split('=').Count() > 1
                        ? result.Path.Split(',')[0].Split('=')[1]
                        : ""
                    : "";
                ///---Если прошли аутентификацию сохраняем пользователя в базе    
                SaveUser(user);
            }
            catch (LdapException ex)
            {
                user = null;
            }
            return user;
        }

        public static void SaveUser(Models.AuthUser user)
        {
            using (OfficeMapper.Models.PhonesDataContext model = new Models.PhonesDataContext())
            {
                if(model.Users.Count(x=>x.UTNLogin == user.UTNLogin) == 0)
                {
                    model.Users.InsertOnSubmit(new Models.User() {
                        UTNLogin = user.UTNLogin,
                        Domain = user.domain,
                        FIO = user.FIO,
                        Email = user.email                        
                    });
                    model.SubmitChanges();
                }                                       
            }
        }

        public static Models.AuthUser GetUser(string username)
        {
            Models.AuthUser user = null;
            using (OfficeMapper.Models.PhonesDataContext model = new Models.PhonesDataContext())
            {
                if (model.Users.Count(x => x.UTNLogin == username) > 0)
                {
                    user = new Models.AuthUser();
                    user = model.Users.Where(x => x.UTNLogin == username)
                        .ToList()
                        .Select(x => new Models.AuthUser()
                        {
                            UTNLogin = x.UTNLogin,
                            domain = x.Domain,
                            email = x.Email,
                            FIO = x.FIO,
                            post = x.Post,
                            ITResponsible = model.Acceptors.Count(f => f.username == x.UTNLogin && f.serviceDomain != null) > 0
                        }).First();                                              
                }
            }
            return user;
        }

        public static Models.AuthUser GetUserByAccessKey(string accessKey)
        {
            Models.AuthUser user = null;
            using (OfficeMapper.Models.PhonesDataContext model = new Models.PhonesDataContext())
            {
                if (model.Users.Count(x => x.AccessKey == accessKey) > 0)
                {
                    user = new Models.AuthUser();
                    user = model.Users.Where(x => x.AccessKey == accessKey)
                        .ToList()
                        .Select(x => new Models.AuthUser()
                        {
                            UTNLogin = x.UTNLogin,
                            domain = x.Domain,
                            email = x.Email,
                            FIO = x.FIO,
                            post = x.Post,
                            ITResponsible = model.Acceptors.Count(f=>f.username == x.UTNLogin && f.serviceDomain != null )>0
                        }).First();
                }
            }
            return user;
        }

        public static Models.AuthUser GetUserByEmail(string email)
        {
            Models.AuthUser user = null;
            using (OfficeMapper.Models.PhonesDataContext model = new Models.PhonesDataContext())
            {
                if (model.Users.Count(x => x.Email == email) > 0)
                {
                    user = new Models.AuthUser();
                    user = model.Users.Where(x => x.Email == email)
                        .Select(x => new Models.AuthUser()
                        {
                            UTNLogin = x.UTNLogin,
                            domain = x.Domain,
                            email = x.Email,
                            FIO = x.FIO,
                            post = x.Post
                        }).First();
                }
            }
            return user;
        }

        /// <summary>
        /// Возвращает данные пользователя из AD
        /// </summary>
        /// <param name="login"></param>
        /// <returns></returns>
        public static Models.AuthUser GetUserFromAD(string login)
        {
            Models.AuthUser user = new Models.AuthUser();
            string domainNetBIOS = login.Split('\\')[0];
            string samAccountName = login.Split('\\')[1];
            string controller = "";
            string username = "";
            string password = "";
            string domain = "";
            string ldap = "";
            try { 
            switch (domainNetBIOS)
            {
                case "UKRTRANSNAFTA":
                    controller = System.Configuration.ConfigurationManager.AppSettings["kyivDC"];
                    username = "anisimov";
                    password = "";
                    domain = "ukrtransnafta.com";
                    ldap = "DC=ukrtransnafta,DC=com";
                    break;
                case "ODESSA":
                    controller = System.Configuration.ConfigurationManager.AppSettings["odessaDC"];
                    username = "anisimov";
                    password = "";
                    domain = "odessa.ukrtransnafta.com";
                    ldap = "DC=odessa,DC=ukrtransnafta,DC=com";
                    break;
                case "KREMEN":
                    controller = System.Configuration.ConfigurationManager.AppSettings["kremenDC"];
                    username = "demon1";
                    password = "";
                    domain = "kremen.ukrtransnafta.com";
                    ldap = "DC=kremen,DC=ukrtransnafta,DC=com";
                    break;
                case "DRUZHBA_AD":
                    controller = System.Configuration.ConfigurationManager.AppSettings["lvivDC"];
                    username = "ranisimov";
                    password = "";
                    domain = "druzhba.ukrtransnafta.com";
                    ldap = "DC=druzhba,DC=ukrtransnafta,DC=com";
                    break;
                case "DRUZHBA":
                    controller = System.Configuration.ConfigurationManager.AppSettings["lvivDCOLD"];
                    domain = "druzhba.lviv.ua";
                    ldap = "DC=druzhba,DC=lviv,DC=ua";

                    var ldapPort = 389; 
                    var pageSize = 1000;

                    var openLDAPHelper = new LDAPHelper(
                        ldap,
                        controller,
                        ldapPort,
                        AuthType.Basic,
                        pageSize);

                    var searchFilter = "uid="+ samAccountName;
                    var attributesToLoad = new[] { "mail", "cn","displayName" };
                    var pagedSearchResults = openLDAPHelper.PagedSearch(
                        searchFilter,
                        attributesToLoad);

                    foreach (var searchResultEntryCollection in pagedSearchResults)
                        foreach (SearchResultEntry searchResultEntry in searchResultEntryCollection)
                        {
                            if(searchResultEntry.Attributes.Count > 0)
                            {
                                user.email = searchResultEntry.Attributes["mail"][0].ToString();
                                user.FIO = searchResultEntry.Attributes["displayName"][0].ToString();
                            }
                        }
                    break;
            }

            if(domainNetBIOS != "DRUZHBA")
            {
                PrincipalContext ctx = new PrincipalContext(ContextType.Domain, controller);
                UserPrincipal u = UserPrincipal.FindByIdentity(ctx, samAccountName);
                if (u != null)
                {
                    user.email = u.EmailAddress;
                    user.FIO = u.DisplayName;
                }
            }
            }
            catch (Exception ex)
            {

            }
            return user;
        }

        public static void ImportAD()
        {
            using (OfficeMapper.Models.PhonesDataContext model = new Models.PhonesDataContext())
            {
                List<string> controllers = new List<string>();
                controllers.Add(System.Configuration.ConfigurationManager.AppSettings["kyivDC"]);
                controllers.Add(System.Configuration.ConfigurationManager.AppSettings["kremenDC"]);
                controllers.Add(System.Configuration.ConfigurationManager.AppSettings["odessaDC"]);
                controllers.Add(System.Configuration.ConfigurationManager.AppSettings["lvivDC"]);

                List<string> users = new List<string>();
                users.Add("anisimov");
                users.Add("demon1");
                users.Add("anisimov");
                users.Add("ranisimov");

                List<string> passwords = new List<string>();
                passwords.Add("");
                passwords.Add("");
                passwords.Add("");
                passwords.Add("");

                List<string> domains = new List<string>();
                domains.Add("ukrtransnafta.com");
                domains.Add("kremen.ukrtransnafta.com");
                domains.Add("odessa.ukrtransnafta.com");
                domains.Add("druzhba.ukrtransnafta.com");

                List<string> domainAlias = new List<string>();
                domainAlias.Add("kyiv");
                domainAlias.Add("kremen");
                domainAlias.Add("odesa");
                domainAlias.Add("lviv");

                List<string> ldap = new List<string>();
                ldap.Add("OU=Укртранснафта,DC=ukrtransnafta,DC=com");
                ldap.Add("OU=1UsersUTN,DC=kremen,DC=ukrtransnafta,DC=com");
                ldap.Add("OU=1UsersUTN,DC=odessa,DC=ukrtransnafta,DC=com");
                ldap.Add("CN=Users,DC=druzhba,DC=ukrtransnafta,DC=com");

                for (int i=0;i<4;i++)
                {

                    LdapConnection lcon = new LdapConnection(controllers[i]);
                    NetworkCredential nc = new NetworkCredential(users[i], passwords[i], domains[i]);
                    lcon.Credential = nc;
                    lcon.AuthType = AuthType.Negotiate;
                    lcon.Bind(nc);

                    DirectoryEntry de = new DirectoryEntry(@"LDAP://" + controllers[i] + @"/" +  ldap[i], users[i], passwords[i]);
                    DirectorySearcher ds = new DirectorySearcher(de);
                    ds.SearchScope = System.DirectoryServices.SearchScope.Subtree;
                    ds.Filter = "(&(objectCategory=User))";
                    ds.PropertiesToLoad.Add("mail");
                    ds.PropertiesToLoad.Add("phone");
                    ds.PropertiesToLoad.Add("sAMAccountName");
                    ds.PropertiesToLoad.Add("lastLogon");
                    SearchResultCollection collection = ds.FindAll();

                    foreach(SearchResult result in collection)
                    {
                        if(result.Properties["lastLogon"].Count > 0
                            && result.Properties["sAMAccountName"].Count > 0
                            )
                        {
                            DateTime lastLogon = DateTime.FromFileTime((long)result.Properties["lastLogon"][0]);
                            if(lastLogon > DateTime.Now.AddMonths(-12))
                            {
                                string sAMAccountName = result.Properties["sAMAccountName"][0].ToString();

                                string name = result.Properties["sAMAccountName"][0] + @"@" + domainAlias[i];
                                string domain = domains[i];
                                string email = result.Properties["mail"].Count > 0 ? result.Properties["mail"][0].ToString() : "";
                                string FIO = result.Path.Split(',').Count() > 0
                                    ? result.Path.Split(',')[0].Split('=').Count() > 1
                                        ? result.Path.Split(',')[0].Split('=')[1]
                                        : ""
                                    : "";

                                ///---Добавляю только записи новые
                                if (model.Users.Count(x => x.UTNLogin == name) == 0)
                                {
                                    model.Users.InsertOnSubmit(new Models.User()
                                    {
                                        UTNLogin = name,
                                        AccessKey = Guid.NewGuid().ToString(),
                                        AccessKeyCreateTime = DateTime.Now,
                                        Domain = domainAlias[i],
                                        Email = email,
                                        FIO = FIO
                                    });
                                }
                            }
                        }                       
                    }
                    model.SubmitChanges();
                }

            }
        }
    }
}