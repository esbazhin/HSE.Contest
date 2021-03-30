using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HSE.Contest.ClassLibrary
{
    public class TestResult : Response
    {
        public virtual string Commentary { get; set; }
        public virtual double Score { get; set; }
    }

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

    public class FunctionalTestResult : TestResult
    {
        public SingleFunctTestResult[] Results { get; set; }
        public override string Commentary
        {
            get
            {
                if (Results != null)
                {
                    var errors = Results.Where(r => !r.Passed).ToArray();

                    string comment = "";
                    foreach (var error in errors)
                    {
                        string cur = "Произошла ошибка в тесте " + error.Name + ":\n";
                        cur += $"Expected: {error.Expected}, Actual: {error.Actual}\n";
                        cur += $"Ошибки: {error.Errors}\n";
                        cur += $"Вердикт: {error.Result}\n\n";
                        comment += cur;
                    }

                    comment += $"Пройдено {Results.Length - errors.Length} тестов из {Results.Length}";
                    return comment;
                }
                else
                {
                    return "no results";
                }
            }
        }
        public override double Score
        {
            get
            {
                var errors = Results?.Where(r => !r.Passed).ToArray();
                return errors is null || errors.Length == 0 ? 10 : 0;
            }
        }
        public override ResultCode Result
        {
            get
            {
                var errors = Results?.Where(r => !r.Passed).ToArray();
                return errors is null || errors.Length == 0 ? ResultCode.OK : errors[0].Result;
            }
        }
    }
    public class SingleFunctTestResult
    {
        public SingleFunctTestResult(string expected, string actual, string errors, string name, string input, ResultCode result)
        {
            Expected = expected;
            Actual = actual;
            Errors = errors;
            Name = name;
            Input = input;
            Result = result;

            if (Result == ResultCode.OK)
            {
                if (!string.IsNullOrEmpty(Errors))
                {
                    Result = ResultCode.RE;
                    Passed = false;

                    if (Errors.Contains("Out of memory.") || Errors.Contains("OutOfMemoryException"))
                    {
                        Result = ResultCode.ML;
                    }
                }
                else if (Expected != Actual)
                {
                    Result = ResultCode.WA;
                    Passed = false;
                }
                else
                {
                    Passed = true;
                }
            }
            else
            {
                Passed = false;
            }
        }

        public string Name { get; set; }
        public string Input { get; set; }
        public string Expected { get; set; }
        public string Actual { get; set; }
        public string Errors { get; set; }
        public virtual ResultCode Result { get; set; }
        public bool Passed { get; set; }        
    }

    public class ReflectionTestResult : TestResult
    {
        public SingleReflectionTestResult[] Results { get; set; }
        public override string Commentary
        {
            get
            {
                if (Results != null)
                {
                    var errors = Results.Where(r => !r.Passed).ToArray();
                    return string.Join("\n", errors.Select(r => r.Message));
                }
                else
                {
                    return "no results";
                }
            }
        }


        public override double Score
        {
            get
            {
                var errors = Results?.Where(r => !r.Passed).ToArray();
                return errors is null || errors.Length == 0 ? 10 : 0;
            }
        }

        public override ResultCode Result
        {
            get
            {
                var errors = Results?.Where(r => !r.Passed).ToArray();
                return errors is null || errors.Length == 0 ? ResultCode.OK : ResultCode.WA;
            }
        }
    }
    public class SingleReflectionTestResult
    {
        [JsonConstructor]
        public SingleReflectionTestResult() { }
        public string Message { get; set; }
        public bool Passed { get; set; }

        public SingleReflectionTestResult(string expected, string actual, string msg, Func<string, string, bool> comparator = null)
        {
            expected = expected.Replace(" ", "");
            actual = actual.Replace(" ", "");

            Passed = expected == actual;
            if (comparator != null)
            {
                Passed = comparator(expected, actual);
            }

            if (!Passed)
            {
                Message = $"{msg} Expected: {expected}, Actual: {actual}";
            }
        }

        public SingleReflectionTestResult(bool expected, bool actual, string msg)
        {
            Passed = expected == actual;

            if (!Passed)
            {
                Message = $"{msg} Expected: {expected}, Actual: {actual}";
            }
        }

        public SingleReflectionTestResult(string msg)
        {
            Passed = false;
            Message = msg;
        }
    }
}
