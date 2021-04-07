using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HSE.Contest.ClassLibrary.DbClasses.TestingSystem
{
    public class TaskTest
    {
        [Key]
        [Column(name: "id")]
        public int Id { get; set; }

        [Column(name: "taskId")]
        public int TaskId { get; set; }
        public virtual StudentTask Task { get; set; }

        [Column(name: "testType")]
        public string TestType { get; set; }

        [Column(name: "weight")]
        public double Weight { get; set; }

        [Column(name: "block")]
        public bool Block { get; set; }

        [Column(name: "data", TypeName = "json")]
        public string TestData { get; set; }
    }
}
