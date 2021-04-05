namespace HSE.Contest.ClassLibrary.Communication.Responses
{
   public class TestingSystemResponse : Response
    {
        public int SolutionId { get; set; }
        public double Score { get; set; }
        public TestResponse[] Responses { get; set; }
    }
}
