namespace HSE.Contest.ClassLibrary
{
    public class Response
    {
        public bool OK { get; set; }
        public string Message { get; set; }
        public virtual ResultCode Result { get; set; }
    }

    public class TestResponse : Response
    {
        public double Score { get; set; }
        public int TestId { get; set; }
        public int TestResultId { get; set; }
    }

    public class TestingSystemResponse : Response
    {
        public int SolutionId { get; set; }
        public double Score { get; set; }
        public TestResponse[] Responses { get; set; }
    }

    public enum ResultCode : int
    {
        NT, //NotTested
        OK,
        CS, //CodeStyleError
        WA,
        RE,
        TL,
        ML,
        CE,
        IE, //InternalError
        PL, //Plagiat                       
    }

    public class HealthResponse
    {
        public string Status { get; set; }
    }
}
