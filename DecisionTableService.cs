using Camunda.Api.Client;
using Camunda.Api.Client.DecisionDefinition;
using RobotCuratorApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore.Internal;
using Newtonsoft.Json;
using DAL;

namespace RobotCuratorApi.RobotServices
{
    public class DecisionTableService
    {
        private readonly CamundaClient _camundaClient;

        private readonly IUnitOfWork _unitOfWork;

        public DecisionTableService(IUnitOfWork unitOfWork, CamundaClient camundaClient)
        {
            _camundaClient = camundaClient;
            _unitOfWork = unitOfWork;
        }

        public int TypeOfCase { get; set; }
        public int TypeOfTask { get; set; }
        public int TaskId { get; set; }
        public int CaseId { get; set; }


        public async Task<string> ProcessAvaliableAnswers(AvailableAnswersInput availableAnswersInput)
        {
            var evaluateDecision = new EvaluateDecision
            {
                Variables = new Dictionary<string, VariableValue>()
            };
            evaluateDecision.Variables.Add("taskType", VariableValue.FromObject(availableAnswersInput.TaskType));
            evaluateDecision.Variables.Add("projectID", VariableValue.FromObject(availableAnswersInput.Project));
            evaluateDecision.Variables.Add("stage", VariableValue.FromObject(availableAnswersInput.Stage));
            evaluateDecision.Variables.Add("etapNeStadiya", VariableValue.FromObject(availableAnswersInput.EtapNeStadiya));
            evaluateDecision.Variables.Add("caseCategory", VariableValue.FromObject(availableAnswersInput.CaseCategoty));
            evaluateDecision.Variables.Add("valueOfStatusClaimRequirements", VariableValue.FromObject(availableAnswersInput.ValueOfStatusClaimRequirements));
            evaluateDecision.Variables.Add("caseType", VariableValue.FromObject(availableAnswersInput.CaseType));
            var selector = availableAnswersInput.TaskType.GetValueOrDefault();
            string bykey = "dmnCheckAvailableAnswers";
            if (selector > 900 && selector < 1000)
                bykey = "dmnCheckAvailableArbitralAnswers";
            var availableAnswers = await _camundaClient.DecisionDefinitions
                .ByKey(bykey)
                .Evaluate(evaluateDecision);
            var needecid=_unitOfWork.CaseRepository.GetSubjectExecutiveCase(availableAnswersInput.CaseId).Result;
            var availableAnswersOutput = availableAnswers.Select(a => new AvailableAnswersOutput
            {
                IdOfAnswer = a.GetValueOrDefault("answerId")?.GetValue<int>(),
                AnswerVariant = a.GetValueOrDefault("answerVariant")?.GetValue<string>(),
                Validate = a.GetValueOrDefault("Validate")?.GetValue<string>(),
                ValueOfStatusSectionAct = a.GetValueOrDefault("valueOfStatusSectionAct")?.GetValue<int>(),
                ShowListFromWhatInformationGot =
                    a.GetValueOrDefault("showListFromWhatInformationGot")?.GetValue<bool>(),
                ShowListOfDirectionOfDocument = a.GetValueOrDefault("showListOfDirectionOfDocument")?.GetValue<bool>(),
                NeedExecutive= needecid==null || needecid.Count()==0?false:a.GetValueOrDefault("NeedExecutive")?.GetValue<bool>(),
                Fileuploader = a.GetValueOrDefault("fileuploader")?.GetValue<bool>()
            });
            
            TaskId = availableAnswersInput.TaskId;
            CaseId = availableAnswersInput.CaseId;
            TypeOfCase = availableAnswersInput.CaseType.GetValueOrDefault();
            TypeOfTask = availableAnswersInput.TaskType.GetValueOrDefault();
           

            List<(string, int)> nameOfClasses = new List<(string, int)>();

            foreach (var item in availableAnswersOutput)
            {
                //string validate = item.Validate.Trim();
                if (item.Validate != null)
                {
                    string validate = item.Validate.Replace(" ", "");
                    List<string> names = validate.Split(',').ToList<string>();
                    foreach (var item1 in names)
                    {
                        if (!nameOfClasses.Contains((item1, TaskId)))
                            nameOfClasses.Add((item1, TaskId));
                        //HandleDecisionTableTaskByNames()
                    }
                }
            }


            List<(string, string)> resultOfProcesses = HandleDecisionTableTaskByNames(nameOfClasses);

            var qqq = LetsFormOurArray(availableAnswersOutput.ToList(), resultOfProcesses);
            string result = LetsFormOurJson(qqq);
            return result;

        }

