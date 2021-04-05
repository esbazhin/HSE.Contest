using Newtonsoft.Json;
using System;
using System.Linq;

namespace HSE.Contest.ClassLibrary.TestsClasses.ReflectionTest
{
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
