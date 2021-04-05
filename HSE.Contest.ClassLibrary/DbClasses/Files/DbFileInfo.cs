using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HSE.Contest.ClassLibrary.DbClasses.Files
{
    public class DbFileInfo
    {
        [Key]
        [Column(name: "id")]
        public int Id { get; set; }
        [Column(name: "name")]
        public string Name { get; set; }
        [Column(name: "content", TypeName = "bytea")]
        public byte[] Content { get; set; }
    }
}
