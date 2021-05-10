using System.Collections.Generic;

namespace HSE.Contest.Areas.TestingSystem.ViewModels
{
    public class StudentsResultsViewModel
    {             
        public IEnumerable<ResultsViewModel> Results { get; set; }
        public ExtendedTaskViewModel TaskData { get; set; }
    }
}
