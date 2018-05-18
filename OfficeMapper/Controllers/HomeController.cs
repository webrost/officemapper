using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace OfficeMapper.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        [Authorize]
        [HttpGet]
        public ActionResult ChangePhone()
        {
            Models.ChangePhoneModel modelOut = new Models.ChangePhoneModel();
            modelOut.CurrentUser = System.Web.HttpContext.Current.User.Identity.Name;
            return View(modelOut);
        }

        [Authorize]
        [HttpPost]
        public ActionResult ChangePhone(Models.ChangePhoneModel modelIn)
        {
            Models.ChangePhoneModel modelOut = new Models.ChangePhoneModel();
            modelOut.CurrentUser = System.Web.HttpContext.Current.User.Identity.Name;
            HttpCookie myCookie = new HttpCookie("currentPhone");
            myCookie["currentPhone"] = modelIn.Phone;
            myCookie.Expires = DateTime.Now.AddDays(3650d);
            System.Web.HttpContext.Current.Response.Cookies.Add(myCookie);
            using (Models.PhonesDataContext model = new Models.PhonesDataContext())
            {
                model.ChangePhoneLogs.InsertOnSubmit(new Models.ChangePhoneLog() {
                    Date = DateTime.Now,
                    Login = System.Web.HttpContext.Current.User.Identity.Name,
                    Phone = modelIn.Phone
            });
                model.SubmitChanges();
            }
            return RedirectToAction(@"/");
        }

        /// <summary>
        /// Отображает сылки по выполнению служебных операция для карты
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpGet]
        public ActionResult PhoneTasks()
        {
            return View();
        }

        public ActionResult MapDev()
        {
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}