using HSE.Contest.Areas.TestingSystem.ViewModels;
using HSE.Contest.ClassLibrary.DbClasses.TestingSystem;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace HSE.Contest.Areas.TestingSystem.Controllers
{
    [Authorize(Roles = "student")]
    [Area("TestingSystem")]
    public class StudentController : TestingSystemController
    {

        readonly string solutionsDir;
        public StudentController() : base()
        {
        }

        public IActionResult Index()
        {
            return RedirectToAction("AllTasks");
        }

        public IActionResult AllTasks()
        {
            int curId = GetId();
            var tasks = db.StudentTasks.Where(t => t.Group.Users.Select(s => s.UserId).Contains(curId) && t.From <= DateTime.Now)
            .Select(t => new TaskViewModel
            {
                Id = t.Id,
                Name = t.Name,
                Description = $"Длительность: {(t.To - t.From).TotalMinutes} минут с {t.From} до {t.To}"
            }).OrderByDescending(m => m.Id).ToList();

            return View(tasks);
        }

        public IActionResult SendStudentSolution(int id)
        {
            var t = db.StudentTasks.Find(id);

            if (t is null)
            {
                return NotFound();
            }

            var solutions = db.Solutions.Where(s => s.StudentId == GetId() && s.TaskId == id).Select(s =>
            new SolutionViewModel
            {
                Id = s.Id,
                TotalScore = s.Score,
                Result = s.ResultCode.ToString(),
                Time = s.Time,
            }).OrderByDescending(s => s.Time).ToArray();


            var res = new StudentTaskViewModel
            {
                TaskId = t.Id,
                TaskName = t.Name,
                Description = t.TaskText,
                Solutions = solutions,
                Deadline = t.To,
                NumberOfAttempts = t.NumberOfAttempts,
                CanSend = true,
                Result = "Нет решений"
            };

            if (solutions.Length > 0)
            {
                var maxScore = solutions.Max(s => s.TotalScore);

                var selectedSolution = solutions.Where(s => s.TotalScore == maxScore).OrderByDescending(s => s.Time).First();

                res.TotalScore = selectedSolution.TotalScore;
                res.Result = selectedSolution.Result;

                if (t.IsContest)
                {
                    res.CanSend = solutions.Length < t.NumberOfAttempts;
                }
                else
                {
                    res.CanSend = false;
                }
            }

            //JsonSerializerSettings serializerSettings = new JsonSerializerSettings
            //{
            //    ContractResolver = new CamelCasePropertyNamesContractResolver(),
            //    DateTimeZoneHandling = DateTimeZoneHandling.RoundtripKind
            //};

            //return Content(JsonConvert.SerializeObject(res, serializerSettings));
            return View(res);
        }

        public IActionResult ViewSolutionReport(int id)
        {
            var s = db.Solutions.Find(id);

            if (s is null)
            {
                return NotFound();
            }

            var t = db.StudentTasks.Find(s.TaskId);

            bool canSee = true;

            if (t.To >= DateTime.Now && t.IsContest)
            {
                var solutions = db.Solutions.Where(s => s.StudentId == GetId() && s.TaskId == id).ToArray();

                canSee = solutions.Length >= t.NumberOfAttempts;
            }

            if (!canSee)
            {
                return Forbid();
            }

            var testResults = db.TestingResults.Where(tr => tr.SolutionId == id).Join(db.TaskTests, i => i.TestId, o => o.Id, (i, o) => new TestingResultViewModel(i, o)).ToArray();

            var res = new SolutionReportViewModel
            {
                Id = s.Id,
                TotalScore = s.Score,
                Result = s.ResultCode.ToString(),
                Time = s.Time,
                TestingResults = testResults,
                TaskId = s.TaskId
            };
            //JsonSerializerSettings serializerSettings = new JsonSerializerSettings
            //{
            //    ContractResolver = new CamelCasePropertyNamesContractResolver(),
            //    DateTimeZoneHandling = DateTimeZoneHandling.RoundtripKind
            //};

            //return Content(JsonConvert.SerializeObject(res, serializerSettings));
            return View(res);
        }

        public async Task<IActionResult> CheckSolution(IFormFile file, int taskId)
        {
            var task = db.StudentTasks.Find(taskId);

            if (task == null)
            {
                var response1 = new
                {
                    status = "error",
                    data = "No task found!"
                };
                return Json(response1);
            }

            if (task.To < DateTime.Now)
            {
                var response2 = new
                {
                    status = "error",
                    data = "Time is up!"
                };
                return Json(response2);
            }

            if (file != null)
            {
                string dirPath = "";

                var solution = db.Solutions.FirstOrDefault(s => s.StudentId == GetId() && s.TaskId == taskId);

                if (solution == null)
                {
                    string fileName = String.Join('.', file.FileName.Split('.').TakeWhile(s => s != "zip"));
                    dirPath = solutionsDir + taskId.ToString() + "/" + GetId().ToString() + "/";
                }

                if (Directory.Exists(dirPath))
                {
                    Directory.Delete(dirPath, true);
                }

                var dir = Directory.CreateDirectory(dirPath);
                string fullPath = dirPath + file.FileName;

                // сохраняем файл в папку
                using (var fileStream = new FileStream(fullPath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }
                ZipFile.ExtractToDirectory(fullPath, dirPath, true);

                var pathToProj = FindProjectFile(dir);

                if (pathToProj == null)
                {
                    var response2 = new
                    {
                        status = "error",
                        data = "No project file found!"
                    };
                    return Json(response2);
                }
                bool res = false;
                //var result = await SolutionTester.TestSolution(wwwroot, task, pathToProj, config);

                //bool res = WriteResultToDb(solution, result, taskId, dirPath);

                if (!res)
                {
                    var response2 = new
                    {
                        status = "error",
                        data = "Error on writing to db!"
                    };
                    return Json(response2);
                }

                var response1 = new
                {
                    status = "success",
                    data = "./SendStudentSolution?id=" + taskId.ToString()
                };
                return Json(response1);
            }

            var response = new
            {
                status = "error",
                data = "Error while sending file!"
            };
            return Json(response);
        }

        //bool WriteResultToDb(Solution solution, AllTestsResult result, int taskId, string solPath)
        //{
        //    if (solution == null)
        //    {
        //        var newSolution = new Solution
        //        {
        //            PathToSolution = solPath,
        //            Results = result,
        //            TaskId = taskId,
        //            StudentId = GetId()
        //        };

        //        var x = db.Solutions.Add(newSolution);
        //        var beforeState = x.State;
        //        int r = db.SaveChanges();
        //        var afterState = x.State;
        //        bool ok = beforeState == Microsoft.EntityFrameworkCore.EntityState.Added && afterState == Microsoft.EntityFrameworkCore.EntityState.Unchanged && r == 1;
        //        return ok;
        //    }
        //    else
        //    {
        //        solution.Results = result;
        //        solution.PathToSolution = solPath;

        //        var x = db.Solutions.Update(solution);
        //        var beforeState = x.State;
        //        int r = db.SaveChanges();
        //        var afterState = x.State;
        //        bool ok = beforeState == Microsoft.EntityFrameworkCore.EntityState.Modified && afterState == Microsoft.EntityFrameworkCore.EntityState.Unchanged && r == 1;
        //        return ok;
        //    }
        //}
    }
}
