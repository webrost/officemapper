using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OfficeMapper.Lib
{
    public class UserHelper
    {
        /// <summary>
        /// Проверка является ли пользователь согласователем IT
        /// </summary>
        /// <param name="fullusername"></param>
        /// <returns></returns>
        public static bool IsIT(string fullusername)
        {
            using (Models.PhonesDataContext model = new Models.PhonesDataContext())
            {
                List<string> responsibles = new List<string>();
                foreach(string resp in model.UserServices.Select(x => x.TechnicalResponsible)
                    .ToList())
                {
                    foreach(string r in resp.Split(','))
                    {
                        responsibles.Add(r.Trim());
                    }                    
                }

                return model.Acceptors.Where(x => responsibles.Contains(x.poolname))
                    .ToList().Count(x => x.username == fullusername) > 0;
            }
        }
        
    }
}