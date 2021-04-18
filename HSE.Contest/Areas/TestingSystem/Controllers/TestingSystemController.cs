using HSE.Contest.Areas.TestingSystem.ViewModels;
using HSE.Contest.ClassLibrary;
using HSE.Contest.ClassLibrary.DbClasses;
using HSE.Contest.ClassLibrary.DbClasses.TestingSystem;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HSE.Contest.Areas.TestingSystem.Controllers
{
    public class TestingSystemController : Controller
    {
        protected readonly HSEContestDbContext _db;
        protected readonly TestingSystemConfig _config;
        protected readonly string _pathToConfigDir;

        public TestingSystemController(HSEContestDbContext db, TestingSystemConfig config)
        {
            _pathToConfigDir = "c:\\config";
            _db = db;
            _config = config;
        }

        protected string FindProjectFile(DirectoryInfo dir)
        {
            var f = dir.GetFiles().FirstOrDefault(f => f.Name.EndsWith(".csproj"));
            if (f == null)
            {
                string res = null;
                foreach (var subDir in dir.GetDirectories())
                {
                    res = FindProjectFile(subDir);
                    if (res != null)
                    {
                        break;
                    }
                }
                return res;
            }
            return f.FullName;
        }

        protected StudentTaskViewModel GetStudentTaskView(int taskId, string studentId)
        {
            var t = _db.StudentTasks.Find(taskId);

            if (t is null)
            {
                return null;
            }

            var solutions = _db.Solutions.Where(s => s.StudentId == studentId && s.TaskId == taskId).ToArray();
            var solutionsViewModels = solutions.Select(s =>
            new SolutionViewModel
            {
                Id = s.Id,
                TotalScore = s.Score,
                Result = s.ResultCode.ToString(),
                Time = s.Time,
            }).OrderByDescending(s => s.Time).ToArray();

            var stud = _db.Users.Find(studentId);
            var res = new StudentTaskViewModel
            {
                StudentName = stud.FirstName + " " + stud.LastName,
                StudentId = studentId,
                TaskId = t.Id,
                TaskName = t.Name,
                Description = t.TaskText,
                Solutions = solutionsViewModels,
                Deadline = t.To,
                NumberOfAttempts = t.NumberOfAttempts,
                CanSend = CanSend(t, studentId, solutions),
                Result = "Нет решений",
                FrameworkTypes = _config.CompilerImages.Keys.ToArray()
            };

            var studentResult = _db.StudentResults.Find(studentId, taskId);

            if (studentResult != null)
            {
                res.TotalScore = studentResult.Solution.Score;
                res.Result = studentResult.Solution.ResultCode.ToString();
            }

            return res;
        }

        protected bool CanSend(StudentTask task, string studentId, Solution[] solutions = null)
        {
            bool canSend = false;

            if (task.To > DateTime.Now)
            {
                if (solutions is null)
                {
                    solutions = _db.Solutions.Where(s => s.StudentId == studentId && s.TaskId == task.Id).ToArray();
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

        protected async Task SSEMethod(string ids, int taskId, string studentId)
        {
            int[] solIds = JsonConvert.DeserializeObject<int[]>(ids);
            var response = Response;
            response.Headers.Add("Content-Type", "text/event-stream");
            response.Headers.Add("Cache-Control", "no-cache");
            response.Headers.Add("Connection", "keep-alive");

            var curState = new Dictionary<int, ResultCode>();

            foreach (var id in solIds)
            {
                var s = _db.Solutions.Find(id);

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
                    var s = _db.Solutions.Find(id);
                    _db.Entry(s).Reload();

                    if (s != null && s.ResultCode != ResultCode.NT && curState[id] == ResultCode.NT)
                    {
                        curState[id] = s.ResultCode;
                        update = true;
                        break;
                    }
                }

                if (update)
                {
                    solIds = solIds.Where(i => curState[i] == ResultCode.NT).ToArray();

                    var res = GetStudentTaskView(taskId, studentId);

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
    }
}
