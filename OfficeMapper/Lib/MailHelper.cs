using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net;
using System.Net.Mail;
using System.IO;
using OpenPop.Common;
using OpenPop.Mime;
using OpenPop.Mime.Header;
using OpenPop.Pop3;
using Ninject;

namespace OfficeMapper.Lib
{
    public class MailHelper
    {
        /// <summary>
        /// Отправляет заявку на подтверждение группе, которая должна на данном этапе подтвердить заявку
        /// </summary>
        /// <param name="secret"></param>
        public static void SendClaimRequest(string secret)
        {
            using (OfficeMapper.Models.PhonesDataContext model = new Models.PhonesDataContext())
            {
                
                var service = model.RequestTickets.First(x => x.secret == secret).UserService;
                var user = model.RequestTickets.First(x => x.secret == secret).User;
                var userEmail = user.Email != null && user.Email != "" && user.Email.Contains("@")
                    ? user.Email : System.Configuration.ConfigurationManager.AppSettings["sendMessageFrom"].ToString(); 

                string smtpServer = System.Configuration.ConfigurationManager.AppSettings["smtpServer"].ToString();
                SmtpClient client = new SmtpClient(smtpServer);

                MailMessage message = new MailMessage();
                message.From = new MailAddress(userEmail);

                ///---Получение списка согласователей, согласно workflow
                List<string> approvers = GetApproversEmail(secret);
                if(approvers.Count > 0)
                {
                    foreach (var toEmail in approvers)
                    {
                        message.To.Add(new MailAddress(toEmail));
                    }

                    string filePath = HttpContext.Current.Server.MapPath("~/Lib/ApproveMessageTemplate.html");
                    message.Body = File.ReadAllText(filePath);
                    message.Subject = System.Configuration.ConfigurationManager.AppSettings["RequestMessageSubject"] + " №"+ service.Id + service.ServiceName;
                    message.Body = message.Body
                        .Replace("%%username%%", user.UTNLogin)
                        .Replace("%%FIO%%", user.FIO)
                        .Replace("%%post%%", user.Post)
                        .Replace("%%email%%",user.Email)
                        .Replace("%%applicationName%%", service.ServiceName)
                        .Replace("%%applicationId%%", service.Id)
                        .Replace("%%applicationDescription%%", service.Description)
                        .Replace("%%applicationDeprecationText%%", service.DeprecationText)
                        .Replace("%%secret%%", secret)
                        .Replace("%%username%% %%FIO%% %%post%% %%email%%", "")
                        .Replace("%%username%% %%FIO%% %%post%% %%email%%", "");

                    message.Headers.Add("XSecret", secret);

                    message.IsBodyHtml = true;
                    client.Send(message);
                }
            }

        }

        /// <summary>
        /// ИНВЕНТАРИЗАЦИЯ Отправляет заявку на подтверждение группе технически ответственных 
        /// </summary>
        /// <param name="secret"></param>
        public static void SendClaimRequestToGroup(List<IBaseUserService> services, List<Models.AuthUser> approvers, Models.User whom)
        {
            using (OfficeMapper.Models.PhonesDataContext model = new Models.PhonesDataContext())
            {
                var userEmail = System.Configuration.ConfigurationManager.AppSettings["sendMessageFrom"].ToString();
                string smtpServer = System.Configuration.ConfigurationManager.AppSettings["smtpServer"].ToString();
                SmtpClient client = new SmtpClient(smtpServer);

                MailMessage message = new MailMessage();
                message.From = new MailAddress(userEmail);

                if (approvers.Count > 0)
                {
                    foreach (var toEmail in approvers)
                    {
                        message.To.Add(new MailAddress(toEmail.email));
                    }

                    string applications = "";

                    ///---Формирую перечень приложений
                    foreach(var app in services)
                    {
                        applications += string.Format("<tr><td>{0}</td><td>{1}</td><td>{2}</td><td>{3}</td></tr>"
                            ,app.Id
                            ,app.ServiceName
                            ,app.Description
                            ,app.DeprecationText);
                    }

                    string filePath = HttpContext.Current.Server.MapPath("~/Lib/TechApproveMessageTemplate.html");
                    message.Body = File.ReadAllText(filePath);
                    message.Subject = "Підтвердження інвентаризації програмно-апаратних засобів";
                    message.Body = message.Body
                        .Replace("%%username%%", whom.UTNLogin)
                        .Replace("%%FIO%%", whom.FIO)
                        .Replace("%%post%%", whom.Post)
                        .Replace("%%email%%", whom.Email)
                        .Replace("%%applications%%", applications)
                        .Replace("%%AccessKey%%", whom.AccessKey)
                        .Replace("%%secret%%", services.First().secret);

                    message.Headers.Add("XSecret", services.First().secret);

                    message.IsBodyHtml = true;
                    client.Send(message);
                }
            }

        }

