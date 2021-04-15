using HSE.Contest.ClassLibrary.DbClasses.Administration;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace HSE.Contest.Areas.Administration.ViewModels
{
    public class GroupCRUDViewModel
    {
        public GroupViewModel Group { get; set; }
        public List<TransferViewModel> AllStudents { get; set; }
        public bool IsUpdate { get; set; }
    }

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
                Key = user.Id.ToString();
            }
        }
    }

    public class GroupViewModel
    {
        public string Name { get; set; }
        public int Id { get; set; }
        public List<string> SelectedUsers { get; set; }

        [JsonConstructor]
        public GroupViewModel()
        { }

        public GroupViewModel(Group group)
        {
            if (group is null)
            {
                Name = "new group";
                Id = -1;
                SelectedUsers = new List<string>();
            }
            else
            {
                Name = group.Name;
                Id = group.Id;
                SelectedUsers = group.Users.Select(u => u.UserId.ToString()).Distinct().ToList();
            }
        }
    }
}
