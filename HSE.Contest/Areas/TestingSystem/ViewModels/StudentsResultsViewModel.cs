using System.Collections.Generic;

namespace HSE.Contest.Areas.TestingSystem.ViewModels
{
    public class StudentsResultsViewModel
    {             
        public IEnumerable<ResultsViewModel> Results { get; set; }
        public int TaskId { get; set; }
        public string TaskName { get; set; }
        public string GroupName { get; set; }
        public string Deadline { get; set; }
        public string Type { get; set; }
        public int NumberOfAttempts { get; set; }
    }
}