        /// <summary>
        /// Отправляет заявку на подтверждение группе, которая должна на данном этапе подтвердить заявку
        /// </summary>
        /// <param name="secret"></param>
        public static void SendServiceDeskRequest(string secret)
        {
            using (OfficeMapper.Models.PhonesDataContext model = new Models.PhonesDataContext())
            {
                string serviceDeskEmail = System.Configuration.ConfigurationManager.AppSettings["serviceDeskEmail"].ToString();
                string sendMessageFrom = System.Configuration.ConfigurationManager.AppSettings["sendMessageFrom"].ToString();

                var service = model.RequestTickets.First(x => x.secret == secret).UserService;
                var user = model.RequestTickets.First(x => x.secret == secret).User;


                string smtpServer = System.Configuration.ConfigurationManager.AppSettings["smtpServer"].ToString();
                SmtpClient client = new SmtpClient(smtpServer);

                MailMessage message = new MailMessage();
                message.From = new MailAddress(sendMessageFrom);
                message.To.Add(new MailAddress(serviceDeskEmail));

                string filePath = HttpContext.Current.Server.MapPath("~/Lib/ServiceDeskRequestTemplate.html");
                message.Body = File.ReadAllText(filePath);
                message.Body = message.Body
                    .Replace("%%username%%", user.UTNLogin)
                    .Replace("%%FIO%%", user.FIO)
                    .Replace("%%post%%", user.Post)
                    .Replace("%%email%%", user.Email)
                    .Replace("%%username%% %%FIO%% %%post%% %%email%%", "")
                    .Replace("%%username%% %%FIO%% %%post%% %%email%%", "")
                    .Replace("%%username%% %%FIO%% %%post%% %%email%%", "")
                    .Replace("%%username%% %%FIO%% %%post%% %%email%%", "")
                    .Replace("%%username%% %%FIO%% %%post%% %%email%%", "");

                message.IsBodyHtml = true;
                //client.Send(message);
            }

        }

        /// <summary>
        /// Возвращет список пользователей, которые утверждают пользование приложением
        /// </summary>
        /// <param name="secret"></param>
        /// <returns></returns>
        public static List<string> GetApproversEmail(string secret)
        {
            using (OfficeMapper.Models.PhonesDataContext model = new Models.PhonesDataContext())
            {
                List<string> approvers = new List<string>();
                List<string> approverNames = new List<string>();

                var service = model.RequestTickets.First(x => x.secret == secret).UserService;
                var assignedService = model.RequestTickets.First(x => x.secret == secret)
                    .User.AssignedServices.First(x => x.ServiceId == service.Id);

                ///---Определяю режим ОБУЧЕНИЕ/ПРЕДОСТАВЛЕНИЕ ДОСТУПА
                bool learningMode = bool.Parse(System.Configuration.ConfigurationManager.AppSettings["learningMode"]);
                if(!learningMode)
                {
                    ///--Определяю стадию подтверждения: имя роли для рассылки
                    string approvingRole = "";
                    if (assignedService.ITApproved == null)
                    {
                        approvingRole = Models.AcceptRoleEnum.it.ToString();
                    }
                    else
                    {
                        if (assignedService.OwnerApproved == null && ExistsApproval(Models.AcceptRoleEnum.owner.ToString(), service.Id))
                        {
                            approvingRole = Models.AcceptRoleEnum.owner.ToString();
                        }
                    }

                    ///---Если IT подтвердило - шлем подтверждение собственникам, иначе IT
                    switch (approvingRole)
                    {
                        case "it":
                            approverNames = model.Acceptors.Where(x => x.acceptSequense == Models.AcceptRoleEnum.owner.ToString())
                                .ToList()
                                .Select(x => x.username).ToList();
                            break;
                        case "owner":
                            approverNames = model.Acceptors.Where(x => x.acceptSequense == Models.AcceptRoleEnum.it.ToString())
                                .ToList()
                                .Select(x => x.username).ToList();
                            break;
                    }

                }
                else
                {
                    ///---Рассылка сообщения техническим ответственным, чтобы они подтвердили,
                    ///---что пользователь использует приложение
                    foreach(var techPool in service.TechnicalResponsible.Split(',').Select(x=>x.Trim()))
                    {
                        approverNames.AddRange(model.Acceptors.Where(x => x.poolname == techPool).Select(x => x.username).ToList());
                    }
                }


                ///--Проверяю наличие адресов для рассылки
                foreach (var email in model.Users.Where(x => approverNames.Contains(x.UTNLogin)).Select(x => x.Email).ToList())
                {
                    if(!string.IsNullOrEmpty(email) && email.Contains("@"))
                    {
                        approvers.Add(email);
                    }
                }
                
                return approvers;
            }

        }

