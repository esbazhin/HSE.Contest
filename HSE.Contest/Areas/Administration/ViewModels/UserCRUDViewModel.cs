using HSE.Contest.ClassLibrary.DbClasses.Administration;
using System.Collections.Generic;

namespace HSE.Contest.Areas.Administration.ViewModels
{
    public class UserCRUDViewModel
    {
        public User User { get; set; }
        public List<Role> AllRoles { get; set; }
    }
}
