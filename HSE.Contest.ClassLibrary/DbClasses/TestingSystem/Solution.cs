using HSE.Contest.ClassLibrary.DbClasses.Files;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HSE.Contest.ClassLibrary.DbClasses.TestingSystem
{
    public class Solution
    {
        [Key]
        [Column(name: "id")]
        public int Id { get; set; }

        [Column(name: "taskId")]
        public int TaskId { get; set; }
        public virtual StudentTask Task { get; set; }

        [Column(name: "studentId")]
        public int StudentId { get; set; }
        //public User Student { get; set; }

        [Column(name: "score")]
        public double Score { get; set; }

        [Column(name: "resultCode", TypeName = "integer")]
        public ResultCode ResultCode { get; set; }

        [Column(name: "fileId")]
        public int FileId { get; set; }
        public virtual DbFileInfo File { get; set; }

        [Column(name: "time", TypeName = "timestamptz")]
        public DateTime Time { get; set; }

        [Column(name: "frameworkType")]
        public string FrameworkType { get; set; }

        //[Column(name: "updateRulesFiles")]
        //public bool UpdateRulesFiles { get; set; }


        [Column(name: "compilationId")]
        public int? CompilationId { get; set; }

        //[Column(name: "reflectionId")]
        //public int? ReflectionId { get; set; }

        //[Column(name: "functionalId")]
        //public int? FunctionalId { get; set; }

        //[Column(name: "codeStyleId")]
        //public int? CodeStyleId { get; set; }
    }
}
