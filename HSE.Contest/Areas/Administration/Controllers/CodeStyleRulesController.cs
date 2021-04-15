using HSE.Contest.Areas.Administration.ViewModels;
using HSE.Contest.ClassLibrary;
using HSE.Contest.ClassLibrary.DbClasses;
using HSE.Contest.ClassLibrary.DbClasses.Files;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
    public class CodeStyleRulesController : Controller
    {
        private readonly TestingSystemConfig config;
        private readonly HSEContestDbContext db;

        public CodeStyleRulesController()
        {
            string pathToConfig = "c:\\config\\config.json";
            config = JsonConvert.DeserializeObject<TestingSystemConfig>(System.IO.File.ReadAllText(pathToConfig));

            DbContextOptionsBuilder<HSEContestDbContext> options = new DbContextOptionsBuilder<HSEContestDbContext>();
            options.UseNpgsql(config.DatabaseInfo.GetConnectionStringFrom(config.FrontEnd));
            db = new HSEContestDbContext(options.Options);
        }

        public IActionResult Index()
        {
            return RedirectToAction("AllRecords");
        }

        public IActionResult AllRecords()
        {
            var records = db.CodeStyleFiles.Select(t => new CodeStyleRecordViewModel
            {
                Id = t.Id,
                Name = t.Name,
            }).OrderByDescending(m => m.Id).ToList();
            return View(records);
        }

        public IActionResult DeleteRecord(int id)
        {
            var y = db.CodeStyleFiles.Find(id);
            db.CodeStyleFiles.Remove(y);
            db.SaveChanges();
            return RedirectToAction("AllRecords");
        }

        public IActionResult CreateNewRecord()
        {
            return View(new CodeStyleCRUDViewModel { CodeStyleFiles = new CodeStyleFilesViewModel(null), IsUpdate = false });
        }

        public IActionResult ChangeRecord(int id)
        {
            var cur = db.CodeStyleFiles.Find(id);

            if (cur is null)
            {
                return NotFound();
            }

            return View("CreateNewRecord", new CodeStyleCRUDViewModel { CodeStyleFiles = new CodeStyleFilesViewModel(cur), IsUpdate = true });
        }

        public IActionResult UpdateRecord(string json)
        {
            CodeStyleFilesViewModel jsonRecord = JsonConvert.DeserializeObject<CodeStyleFilesViewModel>(json);

            var y = db.CodeStyleFiles.Find(jsonRecord.Id);

            if (y is null)
            {
                return Content("error");
            }

            y.Name = jsonRecord.Name;
            y.RulesetFile = System.Text.Encoding.UTF8.GetBytes(jsonRecord.RuleSet);
            y.StyleCopFile = System.Text.Encoding.UTF8.GetBytes(jsonRecord.StyleCop);


            var x = db.CodeStyleFiles.Update(y);
            var beforeState = x.State;
            int r = db.SaveChanges();
            var afterState = x.State;
            bool ok = beforeState == EntityState.Modified && afterState == EntityState.Unchanged && r == 1;

            return Content(ok ? "/Administration/CodeStyleRules/ChangeRecord?id=" + x.Entity.Id.ToString() : "error");
        }

        public IActionResult PostNewRecord(string json)
        {
            CodeStyleFilesViewModel jsonRecord = JsonConvert.DeserializeObject<CodeStyleFilesViewModel>(json);
            CodeStyleFiles newRecord = new CodeStyleFiles
            {
                Name = jsonRecord.Name,
                RulesetFile = System.Text.Encoding.UTF8.GetBytes(jsonRecord.RuleSet),
                StyleCopFile = System.Text.Encoding.UTF8.GetBytes(jsonRecord.StyleCop)
            };

            var x = db.CodeStyleFiles.Add(newRecord);
            var beforeState = x.State;
            int r = db.SaveChanges();
            var afterState = x.State;
            bool ok = beforeState == EntityState.Added && afterState == EntityState.Unchanged && r == 1;

            return Content(ok ? "/Administration/CodeStyleRules/ChangeRecord?id=" + newRecord.Id.ToString() : "error");
        }

        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            if(file is null)
            {
                return Content("error");
            }

            var bytes = await file.GetBytes();
            return Content(System.Text.Encoding.UTF8.GetString(bytes));
        }
    }
}
