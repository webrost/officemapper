using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace OfficeMapper.Models
{
    public class ApplicationView
    {
        [Display(Name = "Идентификатор приложения")]
        public string Id { get; set; }

        [Display(Name = "Наименование")]
        public AuthUser User { get; set; }

        [Display(Name = "Приложения пользователя")]
        public List<ApplicationEntry> Applications { get; set; }

        [Display(Name = "Запросы на новые приложения")]
        public List<ApplicationEntry> NewApplications { get; set; }

        [Display(Name = "Файловые ресурты")]
        public List<Models.FileShareEntry> FileShares { get; set; }

        [Display(Name = "Флаг режима приложения")]
        public bool IsLearningMode { get; set; }
    }
}