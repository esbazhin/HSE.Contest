using HSE.Contest.Areas.TestingSystem.ViewModels;
using HSE.Contest.ClassLibrary;
using HSE.Contest.ClassLibrary.Communication.Requests;
using HSE.Contest.ClassLibrary.Communication.Responses;
using HSE.Contest.ClassLibrary.DbClasses.TestingSystem;
using HSE.Contest.ClassLibrary.TestsClasses.ReflectionTest;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Threading.Tasks;

namespace HSE.Contest.Areas.TestingSystem.Controllers
{
    [Authorize(Roles = "professor")]
    [Area("TestingSystem")]
    public class ProfessorController : TestingSystemController
    {
        public ProfessorController() : base()
        {
        }

        public IActionResult Index()
        {
            return RedirectToAction("AllTasks");
        }

        public IActionResult AllTasks()
        {
            var tasks = db.StudentTasks.Select(t => new TaskViewModel
            {
                Id = t.Id,
                Name = t.Name,
                Description = $"{(t.Group == null ? "Не назначена" : $"Назначена: {t.Group.Name}")}, длительность: {(t.To - t.From).TotalMinutes} минут с {t.From} до {t.To}"
            }).OrderByDescending(m => m.Id).ToList();
            return View(tasks);
        }

        public IActionResult DeleteTask(int id)
        {
            var y = db.StudentTasks.Find(id);
            db.StudentTasks.Remove(y);
            db.SaveChanges();
            return RedirectToAction("AllTasks");
        }

        public IActionResult CreateNewTask()
        {
            var groups = db.Groups.Select(g => new GroupViewModel(g)).ToList();
            var testTypes = config.Tests.Keys.Select(k => new TestType(k)).ToList();
            return View(new TaskCRUDViewModel { Groups = groups, TestTypes = testTypes, Task = new TaskViewModelNew(groups[0].Id), IsUpdate = false });
        }

        public IActionResult ChangeTask(int id)
        {
            var groups = db.Groups.Select(g => new GroupViewModel(g)).ToList();
            var testTypes = config.Tests.Keys.Select(k => new TestType(k)).ToList();
            var cur = db.StudentTasks.Find(id);

            if (cur is null)
            {
                return NotFound();
            }

            foreach (var tt in cur.Tests)
            {
                var ttt = testTypes.Find(t => t.Type == tt.TestType);

                if (ttt != null)
                {
                    ttt.Disabled = true;
                }
            }

            return View("CreateNewTask", new TaskCRUDViewModel { Groups = groups, TestTypes = testTypes, Task = new TaskViewModelNew(cur), IsUpdate = true });
        }

        public IActionResult EditTaskTestData(int id)
        {
            var y = db.TaskTests.Find(id);

            if (y is null)
            {
                NotFound();
            }           

            var task = db.StudentTasks.Find(y.TaskId);

            if (task is null)
            {
                NotFound();
            }
           
            var codeStyleFiles = db.CodeStyleFiles.Select(f => new CodeStyleFilesViewModel(f)).ToArray();

            return View(new TaskTestViewModel(y, task, config.CompilerImages.Keys.ToArray(), codeStyleFiles));
        }

        public IActionResult DeleteTaskTest(int id)
        {
            var y = db.TaskTests.Find(id);

            if (y is null)
            {
                return Content("error");
            }

            var taskId = y.TaskId;
            db.TaskTests.Remove(y);
            db.SaveChanges();

            return Content("/TestingSystem/Professor/ChangeTask?id=" + taskId.ToString());
        }