       /* public async Task<GetCloserFunctionsOutput> ProcessCreateCloserFunctionsBefore(GetCloserFunctionsInput getCloserFunctionInput)
        {
            var getCloserFunctionsOutput = await GetCloserFunctionsOutput(getCloserFunctionInput);

            List<(string, int)> actionsList = new List<(string, int)>();
            //actionsList.Add((getCloserFunctionsOutput.ActionBefore, getCloserFunctionInput.TaskId));
            string[] actions;
            if (getCloserFunctionInput.IsItConfirmation == false &&
                getCloserFunctionsOutput.IfMandatoryConfirmationOfEndingTaskRequiredByAliveCurator == false)
            {
                string actionsNotNeedConfirmation =
                    getCloserFunctionsOutput.ActionAfterEndOfTaskNoManualConfirmationRequired;
                actions = actionsNotNeedConfirmation.Split(new string[] { "," }, StringSplitOptions.None);
            }
            if (getCloserFunctionInput.IsItConfirmation == false &&
                getCloserFunctionsOutput.IfMandatoryConfirmationOfEndingTaskRequiredByAliveCurator == true)
            {
                string actionsNeedConfirmation =
                    getCloserFunctionsOutput.ActionAfterEndOfTaskManualConfirmationRequiredBeforeConfirmation;
                actions = actionsNeedConfirmation.Split(new string[] { "," }, StringSplitOptions.None);
            }


            HandleDecisionTableTaskByNames(actionsList);


            return getCloserFunctionsOutput;
        }*/



       



        public List<(string, string)> HandleDecisionTableTaskByNames(List<(string, int)> nameOfClasses)
        {
            List<(string, string)> listWithClassNameAndResultOfProcess = new List<(string, string)>();
            foreach (var item in nameOfClasses)
            {
                var (className, taskId) = item;
                var decisionTableTask = DecisionTableTaskFactory.CreateClassesByName(className);
                if (decisionTableTask != null)
                {
                    //вот сюда надо складывать не бул а стирнг
                    listWithClassNameAndResultOfProcess.Add((className, decisionTableTask.Process(taskId, TypeOfCase, TypeOfTask, _unitOfWork)));
                    //decisionTableTask.Process(taskId);
                }


            }
            return listWithClassNameAndResultOfProcess;
        }

