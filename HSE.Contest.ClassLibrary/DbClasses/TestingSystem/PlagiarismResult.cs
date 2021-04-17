using System.ComponentModel.DataAnnotations.Schema;

namespace HSE.Contest.ClassLibrary.DbClasses.TestingSystem
{
    public class PlagiarismResult
    {
        [Column(name: "solutionId1")]
        public int SolutionId1 { get; set; }

        [Column(name: "solutionId2")]
        public int SolutionId2 { get; set; }

        [Column(name: "percent1")]
        public double Percent1 { get; set; }

        [Column(name: "percent2")]
        public double Percent2 { get; set; }

        [Column(name: "linesMatched")]
        public int LinesMatched { get; set; }

        [Column(name: "taskId")]
        public int TaskId { get; set; }
    }
}
