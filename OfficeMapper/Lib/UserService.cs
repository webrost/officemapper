using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OfficeMapper.Lib
{
    public class UserService : IBaseUserService
    {
        public string Id { get; set; }
        public string ServiceName { get; set; }
        public string Description { get; set; }
        public string DeprecationText { get; set; }
        public string secret { get; set; }

        /// <summary>
        /// Сохранение заявки на новый сервис
        /// </summary>
        /// <param name="ar"></param>
        public void Save(AcceptRequest ar) {
            using (Models.PhonesDataContext model = new Models.PhonesDataContext())
            {
                model.RequestTickets.InsertOnSubmit(new Models.RequestTicket() {
                    RequestDate = DateTime.Now,
                    RequestStage = StageEnum.NewRequest.ToString(),
                    ServiceType = ServiceTypeEnum.UserService.ToString(),
                    UTNLogin = ar.UTNLogin,
                    serviceId =ar.ServiceId,
                    secret = ar.Secret,                    
                });
                model.SubmitChanges();
            }
        }

        /// <summary>
        /// Сохранение пула заявок/инвентаризации на сервисы
        /// </summary>
        /// <param name="ar"></param>
        public void Save(List<AcceptRequest> ars) {
            using (Models.PhonesDataContext model = new Models.PhonesDataContext())
            {
                foreach(var ar in ars)
                {
                    model.RequestTickets.InsertOnSubmit(new Models.RequestTicket()
                    {
                        RequestDate = DateTime.Now,
                        RequestStage = StageEnum.NewRequest.ToString(),
                        ServiceType = ServiceTypeEnum.UserService.ToString(),
                        UTNLogin = ar.UTNLogin,
                        serviceId = ar.ServiceId,
                        secret = ar.Secret,
                    });                    
                }
                model.SubmitChanges();
            }
        }

        /// <summary>
        /// Перемещаем сервис по дереву согласования
        /// </summary>
        public StageEnum PushNextStage(string secret, Models.DecisionData data) {
            StageEnum ret = StageEnum.NewRequest;
            bool learningMode = bool.Parse(System.Configuration.ConfigurationManager.AppSettings["learningMode"].ToString());

            ///--- Определяю модель в зависимости от режима работы
            if(learningMode)
            {
                ret = PushNextStageLearningMode(secret,data);
            }else
            {
                ret = PushNextStageAccessMode(secret,data);
            }
            return ret;
        }

        /// <summary>
        /// Процесс согласования в режиме инвентаризации
        /// </summary>
        /// <param name="secret"></param>
        /// <returns></returns>
        StageEnum PushNextStageLearningMode(string secret, Models.DecisionData data)
        {
            StageEnum ret = StageEnum.NewRequest;
            using (Models.PhonesDataContext model = new Models.PhonesDataContext())
            {
                StageEnum stage = GetCurrentStage(secret);
                switch (stage)
                {
                    case StageEnum.NewRequest:

                        ///---Определяем список ИТ ответственных и отправляем им письма на подтверждение
                        List<Models.AuthUser> responsers = GetITResponsers(secret);
                        List<IBaseUserService> services = GetServices(secret);
                        Models.User whom = GetRequesterUser(secret);
                        MailHelper.SendClaimRequestToGroup(services, responsers, whom);
                        ret = StageEnum.WaitITAccept;

                        break;
                    case StageEnum.WaitITAccept:

                        ///---Пришло подтверждение -- фиксируем в базе, отправляем уведомление пользователю
                        if(data.Decision == Models.DecisionEnum.Accept)
                        {
                            AcceptServicesInvent(secret, data);
                            ret = StageEnum.Accepted;
                        }

                        ///---Отказ инвентаризации фиксируем в БД
                        if (data.Decision == Models.DecisionEnum.Decline)
                        {
                            DeclineServicesInvent(secret, data);
                            ret = StageEnum.Declined;
                        }
                        break;
                }
            }
            return ret;
        }

        #region Private methods
        /// <summary>
        /// Процесс согласования в режиме предоставления доступа
        /// </summary>
        /// <param name="secret"></param>
        /// <returns></returns>
        StageEnum PushNextStageAccessMode(string secret, Models.DecisionData data)
        {
            StageEnum ret = StageEnum.NewRequest;
            using (Models.PhonesDataContext model = new Models.PhonesDataContext())
            {
                StageEnum stage = GetCurrentStage(secret);
                switch (stage)
                {
                    case StageEnum.NewRequest:
                        ///----Отправляем согласование Айтишникам
                        ret = StageEnum.SendITRequest;
                        break;
                    case StageEnum.SendITRequest:
                        ///---Ждем подтверждения
                        ret = StageEnum.WaitITAccept;
                        break;
                    case StageEnum.WaitITAccept:
                        break;
                }
            }
            return ret;
        }

        /// <summary>
        /// Возвращает текущую стадию согласования сервиса
        /// </summary>
        /// <param name="secret"></param>
        /// <returns></returns>
        StageEnum GetCurrentStage(string secret)
        {
            StageEnum ret = StageEnum.NewRequest;
            using (Models.PhonesDataContext model = new Models.PhonesDataContext())
            {
                ret = (StageEnum)Enum.Parse(typeof(StageEnum), model.RequestTickets.First(x => x.secret == secret).RequestStage);                
            }
            return ret;
        }

        /// <summary>
        /// Возвращает список сервисов в очереди запросов 
        /// </summary>
        /// <param name="secret"></param>
        /// <returns></returns>
        List<IBaseUserService> GetServices(string secret)
        {
            List<IBaseUserService> ret = new List<IBaseUserService>();
            using (Models.PhonesDataContext model = new Models.PhonesDataContext())
            {
                foreach(var s in model.RequestTickets
                    .Where(x=>x.secret == secret)
                    .Select(x=>x.UserService)
                    )
                {
                    ret.Add(new OfficeMapper.Lib.UserService() {
                        ServiceName = s.ServiceName,
                        DeprecationText = s.DeprecationText,
                        Description = s.Description,
                        secret = secret
                    });
                }
            }
            return ret;
        }

        /// <summary>
        /// Возвращает список согласователей
        /// </summary>
        /// <param name="secret"></param>
        /// <returns></returns>
        List<Models.AuthUser> GetITResponsers(string secret)
        {
            List<Models.AuthUser> users = new List<Models.AuthUser>();
            using (Models.PhonesDataContext model = new Models.PhonesDataContext())
            {
                ///---Получаю список ИТ ответственных по пользователю, который проинвертился
                string d = model.RequestTickets.First(x => x.secret == secret).UTNLogin.Split('@')[1];
                DomainEnum domain = (DomainEnum)Enum.Parse(typeof(DomainEnum), d);
                foreach (string UTNName in
                        model.Acceptors.Where(x => x.serviceDomain == domain.ToString())
                            .Select(x => x.username).ToList())
                {
                    string sn = ConvertFromUTNNameTosAMAccountName(UTNName);
                    var login = model.Logins.First(l => l.sAMAccountName == sn);
                    users.Add(new Models.AuthUser()
                    {
                        email = login.Email,
                        FIO = login.FIO != null ? login.FIO : ""
                    });
                }
            }
            return users;
        }

        /// <summary>
        /// Подтверждает перечень сервисов как ПОДТВЕРЖДЕННЫЕ для ИНВЕНТАРИЗАЦИИ
        /// </summary>
        /// <param name="secret"></param>
        /// <param name="data"></param>
        void AcceptServicesInvent(string secret, Models.DecisionData data)
        {
            using (Models.PhonesDataContext model = new Models.PhonesDataContext())
            {
                foreach(var service in model.RequestTickets.Where(x=>x.secret == secret))
                {
                    service.RequestStage = StageEnum.Accepted.ToString();
                    if(model.AssignedServices
                        .Count(x=>x.ServiceId == service.serviceId && x.UTNLogin == service.UTNLogin) > 0)
                    {
                        var assignedService = model.AssignedServices
                            .First(x => x.ServiceId == service.serviceId && x.UTNLogin == service.UTNLogin);
                        assignedService.TechnicalResponsibleApproved = true;
                        assignedService.TechnicalResponsibleApprovedBy = data.Acceptor.UTNLogin;                        
                    }
                    service.RequestStage = StageEnum.Accepted.ToString();
                    model.SubmitChanges();
                }
            }
        }

        /// <summary>
        /// Подтверждает перечень сервисов как ОТКЛОНЕННЫЕ для ИНВЕНТАРИЗАЦИИ
        /// </summary>
        /// <param name="secret"></param>
        /// <param name="data"></param>
        void DeclineServicesInvent(string secret, Models.DecisionData data)
        {
            using (Models.PhonesDataContext model = new Models.PhonesDataContext())
            {
                foreach (var service in model.RequestTickets.Where(x => x.secret == secret))
                {
                    service.RequestStage = StageEnum.Declined.ToString();
                    model.SubmitChanges();
                }
            }
        }
        #endregion

        /// <summary>
        /// Преобразует имя пользователя в sAMAccountName
        /// </summary>
        /// <param name="UTNName"></param>
        /// <returns></returns>
        public static string ConvertFromUTNNameTosAMAccountName(string UTNName)
        {
            string user = UTNName.Split('@')[0];
            DomainEnum domain = (DomainEnum)Enum.Parse(typeof(DomainEnum), UTNName.Split('@')[1]);
            switch(domain)
            {
                case DomainEnum.kyiv:
                    return String.Format(@"{0}\{1}",DomainNETBIOSEnum.UKRTRANSNAFTA.ToString(), user);
                case DomainEnum.kremen:
                    return String.Format(@"{0}\{1}", DomainNETBIOSEnum.KREMEN.ToString(), user);
                case DomainEnum.odessa:
                    return String.Format(@"{0}\{1}", DomainNETBIOSEnum.ODESSA.ToString(), user);
                case DomainEnum.lviv:
                    return String.Format(@"{0}\{1}", DomainNETBIOSEnum.DRUZHBA.ToString(), user);
                default:
                    return String.Format(@"{0}\{1}", DomainNETBIOSEnum.DRUZHBA_AD.ToString(), user);

            }
        }

        /// <summary>
        /// Возвращает пользователя, который запросил сервис
        /// </summary>
        /// <param name="secret"></param>
        /// <returns></returns>
        public static Models.User GetRequesterUser(string secret)
        {
            using (Models.PhonesDataContext model = new Models.PhonesDataContext())
            {
                string UTNLogin = model.RequestTickets.First(x => x.secret == secret).UTNLogin;
                Models.User user = model.Users.First(x => x.UTNLogin == UTNLogin);
                return user;
            }
        }
    }
}