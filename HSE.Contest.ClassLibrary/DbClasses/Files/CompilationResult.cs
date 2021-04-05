using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HSE.Contest.ClassLibrary.DbClasses.Files
{
    public class CompilationResult
    {
        [Key]
        [Column(name: "solutionId")]
        public int SolutionId { get; set; }

        [Column(name: "stOutput", TypeName = "text")]
        public string StOutput { get; set; }

        [Column(name: "stError", TypeName = "text")]
        public string StError { get; set; }

        [Column(name: "resultCode", TypeName = "integer")]
        public ResultCode ResultCode { get; set; }

        [Column(name: "didUpdateRules")]
        public bool DidUpdateRules { get; set; }

        [Column(name: "fileId")]
        public int? FileId { get; set; }
        public virtual DbFileInfo File { get; set; }
    }
}
