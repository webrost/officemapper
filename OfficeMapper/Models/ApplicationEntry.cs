using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace OfficeMapper.Models
{
    public class ApplicationEntry
    {
        [Display(Name = "Заказано пользователем")]
        public bool UserClamed { get; set; }

        [Display(Name = "Код сервиса")]
        public string Id { get; set; }

        [Display(Name = "Группа/сервис")]
        public string Type { get; set; }

        [Display(Name = "Наименование")]
        public string Name { get; set; }

        [Display(Name = "Ссылка на документацию приложения")]
        public string HelpDocumentationLink { get; set; }

        [Display(Name = "Пул")]
        public Pool Pool { get; set; }

        [Display(Name = "Владелец данных/системы")]
        public AuthUser DataOwner { get; set; }

        [Display(Name = "Класс информации приложения")]
        public string InformationClass { get; set; }

        [Display(Name = "Описание")]
        public string Description { get; set; }

        [Display(Name = "Утверждено от IT")]
        public AuthUser AcceptedITBy { get; set; }
        public bool AcceptedIT { get; set; }

        [Display(Name = "Утверждено владельцем данных")]
        public AuthUser AcceptedOwnerBy { get; set; }
        public bool AcceptedOwner { get; set; }
        public bool NeedAcceptedOwner { get; set; }
        public string DeprecationText { get; set; }
        public bool AcceptedTechnicalResponsible { get; set; }

    }
}