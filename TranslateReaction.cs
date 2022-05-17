using DAL;
using DAL.WSLawyer;
using Microsoft.eShopOnContainers.BuildingBlocks.EventBus.Abstractions;
using RobotCuratorApi.IntegrationEvents.Events;
using RobotCuratorApi.IntegrationEvents.Handlers;
using RobotCuratorApi.Models;
using RobotCuratorApi.RobotServices.Abstractions;
using RobotCuratorApi.RobotServices.MailService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RobotCuratorApi.RobotServices.DecisionTableTaskImpl
{
    public interface ITranslateReaction
    {
        Task<string> React(ReactionIntegrationEvent @event);
    }

    public class TranslateReaction : ITranslateReaction
    {
        private readonly DecisionTableServiceLetsFormInputForActionWhenClose _decisionTable;
        private readonly DecisionTableAutoClaimGasServiceWhenClose _decisionTableAutoClaimGasServiceWhenClose;
        private readonly WsLawyerDbContext _context;

        public TranslateReaction(DecisionTableServiceLetsFormInputForActionWhenClose decisionTable,
            DecisionTableAutoClaimGasServiceWhenClose decisionTableAutoClaimGasServiceWhenClose,
            WsLawyerDbContext context)
        {
            _decisionTable = decisionTable;
            _decisionTableAutoClaimGasServiceWhenClose = decisionTableAutoClaimGasServiceWhenClose;
            _context = context;
        }

        public async Task<string> React(ReactionIntegrationEvent @event)
        {            
            var docSubTypeId = @event.DocSubTypeId;
            var projectId = @event.ProjectId;
            var caseId = @event.CaseId;
            var isScanOrCopy = @event.IsScanOrCopy;

            var currentCase = _context.Case.FirstOrDefault(x=>x.Id == caseId && x.StatusType == 20);
            if (currentCase == null)
                return "Incorrect case";

            GetReactionsInput getReactionsInput = new GetReactionsInput
            {
                DocSubTypeID = docSubTypeId,
                ProjectID = projectId,
                forScanOrCopy = isScanOrCopy,
                forOriginal = !isScanOrCopy
            };
            var getReactionsOutput = await _decisionTable.GetReactionsOutput(getReactionsInput);
            if (getReactionsOutput == null)
                return "Not found";

            var calledMethods = getReactionsOutput.CalledMethods.Replace(" ", "").Split(',');

            Dictionary<string, string> dict = new Dictionary<string, string>();
            dict.Add("caseid", caseId.ToString());
            dict.Add("dateExecution", @event.CreationDate.ToString());
            int taskId = -1;
            _decisionTableAutoClaimGasServiceWhenClose.RunOurProcessesWhenReactionCome(calledMethods, dict, taskId);

            var taskListString = "";
            var taskCount = 1;
            try
            {
                foreach (var method in calledMethods)
                {
                    var task = method.Substring(method.IndexOf("CreateTask") + "CreateTask".Length);
                    task = task.Substring(0, task.IndexOf("_"));
                    var taskData = _context.Status.FirstOrDefault(x => x.ColumnListId == 202 && x.Value == Convert.ToInt32(task));
                    if (taskData != null)

                        taskListString += $"{taskCount}. {taskData.Name} ";
                    taskCount++;
                }
            }
            catch
            {
                //log
            }

            #region ////if reaction was found then send mail
            new EmailService().SendMail("stefanov@quorumlegal.ru",
                            $"Уведомление о задачах по АУ (проект {GetProjectNameById(projectId)})",
                            $@"Добрый день! По делу №{GetCaseNumById(caseId)} {DateTime.Now.Date.ToString("dd.MM.yyyy")} {GetCaseSubjectsById(caseId)} поступил документ {GetDocSubTypeNameById(docSubTypeId)}. Назначены задачи: {taskListString}",
                            new List<string>
                            {
                                //"buzhan@quorumlegal.ru",
                                GetCuratorEmailByProjectId(projectId),
                                "privalova@quorumlegal.ru",
                                });
            #endregion

            return getReactionsOutput?.CalledMethods;
        }

        private string GetCaseSubjectsById(int caseId)
        {
            try
            {
                var res = "";
                var subjects = _context.SubjectCase
                    .Where(x => x.CaseId == caseId);
                if (subjects!=null && subjects.Any())
                {
                    var sub1 = subjects.Where(x => x.Status == 10);
                    if(sub1 != null && sub1.Any())
                    {
                        res += "Заявители: ";
                        foreach(var subject in sub1)
                        {
                            var subjectData = _context.Subject.FirstOrDefault(x => x.Id == subject.SubjectId);
                            if (subjectData != null)
                                res += (string.IsNullOrEmpty(subjectData.Name) ?
                                    (string.IsNullOrEmpty(subjectData.FullName) ? "" : subjectData.FullName + "; ")
                                    : subjectData.Name + "; ");
                        }
                    }

                    var sub2 = subjects.Where(x => x.Status == 20);
                    if (sub2 != null && sub2.Any())
                    {
                        res += "Должники: ";
                        foreach (var subject in sub2)
                        {
                            var subjectData = _context.Subject.FirstOrDefault(x => x.Id == subject.SubjectId);
                            if (subjectData != null)
                                res += (string.IsNullOrEmpty(subjectData.Name) ?
                                    (string.IsNullOrEmpty(subjectData.FullName) ? "" : subjectData.FullName + "; ")
                                    : subjectData.Name + "; ");
                        }
                    }

                    var sub3 = subjects.Where(x => x.Status == 30);
                    if (sub3 != null && sub3.Any())
                    {
                        res += "Третьи лица: ";
                        foreach (var subject in sub1)
                        {
                            var subjectData = _context.Subject.FirstOrDefault(x => x.Id == subject.SubjectId);
                            if (subjectData != null)
                                res += (string.IsNullOrEmpty(subjectData.Name) ?
                                    (string.IsNullOrEmpty(subjectData.FullName) ? "" : subjectData.FullName + "; ")
                                    : subjectData.Name + "; ");
                        }
                    }

                }

                return res=="" ? res : $"({res})";
            }
            catch
            {
                return "";
            }
        }

        private string GetCaseNumById(int caseId)
        {
            try
            {
                var currentCase = _context.Case.FirstOrDefault(x => x.Id == caseId);
                if (currentCase == null)
                    return string.Empty;
                return currentCase.NumberCase.ToString();
            }
            catch
            {
                return "";
            }
        }

        private string GetDocSubTypeNameById(int docSubTypeId)
        {
            try
            {
                var doc = _context.DocumentWorkFlowSubType.FirstOrDefault(x => x.ID == docSubTypeId);
                if (doc == null)
                    return docSubTypeId.ToString();
                return $"\"{doc.Description}\"";
            }
            catch (Exception ex)
            {
                switch (docSubTypeId)
                {
                    case 190: return "\"Требования о включении в реестр кредиторов\"";
                    case 191: return "\"Заявление о привлечении к субсидиарной ответственности\"";
                    case 192: return "\"Запрос от правоохранительных органов\"";
                    case 193: return "\"Запрос Росреестра\"";
                    case 194: return "\"Запрос арбитражного управляющего\"";
                    case 195: return "\"Запрос от кредитора/запрос выписки из РТК\"";
                    case 196: return "\"Иной запрос\"";
                    case 197: return "\"Заявление об оспаривании сделки должника\"";
                    default: return docSubTypeId.ToString();
                }
            }
        }

        private string GetProjectNameById(int projectId)
        {
            try
            {
                var project = _context.Project.FirstOrDefault(x => x.Id == projectId);
                if (project == null)
                    return projectId.ToString();
                return project.Name;
            }
            catch
            {
                return projectId.ToString();
            }
        }

        private static string defaultEmail = "stefanov@quorumlegal.ru";

        private string GetCuratorEmailByProjectId(int projectId)
        {
            try
            {

                var curator = _context.BankruptUser.FirstOrDefault(x => x.ProjectID == projectId);
                if (curator != null)
                {
                    var user = _context.User.FirstOrDefault(x => x.Id == curator.UserID);
                    if (user != null)
                    {
                        if (!string.IsNullOrEmpty(user.Email))
                            return user.Email;
                    }
                }
                return defaultEmail;
            }
            catch
            {
                return defaultEmail;    
            }
        }
    }
}
