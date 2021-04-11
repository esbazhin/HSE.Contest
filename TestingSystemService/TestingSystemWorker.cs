using HSE.Contest.ClassLibrary;
using HSE.Contest.ClassLibrary.Communication.Requests;
using HSE.Contest.ClassLibrary.Communication.Responses;
using HSE.Contest.ClassLibrary.DbClasses;
using HSE.Contest.ClassLibrary.DbClasses.TestingSystem;
using HSE.Contest.ClassLibrary.RabbitMQ;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

namespace TestingSystemService
{
    public class TestingSystemWorker : BackgroundService
    {
        private readonly IBus _busControl;
        private readonly TestingSystemConfig config;
        private readonly HSEContestDbContext db;
        public TestingSystemWorker()
        {
            string pathToConfig = "c:\\config\\config.json";
            config = JsonConvert.DeserializeObject<TestingSystemConfig>(System.IO.File.ReadAllText(pathToConfig));

            DbContextOptionsBuilder<HSEContestDbContext> options = new DbContextOptionsBuilder<HSEContestDbContext>();
            options.UseNpgsql(config.DatabaseInfo.GetConnectionStringFrom(config.TestingSystemWorker));
            db = new HSEContestDbContext(options.Options);

            _busControl = RabbitHutch.CreateBus(config.MessageQueueInfo, config.TestingSystemWorker);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await _busControl.ReceiveAsync<SolutionTestingRequest>(config.MessageQueueInfo.TestingQueueName, x => CheckSolution(x).Start());
        }

        public async Task<TestingSystemResponse> CheckSolution(SolutionTestingRequest solutionId)
        {
            var solution = db.Solutions.Find(solutionId.SolutionId);

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
                            SolutionId = solutionId.SolutionId,
                            TestId = codeStyleTask is null ? -1 : codeStyleTask.Id
                        };

                        string url = config.CompilerServicesOrchestrator.GetFullTestLinkFrom(config.TestingSystemWorker);
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
                                                testTasks.Add(StartTest(test.TestType, solutionId.SolutionId, test.Id));
                                            }
                                            else
                                            {
                                                testTasks.Add(Task.Run(() => NoTestFound(test.TestType, solutionId.SolutionId, test.Id)));
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

            HttpResponseMessage response = await httpClient.GetAsync(config.GetHostLinkFrom(this.config.TestingSystemWorker) + "/health");

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
                var url = serviceConfig.GetFullTestLinkFrom(config.TestingSystemWorker);
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
            if (solution is null)
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
