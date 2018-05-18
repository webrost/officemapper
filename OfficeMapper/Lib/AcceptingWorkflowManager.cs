using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OfficeMapper.Lib
{
    public class AcceptingWorkflowManager
    {
        readonly IBaseUserService service;

        public AcceptingWorkflowManager(IBaseUserService service)
        {
            this.service = service;
        }

        public void Save(AcceptRequest ar)
        {
            service.Save(ar);
            PushNextStage(ar.Secret);
        }

        /// <summary>
        /// ИНВЕНТАРИЗАЦИЯ. Сохраняю перечень заявок на подтверждение.
        /// </summary>
        /// <param name="ars"></param>
        public void Save(List<AcceptRequest> ars)
        {
            service.Save(ars);
            PushNextStage(ars.First().Secret);
        }

        /// <summary>
        /// Отправляю заявку дальше по маршруту
        /// </summary>
        /// <param name="secret"></param>
        /// <returns></returns>
        public StageEnum PushNextStage(string secret, Models.DecisionData data = null)
        {
            StageEnum nextStage = service.PushNextStage(secret, data);

            ///---Обновляю статус заявки в базе
            using (Models.PhonesDataContext model = new Models.PhonesDataContext())
            {
                foreach(var rt in model.RequestTickets.Where(x => x.secret == secret))
                {
                    rt.RequestStage = nextStage.ToString();
                }
                model.SubmitChanges();
            }
            return nextStage;
        }


        //-------------------------------------------------------------------------------------------------------------
        //-- Private methods
        //-------------------------------------------------------------------------------------------------------------        
        /// <summary>
        /// Текущая стадия заявки
        /// </summary>
        /// <param name="secret"></param>
        /// <returns></returns>
        StageEnum GetStage(string secret)
        {
            return StageEnum.NewRequest;
        }

        /// <summary>
        /// Возвращает список согласовантов для следующей стадии
        /// </summary>
        /// <param name="secret"></param>
        /// <returns></returns>
        List<string> GetAcceptorsEmail(string secret)
        {
            return new List<string>();
        }


    }
}