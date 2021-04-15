using HSE.Contest.Areas.TestingSystem.ViewModels;
using HSE.Contest.ClassLibrary;
using HSE.Contest.ClassLibrary.Communication.Requests;
using HSE.Contest.ClassLibrary.DbClasses.TestingSystem;
using HSE.Contest.ClassLibrary.RabbitMQ;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        public StudentController() : base()
        {           
        }

        public IActionResult Index()
        {
            return RedirectToAction("AllTasks");
        }       

        //public async Task<IActionResult> Receive()
        //{
        //    IActionResult res = Content("empty");
        //    await msgQueue.ReceiveAsync<TestRequest>("check", x =>
        //    {
        //        res = Json(x);
        //    });

        //    return res;
        //}

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
            var res = GetStudentTaskView(id);

            if (res is null)
            {
                return NotFound();
            }

            //JsonSerializerSettings serializerSettings = new JsonSerializerSettings
            //{
            //    ContractResolver = new CamelCasePropertyNamesContractResolver(),
            //    DateTimeZoneHandling = DateTimeZoneHandling.RoundtripKind
            //};

            //return Content(JsonConvert.SerializeObject(res, serializerSettings));

            return View(res);
        }

        StudentTaskViewModel GetStudentTaskView(int taskId)
        {
            var t = db.StudentTasks.Find(taskId);

            if (t is null)
            {
                return null;
            }

            var solutions = db.Solutions.Where(s => s.StudentId == GetId() && s.TaskId == taskId).ToArray();
            var solutionsViewModels = solutions.Select(s =>
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
                Solutions = solutionsViewModels,
                Deadline = t.To,
                NumberOfAttempts = t.NumberOfAttempts,
                CanSend = CanSend(t, solutions),
                Result = "Нет решений",
                FrameworkTypes = config.CompilerImages.Keys.ToArray()
            };

            if (solutions.Length > 0)
            {
                var maxScore = solutionsViewModels.Max(s => s.TotalScore);

                var selectedSolution = solutionsViewModels.Where(s => s.TotalScore == maxScore).OrderByDescending(s => s.Time).First();

                res.TotalScore = selectedSolution.TotalScore;
                res.Result = selectedSolution.Result;
            }

            return res;          
        }

        bool CanSend(StudentTask task, Solution[] solutions = null)
        {
            bool canSend = false;

            if (task.To > DateTime.Now)
            {
                if (solutions is null)
                {
                    solutions = db.Solutions.Where(s => s.StudentId == GetId() && s.TaskId == task.Id).ToArray();
                }

                if (task.IsContest)
                {
                    canSend = solutions.Length < task.NumberOfAttempts;
                }
                else
                {
                    canSend = solutions.Length == 0;
                }
            }

            return canSend;
        }

        public IActionResult ViewSolutionReport(int id)
        {
            var s = db.Solutions.Find(id);

            if (s is null)
            {
                return NotFound();
            }

            var t = db.StudentTasks.Find(s.TaskId);
           
            if (CanSend(t))
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

        public async Task<IActionResult> CheckSolution(IFormFile file, int taskId, string framework)
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

            if (!CanSend(task))
            {
                var response2 = new
                {
                    status = "error",
                    data = "Can't send new solutions!"
                };
                return Json(response2);
            }
            
            if (file != null)
            {
                string dirPath = "c:/TemporaryTaskDownloads";

                dirPath += "/" + Guid.NewGuid().ToString();
                while (Directory.Exists(dirPath))
                {
                    dirPath += "/" + Guid.NewGuid().ToString();
                }

                var dir = Directory.CreateDirectory(dirPath);

                string fullZipFilePath = dirPath + "/" + file.FileName;
                string fullDirPath = dirPath + "/unpacked";

                // сохраняем файл в папку
                using (var fileStream = new FileStream(fullZipFilePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }
                ZipFile.ExtractToDirectory(fullZipFilePath, fullDirPath, true);

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

                CleanSolutionFiles(fullDirPath);

                string newfullZipFilePath = dirPath + "/cleaned_" + file.FileName;
                ZipFile.CreateFromDirectory(fullDirPath, newfullZipFilePath);

                var dataBytes = System.IO.File.ReadAllBytes(newfullZipFilePath);

                int fileId = db.UploadFile(file.FileName, dataBytes);

                if (fileId == -1)
                {
                     var response2 = new
                    {
                        status = "error",
                        data = "Can't upload file to db!"
                    };
                    return Json(response2);
                }

                dir.Delete(true);

                var solution = new Solution
                {
                    TaskId = taskId,
                    FrameworkType = framework,
                    FileId = fileId,
                    StudentId = GetId(),
                    ResultCode = ResultCode.NT,
                    Time = DateTime.Now,
                };

                var x = db.Solutions.Add(solution);
                var beforeState = x.State;
                int r = db.SaveChanges();
                var afterState = x.State;
                bool ok = beforeState == EntityState.Added && afterState == EntityState.Unchanged && r == 1;
            
                if (!ok)
                {
                    var response2 = new
                    {
                        status = "error",
                        data = "Error on writing to db!"
                    };
                    return Json(response2);
                }

                try
                {
                    var msgQueue = RabbitHutch.CreateBus(config.MessageQueueInfo, config.FrontEnd);

                    var request = new SolutionTestingRequest
                    {
                        SolutionId = solution.Id,
                    };
                    await msgQueue.SendAsync(config.MessageQueueInfo.TestingQueueName, request);
                }
                catch (RabbitMQ.Client.Exceptions.BrokerUnreachableException e)
                {
                    var response1 = new
                    {
                        status = "connectionError",
                        msg = "Can't connect to RabbitMQ!",
                        data = GetStudentTaskView(taskId)
                    };
                    return Json(response1);
                }

                var response = new
                {
                    status = "success",
                    data = GetStudentTaskView(taskId)
                };
                return Json(response);
            }

            var response3 = new
            {
                status = "error",
                data = "Error while sending file!"
            };
            return Json(response3);
        }

        public IActionResult CheckForUpdates(string ids, int taskId)
        {
            int[] solIds = JsonConvert.DeserializeObject<int[]>(ids);

            foreach(var id in solIds)
            {
                var s = db.Solutions.Find(id);

                if(s != null && s.ResultCode != ResultCode.NT)
                {
                    var res = GetStudentTaskView(taskId);
                    
                    //JsonSerializerSettings serializerSettings = new JsonSerializerSettings
                    //{
                    //    ContractResolver = new CamelCasePropertyNamesContractResolver(),
                    //    DateTimeZoneHandling = DateTimeZoneHandling.RoundtripKind
                    //};

                    //var data = JsonConvert.SerializeObject(res, serializerSettings);

                    var response1 = new
                    {
                        status = "success",
                        //data = "./SendStudentSolution?id=" + taskId.ToString()
                        data = res
                    };
                    return Json(response1);
                }
            }

            var response2 = new
            {
                status = "error",
            };
            return Json(response2);
        }

        public async Task SSE(string ids, int taskId)
        {
            int[] solIds = JsonConvert.DeserializeObject<int[]>(ids);
            var response = Response;
            response.Headers.Add("Content-Type", "text/event-stream");
            response.Headers.Add("Cache-Control", "no-cache");
            response.Headers.Add("Connection", "keep-alive");

            var curState = new Dictionary<int, ResultCode>();

            foreach (var id in solIds)
            {
                var s = db.Solutions.Find(id);

                if (s != null)
                {
                    curState[id] = s.ResultCode;
                }
            }

            while (true)
            {
                bool update = false;
                foreach (var id in solIds)
                {                    
                    var s = db.Solutions.Find(id);
                    db.Entry(s).Reload();

                    if (s != null && s.ResultCode != ResultCode.NT && curState[id] == ResultCode.NT)
                    {
                        curState[id] = s.ResultCode;
                        update = true;
                        break;
                    }
                }    
                
                if(update)
                {
                    solIds = solIds.Where(i => curState[i] == ResultCode.NT).ToArray();

                    var res = GetStudentTaskView(taskId);

                    JsonSerializerSettings serializerSettings = new JsonSerializerSettings
                    {
                        ContractResolver = new CamelCasePropertyNamesContractResolver(),
                        DateTimeZoneHandling = DateTimeZoneHandling.RoundtripKind
                    };

                    var data = JsonConvert.SerializeObject(res, serializerSettings);

                    await response
                        .WriteAsync("data:" + data + "\n\n");
                }

                await Task.Delay(5 * 1000);
            }
        }

        void CleanSolutionFiles(string dir)
        {
            string gitignorePath = pathToConfigDir + "\\.gitignore";
            var patterns = System.IO.File.ReadAllLines(gitignorePath).Where(l => !string.IsNullOrEmpty(l) && !l.StartsWith("#")).ToList();


            var dirsToDelete = new List<string>();
            var dirs = new string[] { "bin", "obj", ".vs" };

            foreach(var dirr in dirs)
            {
                dirsToDelete.AddRange(Directory.GetDirectories(dir, dirr, SearchOption.AllDirectories));
            }

            foreach(var dirr in dirsToDelete)
            {
                Directory.Delete(dirr, true);
            }

            var filesToDelete = new List<string>();
            foreach (var pattern in patterns)
            {
                var files = Directory.GetFiles(dir, pattern, SearchOption.AllDirectories);
                filesToDelete.AddRange(files);
            }

            foreach (var file in filesToDelete)
            {
                System.IO.File.Delete(file);
            }
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
