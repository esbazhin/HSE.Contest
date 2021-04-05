using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HSE.Contest.ClassLibrary.DbClasses.Administration
{
    public class Group
    {
        [Key]
        [Column(name: "id")]
        public int Id { get; set; }
        [Column(name: "name")]
        public string Name { get; set; }
        public virtual List<UserGroup> Users { get; set; } = new List<UserGroup>();
    }

    public class UserGroup
    {
        [Column(name: "userId")]
        public int UserId { get; set; }
        public virtual User User { get; set; }
        [Column(name: "groupId")]
        public int GroupId { get; set; }
        public virtual Group Group { get; set; }
    }
}
