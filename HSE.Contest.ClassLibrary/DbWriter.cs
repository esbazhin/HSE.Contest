using HSE.Contest.ClassLibrary.Communication.Responses;
using HSE.Contest.ClassLibrary.DbClasses;
using HSE.Contest.ClassLibrary.DbClasses.TestingSystem;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace HSE.Contest.ClassLibrary
{
    public static class DbWriter
    {
        public static TestResponse WriteToDb(HSEContestDbContext _db, TestingResult res, bool recheck)
        {
            bool ok = false;
            if (recheck)
            {
                var existing = _db.TestingResults.FirstOrDefault(r => r.SolutionId == res.SolutionId && r.TestId == res.TestId);

                if (existing != null)
                {
                    existing.Commentary = res.Commentary;
                    existing.ResultCode = res.ResultCode;
                    existing.Score = res.Score;
                    existing.TestData = res.TestData;

                    _db.SaveChanges();

                    ok = true;
                }
            }
            else
            {
                var x = _db.TestingResults.Add(res);
                var beforeState = x.State;
                int r = _db.SaveChanges();
                var afterState = x.State;

                ok = beforeState == EntityState.Added && afterState == EntityState.Unchanged && r == 1;
            }

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
    }
}
