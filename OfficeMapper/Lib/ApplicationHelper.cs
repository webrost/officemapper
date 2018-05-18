using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Ninject;

namespace OfficeMapper.Lib
{
    public class ApplicationHelper
    {
        public static IKernel AppKernel;

        /// <summary>
        /// Возвращает перечень сервисов для пользователя
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public List<Models.ApplicationEntry> GetApplications(Models.AuthUser user)
        {
            List<Models.ApplicationEntry> apps = new List<Models.ApplicationEntry>();
            user = Lib.ADAuth.GetUser(user.UTNLogin);
            string FileSharesCode = System.Configuration.ConfigurationManager.AppSettings["FileSharesCode"].ToString().Trim();
            using (OfficeMapper.Models.PhonesDataContext model = new Models.PhonesDataContext())
            {        
                apps = model.UserServices.ToList()
                    .Where(x => !string.IsNullOrEmpty(x.Id) 
                    && x.Id != FileSharesCode
                    && !x.Id.StartsWith(FileSharesCode + ".")
                    && ApplicationFilter(x.Department,user.UTNLogin) )     
                    .Select(x => new Models.ApplicationEntry() {
                        Id = x.Id,
                        Name = x.Id.Contains('.')?x.ServiceName:x.Group,
                        Description = x.Description,
                        Type = x.Id.Contains('.')?Models.ServiceLevelEnum.Service.ToString():
                        Models.ServiceLevelEnum.Group.ToString(),
                        HelpDocumentationLink = x.HelpDocumentationLink,
                        DeprecationText = x.DeprecationText != null? x.DeprecationText:""
                    })
                    .ToList();

                ///---Проставляю статусы по каждому сервису
                foreach(var app in apps)
                {
                    if(model.AssignedServices.Count(x=>x.ServiceId == app.Id
                    && x.UTNLogin == user.UTNLogin)>0)
                    {
                        ///---Отображение состояние "заказанности" пользователем
                        var assign = model.AssignedServices.First(x => x.ServiceId == app.Id
                        && x.UserId == user.Id);
                        app.UserClamed = assign.UserClaimed.Value;
                        
                        ///---Отображение состояния "Подтвержденности IT"
                        if(assign.ITApproved != null && assign.ITApproved == true 
                            && assign.ITApprovedBy != null && assign.ITApprovedBy != "")
                        {
                            ///---Получаю данные о пользователе, который подписал со стороны IT
                            Models.AuthUser itUser = Lib.ADAuth.GetUser(assign.ITApprovedBy);
                            if (itUser == null) itUser = new Models.AuthUser();
                            app.AcceptedIT = assign.ITApproved.Value;
                            app.AcceptedITBy = itUser;
                        }else
                        {
                            app.AcceptedITBy = new Models.AuthUser();
                            app.AcceptedITBy.UTNLogin = "";
                        }

                        ///---Отображение состояние "инвентаризации" пользователем
                        var assign1 = model.AssignedServices.First(x => x.ServiceId == app.Id
                        && x.UserId == user.Id);
                        app.AcceptedTechnicalResponsible = assign1.TechnicalResponsibleApproved != null && assign1.TechnicalResponsibleApproved.Value;



                        ///---Отображение необходимости и состояния "Подтвержденности Владельца"
                        if (assign.OwnerApproved != null && assign.OwnerApproved == true
                            && assign.OwnerApprovedBy != null && assign.OwnerApprovedBy != "")
                        {
                            ///---Получаю данные о пользователе, который подписал со стороны IT
                            Models.AuthUser ownerUser = Lib.ADAuth.GetUser(assign.OwnerApprovedBy);
                            if (ownerUser == null) ownerUser = new Models.AuthUser();
                            app.AcceptedOwner = assign.OwnerApproved.Value;
                            app.AcceptedOwnerBy = ownerUser;
                            app.AcceptedITBy.UTNLogin = "";
                        }else
                        {
                            app.AcceptedOwnerBy = new Models.AuthUser();
                            app.AcceptedOwnerBy.UTNLogin = "";
                        }

                    }

                    ///---Нужно ли подтверждать владельцу сервиса
                    app.NeedAcceptedOwner = Lib.MailHelper.ExistsApproval(Models.AcceptRoleEnum.owner.ToString(), app.Id);
                }
            }
            return apps;
        }

