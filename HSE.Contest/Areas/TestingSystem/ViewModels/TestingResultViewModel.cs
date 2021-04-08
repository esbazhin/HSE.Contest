using HSE.Contest.ClassLibrary;
using HSE.Contest.ClassLibrary.DbClasses.TestingSystem;
using HSE.Contest.ClassLibrary.TestsClasses.CodeStyleTest;
using HSE.Contest.ClassLibrary.TestsClasses.FunctionalTest;
using HSE.Contest.ClassLibrary.TestsClasses.ReflectionTest;
using Newtonsoft.Json;

namespace HSE.Contest.Areas.TestingSystem.ViewModels
{
    public class SolutionReportViewModel : SolutionViewModel
    {
        public int TaskId { get; set; }
        public TestingResultViewModel[] TestingResults { get; set; }
    }

    public class TestingResultViewModel
    {
        public double Score { get; set; }
        public string Commentary { get; set; }
        public string Result { get; set; }
        public string TestName { get; set; }
        public string TestType { get; set; }
        public object Data { get; set; }

        [JsonConstructor]
        public TestingResultViewModel() { }

        public TestingResultViewModel(TestingResult tr, TaskTest t)
        {
            Score = tr.Score;
            Commentary = tr.Commentary;
            Result = tr.ResultCode.ToString();
            TestType = t.TestType;
            TestName = TestsNamesConverter.ConvertTypeToName(TestType);

            if (TestsNamesConverter.IsValidType(TestType))
            {
                object obj = null;
                if (tr.TestData != null)
                {
                    switch (TestType)
                    {
                        case "reflectionTest":
                            obj = JsonConvert.DeserializeObject<ReflectionTestResult>(tr.TestData);
                            break;

                        case "functionalTest":
                            obj = JsonConvert.DeserializeObject<FunctionalTestResult>(tr.TestData);
                            break;

                        case "codeStyleTest":
                            obj = JsonConvert.DeserializeObject<CodeStyleTestResult>(tr.TestData);
                            break;
                    }
                }

                Data = obj;
            }            
        }
    }
}
