using System.ComponentModel.DataAnnotations.Schema;

namespace HSE.Contest.ClassLibrary.DbClasses.TestingSystem
{
    public class PlagiarismCheck
    {
        [Column(name: "taskId")]
        public int TaskId { get; set; }

        [Column(name: "link")]
        public string Link { get; set; }

        [Column(name: "settings", TypeName = "json")]
        public PlagiarismCheckSettings Settings { get; set; }

    }

    public class PlagiarismCheckSettings
    {
        public bool MakeCheck { get; set; }
        public int MaxMatches { get; set; }
        public double MinPercent { get; set; }
        public string Language { get; set; }
    }
}