        /// <summary>
        /// Сохраняю запросы пользователя на приложения
        /// акцептирование айтишниками и владельцами
        /// </summary>
        /// <param name="data"></param>
        public void SaveApplications(Models.ApplicationView data)
        {
            using (OfficeMapper.Models.PhonesDataContext model = new Models.PhonesDataContext())
            {
                Models.AuthUser user = Lib.ADAuth.GetUser(data.User.UTNLogin);

                #region TODO: Applications
                /////----Проверяю все поданные заявки и сохраняю их в базе
                //foreach (var app in data.Applications)
                //{
                //    ///---Если записи нет - добавляю
                //    if(model.AssignedServices.Count(x=>x.UserId == user.Id 
                //    && x.ServiceId == app.Id) == 0 && app.UserClamed == true)
                //    {
                //        model.AssignedServices.InsertOnSubmit(new Models.AssignedService() {
                //            ServiceId = app.Id,
                //            UserId = user.Id,
                //            UserClaimed = app.UserClamed                            
                //        });
                //        model.SubmitChanges();

                //        ///---Если это не режим инвентаризации, отправляю заявки по каждому приложению отдельно
                //        if(bool.Parse(System.Configuration.ConfigurationManager.AppSettings["learningMode"]) != true)
                //        {
                //            ///---Создаем новый запрос на сервис
                //            AcceptRequest ar = new AcceptRequest() {
                //                ServiceId = app.Id,
                //                ServiceType = ServiceTypeEnum.UserService,
                //                RequestDate = DateTime.Now,
                //                UserId = user.sAMAccountName,
                //                RequestStage = StageEnum.NewRequest,
                //                Secret =Guid.NewGuid().ToString()
                //            };
                //            var wfManager = new StandardKernel(new AcceptingWorkflowBindingModule(ServiceTypeEnum.UserService)).Get<AcceptingWorkflowManager>();
                //            wfManager.Save(ar);
                //            //ApproveClaimRequest(user.sAMAccountName, app.Id);
                //        }
                //    }


                //    ///--Если запись есть, но нет галки - удаляю
                //    if (model.AssignedServices.Count(x => x.UserId == user.Id
                //     && x.ServiceId == app.Id) > 0 && app.UserClamed == false)
                //    {
                //        var deleteRec = model.AssignedServices.First(x => x.UserId == user.Id
                //     && x.ServiceId == app.Id);
                //        model.AssignedServices.DeleteOnSubmit(deleteRec);
                //        model.SubmitChanges();
                //    }
                //}
                #endregion

                #region TODO: Shares
                /////----Проверяю все поданные заявки и сохраняю их в базе
                //foreach (var app in data.Applications)
                //{
                //    ///---Если записи нет - добавляю
                //    if (model.AssignedServices.Count(x => x.UserId == user.Id
                //     && x.ServiceId == app.Id) == 0 && app.UserClamed == true)
                //    {
                //        model.AssignedServices.InsertOnSubmit(new Models.AssignedService()
                //        {
                //            ServiceId = app.Id,
                //            UserId = user.Id,
                //            UserClaimed = app.UserClamed
                //        });
                //        model.SubmitChanges();

                //        ///---Если это не режим инвентаризации, отправляю заявки по каждому приложению отдельно
                //        if (bool.Parse(System.Configuration.ConfigurationManager.AppSettings["learningMode"]) != true)
                //        {
                //            ///---Создаем новый запрос на сервис
                //            AcceptRequest ar = new AcceptRequest()
                //            {
                //                ServiceId = app.Id,
                //                ServiceType = ServiceTypeEnum.UserService,
                //                RequestDate = DateTime.Now,
                //                UserId = user.sAMAccountName,
                //                RequestStage = StageEnum.NewRequest,
                //                Secret = Guid.NewGuid().ToString()
                //            };
                //            var wfManager = new StandardKernel(new AcceptingWorkflowBindingModule(ServiceTypeEnum.UserService)).Get<AcceptingWorkflowManager>();
                //            wfManager.Save(ar);
                //            //ApproveClaimRequest(user.sAMAccountName, app.Id);
                //        }
                //    }


                //    ///--Если запись есть, но нет галки - удаляю
                //    if (model.AssignedServices.Count(x => x.UserId == user.Id
                //     && x.ServiceId == app.Id) > 0 && app.UserClamed == false)
                //    {
                //        var deleteRec = model.AssignedServices.First(x => x.UserId == user.Id
                //     && x.ServiceId == app.Id);
                //        model.AssignedServices.DeleteOnSubmit(deleteRec);
                //        model.SubmitChanges();
                //    }
                //}
                #endregion

                #region Application Inventarization
                ///---Если это режим инвентаризации, перечень всех приложений на утверждение Техответственных
                if (bool.Parse(System.Configuration.ConfigurationManager.AppSettings["learningMode"]) == true)
                {
                    ///---Создаю пачку запросов с одним Secret для 
                    List<AcceptRequest> ars = new List<AcceptRequest>();
                    string Secret = Guid.NewGuid().ToString();
                    foreach (var app in data.Applications.Where(x => x.UserClamed == true).ToList())
                    {
                        model.AssignedServices.InsertOnSubmit(new Models.AssignedService()
                        {
                            ServiceId = app.Id,
                            UserId = user.Id,
                            UserClaimed = app.UserClamed,
                            UTNLogin = data.User.UTNLogin
                        });
                        model.SubmitChanges();

                        AcceptRequest ar = new AcceptRequest()
                        {
                            ServiceId = app.Id,
                            ServiceType = ServiceTypeEnum.UserService,
                            RequestDate = DateTime.Now,
                            UTNLogin = user.UTNLogin,
                            RequestStage = StageEnum.NewRequest,
                            Secret = Secret
                        };
                        ars.Add(ar);
                    }

                    ///---Принимаю запросы на новые сеервисы, которые уже используются, но не проинвенчены
                    //foreach (var newService in )
                    //{

                    //}

                    ///---Сохраняю запрос
                    var wfManager = new StandardKernel(new AcceptingWorkflowBindingModule(ServiceTypeEnum.UserService)).Get<AcceptingWorkflowManager>();
                    wfManager.Save(ars);
                }
                #endregion

                #region TODO: Shares Inventarization
                /////---Если это режим инвентаризации, перечень всех приложений на утверждение Техответственных
                //if (bool.Parse(System.Configuration.ConfigurationManager.AppSettings["learningMode"]) == true)
                //{
                //    ///---Создаю пачку запросов с одним Secret для 
                //    List<AcceptRequest> ars = new List<AcceptRequest>();
                //    string Secret = Guid.NewGuid().ToString();
                //    foreach (var app in data.Applications.Where(x => x.UserClamed == true).ToList())
                //    {
                //        AcceptRequest ar = new AcceptRequest()
                //        {
                //            ServiceId = app.Id,
                //            ServiceType = ServiceTypeEnum.UserService,
                //            RequestDate = DateTime.Now,
                //            UserId = user.sAMAccountName,
                //            RequestStage = StageEnum.NewRequest,
                //            Secret = Secret
                //        };
                //        ars.Add(ar);
                //    }

                //    var wfManager = new StandardKernel(new AcceptingWorkflowBindingModule(ServiceTypeEnum.UserService)).Get<AcceptingWorkflowManager>();
                //    wfManager.Save(ars);
                //}
                #endregion
            }
        }


