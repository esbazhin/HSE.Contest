using HSE.Contest.ClassLibrary.Communication.Responses;

namespace HSE.Contest.ClassLibrary.TestsClasses
{
    public class TestResult : Response
    {
        public virtual string Commentary { get; set; }
        public virtual double Score { get; set; }
    }  
}
