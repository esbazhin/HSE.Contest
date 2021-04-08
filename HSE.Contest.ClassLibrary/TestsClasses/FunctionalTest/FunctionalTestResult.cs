using System.Linq;

namespace HSE.Contest.ClassLibrary.TestsClasses.FunctionalTest
{
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
        public  string ResultString { get { return Result.ToString(); } }
        public bool Passed { get; set; }
    }
}
