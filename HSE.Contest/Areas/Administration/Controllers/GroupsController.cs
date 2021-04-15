using HSE.Contest.Areas.Administration.ViewModels;
using HSE.Contest.ClassLibrary;
using HSE.Contest.ClassLibrary.DbClasses;
using HSE.Contest.ClassLibrary.DbClasses.Administration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HSE.Contest.Areas.Administration.Controllers
{
    [Authorize(Roles = "admin, professor")]
    [Area("Administration")]
    public class GroupsController : Controller
    {
        private readonly TestingSystemConfig config;
        private readonly HSEContestDbContext db;

        public GroupsController()
        {
            string pathToConfig = "c:\\config\\config.json";
            config = JsonConvert.DeserializeObject<TestingSystemConfig>(System.IO.File.ReadAllText(pathToConfig));

            DbContextOptionsBuilder<HSEContestDbContext> options = new DbContextOptionsBuilder<HSEContestDbContext>();
            options.UseNpgsql(config.DatabaseInfo.GetConnectionStringFrom(config.FrontEnd));
            db = new HSEContestDbContext(options.Options);
        }

        async Task<List<Group>> GetAllGroups()
        {
            return await db.Groups.Include(c => c.Users).ThenInclude(c => c.User).ToListAsync();
        }

        async Task<Group> GetGroup(int id)
        {
            return await db.Groups.Include(c => c.Users).ThenInclude(c => c.User).FirstOrDefaultAsync(m => m.Id == id);
        }

        async Task<List<TransferViewModel>> GetAllStudents()
        {
            var res = await db.Users.Where(u => u.Roles.Select(ur => ur.Role.Name).Contains("student")).ToListAsync();
            return res.Select(u => new TransferViewModel(u)).ToList();
        }

        public IActionResult Index()
        {
            return RedirectToAction("AllGroups");
        }

        public IActionResult AllGroups()
        {
            var records = db.Groups.Select(t => new CodeStyleRecordViewModel
            {
                Id = t.Id,
                Name = t.Name,
            }).OrderByDescending(m => m.Id).ToList();
            return View(records);
        }

        public IActionResult DeleteGroup(int id)
        {
            var y = db.Groups.Find(id);
            db.Groups.Remove(y);
            db.SaveChanges();
            return RedirectToAction("AllGroups");
        }

        public IActionResult CreateNewGroup()
        {
            return View(new GroupCRUDViewModel { AllStudents = GetAllStudents().Result, Group = new GroupViewModel(null), IsUpdate = false });
        }

        public IActionResult ChangeGroup(int id)
        {
            var cur = db.Groups.Find(id);

            if (cur is null)
            {
                return NotFound();
            }

            return View("CreateNewGroup", new GroupCRUDViewModel { AllStudents = GetAllStudents().Result, Group = new GroupViewModel(cur), IsUpdate = true });
        }

        public IActionResult UpdateGroup(string json)
        {
            GroupViewModel groupRecord = JsonConvert.DeserializeObject<GroupViewModel>(json);

            var y = db.Groups.Find(groupRecord.Id);

            if (y is null)
            {
                return Content("error");
            }

            db.UserGroups.RemoveRange(y.Users);

            y.Name = groupRecord.Name;
            y.Users = groupRecord.SelectedUsers.Select(u => new UserGroup { UserId = int.Parse(u), GroupId = groupRecord.Id }).ToList();            

            var x = db.Groups.Update(y);
            var beforeState = x.State;
            int r = db.SaveChanges();
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
                Users = groupRecord.SelectedUsers.Select(u => new UserGroup { UserId = int.Parse(u), GroupId = groupRecord.Id }).ToList()
            };

            var x = db.Groups.Add(newGroup);
            var beforeState = x.State;
            int r = db.SaveChanges();
            var afterState = x.State;
            bool ok = beforeState == EntityState.Added && afterState == EntityState.Unchanged;

            return Content(ok ? "/Administration/Groups/ChangeGroup?id=" + newGroup.Id.ToString() : "error");
        }
    }
}
