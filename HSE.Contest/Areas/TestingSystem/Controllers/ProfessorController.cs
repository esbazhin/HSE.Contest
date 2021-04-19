using HSE.Contest.Areas.TestingSystem.ViewModels;
using HSE.Contest.ClassLibrary;
using HSE.Contest.ClassLibrary.Communication.Requests;
using HSE.Contest.ClassLibrary.Communication.Responses;
using HSE.Contest.ClassLibrary.DbClasses;
using HSE.Contest.ClassLibrary.DbClasses.TestingSystem;
using HSE.Contest.ClassLibrary.RabbitMQ;
using HSE.Contest.ClassLibrary.TestsClasses.ReflectionTest;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
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
        public ProfessorController(HSEContestDbContext db, TestingSystemConfig config) : base(db, config)
        {
        }

        public IActionResult Index()
        {
            return RedirectToAction("AllTasks");
        }

        public IActionResult AllTasks()
        {
            var tasks = _db.StudentTasks.Select(t => new TaskViewModel
            {
                Id = t.Id,
                Name = t.Name,
                Description = $"{(t.Group == null ? "Не назначена" : $"Назначена: {t.Group.Name}")}, длительность: {(t.To - t.From).TotalMinutes} минут с {t.From} до {t.To}"
            }).OrderByDescending(m => m.Id).ToList();
            return View(tasks);
        }

        public IActionResult DeleteTask(int id)
        {
            var y = _db.StudentTasks.Find(id);
            _db.StudentTasks.Remove(y);
            _db.SaveChanges();
            return RedirectToAction("AllTasks");
        }

        public IActionResult CreateNewTask()
        {
            var groups = _db.Groups.Select(g => new GroupViewModel(g)).ToList();
            var testTypes = _config.Tests.Keys.Select(k => new TestType(k)).ToList();
            if (groups.Count == 0)
            {
                return NoContent();
            }
            return View(new TaskCRUDViewModel { Groups = groups, TestTypes = testTypes, Task = new TaskViewModelNew(groups[0].Id), IsUpdate = false });
        }

        public IActionResult ChangeTask(int id)
        {
            var groups = _db.Groups.Select(g => new GroupViewModel(g)).ToList();
            var testTypes = _config.Tests.Keys.Select(k => new TestType(k)).ToList();
            var cur = _db.StudentTasks.Find(id);

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
            var y = _db.TaskTests.Find(id);

            if (y is null)
            {
                NotFound();
            }

            var task = _db.StudentTasks.Find(y.TaskId);

            if (task is null)
            {
                NotFound();
            }

            var codeStyleFiles = _db.CodeStyleFiles.Select(f => new CodeStyleFilesViewModel(f)).ToArray();

            return View(new TaskTestViewModel(y, task, _config.CompilerImages.Keys.ToArray(), codeStyleFiles));
        }

        public IActionResult DeleteTaskTest(int id)
        {
            var y = _db.TaskTests.Find(id);

            if (y is null)
            {
                return Content("error");
            }

            var taskId = y.TaskId;
            _db.TaskTests.Remove(y);
            _db.SaveChanges();

            return Content("/TestingSystem/Professor/ChangeTask?id=" + taskId.ToString());
        }

        public IActionResult UpdateTask(string json)
        {
            TaskViewModelNew jsonTask = JsonConvert.DeserializeObject<TaskViewModelNew>(json);

            var y = _db.StudentTasks.Find(jsonTask.Id);

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

            var x = _db.StudentTasks.Update(y);
            var beforeState = x.State;
            int r = _db.SaveChanges();
            var afterState = x.State;
            bool ok = beforeState == Microsoft.EntityFrameworkCore.EntityState.Modified && afterState == Microsoft.EntityFrameworkCore.EntityState.Unchanged && r == 1;

            if (ok)
            {
                var oldTaskTests = jsonTask.Tests.Where(tt => !tt.IsNew).ToList();

                foreach (var tt in oldTaskTests)
                {
                    var oldTt = _db.TaskTests.Find(tt.Id);

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

                    x = _db.StudentTasks.Update(y);
                    beforeState = x.State;
                    _db.SaveChanges();
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

                _db.TaskTests.AddRange(newTaskTests);
                r = _db.SaveChanges();
                ok = r == newTaskTests.Length;
            }


            return Content(ok ? "/TestingSystem/Professor/ChangeTask?id=" + x.Entity.Id.ToString() : "error");
        }

        public async Task<IActionResult> PostNewTask(string json)
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

            var x = _db.StudentTasks.Add(newTask);
            var beforeState = x.State;
            int r = _db.SaveChanges();
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

                _db.TaskTests.AddRange(newTaskTests);
                r = _db.SaveChanges();
                ok = r == newTaskTests.Length;
            }

            if (ok)
            {
                var plagCheck = new PlagiarismCheck
                {
                    TaskId = newTask.Id,
                    Settings = new PlagiarismCheckSettings
                    {
                        Language = "csharp",
                        MaxMatches = 5,
                        MinPercent = 0.4,
                        MakeCheck = false
                    }
                };

                _db.Add(plagCheck);
                _db.SaveChanges();

                try
                {
                    var msgQueue = RabbitHutch.CreateBus(_config.MessageQueueInfo, _config.FrontEnd);

                    var request = new PlagiarismCheckRequest
                    {
                        TaskId = newTask.Id,
                    };
                    await msgQueue.SendAsync(_config.MessageQueueInfo.PlagiarismQueueName, request);
                }
                catch (RabbitMQ.Client.Exceptions.BrokerUnreachableException e)
                {
                    return Content("error");
                }
            }

            return Content(ok ? "/TestingSystem/Professor/ChangeTask?id=" + newTask.Id.ToString() : "error");
        }

        public IActionResult UpdateTaskTest(string json)
        {
            TaskTestViewModel jsonTask = JsonConvert.DeserializeObject<TaskTestViewModel>(json);

            var y = _db.TaskTests.Find(jsonTask.Id);

            if (y is null)
            {
                return Content("error");
            }

            y.TestData = jsonTask.IsRaw ? jsonTask.Data.ToString() : JsonConvert.SerializeObject(jsonTask.Data);

            var x = _db.TaskTests.Update(y);
            var beforeState = x.State;
            int r = _db.SaveChanges();
            var afterState = x.State;
            bool ok = beforeState == Microsoft.EntityFrameworkCore.EntityState.Modified && afterState == Microsoft.EntityFrameworkCore.EntityState.Unchanged && r == 1;

            return Content(ok ? "/TestingSystem/Professor/EditTaskTestData?id=" + y.Id.ToString() : "error");
        }

        public async Task<IActionResult> ChangeTaskWithFile(IFormFile file, string framework)
        {
            if (file != null)
            {
                var isAlive = await CheckIfAlive(_config.CompilerServicesOrchestrator);

                if (isAlive)
                {
                    var req = new CompilationRequest
                    {
                        File = await file.GetBytes(),
                        Framework = framework
                    };

                    string url = _config.CompilerServicesOrchestrator.GetFullTaskLinkFrom(_config.FrontEnd);
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
                            }

                            List<ClassDefinition> result = null;
                            if (pathToDll != null)
                            {
                                result = ConvertProject(pathToDll);


                            }

                            dir.Delete(true);

                            if (result != null)
                            {
                                var response1 = new
                                {
                                    status = "success",
                                    data = result
                                };

                                return Json(response1);
                            }
                            else
                            {
                                var response2 = new
                                {
                                    status = "error",
                                    data = "No dll or exe file found!"
                                };

                                return Json(response2);
                            }
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

            HttpResponseMessage response = await httpClient.GetAsync(config.GetHostLinkFrom(this._config.FrontEnd) + "/health");

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

        public IActionResult ChangePlagiarism(int taskId)
        {
            var cur = _db.PlagiarismChecks.Find(taskId);

            return View(cur);
        }

        public IActionResult UpdatePlagiarism(string json)
        {
            var plagJson = JsonConvert.DeserializeObject<PlagiarismCheck>(json);

            var plag = _db.PlagiarismChecks.Find(plagJson.TaskId);

            plag.Settings = plagJson.Settings;

            _db.SaveChanges();

            return Content("/TestingSystem/Professor/ChangePlagiarism?taskId=" + plagJson.TaskId);
        }

        public async Task<IActionResult> ReCheckPlagiarism(int taskId)
        {
            try
            {
                var msgQueue = RabbitHutch.CreateBus(_config.MessageQueueInfo, _config.FrontEnd);

                var request = new PlagiarismCheckRequest
                {
                    TaskId = taskId,
                };
                await msgQueue.SendAsync(_config.MessageQueueInfo.PlagiarismQueueName, request);
                return Content("/TestingSystem/Professor/ChangePlagiarism?taskId=" + taskId);
            }
            catch (RabbitMQ.Client.Exceptions.BrokerUnreachableException e)
            {
                return Content("error");
            }
        }

        public IActionResult ManageSolutions(int taskId)
        {
            var allResults = _db.StudentResults.Where(s => s.TaskId == taskId).ToList();
            var allResultsViews = allResults.Select(r => new ResultsViewModel(r)).ToList();

            return View(allResultsViews);
        }

        public IActionResult ManageStudentSolutions(int taskId, string studentId)
        {
            var res = GetStudentTaskView(taskId, studentId);

            return View(res);
        }

        public async Task SSE(string ids, int taskId, string studentId)
        {
            await SSEMethod(ids, taskId, studentId);
        }

        public IActionResult EditSolution(int id)
        {
            var sol = _db.Solutions.Find(id);

            return View((id, sol.Score));
        }

        public IActionResult UpdateSolutionMark(int id, double score)
        {
            var sol = _db.Solutions.Find(id);
            sol.Score = score;

            //проверяем результат студента, если этот лучше - обновляем
            var studentResult = _db.StudentResults.Find(sol.StudentId, sol.TaskId);

            if (sol.Score >= studentResult.Solution.Score)
            {
                studentResult.SolutionId = id;
            }

            _db.SaveChanges();

            return Redirect("EditSolution?id=" + id);
        }
    }
}
