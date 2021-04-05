using System;
using System.Collections.Generic;
using System.Linq;

namespace HSE.Contest.ClassLibrary.TestsClasses.CodeStyleTest
{
    public class CodeStyleTestResult : TestResult
    {
        public CodeStyleResults Results { get; set; }
        public override string Commentary
        {
            get
            {
                var warnings = Results.Warnings is null || Results.Warnings.Count == 0 ? "" : "Warnings:\n" + string.Join("\n", Results.Warnings.Select(w => w.ToString()));
                var errors = Results.Errors is null || Results.Errors.Count == 0 ? "" : "Errors:\n" + string.Join("\n", Results.Errors.Select(w => w.ToString()));

                if (warnings != "" && errors != "")
                {
                    warnings += "\n";
                }

                return warnings + errors;
            }
        }

        public override double Score
        {
            get
            {
                int score = Results.Errors.Count == 0 ? 10 - Results.Warnings.Count : 0;
                return score < 0 ? 0 : score;
            }
        }

        public override ResultCode Result
        {
            get
            {
                if (Results.Errors is null || Results.Errors.Count == 0)
                {
                    return Results.Warnings is null || Results.Warnings.Count == 0 ? ResultCode.OK : ResultCode.CS;
                }
                else
                {
                    return ResultCode.RE;
                }
            }
        }
    }

    public class CodeStyleResults
    {
        public List<CodeStyleCommentary> Warnings { get; set; }
        public List<CodeStyleCommentary> Errors { get; set; }
    }

    public class CodeStyleCommentary : IEquatable<CodeStyleCommentary>
    {
        public CodeStyleCommentary(string line)
        {
            var arr = line.Split(":").ToList();
            int warningInd = arr.FindIndex(s => s.Contains("warning") || s.Contains("error"));
            Postition = arr[warningInd - 1].Split("\\").Last();
            ID = arr[warningInd].Trim().Split(" ").Last();
            Message = arr[warningInd + 1].Replace(" [C", "");
        }
        public string Postition { get; set; }
        public string ID { get; set; }
        public string Message { get; set; }

        public bool Equals(CodeStyleCommentary other)
        {
            return ID == other.ID && Postition == other.Postition;
        }

        public override string ToString()
        {
            return "ID: " + ID + " Position: " + Postition + " Message: " + Message;
        }
    }

    public class WarningsComparer : IEqualityComparer<CodeStyleCommentary>
    {
        public bool Equals(CodeStyleCommentary x, CodeStyleCommentary y)
        {
            return x.ID == y.ID && x.Postition == y.Postition;
        }

        public int GetHashCode(CodeStyleCommentary obj)
        {
            return obj is null ? 0 : obj.ID.GetHashCode() ^ obj.Postition.GetHashCode();
        }
    }
}