        #region FOR DELETE
        ///// <summary>
        ///// Запрос на утверждение заявки соответствующим подразделением
        ///// </summary>
        //public void ApproveClaimRequest(string username, string serviceId)
        //{
        //    using (OfficeMapper.Models.PhonesDataContext model = new Models.PhonesDataContext())
        //    {
        //        string secret = Guid.NewGuid().ToString();
        //        Models.UserService service = model.UserServices.First(x => x.Id == serviceId);
        //        Models.User user = model.Users.First(x => x.sAMAccountName == username);

        //        model.RequestTickets.InsertOnSubmit(new Models.RequestTicket() {
        //            acceptRole = Models.AcceptRoleEnum.it.ToString(),
        //            secret = secret,
        //            serviceId = serviceId,
        //            UserService = service,
        //            User = user
        //        });
        //        model.SubmitChanges();

        //        Lib.MailHelper.SendClaimRequest(secret);
        //    }
        //}

        ///// <summary>
        ///// Запрос на подтверждение ПО по соответствующим техответственным
        ///// </summary>
        //public void ApproveClaimRequest(string username, List<string> servicesId)
        //{
        //    using (OfficeMapper.Models.PhonesDataContext model = new Models.PhonesDataContext())
        //    {
        //        List<string> technicalResponsive = new List<string>();

