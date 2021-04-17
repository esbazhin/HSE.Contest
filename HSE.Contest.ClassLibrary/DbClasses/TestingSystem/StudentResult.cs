using HSE.Contest.ClassLibrary.DbClasses.Administration;
using System.ComponentModel.DataAnnotations.Schema;

namespace HSE.Contest.ClassLibrary.DbClasses.TestingSystem
{
    public class StudentResult
    {
        [Column(name: "studentId")]
        public string StudentId { get; set; }
        public virtual User Student { get; set; }

        [Column(name: "taskId")]
        public int TaskId { get; set; }
        public virtual StudentTask Task { get; set; }

        [Column(name: "solutionId")]
        public int SolutionId { get; set; }
        public virtual Solution Solution { get; set; }
    }
}
