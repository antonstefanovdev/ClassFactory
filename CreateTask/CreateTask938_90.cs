using RobotCuratorApi.RobotServices.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DAL;
using Microsoft.AspNetCore.Identity;

namespace RobotCuratorApi.RobotServices.DecisionTableTaskImpl
{
    public class CreateTask938_90 : ProcessMethodAfterCloseTask
    {
        public override string Name { get; set; }

        public override string Process(int taskID, Dictionary<string, string> dict, IUnitOfWork _unitOfWork)
        {
            if (dict.ContainsKey("caseid") && dict.ContainsKey("dateExecution"))
            {
                var caseid_val = dict["caseid"];
                var DateExecution_val = dict["dateExecution"];
                var caseid = 0;
                var date = new DateTime();

                if (Int32.TryParse(caseid_val, out caseid) && DateTime.TryParse(DateExecution_val, out date))
                    return _unitOfWork.CaseRepository.CreateTask938_90(taskID, caseid, date, caseid); //executivecaseid
                else return "Ошибка анализа результата задачи.";
            }
            else return "Ошибка анализа результата задачи.";


            //throw new NotImplementedException();
        }
    }
}
