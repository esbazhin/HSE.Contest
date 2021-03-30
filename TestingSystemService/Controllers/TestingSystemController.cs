using HSE.Contest.ClassLibrary;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace TestingSystemService.Controllers
{
    [Route("[controller]/[action]")]
    [ApiController]
    public class TestingSystemController : ControllerBase
    {
        TestingSystemConfig config;
        HSEContestDbContext db;
        public TestingSystemController()
        {
            string pathToConfig = "c:\\config\\config.json";
            config = JsonConvert.DeserializeObject<TestingSystemConfig>(System.IO.File.ReadAllText(pathToConfig));

            DbContextOptionsBuilder<HSEContestDbContext> options = new DbContextOptionsBuilder<HSEContestDbContext>();
            options.UseNpgsql(config.DatabaseInfo.GetConnectionStringFrom(config.TestingSystem));
            db = new HSEContestDbContext(options.Options);
        }

        [HttpPost]
        public async Task<TestingSystemResponse> CheckSolutionDebug([FromForm] IFormFile file, [FromForm] int taskId, [FromForm] int studentId, [FromForm] string frameworkType)
        {
            int fileId = db.UploadFile(file.FileName, file.GetBytes().Result);

            if (fileId == -1)
            {
                return new TestingSystemResponse
                {
                    Message = "error on uploading file"
                };
            }
            var solution = new Solution
            {
                TaskId = taskId,
                FrameworkType = frameworkType,
                FileId = fileId,
                StudentId = studentId,
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
                return new TestingSystemResponse
                {
                    Message = "error on uploading solution"
                };
            }
            return await CheckSolution(solution.Id);
        }

        [HttpPost]
        public Response UploadCodeStyleFilesDebug([FromForm] string name, [FromForm] IFormFile stylecop, [FromForm] IFormFile ruleset)
        {
            if (stylecop is null || ruleset is null)
            {
                return new Response
                {
                    OK = false,
                    Message = "codestyle files are null!"
                };
            }

            CodeStyleFiles files = new CodeStyleFiles
            {
                Name = name,
                StyleCopFile = stylecop.GetBytes().Result,
                RulesetFile = ruleset.GetBytes().Result
            };

            db.CodeStyleFiles.Add(files);
            db.SaveChanges();

            return new Response
            {
                OK = true,
                Message = "codestyle files are updated!"
            };
        }

        [HttpPost]
        public async Task<TestingSystemResponse> CheckSolution([FromForm] int solutionId)
        {
            var solution = db.Solutions.Find(solutionId);

            if (solution != null && solution.File != null)
            {
                var taskTests = db.TaskTests.Where(t => t.TaskId == solution.TaskId).ToArray();

                if (taskTests != null && taskTests.Length > 0)
                {
                    var isAlive = await CheckIfAlive(config.CompilerServicesOrchestrator);

                    if (isAlive)
                    {
                        //var dataBytes = await file.GetBytes();                        
                        var codeStyleTask = taskTests.FirstOrDefault(t => t.TestType == "codeStyleTest");

                        var req = new TestRequest
                        {
                            SolutionId = solutionId,
                            TestId = codeStyleTask is null ? -1 : codeStyleTask.Id
                        };

                        string url = config.CompilerServicesOrchestrator.GetFullLinkFrom(config.TestingSystem);
                        using var httpClient = new HttpClient();
                        using var form = JsonContent.Create(req);
                        HttpResponseMessage response = await httpClient.PostAsync(url, form);
                        string apiResponse = await response.Content.ReadAsStringAsync();
                        if (response.IsSuccessStatusCode)
                        {
                            TestResponse compResponse = JsonConvert.DeserializeObject<TestResponse>(apiResponse);

                            if (compResponse.OK && compResponse.Result == ResultCode.OK)
                            {
                                var compResult = db.CompilationResults.Find(solutionId);

                                if (compResult != null)
                                {
                                    if (compResult.ResultCode != ResultCode.CE && compResult.File != null)
                                    {
                                        var testTasks = new List<Task<TestResponse>>();
                                        foreach (var test in taskTests)
                                        {
                                            if (config.Tests.ContainsKey(test.TestType))
                                            {
                                                testTasks.Add(StartTest(test.TestType, solutionId, test.Id));
                                            }
                                            else
                                            {
                                                testTasks.Add(Task.Run(() => NoTestFound(test.TestType, solutionId, test.Id)));
                                            }
                                        }
                                        await Task.WhenAll(testTasks);

                                        var responses = testTasks.Select(t => t.Result).ToArray();
                                        var results = responses.Join(taskTests, i => i.TestId, o => o.Id, (i, o) => new { Result = i, Definition = o }).ToArray();

                                        double totalScore = 0;
                                        ResultCode totalResult = results.Select(r => r.Result.Result).Max();

                                        foreach (var res in results)
                                        {
                                            if (res.Definition.Block && res.Result.Score == 0)
                                            {
                                                totalScore = 0;                                              

                                                break;
                                            }
                                            else
                                            {
                                                totalScore += res.Result.Score * res.Definition.Weight;
                                            }
                                        }

                                        return WriteToDb(solution, totalResult, totalScore, "success", true, responses);                                       
                                    }
                                    else
                                    {
                                        return WriteToDb(solution, compResult.ResultCode, 0, "compilation error!", true);
                                    }
                                }
                                else
                                {
                                    return WriteToDb(solution, compResponse.Result, 0, "can't find compilation! Inner message: " + compResponse.Message, false);
                                }
                            }
                            else
                            {
                                return WriteToDb(solution, compResponse.Result, 0, "something went wrong during compilation! Inner message: " + compResponse.Message, false);                               
                            }
                        }
                        else
                        {
                            return WriteToDb(solution, ResultCode.IE, 0, "bad response from compilation container: " + response.StatusCode, false);                           
                        }
                    }
                }
                else
                {
                    return WriteToDb(solution, ResultCode.IE, 0, "Compiler Service Is Dead!", false);                    
                }
            }
            return WriteToDb(null, ResultCode.IE, 0, "Can't find solution!", false);
        }

        async Task<bool> CheckIfAlive(ServiceConfig config)
        {
            using var httpClient = new HttpClient();

            HttpResponseMessage response = await httpClient.GetAsync(config.GetHostLinkFrom(this.config.TestingSystem) + "/health");

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

        async Task<TestResponse> StartTest(string testName, int solutionId, int testId)
        {
            var serviceConfig = config.Tests[testName];
            var isAlive = await CheckIfAlive(serviceConfig);

            if (isAlive)
            {
                var req = new TestRequest
                {
                    SolutionId = solutionId,
                    TestId = testId,
                };

                using var httpClient = new HttpClient();
                using var form = JsonContent.Create(req);
                var url = serviceConfig.GetFullLinkFrom(config.TestingSystem);
                HttpResponseMessage response = await httpClient.PostAsync(url, form);
                string apiResponse = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {
                    TestResponse testResponse = JsonConvert.DeserializeObject<TestResponse>(apiResponse);

                    if (!testResponse.OK || testResponse.Result == ResultCode.IE)
                    {
                        return WriteTestResponseToDb(testResponse, solutionId, testId);
                    }
                    return testResponse;
                }
                else
                {
                    return WriteTestResponseToDb(new TestResponse
                    {
                        OK = false,
                        Message = "Container of " + testName + " replied with " + response.StatusCode,
                        Result = ResultCode.IE,
                        TestId = testId,
                    }, solutionId, testId);
                }
            }
            else
            {
                return WriteTestResponseToDb(new TestResponse
                {
                    OK = false,
                    Message = "Container of " + testName + " Is Dead!",
                    Result = ResultCode.IE,
                    TestId = testId,
                }, solutionId, testId);
            }
        }

        TestResponse NoTestFound(string testName, int solutionId, int testId)
        {
            return WriteTestResponseToDb(new TestResponse
            {
                OK = false,
                Message = "Container of " + testName + " Is Dead!",
                Result = ResultCode.IE,
                Score = 10,
            }, solutionId, testId);

        }

        TestResponse WriteTestResponseToDb(TestResponse resp, int solutionId, int testId)
        {
            var testResult = db.TestingResults.FirstOrDefault(t => t.SolutionId == solutionId && t.TestId == testId);

            if (testResult is null)
            {
                var res = new TestingResult
                {
                    Score = resp.Score,
                    Commentary = resp.Message,
                    SolutionId = solutionId,
                    TestId = testId,
                    ResultCode = resp.Result,
                };
                var x = db.TestingResults.Add(res);
                var beforeState = x.State;
                int r = db.SaveChanges();
                var afterState = x.State;

                bool ok = beforeState == EntityState.Added && afterState == EntityState.Unchanged && r == 1;

                TestResponse response;

                if (ok)
                {
                    response = new TestResponse
                    {
                        OK = true,
                        Message = "success",
                        Result = ResultCode.OK,
                        Score = res.Score,
                        TestResultId = res.Id,
                        TestId = testId,
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
                        TestId = testId,
                    };
                }

                return response;
            }
            else
            {
                return resp;
            }
        }

        TestingSystemResponse WriteToDb(Solution solution, ResultCode totalResult, double totalScore, string msg, bool ok, TestResponse[] responses = null)
        {
            if(solution is null)
            {
                return new TestingSystemResponse
                {
                    OK = ok,
                    Message = msg,
                    Responses = responses,
                };
            }

            solution.ResultCode = totalResult;
            solution.Score = totalScore;
            db.SaveChanges();

            return new TestingSystemResponse
            {
                OK = ok,
                Message = msg,
                SolutionId = solution.Id,
                Result = solution.ResultCode,
                Score = solution.Score,
                Responses = responses,
            };
        }
    }
}