        /// <summary>
        /// Определяю есть ли для данной роли эпруверы
        /// </summary>
        /// <param name="roleName"></param>
        /// <returns></returns>
        public static bool ExistsApproval(string roleName, string serviceId)
        {
            using (OfficeMapper.Models.PhonesDataContext model = new Models.PhonesDataContext())
            {
                if(model.Acceptors.Count(x=>x.acceptSequense == roleName && x.serviceId == serviceId) > 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Проверяет почтовый ящик на предмет ПОДТВЕРЖДЕНИЯ/ОТКЛОНЕНИЯ доступа к приложениям
        /// </summary>
        public static void RunMailHandler()
        {
            string server = System.Configuration.ConfigurationManager.AppSettings["smtpServer"];
            string approveserviceUser = System.Configuration.ConfigurationManager.AppSettings["approveserviceUser"];
            string approveservicePassword = System.Configuration.ConfigurationManager.AppSettings["approveservicePassword"];
            string declineserviceUser = System.Configuration.ConfigurationManager.AppSettings["declineserviceUser"];
            string declineservicePassword = System.Configuration.ConfigurationManager.AppSettings["declineservicePassword"];
            Lib.ApplicationHelper helper = new ApplicationHelper();

            ///---Сначала обрабатываю отказы
            List<Models.ClamedServices> declinedServices = GetClaimResult(server, declineserviceUser, declineservicePassword);
            foreach(var declinedService in declinedServices)
            {
                ///---Сохраняю запрос
                Models.DecisionData data = new Models.DecisionData() {
                    Decision = Models.DecisionEnum.Decline,
                    Acceptor = declinedService.user
                };
                var wfManager = new StandardKernel(new AcceptingWorkflowBindingModule(declinedService.secret)).Get<AcceptingWorkflowManager>();
                wfManager.PushNextStage(declinedService.secret, data);

            }

            ///---Потом обрабатываю подтверждения
            List<Models.ClamedServices> acceptedServices = GetClaimResult(server, approveserviceUser, approveservicePassword);
            foreach (var acceptedService in acceptedServices)
            {
                ///---Сохраняю запрос
                Models.DecisionData data = new Models.DecisionData()
                {
                    Decision = Models.DecisionEnum.Accept,
                    Acceptor = acceptedService.user
                };
                var wfManager = new StandardKernel(new AcceptingWorkflowBindingModule(acceptedService.secret)).Get<AcceptingWorkflowManager>();
                wfManager.PushNextStage(acceptedService.secret, data);
            }
        }

        /// <summary>
        /// Запуск процесса обработки почтового ящика
        /// </summary>
        public static void RunMailHandlerThread()
        {
            System.Threading.Thread thread = new System.Threading.Thread(MailHandlerThread);
            thread.Start();
        }

        /// <summary>
        /// Циклический процесс проверки почтового ящика
        /// </summary>
        public static void MailHandlerThread()
        {
            while(true)
            {
                RunMailHandler();
                System.Threading.Thread.Sleep(300000);
            }
        }



        /// <summary>
        /// Возвращает перечень подтвержденных/неподтвержденных заявок
        /// </summary>
        /// <param name="email"></param>
        /// <param name="user"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public static List<Models.ClamedServices> GetClaimResult(string server, string user, string password)
        {
            List<Models.ClamedServices> claims = new List<Models.ClamedServices>();
            Pop3Client client = new Pop3Client();

            ///---Проверка ящика с подтвержденными сообщениями
            if (client.Connected) client.Disconnect();
            client.Connect(server, 110, false);
            client.Authenticate(user, password);
            int count = client.GetMessageCount();
            List<string> uids = client.GetMessageUids();
            List<string> seenUids = GetSeenUIDs();

            for (int i = 0; i < uids.Count; i++)
            {
                string currentUidOnServer = uids[i];
                if (!seenUids.Contains(currentUidOnServer))
                {
                    try
                    {
                        string body;
                        Message unseenMessage = client.GetMessage(i+1);
                        MessagePart plainTextPart = unseenMessage.FindFirstPlainTextVersion();
                        if (plainTextPart != null)
                        {
                            body = plainTextPart.GetBodyAsText();

                            ///---Проверяю сообщение на предмет валидного secret.
                            if (body.Trim().Length >= 42)
                            {
                                MessageHeader headers = client.GetMessageHeaders(i+1);
                                string secret = body.Trim().Substring(6, body.Trim().Length - 6);

                                ///---Проверяю есть ли в базе запрос с данным secret
                                if (Lib.ApplicationHelper.SecretExists(secret))
                                {
                                    claims.Add(new Models.ClamedServices()
                                    {
                                        secret = secret,
                                        user = Lib.ADAuth.GetUserByEmail(headers.From.Address.Trim())
                                    });
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                    }

                    ///---Помечаю сообщение как прочитанное
                    MarkMessageAsReaded(currentUidOnServer);
                }
            } 

            return claims;
        }

        /// <summary>
        /// Возвращаю перечень UIDs прочитанных сообщений
        /// </summary>
        /// <param name="secret"></param>
        /// <returns></returns>
        public static List<string> GetSeenUIDs()
        {
            using (OfficeMapper.Models.PhonesDataContext model = new Models.PhonesDataContext())
            {
                return model.ReadedMailMessages.Select(x => x.uid).ToList();
            }
        }


        /// <summary>
        /// Помечаю сообщение как прочитанное
        /// </summary>
        /// <param name="secret"></param>
        /// <returns></returns>
        public static void MarkMessageAsReaded(string uid)
        {
            using (OfficeMapper.Models.PhonesDataContext model = new Models.PhonesDataContext())
            {
                model.ReadedMailMessages.InsertOnSubmit(new Models.ReadedMailMessage()
                {
                    uid = uid
                });
                model.SubmitChanges();
            }
        }

        /// <summary>
        /// Отправляю пользователю сообщение об отказе
        /// <returns></returns>
        public static void SendDeclineMessage(string secret, string declinedBy)
        {
            using (OfficeMapper.Models.PhonesDataContext model = new Models.PhonesDataContext())
            {
                var service = model.RequestTickets.First(x => x.secret == secret).UserService;
                var user = model.RequestTickets.First(x => x.secret == secret).User;
                var userDeclinedBy = model.Users.First(x => x.UTNLogin == declinedBy);
                var userEmail = user.Email != null && user.Email != "" && user.Email.Contains("@")
                    ? user.Email : System.Configuration.ConfigurationManager.AppSettings["sendMessageFrom"].ToString();

                string smtpServer = System.Configuration.ConfigurationManager.AppSettings["smtpServer"].ToString();
                SmtpClient client = new SmtpClient(smtpServer);

                MailMessage message = new MailMessage();
                message.From = new MailAddress(userDeclinedBy.Email);
                ///---Отправка сообщения пользователю об отказе предоставления доступа
                message.To.Add(new MailAddress(userEmail));

                string filePath = HttpContext.Current.Server.MapPath("~/Lib/DeclinedMessageTemplate.html");
                message.Body = File.ReadAllText(filePath);
                message.Subject = System.Configuration.ConfigurationManager.AppSettings["DeclineMessageSubject"] + " №" + service.Id + service.ServiceName;
                message.Body = message.Body
                    .Replace("%%username%%", user.UTNLogin)
                    .Replace("%%FIO%%", user.FIO)
                    .Replace("%%post%%", user.Post)
                    .Replace("%%email%%", user.Email)
                    .Replace("%%applicationName%%", service.ServiceName)
                    .Replace("%%applicationId%%", service.Id)
                    .Replace("%%applicationDescription%%", service.Description)
                    .Replace("%%applicationDeprecationText%%", service.DeprecationText)
                    .Replace("%%declinedBy%%", userDeclinedBy.FIO)
                    .Replace("%%username%% %%FIO%% %%post%% %%email%%", "")
                    .Replace("%%username%% %%FIO%% %%post%% %%email%%", "");

                message.IsBodyHtml = true;
                client.Send(message);                
            }
        }
    }
}