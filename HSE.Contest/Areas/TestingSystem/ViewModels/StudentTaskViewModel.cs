using System;

namespace HSE.Contest.Areas.TestingSystem.ViewModels
{
    public class StudentTaskViewModel
    {
        public int TaskId { get; set; }
        public string TaskName { get; set; }
        public string Description { get; set; }
        public double TotalScore { get; set; }
        public string Result { get; set; }
        public DateTime Deadline { get; set; }
        public SolutionViewModel[] Solutions { get; set; }
        public bool CanSend { get; set; }
        public int NumberOfAttempts { get; set; }
        public string[] FrameworkTypes { get; set; }
    }

    public class SolutionViewModel
    {
        public int Id { get; set; }       
        public double TotalScore { get; set; }
        public string Result { get; set; }
        public DateTime Time { get; set; }
    }
}
