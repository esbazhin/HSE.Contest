using HSE.Contest.ClassLibrary;
using HSE.Contest.ClassLibrary.Communication.Requests;
using HSE.Contest.ClassLibrary.Communication.Responses;
using HSE.Contest.ClassLibrary.DbClasses;
using HSE.Contest.ClassLibrary.DbClasses.TestingSystem;
using HSE.Contest.ClassLibrary.TestsClasses.FunctionalTest;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace NetCoreFunctionalTesterService.Controllers
{
    [Route("[controller]/[action]")]
    [ApiController]
    public class FunctionalTesterController : ControllerBase
    {
        HSEContestDbContext db;

        public FunctionalTesterController(HSEContestDbContext context)
        {
            db = context;
        }

        [HttpPost]
        public async Task<TestResponse> TestProject([FromBody] TestRequest request)
        {
            try
            {
                var solution = db.Solutions.Find(request.SolutionId);

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

                var compilation = db.CompilationResults.Find(request.SolutionId);

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

                    if (Directory.Exists(dirPath))
                    {
                        Directory.Delete(dirPath, true);
                    }

                    var dir = Directory.CreateDirectory(dirPath);
                    string fullPath = dirPath + "/" + compilation.File.Name;

                    System.IO.File.WriteAllBytes(fullPath, compilation.File.Content);
                    ZipFile.ExtractToDirectory(fullPath, dirPath, true);

                    var pathToDll = FindDllFile(dir);

                    TestingResult result;
                    if (pathToDll == null)
                    {                        
                        dir.Delete(true);

                        result = new TestingResult
                        {
                            SolutionId = solution.Id,
                            TestId = request.TestId,
                            Commentary = "No dll file found!",
                            ResultCode = ResultCode.RE,
                        };

                        return WriteToDb(result);
                    }

                    var resp = await TestProject(pathToDll, solution.TaskId, request.TestId);
                    resp.OK = true;

                    dir.Delete(true);

                    result = new TestingResult
                    {
                        SolutionId = solution.Id,
                        TestId = request.TestId,
                        Commentary = resp.Commentary,
                        ResultCode = resp.Result,
                        Score = resp.Score,
                        TestData = JsonConvert.SerializeObject(resp.Results)
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

        string FindDllFile(DirectoryInfo dir)
        {
            var f = dir.GetFiles().FirstOrDefault(f => f.Name.EndsWith(".dll"));
            if (f == null)
            {
                string res = null;
                foreach (var subDir in dir.GetDirectories())
                {
                    res = FindDllFile(subDir);
                    if (res != null)
                    {
                        break;
                    }
                }
                return res;
            }
            return f.FullName;
        }

        async Task<FunctionalTestResult> TestProject(string pathToDll, int taskId, int testId)
        {
            var task = db.StudentTasks.Find(taskId);

            var task1 = db.TaskTests.Find(testId);
            var data = JsonConvert.DeserializeObject<FunctionalTestData>(task1.TestData);

            var tasks = data.FunctionalTests.Select(t => Task.Run(() =>
            {
                var result = SingleTest(t.Input, pathToDll, task.TimeLimit.HasValue ? task.TimeLimit.Value : 10000);
                return new SingleFunctTestResult(t.Output.Replace("\r\n", "\n"), result.Item1.Replace("\r\n", "\n"), result.Item2, t.Name, t.Input, result.Item3);
            }));

            var results = await Task.WhenAll(tasks);

            return new FunctionalTestResult { Results = results };
        }

        (string, string, ResultCode) SingleTest(string test, string pathToDll, int tl)
        {
            try
            {
                string[] input = test.Split('\n');
                ProcessStartInfo info = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardInput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    Arguments = pathToDll,
                    //UserName = userName,
                    //Password = pswrd
                };
                Process pr = new Process
                {
                    StartInfo = info
                };

                var outputHandler = new ProcessOutputHandler();
                pr.OutputDataReceived += new DataReceivedEventHandler(outputHandler.StrOutputHandler);
                pr.ErrorDataReceived += new DataReceivedEventHandler(outputHandler.StrErrorHandler);

                var res = pr.Start();

                pr.BeginOutputReadLine();
                pr.BeginErrorReadLine();

                if (!res)
                {
                    return (null, "Couldn't start test process!", ResultCode.IE);
                }

                string addErr = null;

                try
                {
                    StreamWriter sw = pr.StandardInput;
                    foreach (string inp in input)
                    {
                        sw.WriteLine(inp);
                    }
                    sw.Close();
                }
                catch (Exception e)
                {
                    addErr = "Could not input data to process! Error:\n" + e.Message;
                }

                res = pr.WaitForExit(tl);
                if (res)
                {
                    pr.WaitForExit();
                }


                var strOutput = string.IsNullOrEmpty(outputHandler.strOutput) ? outputHandler.strOutput : outputHandler.strOutput.TrimEnd();
                var err = string.IsNullOrEmpty(outputHandler.err) ? outputHandler.err : outputHandler.err.TrimEnd();
                err = string.IsNullOrEmpty(addErr) ? err : addErr + "\nOther errors from sterr:\n" + err;

                if (!res)
                {
                    pr.Kill();
                    return (strOutput, err + "\nTime Limit!", ResultCode.TL);
                }

                return (strOutput, err, ResultCode.OK);
            }
            catch (OutOfMemoryException e)
            {
                return ("", "Memory Limit!\n" + e.Message, ResultCode.ML);
            }
            catch (Exception e)
            {
                return ("", e.Message, ResultCode.IE);
            }
        }
    }
}
