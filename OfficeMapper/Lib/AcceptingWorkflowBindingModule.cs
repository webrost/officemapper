using Ninject.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;

namespace OfficeMapper.Lib
{
    public class AcceptingWorkflowBindingModule : NinjectModule
    {
        readonly ServiceTypeEnum serviceType;
        readonly string NameSpace = "OfficeMapper.Lib.";

        /// <summary>
        /// Получение типа сервиса напрямую
        /// </summary>
        /// <param name="serviceType"></param>
        public AcceptingWorkflowBindingModule(ServiceTypeEnum serviceType)
        {
            this.serviceType = serviceType;
        }

        /// <summary>
        /// Получение типа сервиса по коду (уже зарегистрированному в базе)
        /// </summary>
        /// <param name="secret"></param>
        public AcceptingWorkflowBindingModule(string secret)
        {
            string FileSharesCode = System.Configuration.ConfigurationManager.AppSettings["FileSharesCode"].ToString();
            using (Models.PhonesDataContext model = new Models.PhonesDataContext())
            {
                if (model.RequestTickets.Count(x => x.secret == secret) > 0)
                {
                    string serviceId = model.RequestTickets.First(x => x.secret == secret).serviceId;                    
                    this.serviceType = ServiceTypeEnum.UserService;

                    ///----Если сервис ФайловыеШары
                    if (serviceId.StartsWith(FileSharesCode + "."))
                    {
                        this.serviceType = ServiceTypeEnum.FileShare;
                    }
                }
                else {
                    throw new Exception("Cannot find RequestTickets with secret = " + secret);
                }
            }
        }

        public override void Load()
        {
            string typeName = serviceType.ToString();
            Assembly assembly = Assembly.GetExecutingAssembly();
            Type t = assembly.GetType(NameSpace + typeName);
            this.Bind<IBaseUserService>().To(t);
        }
    }
}