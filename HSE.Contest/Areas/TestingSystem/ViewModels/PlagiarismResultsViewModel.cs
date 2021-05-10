using HSE.Contest.ClassLibrary.DbClasses.TestingSystem;
using System.Collections.Generic;

namespace HSE.Contest.Areas.TestingSystem.ViewModels
{
    public class PlagiarismResultsViewModel
    {
        public List<PlagiarismResultViewModel> Results { get; set; }
        public ExtendedTaskViewModel TaskData { get; set; }
        public List<AddPlagResultViewModel> AddPlag { get; set; }
    }

    public class PlagiarismResultViewModel
    {
        public int SolutionId1 { get; set; }
        public string StudentName1 { get; set; }
        public int SolutionId2 { get; set; }
        public string StudentName2 { get; set; }
        public double Percent1 { get; set; }
        public double Percent2 { get; set; }
        public int LinesMatched { get; set; }
        public int TaskId { get; set; }
    }

    public class AddPlagResultViewModel
    {
        public string StudentName { get; set; }
        public int SolutionId { get; set; }
    }
}
