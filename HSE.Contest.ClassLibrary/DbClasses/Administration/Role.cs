using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HSE.Contest.ClassLibrary.DbClasses.Administration
{
    public class Role
    {
        [Key]
        [Column(name: "id")]
        public int Id { get; set; }
        [Column(name: "name")]
        public string Name { get; set; }
        public virtual List<UserRole> Users { get; set; } = new List<UserRole>();
    }

    public class UserRole
    {
        [Column(name: "userId")]
        public int UserId { get; set; }
        public virtual User User { get; set; }
        [Column(name: "roleId")]
        public int RoleId { get; set; }
        public virtual Role Role { get; set; }
    }
}
