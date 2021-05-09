using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HSE.Contest.ClassLibrary.DbClasses.TestingSystem
{
    public class TestingResult
    {
        [Key]
        [Column(name: "id")]
        public int Id { get; set; }

        [Column(name: "solutionId")]
        public int SolutionId { get; set; }

        [Column(name: "score")]
        public double Score { get; set; }

        [Column(name: "comment")]
        public string Commentary { get; set; }

        [Column(name: "resultCode", TypeName = "integer")]
        public ResultCode ResultCode { get; set; }

        [Column(name: "testId")]
        public int TestId { get; set; }

        [Column(name: "data", TypeName = "json")]
        public string TestData { get; set; }

        public virtual TaskTest Test { get; set; }
    }
}
