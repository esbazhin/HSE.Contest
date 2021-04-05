using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace HSE.Contest.ClassLibrary.DbClasses.Administration
{
    public class User
    {
        [Key]
        [Column(name: "id")]
        public int Id { get; set; }
        [Column(name: "email")]
        public string Email { get; set; }
        [Column(name: "password")]
        public string Password { get; set; }
        [Column(name: "firstName")]
        public string FirstName { get; set; }
        [Column(name: "lastName")]
        public string LastName { get; set; }

        public virtual List<UserRole> Roles { get; set; } = new List<UserRole>();
        public string RolesString { get { return string.Join(", ", Roles.Select(r => r.Role.Name)); } }

        public virtual List<UserGroup> Groups { get; set; } = new List<UserGroup>();
    }
}