        public List<LittleObjectFromDecisionTableOutputWhichGoesToOptionData> 
            LetsFormOurArray(List<AvailableAnswersOutput> resultOfDecisionTable, List<(string, string)> resultOfProcesses)
        {
            List<LittleObjectFromDecisionTableOutputWhichGoesToOptionData> result =
                new List<LittleObjectFromDecisionTableOutputWhichGoesToOptionData>();
            foreach (var item in resultOfDecisionTable)
            {
                string commonResult = "";
                if (item.Validate != null)
                {
                    string stringWithFunctions = item.Validate.Replace(" ", "");
                    List<string> stringsWithFunctions = stringWithFunctions.Split(',').ToList<string>();

                    int indexMessage = 0;
                    foreach (var item1 in stringsWithFunctions)
                    {
                        string currentMessage = resultOfProcesses.FirstOrDefault(x => x.Item1 == item1).Item2;
                        if (currentMessage != null && currentMessage != "")
                        {
                            indexMessage = indexMessage + 1;
                            commonResult = commonResult + indexMessage + ". " + currentMessage + "<br >";
                        }                           
                    }
                }

                result.Add(new LittleObjectFromDecisionTableOutputWhichGoesToOptionData
                {
                    VariantText = (TypeOfTask!=551 && TypeOfTask!=1037)? item.AnswerVariant:PrepareAnswerForVeb(item.AnswerVariant,TaskId),
                    IdOfAnswer = item.IdOfAnswer.GetValueOrDefault(),
                    ErrorMessage = commonResult,
                    ValueOfStatusSectionAct = item.ValueOfStatusSectionAct.GetValueOrDefault(),
                    InputDocumentListEnabled = item.ShowListFromWhatInformationGot.GetValueOrDefault(),
                    OutputDocumentListEnabled = item.ShowListOfDirectionOfDocument.GetValueOrDefault(),
                    NeedExecutive = item.NeedExecutive.GetValueOrDefault(),
                    Fileuploader = item.Fileuploader.GetValueOrDefault(),

                });
            }
            return result;
        }

    
        public string LetsFormOurJson(List<LittleObjectFromDecisionTableOutputWhichGoesToOptionData> arrayWithStringAndBool)
        {
            List<ObjectForJsonNew> resultDictionary = new List<ObjectForJsonNew>();
            Regex reg = new Regex(@"\?\?(.+?)\?\?");

            foreach (var item in arrayWithStringAndBool)
            {
                if (item.VariantText != null)
                {
                    string[] ar = reg.Split(item.VariantText);
                    ObjectForJsonNew wholeObjectForOnePoint = new ObjectForJsonNew();
                    List<Dictionary<string, object>> dictionaryForOnePoint = new List<Dictionary<string, object>>();
                    string questionId = Guid.NewGuid().ToString();
                    OptionData UniqueInformation = new OptionData();
                    UniqueInformation.question_id = questionId;
                    UniqueInformation.enable = item.ErrorMessage == "" ? true : false;
                    UniqueInformation.errormessage = item.ErrorMessage;
                    UniqueInformation.caseid = CaseId;
                    UniqueInformation.idOfAnswer = item.IdOfAnswer;
                    UniqueInformation.taskid = TaskId;
                    UniqueInformation.taskType = TypeOfTask;
                    UniqueInformation.statusSectionAct = item.ValueOfStatusSectionAct;
                    UniqueInformation.inputDocumentListEnabled = item.InputDocumentListEnabled;
                    UniqueInformation.outputDocumentListEnabled = item.OutputDocumentListEnabled;
                    UniqueInformation.requestInitiator = 1;
                    UniqueInformation.takepartflag = (item.VariantText.Contains("TakePart") ? true : false);
                    UniqueInformation.needexucutive = item.NeedExecutive;
                    UniqueInformation.fileneed = item.Fileuploader;
                    UniqueInformation.executiveCaseID =(int?) _unitOfWork.TaskRepository.GetTaskById(TaskId).ExecutiveCaseId;
                    foreach (var item1 in ar.Select((value, i) => new { i, value }))
                    {
                        string message = item1.value;
                        string[] keys = new string[] { "datepicker", "input", "fileuploader", "timepicker" };

                        var partOfThePoint = letsReturnOneElementOfPointOfClosingTask(message, keys, questionId);
                        if (partOfThePoint != null)
                            dictionaryForOnePoint.Add(partOfThePoint);
                    }

                    wholeObjectForOnePoint.visualPointForClosingTask = dictionaryForOnePoint;
                    wholeObjectForOnePoint.uniqueInformationAboutThisPoint = UniqueInformation;
                    resultDictionary.Add(wholeObjectForOnePoint);
                }

            }

            var rootObject = letsFormObjectAppliedToJsonFormat(resultDictionary);

            var qwert = JsonConvert.SerializeObject(rootObject,
                Formatting.Indented,
                new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                });

            return qwert;
        }
        public string PrepareAnswerForVeb(string answer, int taskid)
        {
            var creditor_string = _unitOfWork.TaskFileListRepository.GetCreditorName(taskid);
            var claim = _unitOfWork.TaskFileListRepository.GetClaimName(taskid);
            var docs_string = _unitOfWork.TaskFileListRepository.GetCreditorDocs(taskid);
            answer = answer.Replace("кредитора", creditor_string+ " "+ claim).Replace("договору поручительства", docs_string);
            return answer;
        }
        public Dictionary<string, object> letsReturnOneElementOfPointOfClosingTask(string valueOfPoint, string[] keys, string questionId)
        {
            Dictionary<string, object> dctLittle = new Dictionary<string, object>();
            string sKeyResult = keys.FirstOrDefault<string>(s => valueOfPoint.Contains(s));
            string myKeyFromDecisionTable = valueOfPoint.Substring(valueOfPoint.LastIndexOf('^') + 1);

            switch (sKeyResult)
            {
                case "datepicker":
                    {
                        dctLittle["datepicker"] = true;
                        dctLittle["datepickerKey"] = "datepicker_" + myKeyFromDecisionTable;
                        dctLittle["question_id"] = questionId;
                        //dctLittle["id_of_this_datepicker_from_decision_table_"] = myKeyFromDecisionTable;
                    }
                    break;
                case "input":
                    {
                        dctLittle["input"] = true;
                        dctLittle["inputKey"] = "input_" + myKeyFromDecisionTable;
                        dctLittle["question_id"] = questionId;
                        //dctLittle["id_of_this_input_from_decision_table_"] = myKeyFromDecisionTable;
                    }
                    break;
                case "fileuploader":
                    {
                        dctLittle["fileuploader"] = true;
                        dctLittle["fileuploaderKey"] = "fileuploader_" + myKeyFromDecisionTable;
                        dctLittle["question_id"] = questionId;
                        //dctLittle["id_of_this_fileuploader_from_decision_table_"] = myKeyFromDecisionTable;
                    }
                    break;
                case "timepicker":
                {
                    dctLittle["timepicker"] = true;
                    dctLittle["timepickerKey"] = "timepicker_" + myKeyFromDecisionTable;
                    dctLittle["question_id"] = questionId;
                    //dctLittle["id_of_this_fileuploader_from_decision_table_"] = myKeyFromDecisionTable;
                }
                    break;
                default:
                    {
                        dctLittle["title"] = valueOfPoint;
                        dctLittle["question_id"] = questionId;
                    }

                    break;
            }
            return dctLittle;
        }

