using System.Collections.Generic;

namespace HSE.Contest.Areas.TestingSystem.ViewModels
{
    public class EditTaskResultViewModel
    {
        public EditSolutionViewModel Data { get; set; }
        public string[] AllResultCodes { get; set; }
    }

    public class EditSolutionViewModel
    {
        public int TaskId { get; set; }
        public int SolutionId { get; set; }
        public string ResultCode { get; set; }
        public double SolutionScore { get; set; }
        public List<EditTestingResultViewModel> TestingResults { get; set; }    
    }

    public class EditTestingResultViewModel
    {
        public int TestResultId { get; set; }
        public string TestType { get; set; }
        public string TestName { get; set; }
        public string ResultCode { get; set; }
        public double TestScore { get; set; }
        public string TestCommentary { get; set; }
        public bool UpdateData { get; set; }
    }
}