        public IActionResult UpdateTask(string json)
        {
            TaskViewModelNew jsonTask = JsonConvert.DeserializeObject<TaskViewModelNew>(json);

            var y = db.StudentTasks.Find(jsonTask.Id);

            if (y is null)
            {
                return Content("error");
            }

            y.Name = jsonTask.Name;
            y.GroupId = jsonTask.GroupId;
            y.TaskText = jsonTask.Text;
            y.IsContest = jsonTask.IsContest;
            y.From = jsonTask.Time[0];
            y.To = jsonTask.Time[1];
            y.NumberOfAttempts = jsonTask.AttemptsNumber;

            var x = db.StudentTasks.Update(y);
            var beforeState = x.State;
            int r = db.SaveChanges();
            var afterState = x.State;
            bool ok = beforeState == Microsoft.EntityFrameworkCore.EntityState.Modified && afterState == Microsoft.EntityFrameworkCore.EntityState.Unchanged && r == 1;

            if (ok)
            {
                var oldTaskTests = jsonTask.Tests.Where(tt => !tt.IsNew).ToList();

                foreach (var tt in oldTaskTests)
                {
                    var oldTt = db.TaskTests.Find(tt.Id);

                    if (oldTt is null)
                    {
                        return Content("error");
                    }

                    if (oldTt.Weight == tt.Weight / 100.0 && oldTt.Block == oldTt.Block)
                    {
                        continue;
                    }

                    oldTt.Block = tt.Block;
                    oldTt.Weight = tt.Weight / 100.0;

                    x = db.StudentTasks.Update(y);
                    beforeState = x.State;
                    db.SaveChanges();
                    afterState = x.State;
                    ok = beforeState == Microsoft.EntityFrameworkCore.EntityState.Modified && afterState == Microsoft.EntityFrameworkCore.EntityState.Unchanged;

                    if (!ok)
                    {
                        return Content("error");
                    }
                }

                var newTaskTests = jsonTask.Tests.Where(tt => tt.IsNew).Select(tt =>
                 new TaskTest
                 {
                     TaskId = y.Id,
                     TestType = tt.Type,
                     Block = tt.Block,
                     Weight = tt.Weight / 100.0,
                     TestData = null
                 }).ToArray();

                db.TaskTests.AddRange(newTaskTests);
                r = db.SaveChanges();
                ok = r == newTaskTests.Length;
            }


            return Content(ok ? "/TestingSystem/Professor/ChangeTask?id=" + x.Entity.Id.ToString() : "error");
        }

        public IActionResult PostNewTask(string json)
        {
            TaskViewModelNew jsonTask = JsonConvert.DeserializeObject<TaskViewModelNew>(json);
            StudentTask newTask = new StudentTask
            {
                Name = jsonTask.Name,
                GroupId = jsonTask.GroupId,
                TaskText = jsonTask.Text,
                IsContest = jsonTask.IsContest,
                From = jsonTask.Time[0],
                To = jsonTask.Time[1],
                NumberOfAttempts = jsonTask.AttemptsNumber
            };

            var x = db.StudentTasks.Add(newTask);
            var beforeState = x.State;
            int r = db.SaveChanges();
            var afterState = x.State;
            bool ok = beforeState == Microsoft.EntityFrameworkCore.EntityState.Added && afterState == Microsoft.EntityFrameworkCore.EntityState.Unchanged && r == 1;

            if (ok)
            {
                var newTaskTests = jsonTask.Tests.Select(tt =>
                 new TaskTest
                 {
                     TaskId = newTask.Id,
                     TestType = tt.Type,
                     Block = tt.Block,
                     Weight = tt.Weight / 100.0,
                     TestData = null
                 }).ToArray();

                db.TaskTests.AddRange(newTaskTests);
                r = db.SaveChanges();
                ok = r == newTaskTests.Length;
            }

            return Content(ok ? "/TestingSystem/Professor/ChangeTask?id=" + newTask.Id.ToString() : "error");
        }

        public IActionResult UpdateTaskTest(string json)
        {
            TaskTestViewModel jsonTask = JsonConvert.DeserializeObject<TaskTestViewModel>(json);

            var y = db.TaskTests.Find(jsonTask.Id);

            if (y is null)
            {
                return Content("error");
            }

            y.TestData = jsonTask.IsRaw ? jsonTask.Data.ToString() : JsonConvert.SerializeObject(jsonTask.Data);

            var x = db.TaskTests.Update(y);
            var beforeState = x.State;
            int r = db.SaveChanges();
            var afterState = x.State;
            bool ok = beforeState == Microsoft.EntityFrameworkCore.EntityState.Modified && afterState == Microsoft.EntityFrameworkCore.EntityState.Unchanged && r == 1;

            return Content(ok ? "/TestingSystem/Professor/EditTaskTestData?id=" + y.Id.ToString() : "error");
        }