        //        ///---Готовлю списки технически ответственных
        //        foreach (string resp in
        //            model.UserServices.Where(x => servicesId.Contains(x.Id))
        //            .Select(x => x.TechnicalResponsible).ToList())
        //        {
        //            if(!resp.Contains(','))
        //            {
        //                technicalResponsive.Add(resp.Trim());
        //            }
        //            else
        //            {
        //                technicalResponsive.AddRange(resp.Trim().Split(',').Select(x => x.Trim()));
        //            }
        //        }

        //        ///---Для каждого списка создаю заявку
        //        foreach (string resp in technicalResponsive.Distinct())
        //        {
        //            string secret = Guid.NewGuid().ToString();
        //            foreach (var s in model.AssignedServices.Where(x=>x.User.sAMAccountName == username 
        //            && x.UserService.TechnicalResponsible.Contains(resp)))
        //            {
        //                model.RequestTickets.InsertOnSubmit(new Models.RequestTicket()
        //                {
        //                    acceptRole = Models.AcceptRoleEnum.it.ToString(),
        //                    secret = secret,
        //                    serviceId = s.UserService.Id,
        //                    userId = s.User.Id                            
        //                });
        //                model.SubmitChanges();
        //            }

        //            ///---Отправляю письмо  на подтвержение группе ответственных специалистов
        //            Lib.MailHelper.SendClaimRequestToGroup(secret, resp);
        //        }
        //    }
        //}

        ///// <summary>
        ///// Утверждение заявки соответствующим подразделением
        ///// </summary>
        ///// <param name="secret"></param>
        //public void ApprovingClaim(string secret, string approvedBy)
        //{
        //    using (OfficeMapper.Models.PhonesDataContext model = new Models.PhonesDataContext())
        //    {
        //        var requestTicket = model.RequestTickets.First(x => x.secret == secret);
        //        var assignedService = model.AssignedServices.First(x => x.UserId == requestTicket.userId
        //        && x.UserId == requestTicket.userId);

        //        ///---Если система работает в режиме обучения, помечаю Техответственного, 
        //        ///---который подтвердил слова пользователя о приложениях
        //        if(bool.Parse(System.Configuration.ConfigurationManager.AppSettings["learningMode"]))
        //        {
        //            assignedService.TechnicalResponsibleApproved = true;
        //            assignedService.TechnicalResponsibleApprovedBy = approvedBy;

        //            ///---Удаляю запрос на подтверждение как окончание процесса
        //            model.RequestTickets.DeleteOnSubmit(requestTicket);

