using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HSE.Contest.ClassLibrary.DbClasses.Files
{

    public class CodeStyleFiles
    {
        [Key]
        [Column(name: "id")]
        public int Id { get; set; }

        [Column(name: "name")]
        public string Name { get; set; }

        [Column(name: "stylecopFile", TypeName = "bytea")]
        public byte[] StyleCopFile { get; set; }

        [Column(name: "rulesetFile", TypeName = "bytea")]
        public byte[] RulesetFile { get; set; }
    }
}
