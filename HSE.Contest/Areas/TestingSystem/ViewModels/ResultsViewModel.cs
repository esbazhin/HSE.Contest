using HSE.Contest.ClassLibrary.DbClasses.TestingSystem;
using Newtonsoft.Json;

namespace HSE.Contest.Areas.TestingSystem.ViewModels
{
    public class ResultsViewModel
    {
        public string StudentId { get; set; }
        public string StudentFullName { get; set; }
        public int TaskId { get; set; }
        public int SolutionId { get; set; }
        public double Score { get; set; }
        public string Result { get; set; }
        public bool PlagiarismDetected { get; set; }

        [JsonConstructor]
        public ResultsViewModel()
        { 
        }

        public ResultsViewModel(StudentResult res)
        {
            var student = res.Student;
            var solution = res.Solution;

            StudentId = res.StudentId;
            StudentFullName = student.LastName + " " + student.FirstName;
            TaskId = res.TaskId;
            SolutionId = res.SolutionId;
            Score = solution.Score;
            Result = solution.ResultCode.ToString();
            PlagiarismDetected = solution.PlagiarismDetected;
        }
    }
}