        public List<Object> letsFormObjectAppliedToJsonFormat(List<ObjectForJsonNew> resultDictionary)
        {
            List<Object> ofOnePoint = new List<object>();
            foreach (var item in resultDictionary)
            {
                OptionDataForJson optData = new OptionDataForJson();
                
                List<object> elements = new List<object>();
                foreach (var item1 in item.visualPointForClosingTask)
                {
                    elements.Add(item1);
                }
                optData.elements = elements;

                optData.option_data = item.uniqueInformationAboutThisPoint;
                ofOnePoint.Add(optData);
                //rootObject.response.Add(ofOnePoint);

            }
            return ofOnePoint;
            /*RootObject rootObject = new RootObject { response = new List<List<object>>() };
            foreach (var item in resultDictionary)
            {
                List<Object> ofOnePoint = new List<object>();
                OptionDataForJson optData = new OptionDataForJson();
                optData.option_data = item.uniqueInformationAboutThisPoint;
                ofOnePoint.Add(optData);
                foreach (var item1 in item.visualPointForClosingTask)
                {
                    ofOnePoint.Add(item1);
                }
                rootObject.response.Add(ofOnePoint);

            }
            return rootObject;*/
            //return resultDictionary;
        }


    }

    public static class qwe
    {
        public static IEnumerable<string> SplitAndKeep(this string s, string delims)
        {
            int start = 0, index;

            while ((index = s.IndexOf(delims, start)) != -1)
            {
                if (index - start > 0)
                    yield return s.Substring(start, index - start);
                //yield return s.Substring(index, 1);
                //start = index + 1;
                //yield return s.Substring(index, 2);
                start = index + 2;
            }

            if (start < s.Length)
            {
                yield return s.Substring(start);
            }
        }
    }
}



/*public List<(string, bool)> LetsFormOurArray(List<AvailableAnswersOutput> resultOfDecisionTable, List<(string, bool)> resultOfProcesses)
  {
      List<(string, bool)> result = new List<(string, bool)>();
      foreach (var item in resultOfDecisionTable)
      {
          bool commonResult = true;
          if (item.Validate != null)
          {
              string stringWithFunctions = item.Validate.Replace(" ", "");
              List<string> stringsWithFunctions = stringWithFunctions.Split(',').ToList<string>();

              foreach (var item1 in stringsWithFunctions)
              {
                  commonResult = commonResult && resultOfProcesses.FirstOrDefault(x => x.Item1 == item1).Item2;
              }
          }

          result.Add((item.AnswerVar, commonResult));
      }
      return result;
  }*/

/* public string LetsFormOurJson(List<(string, bool)> arrayWithStringAndBool)
 {

     List<Dictionary<string, string>> arrayWithObj = new List<Dictionary<string, string>>();
     Regex reg = new Regex(@"\?\?(.+?)\?\?");

     //List<OneObjectFromJson> arrayWithObj = new List<OneObjectFromJson>();
     foreach (var item in arrayWithStringAndBool)
     {
         string[] ar = reg.Split(item.Item1);
         Dictionary<string, string> dct = new Dictionary<string, string>();
         foreach (var item1 in ar.Select((value, i) => new { i, value }))
         {
             string index = string.Concat("description", item1.i);
             if (item1.value != "")
             {
                 string stringVal = item1.value;
                 if (item1.value.Contains("datepicker") || item1.value.Contains("input") || item1.value.Contains("fileuploader"))
                 {
                     stringVal = "??" + item1.value + "??";
                 }
                 dct[index] = stringVal;
             }

         }
         dct["enable"] = item.Item2.ToString();
         dct["errormessage"] = "";
         arrayWithObj.Add(dct);
     }

     var qwert = JsonConvert.SerializeObject(arrayWithObj,
         Formatting.Indented,
         new JsonSerializerSettings
         {
             NullValueHandling = NullValueHandling.Ignore
         });

     return qwert;

 }
}*/
