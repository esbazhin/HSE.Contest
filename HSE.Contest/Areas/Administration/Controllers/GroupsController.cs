using HSE.Contest.Areas.Administration.ViewModels;
using HSE.Contest.ClassLibrary.DbClasses;
using HSE.Contest.ClassLibrary.DbClasses.Administration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HSE.Contest.Areas.Administration.Controllers
{
    [Authorize(Roles = "admin, professor")]
    [Area("Administration")]
    public class GroupsController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly HSEContestDbContext _db;

        public GroupsController(UserManager<User> userManager, HSEContestDbContext db)
        {
            _db = db;
            _userManager = userManager;
        }

        async Task<List<TransferViewModel>> GetAllStudents()
        {
            var students = await _userManager.GetUsersInRoleAsync("student");
            return students.Select(u => new TransferViewModel(u)).ToList();
        }

        public IActionResult Index()
        {
            return RedirectToAction("AllGroups");
        }

        public IActionResult AllGroups()
        {
            var records = _db.Groups.Select(t => new CodeStyleRecordViewModel
            {
                Id = t.Id,
                Name = t.Name,
            }).OrderByDescending(m => m.Id).ToList();
            return View(records);
        }

        public IActionResult DeleteGroup(int id)
        {
            var y = _db.Groups.Find(id);
            _db.Groups.Remove(y);
            _db.SaveChanges();
            return RedirectToAction("AllGroups");
        }

        public IActionResult CreateNewGroup()
        {
            return View(new GroupCRUDViewModel { AllStudents = GetAllStudents().Result, Group = new GroupViewModel(null), IsUpdate = false });
        }

        public IActionResult ChangeGroup(int id)
        {
            var cur = _db.Groups.Find(id);

            if (cur is null)
            {
                return NotFound();
            }

            return View("CreateNewGroup", new GroupCRUDViewModel { AllStudents = GetAllStudents().Result, Group = new GroupViewModel(cur), IsUpdate = true });
        }

        public IActionResult UpdateGroup(string json)
        {
            GroupViewModel groupRecord = JsonConvert.DeserializeObject<GroupViewModel>(json);

            var y = _db.Groups.Find(groupRecord.Id);

            if (y is null)
            {
                return Content("error");
            }

            _db.UserGroups.RemoveRange(y.Users);

            y.Name = groupRecord.Name;
            y.Users = groupRecord.SelectedUsers.Select(u => new UserGroup { UserId = u, GroupId = groupRecord.Id }).ToList();            

            var x = _db.Groups.Update(y);
            var beforeState = x.State;
            int r = _db.SaveChanges();
            var afterState = x.State;
            bool ok = beforeState == EntityState.Modified && afterState == EntityState.Unchanged;

            return Content(ok ? "/Administration/Groups/ChangeGroup?id=" + x.Entity.Id.ToString() : "error");
        }

        public IActionResult PostNewGroup(string json)
        {
            GroupViewModel groupRecord = JsonConvert.DeserializeObject<GroupViewModel>(json);
            Group newGroup = new Group
            {
                Name = groupRecord.Name,
                Users = groupRecord.SelectedUsers.Select(u => new UserGroup { UserId = u, GroupId = groupRecord.Id }).ToList()
            };

            var x = _db.Groups.Add(newGroup);
            var beforeState = x.State;
            int r = _db.SaveChanges();
            var afterState = x.State;
            bool ok = beforeState == EntityState.Added && afterState == EntityState.Unchanged;

            return Content(ok ? "/Administration/Groups/ChangeGroup?id=" + newGroup.Id.ToString() : "error");
        }
    }
}
