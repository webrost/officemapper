using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.IO;
using System.Globalization;
using System.Web;
using System.ServiceModel.Channels;


namespace OfficeMapper.Controllers
{
    public class DepController : ApiController
    {
        
        /// <summary>
        /// Возвращает список подразделений 1-го уровня (функции)
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("api/structure")]
        public List<Models.StructureUnit> GetStructure()
        {
            List<Models.StructureUnit> ret = new List<Models.StructureUnit>();
            using (Models.PhonesDataContext model = new Models.PhonesDataContext())
            {
                ret = model.Phones.Where(x=>!x.Code.Contains(".")).Select(x => new Models.StructureUnit() {
                    Code = x.Code,
                    Level = 1,
                    Name = x.Dep,
                    FunctionDescription = x.Function,
                    Type = "dep"                                   
                })
                .ToList()
                .OrderBy(x=>int.Parse(x.Code))
                .ToList();
            }
            return ret;
        }

        /// <summary>
        /// Возвращает список всех сотрудников
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("api/users")]
        public List<Models.StructureUnit> GetUsers()
        {
            List<Models.StructureUnit> ret = new List<Models.StructureUnit>();
            using (Models.PhonesDataContext model = new Models.PhonesDataContext())
            {
                ret = model.Phones.Where(x => x.Dep == null)
                    .Select(x => new Models.StructureUnit()
                {
                    Code = x.Code.Replace(".","-"),
                    Level = 1,
                    Name = x.PIB,
                    FunctionDescription = x.Function,
                    Type = "user"
                }).ToList();
            }
            return ret;
        }

        /// <summary>
        /// Возвращает список всех сотрудников
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("api/user/{id}/{filialCode}")]
        public Models.StructureUnit GetUser(string id, string filialCode)
        {
            Models.StructureUnit ret = new Models.StructureUnit();
            using (Models.PhonesDataContext model = new Models.PhonesDataContext())
            {
                ret = model.Phones.Where(x => x.Code == id.Replace("-",".") && x.Dep == null)
                    .ToList()
                    .Select(x => new Models.StructureUnit()
                    {
                        Code = x.Code.Replace(".", "-"),
                        Level = id.Trim().Split('.').Count() + 1,
                        Current = false,
                        Name = x.PIB,
                        Type = "user",
                        FunctionDescription = x.Function,
                        BirthDate = x.Birthday,
                        Email = x.Email,
                        Mobile = x.Mobile,
                        Phones = x.Phone1,
                        Post = GetFullPost(x.Code),
                        //Photo = Photo(x.Email),
                        PhotoFileName = PhotoName(x.Email)
                    }).ToList().First();
            }
            return ret;
        }

        /// <summary>
        /// Возвращает указанное подразделение, его предков и потомков 1-го уровня
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("api/department/{id}/{filialCode}")]
        public List<Models.StructureUnit> GetDepartment(string id, string filialCode)
        {
            List<Models.StructureUnit> ret = new List<Models.StructureUnit>();
            switch(filialCode)
            {
                case "1":
                    ret = GetAUDepartments(id);
                    break;
                case "2":
                    ret = GetDruzhbaDepartments(id);
                    break;
            }
            return ret;
        }

        /// <summary>
        /// Возвращает Структуру по АУ
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private List<Models.StructureUnit> GetAUDepartments(string id)
        {
            List<Models.StructureUnit> ret = new List<Models.StructureUnit>();
            id = id.Replace("-", ".");
            using (Models.PhonesDataContext model = new Models.PhonesDataContext())
            {
                ///---Проверяю есть ли такое подразделение-объект в базе
                if (model.Phones.Where(x => x.Code == id.Trim()).Count() > 0
                    && model.Phones.First(x => x.Code == id.Trim()).Dep != null)
                {
                    ///---Нахожу всех его предков (для отображения структуры)
                    if (id.Trim().Split('.').Count() > 1)
                    {
                        string codePrefix = "";
                        for (int i = 0; i < id.Trim().Split('.').Count(); i++)
                        {
                            if (i == 0)
                            {
                                codePrefix += id.Trim().Split('.')[i];
                            }
                            else
                            {
                                codePrefix += "." + id.Trim().Split('.')[i];
                            }


                            ///---Проверяю - существует ли родительский элемент и добавляю его
                            if (model.Phones.Where(x => x.Code == (codePrefix).Trim()).Count() > 0)
                            {
                                ret.Add(
                                model.Phones.Where(x => x.Code == (codePrefix).Trim())
                                    .Select(x => new Models.StructureUnit()
                                    {
                                        Code = x.Code,
                                        Level = i,
                                        Current = false,
                                        Name = x.Dep,
                                        Type = "dep",
                                        FunctionDescription = x.Function
                                    }
                                    )
                                    .First());
                            }
                        }
                    }

                    ///--Добавляю текущий элемент
                    //ret.Add(
                    //    model.Phones.Where(x => x.Code == id.Trim())
                    //        .Select(x => new Models.StructureUnit()
                    //        {
                    //            Code = x.Code,
                    //            Level = id.Trim().Split('.').Count(),
                    //            Current = true,
                    //            Name = x.Dep,
                    //            Type = "dep",
                    //            FunctionDescription = x.Function
                    //        }).First()
                    //    );

                    ///---Добавляю дочерние элементы-подразделения
                    if (model.Phones.Where(x => x.Code.StartsWith(id.Trim() + ".") && x.Dep != null).Count() > 0)
                    {
                        foreach (var dep in model.Phones.Where(x => x.Code.StartsWith(id.Trim() + ".") && x.Dep != null)
                            .ToList()
                            .Select(x => new Models.StructureUnit()
                            {
                                Code = x.Code,
                                Level = x.Code.Trim().Split('.').Count(),
                                Current = false,
                                Name = x.Dep,
                                Type = "dep",
                                FunctionDescription = x.Function
                            }).ToList())
                        {
                            ret.Add(dep);
                        }
                    }

                    ///---Добавляю дочерние элементы-сотрудники
                    if (model.Phones.Where(x => x.Code.StartsWith(id.Trim() + ".") && x.PIB != null).Count() > 0)
                    {
                        string startWith = id.Trim() + ".";
                        var users = model.Phones.Where(x => x.Code.StartsWith(startWith) && x.PIB != null).ToList();

                        foreach (var user in users
                            .Select(x => new Models.StructureUnit()
                            {
                                Code = x.Code.Replace(".", "-"),
                                Level = id.Trim().Split('.').Count() + 1,
                                Current = false,
                                Name = x.PIB,
                                Type = "user",
                                FunctionDescription = x.Function,
                                BirthDate = x.Birthday,
                                Email = x.Email,
                                Mobile = x.Mobile,
                                Phones = x.Phone1,
                                Post = GetFullPost(x.Code),
                                //Photo = Photo(x.Email),
                                PhotoFileName = PhotoName(x.Email)
                            }).ToList())
                        {
                            ret.Add(user);
                        }
                    }
                }
            }
            return ret;
        }

        /// <summary>
        /// Возвращает дерево структуры АУ
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("api/dt/{function}")]
        public List<Models.StructureUnit> GetAUTree(string function)
        {
            List<Models.StructureUnit> ret = new List<Models.StructureUnit>();
            Models.StructureUnit su = new Models.StructureUnit();
            su.Code = function;
            ret = BuilAUTree(su);
            //using (Models.PhonesDataContext model = new Models.PhonesDataContext())
            //{
            //        ///---Добавляю дочерние элементы-подразделения
            //        if (model.Phones.Where(x => x.Code.StartsWith(id.Trim() + ".") && x.Dep != null).Count() > 0)
            //        {
            //            foreach (var dep in model.Phones.Where(x => x.Code.StartsWith(id.Trim() + ".") && x.Dep != null)
            //                .ToList()
            //                .Select(x => new Models.StructureUnit()
            //                {
            //                    Code = x.Code,
            //                    Level = x.Code.Trim().Split('.').Count(),
            //                    Current = false,
            //                    Name = x.Dep,
            //                    Type = "dep",
            //                    FunctionDescription = x.Function
            //                }).ToList())
            //            {
            //                ret.Add(dep);
            //            }
            //        }

            //        ///---Добавляю дочерние элементы-сотрудники
            //        if (model.Phones.Where(x => x.Code.StartsWith(id.Trim() + ".") && x.PIB != null).Count() > 0)
            //        {
            //            string startWith = id.Trim() + ".";
            //            var users = model.Phones.Where(x => x.Code.StartsWith(startWith) && x.PIB != null).ToList();

            //            foreach (var user in users
            //                .Select(x => new Models.StructureUnit()
            //                {
            //                    Code = x.Code.Replace(".", "-"),
            //                    Level = id.Trim().Split('.').Count() + 1,
            //                    Current = false,
            //                    Name = x.PIB,
            //                    Type = "user",
            //                    FunctionDescription = x.Function,
            //                    BirthDate = x.Birthday,
            //                    Email = x.Email,
            //                    Mobile = x.Mobile,
            //                    Phones = x.Phone1,
            //                    Post = GetFullPost(x.Code),
            //                    //Photo = Photo(x.Email),
            //                    PhotoFileName = PhotoName(x.Email)
            //                }).ToList())
            //            {
            //                ret.Add(user);
            //            }
            //        }                
            //}
            return ret;

        }

        private List<Models.StructureUnit> BuilAUTree(Models.StructureUnit item)
        {
            List<Models.StructureUnit> ret = new List<Models.StructureUnit>();
            using (Models.PhonesDataContext model = new Models.PhonesDataContext())
            {
                if (model.Phones.ToList().Count(x=> x.Code != null 
                && x.Code.StartsWith(item.Code+".")
                && x.Code.Split('.').Count() == (item.Code.Split('.').Count() + 1)
                && x.Dep != null
                ) > 0)
                {
                    foreach(var subItem in model.Phones.ToList().Where(x=> x.Code != null 
                    && x.Code.StartsWith(item.Code + ".")
                    && x.Code.Split('.').Count() == (item.Code.Split('.').Count() + 1)
                    && x.Dep != null))
                    {
                        Models.StructureUnit su = new Models.StructureUnit();
                        su.Code = subItem.Code;
                        su.Name = subItem.Dep;
                        su.Children = BuilAUTree(su);
                        ret.Add(su);                
                    }                    
                }
            }
                return ret;
        }

        /// <summary>
        /// Возвращает Структуру по АУ
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private List<Models.StructureUnit> GetDruzhbaDepartments(string id)
        {
            List<Models.StructureUnit> ret = new List<Models.StructureUnit>();
            id = id.Replace("-", ".");
            using (Models.PhonesDataContext model = new Models.PhonesDataContext())
            {
                ///---Проверяю есть ли такое подразделение-объект в базе
                if (model.PhonesDruzhbas.Where(x => x.Code == id.Trim()).Count() > 0
                    && model.PhonesDruzhbas.First(x => x.Code == id.Trim()).Dep != null)
                {
                    ///---Нахожу всех его предков (для отображения структуры)
                    if (id.Trim().Split('.').Count() > 1)
                    {
                        string codePrefix = "";
                        for (int i = 0; i < id.Trim().Split('.').Count(); i++)
                        {
                            if (i == 0)
                            {
                                codePrefix += id.Trim().Split('.')[i];
                            }
                            else
                            {
                                codePrefix += "." + id.Trim().Split('.')[i];
                            }


                            ///---Проверяю - существует ли родительский элемент и добавляю его
                            if (model.PhonesDruzhbas.Where(x => x.Code == (codePrefix).Trim()).Count() > 0)
                            {
                                ret.Add(
                                model.PhonesDruzhbas.Where(x => x.Code == (codePrefix).Trim())
                                    .Select(x => new Models.StructureUnit()
                                    {
                                        Code = x.Code,
                                        Level = i,
                                        Current = false,
                                        Name = x.Dep,
                                        Type = "dep",
                                        FunctionDescription = x.Function
                                    }
                                    )
                                    .First());
                            }
                        }
                    }

                    ///--Добавляю текущий элемент
                    //ret.Add(
                    //    model.Phones.Where(x => x.Code == id.Trim())
                    //        .Select(x => new Models.StructureUnit()
                    //        {
                    //            Code = x.Code,
                    //            Level = id.Trim().Split('.').Count(),
                    //            Current = true,
                    //            Name = x.Dep,
                    //            Type = "dep",
                    //            FunctionDescription = x.Function
                    //        }).First()
                    //    );

                    ///---Добавляю дочерние элементы-подразделения
                    if (model.PhonesDruzhbas.Where(x => x.Code.StartsWith(id.Trim() + ".") && x.Dep != null).Count() > 0)
                    {
                        foreach (var dep in model.PhonesDruzhbas
                            .OrderBy(x=>x.Code)
                            .Where(x => x.Code.StartsWith(id.Trim() + ".") && x.Dep != null)
                            .ToList()
                            .Select(x => new Models.StructureUnit()
                            {
                                Code = x.Code,
                                Level = x.Code.Trim().Split('.').Count(),
                                Current = false,
                                Name = x.Dep,
                                Type = "dep",
                                FunctionDescription = x.Function
                            }).ToList())
                        {
                            ret.Add(dep);
                        }
                    }

                    ///---Добавляю дочерние элементы-сотрудники
                    if (model.PhonesDruzhbas.Where(x => x.Code.StartsWith(id.Trim() + ".") && x.PIB != null).Count() > 0)
                    {
                        string startWith = id.Trim() + ".";
                        var users = model.PhonesDruzhbas.Where(x => x.Code.StartsWith(startWith) && x.PIB != null).ToList();

                        foreach (var user in users
                            .Select(x => new Models.StructureUnit()
                            {
                                Code = x.Code.Replace(".", "-"),
                                Level = id.Trim().Split('.').Count() + 1,
                                Current = false,
                                Name = x.PIB,
                                Type = "user",
                                FunctionDescription = x.Function,
                                BirthDate = x.Birthday,
                                Email = x.Email,
                                Mobile = x.Mobile,
                                Phones = x.Phone,
                                Post = GetFullPost(x.Code),
                                //Photo = Photo(x.Email),
                                PhotoFileName = PhotoName(x.Email)
                            }).ToList())
                        {
                            ret.Add(user);
                        }
                    }
                }
            }
            return ret;

        }

        /// <summary>
        /// Возвращает подразделения по результату запроса поиска
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("api/search/{name}")]
        public List<Models.StructureUnit> SearchFunctions(string name)
        {
            List<Models.StructureUnit> ret = new List<Models.StructureUnit>();
            string searchString = name.Trim();
            if (searchString.Length < 3) return ret;

            using (Models.PhonesDataContext model = new Models.PhonesDataContext())
            {

                ///---Добавляю элементы-сотрудники
                if (model.Phones.Where(x => x.Function.Contains(searchString)
                || x.Post.Contains(searchString)
                || x.PIB.Contains(searchString)).Count() > 0)
                {
                    foreach (var user in model.Phones.Where(x=>x.Function.Contains(searchString)
                    || x.Post.Contains(searchString)
                    || x.PIB.Contains(searchString))
                        .ToList()
                        .Select(x => new Models.StructureUnit()
                        {
                            Code = x.Code,
                            Level = x.Code != null?x.Code.Trim().Split('.').Count() + 1:1,
                            Current = false,
                            Name = x.PIB,
                            Type = "user",
                            FunctionDescription = x.Function,
                            BirthDate = x.Birthday,
                            Email = x.Email,
                            Mobile = x.Mobile,
                            Phones = x.Phone1,
                            Post = GetFullPost(x.Code),
                            //Photo = Photo(x.Email),
                            PhotoFileName = PhotoName(x.Email)
                        }).ToList())
                    {
                        ret.Add(user);
                    }
                }
            }
            return ret;
        }

        /// <summary>
        /// Возвращает именинников
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("api/birthdays")]
        public List<Models.StructureUnit> GetBirthdays()
        {
            List<Models.StructureUnit> ret = new List<Models.StructureUnit>();
            //List<Models.StructureUnit> ret1 = new List<Models.StructureUnit>();

            string birthdayStart = DateTime.Now.ToString("dd.MM");

            using (Models.PhonesDataContext model = new Models.PhonesDataContext())
            {
                ///---Добавляю элементы-сотрудники
                foreach (var user in model.Phones.Where(x => x.Birthday != null
                && x.Birthday.Trim().StartsWith(birthdayStart))
                        .ToList()
                        .Select(x => new Models.StructureUnit()
                        {
                            Code = x.Code,
                            Level = x.Code.Trim().Split('.').Count() + 1,
                            Current = false,
                            Name = x.PIB,
                            Type = "user",
                            FunctionDescription = x.Function,
                            BirthDate = x.Birthday,
                            BirthDateDate = ConvertDate(x.Birthday),
                            Email = x.Email,
                            Mobile = x.Mobile,
                            Phones = x.Phone1,
                            Post = GetFullPost(x.Code),
                            //Photo = Photo(x.Email),
                            PhotoFileName = PhotoName(x.Email)
                        }).ToList())
                    {
                        ret.Add(user);
                    }
                    
                //foreach(var bu in ret.Where(x=>x.BirthDateDate.Day == DateTime.Now.Day && 
                //x.BirthDateDate.Month == DateTime.Now.Month))
                //{
                //    ret1.Add(bu);
                //}
            }
            return ret;
        }

        [HttpGet]
        [Route("api/getCurrentUser")]
        public Models.OnlineUser getCurrentUser()
        {
            Models.OnlineUser ret = new Models.OnlineUser();
            using (Models.PhonesDataContext model = new Models.PhonesDataContext())
            {

                ret.LastLogonIP = GetClientIp();

                ///---Логирую обращение пользователя
                LogUserAccess(GetRequestURI(), ret.LastLogonIP);

                if (model.Logins.Count(x => x.IPAddress.Trim() == ret.LastLogonIP) > 0)
                {
                    var xx = model.Logins.OrderByDescending(x=>x.Date).First(x => x.IPAddress.Trim() == ret.LastLogonIP);
                    ret.FIO = xx.FIO;
                    ret.Email = xx.Email;
                    ret.Login = xx.sAMAccountName;

                    ///---Ищу телефон
                    if (model.Phones.Where(x => x.Email.Trim() == xx.Email.Trim()).Count() > 0
                        && model.Phones.First(x => x.Email.Trim() == xx.Email.Trim()).Phone1 != null
                        )
                    {
                        ret.Phone = model.Phones.First(x => x.Email.Trim() == xx.Email.Trim()).Phone1;
                    }
                }
                else {
                    ///---Пытаюсь получить Cookie с клиента для его аутентификации                    
                    if (HttpContext.Current.Request.Cookies["currentPhone"] != null)
                    {                        
                        ret.Phone = HttpContext.Current.Server.HtmlEncode(HttpContext.Current.Request.Cookies["currentPhone"].Value);
                        ret.Phone = ret.Phone.Split('=').Count() > 1 ? ret.Phone.Split('=')[1] : "";
                        ret.FIO = "";
                        ret.Email = "";
                        ret.Login = "";
                    }                      
                }
            }
            return ret;
        }

        /// <summary>
        /// Возвращает статистику по указанному сайту
        /// </summary>
        /// <param name="site"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("api/getStatistics")]
        public Models.SiteStatistic GetStatistics()
        {
            Models.SiteStatistic s = new Models.SiteStatistic();
            using (Models.PhonesDataContext model = new Models.PhonesDataContext())
            {
                s.TodayHitCount = model.LogSiteAccesses.Count(x => x.Date >= DateTime.Today);
                s.TodayVisitors = model.LogSiteAccesses
                    .Where(x => x.Date >= DateTime.Today)
                    .Select(x => x.IP)
                    .Distinct()
                    .Count();
                s.TotalHitCount = model.LogSiteAccesses.Count();
                s.TotalVisitors = model.LogSiteAccesses
                    .Select(x => x.IP)
                    .Distinct()
                    .Count();
            }
            return s;
        }


        /// <summary>
        /// Сохраняю данные пользователя в Cookie
        /// </summary>
        /// <param name="username"></param>
        [HttpPost]
        [Authorize]
        [Route("api/saveCurrentUser")]
        public void saveCurrentUser(Models.OnlineUser user)
        {
            HttpCookie myCookie = new HttpCookie("currentPhone");
            myCookie["currentPhone"] = user.Phone;
            myCookie.Expires = DateTime.Now.AddDays(3650d);
            HttpContext.Current.Response.Cookies.Add(myCookie);
        }


        ///**********************************************************************************************
        /// Internal function
        ///**********************************************************************************************
        /// <summary>
        /// Возвращает фото по имени
        /// </summary>
        /// <param name="Name"></param>
        /// <returns></returns>
        private string Photo(string email)
        {
            string fileName = email != null && email.Split('@').Count() > 1 ? email.Split('@')[0] : "";
            string absolute = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", fileName+".jpg");

            string ret = "";
            if (File.Exists(absolute))
            {
                System.Drawing.Image i = System.Drawing.Image.FromFile(absolute);
                ret = ImageToBase64(i, System.Drawing.Imaging.ImageFormat.Jpeg);
            }
            return ret;
        }

        private string GetClientIp(HttpRequestMessage request = null)
        {
            request = request ?? Request;

            if (request.Properties.ContainsKey("MS_HttpContext"))
            {
                return ((HttpContextWrapper)request.Properties["MS_HttpContext"]).Request.UserHostAddress;
            }
            else if (request.Properties.ContainsKey(RemoteEndpointMessageProperty.Name))
            {
                RemoteEndpointMessageProperty prop = (RemoteEndpointMessageProperty)request.Properties[RemoteEndpointMessageProperty.Name];
                return prop.Address;
            }
            else if (HttpContext.Current != null)
            {
                return HttpContext.Current.Request.UserHostAddress;
            }
            else
            {
                return null;
            }
        }

        private string GetRequestURI(HttpRequestMessage request = null)
        {
            request = request ?? Request;
            return request.RequestUri.ToString();
        }

        /// <summary>
        /// Конвертирует строку в DateTime
        /// </summary>
        /// <param name="d"></param>
        /// <returns></returns>
        private DateTime ConvertDate(string d)
        {
            DateTime ret = DateTime.MinValue;
            try
            {
                ret = DateTime.ParseExact(d, "dd.MM.yyyy", new CultureInfo("ru-RU"));

            }catch(Exception ex)
            {

            }
            return ret;
        }

        /// <summary>
        /// Возвращает имя файла с фотографией 
        /// </summary>
        /// <param name="Name"></param>
        /// <returns></returns>
        public static string PhotoName(string email)
        {
            string fileName = email != null && email.Split('@').Count() > 1 ? email.Split('@')[0] : "";
            string absolute = @"/api/Resource/"+ fileName + @".jpg";
            return absolute;
        }

        /// <summary>
        /// Возвращает строку картинки
        /// </summary>
        /// <param name="image"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        private string ImageToBase64(System.Drawing.Image image,  System.Drawing.Imaging.ImageFormat format)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                // Convert Image to byte[]
                image.Save(ms, format);
                byte[] imageBytes = ms.ToArray();

                // Convert byte[] to Base64 String
                string base64String = Convert.ToBase64String(imageBytes);
                return base64String;
            }
        }


        private string GetFullPost(string userCode)
        {
            string fullPost = "";
            int filialCode = userCode.Split('.').Count() > 1 ? int.Parse(userCode.Split('.')[1]) : 0;
            switch(filialCode / 100)
            {
                case 0:
                    fullPost = GetGetFullPostAU(userCode);
                    break;
                case 2:
                    fullPost = GetGetFullPostDruzhba(userCode);
                    break;           
            }            

            return fullPost;
        }

        private string GetGetFullPostAU(string userCode)
        {
            using (Models.PhonesDataContext model = new Models.PhonesDataContext())
            {
                string currDepId = "";
                string fullPost = "";
                foreach (var depId in userCode.Split('.').ToList())
                {
                    if (currDepId.Split('.').Count() > 1)
                    {
                        currDepId += depId;
                        if (model.Phones.Count(x => x.Dep != null && x.Code == currDepId) > 0)
                        {
                            fullPost += model.Phones.First(x => x.Dep != null && x.Code == currDepId).Dep + "<br/> ";
                        }
                        currDepId += ".";
                    }
                    else
                    {
                        currDepId += depId;
                        currDepId += ".";
                    }
                }

                if (model.Phones.Count(x => x.PIB != null && x.Code == userCode) > 0)
                {
                    fullPost += model.Phones.First(x => x.PIB != null && x.Code == userCode).Post;
                }
                return fullPost;
            }
        }

        private string GetGetFullPostDruzhba(string userCode)
        {
            using (Models.PhonesDataContext model = new Models.PhonesDataContext())
            {
                string currDepId = "";
                string fullPost = "";
                foreach (var depId in userCode.Split('.').ToList())
                {
                    if (currDepId.Split('.').Count() > 1)
                    {
                        currDepId += depId;
                        if (model.PhonesDruzhbas.Count(x => x.Dep != null && x.Code == currDepId) > 0)
                        {
                            fullPost += model.PhonesDruzhbas.First(x => x.Dep != null && x.Code == currDepId).Dep + "<br/> ";
                        }
                        currDepId += ".";
                    }
                    else
                    {
                        currDepId += depId;
                        currDepId += ".";
                    }
                }

                ///---Add function to POST
                string functionName = model.Phones.First(x => x.Code == userCode.Split('.')[0]).Dep;
                

                if (model.PhonesDruzhbas.Count(x => x.PIB != null && x.Code == userCode) > 0)
                {
                    fullPost += model.PhonesDruzhbas.First(x => x.PIB != null && x.Code == userCode).Post;
                }
                return functionName + "<br/>" + fullPost;
            }
        }

        /// <summary>
        /// Сохраняет в базе загрузку сайта
        /// </summary>
        void LogUserAccess(string URI, string IPAddress)
        {
            using (Models.PhonesDataContext model = new Models.PhonesDataContext())
            {
                model.LogSiteAccesses.InsertOnSubmit(new Models.LogSiteAccess() {
                    Date = DateTime.Now,
                    IP = IPAddress,
                    URI = URI
                });
                model.SubmitChanges();
            }
        }
    }
}
