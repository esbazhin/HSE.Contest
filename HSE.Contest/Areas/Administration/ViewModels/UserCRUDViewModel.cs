using HSE.Contest.ClassLibrary.DbClasses.Administration;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace HSE.Contest.Areas.Administration.ViewModels
{
    public class UserCRUDViewModel
    {
        public UserViewModel User { get; set; }
        public List<TransferViewModel> AllGroups { get; set; }
        public List<TransferViewModel> AllRoles { get; set; }
        public bool IsUpdate { get; set; }
    }

    public class UserViewModel
    {
        public string Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public List<string> SelectedRoles { get; set; }
        public List<string> SelectedGroups { get; set; }
        public string Password { get; set; }

        [JsonConstructor]
        public UserViewModel()
        { 
        }

        public UserViewModel(User user, List<string> roles)
        {
            if(user is null)
            {
                Id = "-1";
                SelectedGroups = new List<string>();
                SelectedRoles = new List<string>();
            }
            else
            {
                Id = user.Id;
                FirstName = user.FirstName;
                LastName = user.LastName;
                Email = user.Email;
                SelectedGroups = user.Groups.Select(g => g.Group.Id.ToString()).ToList();
                SelectedRoles = roles;
            }
        }
    }
}
