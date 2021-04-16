using HSE.Contest.ClassLibrary.DbClasses.Administration;
using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json;

namespace HSE.Contest.Areas.Administration.ViewModels
{
    public class TransferViewModel
    {
        public string Title { get; set; }
        public string Key { get; set; }

        [JsonConstructor]
        public TransferViewModel()
        { }

        public TransferViewModel(User user)
        {
            if (user is null)
            {
                Title = "new user";
                Key = "-1";
            }
            else
            {
                Title = user.FirstName + " " + user.LastName;
                Key = user.Id;
            }
        }

        public TransferViewModel(IdentityRole role)
        {
            if (role is null)
            {
                Title = "new role";
                Key = "-1";
            }
            else
            {
                Title = role.Name;
                Key = role.Name;
            }
        }

        public TransferViewModel(Group group)
        {
            if (group is null)
            {
                Title = "new group";
                Key = "-1";
            }
            else
            {
                Title = group.Name;
                Key = group.Id.ToString();
            }
        }
    }
}