        public async Task<IActionResult> ChangeTaskWithFile(IFormFile file, string framework)
        {
            if (file != null)
            {
                var isAlive = await CheckIfAlive(config.CompilerServicesOrchestrator);

                if (isAlive)
                {
                    var req = new CompilationRequest
                    {
                        File = await file.GetBytes(),
                        Framework = framework
                    };

                    string url = config.CompilerServicesOrchestrator.GetFullTaskLinkFrom(config.FrontEnd);
                    using var httpClient = new HttpClient();
                    using var form = JsonContent.Create(req);
                    HttpResponseMessage response = await httpClient.PostAsync(url, form);
                    string apiResponse = await response.Content.ReadAsStringAsync();
                    if (response.IsSuccessStatusCode)
                    {
                        CompilationResponse compResponse = JsonConvert.DeserializeObject<CompilationResponse>(apiResponse);

                        if (compResponse.OK)
                        {
                            string dirPath = "c:/TemporaryTaskDownloads";

                            dirPath += "/" + Guid.NewGuid().ToString();
                            while (Directory.Exists(dirPath))
                            {
                                dirPath += "/" + Guid.NewGuid().ToString();
                            }

                            var dir = Directory.CreateDirectory(dirPath);

                            string fullPath = dirPath + "/" + file.Name;

                            System.IO.File.WriteAllBytes(fullPath, compResponse.File);
                            ZipFile.ExtractToDirectory(fullPath, dirPath, true);

                            var pathToDll = FindAssemblyFile(dir, ".dll");

                            if (pathToDll == null)
                            {
                                pathToDll = FindAssemblyFile(dir, ".exe");
                                if (pathToDll == null)
                                {                                   
                                    var result = ConvertProject(pathToDll);

                                    var response1 = new
                                    {
                                        status = "success",
                                        data = result
                                    };

                                    return Json(response1);
                                }
                            }

                            dir.Delete(true);

                            var response2 = new
                            {
                                status = "error",
                                data = "No dll or exe file found!"
                            };

                            return Json(response2);
                        }

                        var response3 = new
                        {
                            status = "error",
                            data = "Compilation container returned error with message: " + compResponse.Message
                        };

                        return Json(response3);
                    }

                    var response4 = new
                    {
                        status = "error",
                        data = "Compilation container returned " + response.StatusCode + " !"
                    };

                    return Json(response4);
                }

                var response5 = new
                {
                    status = "error",
                    data = "Compilation serviec is dead!"
                };

                return Json(response5);
            }
            var response6 = new
            {
                status = "error",
                data = "No file uploaded!"
            };

            return Json(response6);
        }

        string FindAssemblyFile(DirectoryInfo dir, string type)
        {
            var f = dir.GetFiles().FirstOrDefault(f => f.Name.EndsWith(type));
            if (f == null)
            {
                string res = null;
                foreach (var subDir in dir.GetDirectories())
                {
                    res = FindAssemblyFile(subDir, type);
                    if (res != null)
                    {
                        break;
                    }
                }
                return res;
            }
            return f.FullName;
        }

        async Task<bool> CheckIfAlive(ServiceConfig config)
        {
            using var httpClient = new HttpClient();

            HttpResponseMessage response = await httpClient.GetAsync(config.GetHostLinkFrom(this.config.FrontEnd) + "/health");

            if (response.IsSuccessStatusCode)
            {
                string apiResponse = await response.Content.ReadAsStringAsync();
                HealthResponse obj = JsonConvert.DeserializeObject<HealthResponse>(apiResponse);
                return obj.Status == "Healthy";
            }
            else
            {
                return false;
            }
        }

        private List<ClassDefinition> ConvertProject(string pathToDll)
        {
            Assembly ass = Assembly.Load(System.IO.File.ReadAllBytes(pathToDll));
            var result = GetAllClasses(ass);

            return result;
        }

        private List<ClassDefinition> GetAllClasses(Assembly ass)
        {
            return ass.GetTypes().Where(t => t.IsClass).Select((t, i) => new ClassDefinition(t, i)).ToList();
        }      
    }
}