        //            ///---Логирую данные процесса подтверждения
        //            model.ActionLogs.InsertOnSubmit(new Models.ActionLog() {
        //                actionBy = approvedBy,
        //                actionName = Models.ActionEnum.Approve.ToString(),
        //                actionDate = DateTime.Now,
        //                serviceId = assignedService.ServiceId,
        //                username = requestTicket.User.sAMAccountName                        
        //            });
        //        }
        //        else { 
        //            ///--Определяю стадию подтверждения и подтверждаю
        //            if (assignedService.ITApproved == null)
        //            {
        //                assignedService.ITApproved = true;
        //                assignedService.ITApprovedBy = approvedBy;
        //                Lib.MailHelper.SendClaimRequest(secret);
        //            }
        //            else
        //            {
        //                if (assignedService.OwnerApproved == null && MailHelper.ExistsApproval(Models.AcceptRoleEnum.owner.ToString(), assignedService.ServiceId))
        //                {
        //                    assignedService.OwnerApproved = true;
        //                    assignedService.OwnerApprovedBy = approvedBy;
        //                    ApprovingComplete(secret);
        //                }
        //            }
        //        }
        //        model.SubmitChanges();                
        //    }
        //}

        ///// <summary>
        ///// Отказ в предосталвлении сервиса
        ///// </summary>
        ///// <param name="secret"></param>
        ///// <param name="declinedBy"></param>
        //public void DecliningClaim(string secret, string declinedBy)
        //{
        //    using (OfficeMapper.Models.PhonesDataContext model = new Models.PhonesDataContext())
        //    {
        //        Lib.MailHelper.SendDeclineMessage(secret,declinedBy);
        //        model.RequestTickets.DeleteOnSubmit(model.RequestTickets.First(x => x.secret == secret));
        //        model.SubmitChanges();

        //        ///---Логирую данные процесса подтверждения
        //        var assignedService = model.RequestTickets.First(x => x.secret == secret).UserService;
        //        var user = model.RequestTickets.First(x => x.secret == secret).User;
        //        model.ActionLogs.InsertOnSubmit(new Models.ActionLog()
        //        {
        //            actionBy = declinedBy,
        //            actionName = Models.ActionEnum.Decline.ToString(),
        //            actionDate = DateTime.Now,
        //            serviceId = assignedService.Id,
        //            username = user.sAMAccountName
        //        });
        //    }
        //}

        ///// <summary>
        ///// Удаляю заявку на доступ и отправляю заявку на ServiceDesk
        ///// </summary>
        ///// <param name="secret"></param>
        //public void ApprovingComplete(string secret)
        //{
        //    using (OfficeMapper.Models.PhonesDataContext model = new Models.PhonesDataContext())
        //    {
        //        ///---Отправляю заявку на ServiceDesk           
        //        MailHelper.SendServiceDeskRequest(secret);

        //        ///---Удаляю запрос на подтверждение как окончание процесса
        //        var requestTicket = model.RequestTickets.First(x => x.secret == secret);
        //        model.RequestTickets.DeleteOnSubmit(requestTicket);
        //    }
        //}
        #endregion

        /// <summary>
        /// Определяет показывать ли данное приложение для указанного пользователя
        /// </summary>
        /// <param name="department"></param>
        /// <param name="userDepartment"></param>
        /// <returns></returns>
        private bool ApplicationFilter(string department, string username)
        {
            string userDepartment = username.Split('@').Count() > 1
                ? username.Split('@')[1] : "";
            if (department == null) return true;
            if(department == "Кременчуг" && userDepartment == "kremen") return true;
            if (department == "Львів" && userDepartment == "lviv") return true;
            if (department == "Одеса" && userDepartment == "odesa") return true;
            return false;
        }

        /// <summary>
        /// Проверяю есть ли в базе запросы по данному тикету
        /// </summary>
        /// <param name="secret"></param>
        /// <returns></returns>
        public static bool SecretExists(string secret)
        {
            using (OfficeMapper.Models.PhonesDataContext model = new Models.PhonesDataContext())
            {
                return model.RequestTickets.Count(x => x.secret == secret) > 0;
            }
        }

    }
}