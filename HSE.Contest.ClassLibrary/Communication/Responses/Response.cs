namespace HSE.Contest.ClassLibrary.Communication.Responses
{
    public class Response
    {
        public bool OK { get; set; }
        public string Message { get; set; }
        public virtual ResultCode Result { get; set; }
    }
}
