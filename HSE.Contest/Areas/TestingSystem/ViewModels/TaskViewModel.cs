using HSE.Contest.ClassLibrary.DbClasses.TestingSystem;
using Newtonsoft.Json;

namespace HSE.Contest.Areas.TestingSystem.ViewModels
{
    public class TaskViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public class ExtendedTaskViewModel
    {
        public int TaskId { get; set; }
        public string TaskName { get; set; }
        public string GroupName { get; set; }
        public string Deadline { get; set; }
        public string Type { get; set; }
        public int NumberOfAttempts { get; set; }

        public ExtendedTaskViewModel(StudentTask task)
        {
            TaskName = task.Name;
            Type = task.IsContest ? "Контест" : "Контрольная работа";
            GroupName = task.Group.Name;
            NumberOfAttempts = task.NumberOfAttempts;
            Deadline = task.To.ToString();
            TaskId = task.Id;
        }

        [JsonConstructor]
        public ExtendedTaskViewModel()
        { 
        }
    }
}
