using HSE.Contest.ClassLibrary;
using HSE.Contest.ClassLibrary.Communication.Requests;
using HSE.Contest.ClassLibrary.Communication.Responses;
using HSE.Contest.ClassLibrary.DbClasses;
using HSE.Contest.ClassLibrary.DbClasses.TestingSystem;
using HSE.Contest.ClassLibrary.TestsClasses.CodeStyleTest;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CodeStyleTesterService.Controllers
{
    [Route("[controller]/[action]")]
    [ApiController]
    public class CodeStyleTesterController : ControllerBase
    {
        private readonly HSEContestDbContext _db;
        public CodeStyleTesterController(HSEContestDbContext db)
        {           
            _db = db;
        }

        [HttpPost]
        public TestResponse TestProject([FromBody] TestRequest request)
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
                    TestingResult result;                   

                    var resp = CheckCodeStyle(compilation.StOutput);
                    resp.OK = true;

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

        private CodeStyleTestResult CheckCodeStyle(string stOutput)
        {
            var lines = stOutput.Split("\r\n");
            var comp = new WarningsComparer();
            var warnings = lines.Where(l => l.Contains("warning")).Select(w => new CodeStyleCommentary(w)).Distinct(comp).ToList();
            var errors = lines.Where(l => l.Contains("error")).Select(w => new CodeStyleCommentary(w)).Distinct(comp).ToList();

            var results = new CodeStyleResults
            {
                Warnings = warnings,
                Errors = errors
            };

            return new CodeStyleTestResult
            {
               Results = results
            };
        }
    }
}
