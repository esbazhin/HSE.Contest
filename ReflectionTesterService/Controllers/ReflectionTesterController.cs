using HSE.Contest.ClassLibrary;
using HSE.Contest.ClassLibrary.Communication.Requests;
using HSE.Contest.ClassLibrary.Communication.Responses;
using HSE.Contest.ClassLibrary.DbClasses;
using HSE.Contest.ClassLibrary.DbClasses.TestingSystem;
using HSE.Contest.ClassLibrary.TestsClasses.ReflectionTest;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace ReflectionTesterService.Controllers
{
    [Route("[controller]/[action]")]
    [ApiController]
    public class ReflectionTesterController : ControllerBase
    {
        private readonly HSEContestDbContext _db;
        public ReflectionTesterController(HSEContestDbContext db)
        {
            _db = db;
        }

        [HttpPost]
        public async Task<TestResponse> TestProject([FromBody] TestRequest request)
        {
            try
            {
                var solution = _db.Solutions.Find(request.SolutionId);

                if (solution is null)
                {
                    return new TestResponse
                    {
                        OK = false,
                        Message = "no solution found",
                        Result = ResultCode.IE,
                        TestId = request.TestId,
                    };
                }

                var compilation = _db.CompilationResults.Find(request.SolutionId);

                if (compilation is null)
                {
                    return new TestResponse
                    {
                        OK = false,
                        Message = "no compilation found",
                        Result = ResultCode.IE,
                        TestId = request.TestId,
                    };
                }

                if (compilation.ResultCode != ResultCode.OK || compilation.File is null)
                {
                    return new TestResponse
                    {
                        OK = false,
                        Message = "no compilation file found",
                        Result = ResultCode.IE,
                        TestId = request.TestId,
                    };
                }
                else
                {
                    string dirPath = "/home/solution";

                    dirPath += "/" + Guid.NewGuid().ToString();
                    while (Directory.Exists(dirPath))
                    {
                        dirPath += "/" + Guid.NewGuid().ToString();
                    }

                    var dir = Directory.CreateDirectory(dirPath);

                    string fullPath = dirPath + "/" + compilation.File.Name;

                    System.IO.File.WriteAllBytes(fullPath, compilation.File.Content);
                    ZipFile.ExtractToDirectory(fullPath, dirPath, true);

                    var pathToDll = FindAssemblyFile(dir, ".dll");

                    TestingResult result;
                    if (pathToDll == null)
                    {
                        pathToDll = FindAssemblyFile(dir, ".exe");
                        if (pathToDll == null)
                        {                            
                            dir.Delete(true);

                            result = new TestingResult
                            {
                                SolutionId = solution.Id,
                                TestId = request.TestId,
                                Commentary = "No dll or exe file found!",
                                ResultCode = ResultCode.RE,                               
                            };

                            return WriteToDb(result);
                        }
                    }

                    var resp = await TestReflection(pathToDll, request.TestId);
                    resp.OK = true;

                    dir.Delete(true);

                    result = new TestingResult
                    {
                        SolutionId = solution.Id,
                        TestId = request.TestId,
                        Commentary = resp.Commentary,
                        ResultCode = resp.Result,
                        Score = resp.Score,
                        TestData = JsonConvert.SerializeObject(resp)
                    };

                    return WriteToDb(result);
                }
            }
            catch (Exception e)
            {
                return new TestResponse
                {
                    OK = false,
                    Message = "Error occured: " + e.Message + (e.InnerException is null ? "" : " Inner: " + e.InnerException.Message),
                    Result = ResultCode.IE,
                    TestId = request.TestId,
                };
            }
        }

        TestResponse WriteToDb(TestingResult res)
        {
            var x = _db.TestingResults.Add(res);
            var beforeState = x.State;
            int r = _db.SaveChanges();
            var afterState = x.State;

            bool ok = beforeState == EntityState.Added && afterState == EntityState.Unchanged && r == 1;

            TestResponse response;

            if (ok)
            {
                response = new TestResponse
                {
                    OK = true,
                    Message = "success",
                    Result = res.ResultCode,
                    Score = res.Score,
                    TestResultId = res.Id,
                    TestId = res.TestId,
                };
            }
            else
            {
                response = new TestResponse
                {
                    OK = false,
                    Message = "can't write result to db",
                    Result = ResultCode.IE,
                    Score = res.Score,
                    TestId = res.TestId,
                };
            }

            return response;
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

        async Task<ReflectionTestResult> TestReflection(string pathToDll, int testId)
        {
            var task = _db.TaskTests.Find(testId);
            var data = JsonConvert.DeserializeObject<ReflectionTestData>(task.TestData);
            Assembly ass = Assembly.Load(System.IO.File.ReadAllBytes(pathToDll));
            var tasks = new List<Task<List<SingleReflectionTestResult>>>();
            var allTypes = ass.GetTypes().Select(t => new ClassDefinition(t, 0));

            foreach (var test in data.ClassDefinitions)
            {
                var cur = allTypes.FirstOrDefault(t => t.Name == test.Name.Replace(" ", ""));
                if (cur == null)
                {
                    tasks.Add(Task.Run(() => new List<SingleReflectionTestResult>() { new SingleReflectionTestResult($"Не найден класс {test.Name}") }));
                }
                else
                {
                    tasks.Add(ReflectionTester.TestClass(test, cur));
                }
            }

            var complTasks = await Task.WhenAll(tasks);
            var results = complTasks.Aggregate(new List<SingleReflectionTestResult>(), (res, cur) =>
            {
                var result = new List<SingleReflectionTestResult>(res);
                result.AddRange(cur);
                return result;
            });

            return new ReflectionTestResult { Results = results.ToArray() };
        }
    }
}
