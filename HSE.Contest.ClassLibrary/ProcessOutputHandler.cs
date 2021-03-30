using System.Diagnostics;

namespace HSE.Contest.ClassLibrary
{
    public class ProcessOutputHandler
    {
        public string strOutput;
        public string err;

        public ProcessOutputHandler()
        {
            strOutput = string.Empty;
            err = string.Empty;
        }

        public void StrOutputHandler(object sendingProcess,
             DataReceivedEventArgs outLine)
        {
            if (!string.IsNullOrEmpty(outLine.Data))
            {
                if (string.IsNullOrEmpty(strOutput))
                {
                    strOutput = outLine.Data;
                }
                else
                {
                    strOutput += "\n" + outLine.Data;
                }
            }
        }

        public void StrErrorHandler(object sendingProcess,
            DataReceivedEventArgs outLine)
        {
            if (!string.IsNullOrEmpty(outLine.Data))
            {
                if (string.IsNullOrEmpty(err))
                {
                    err = outLine.Data;
                }
                else
                {
                    err += "\n" + outLine.Data;
                }
            }
        }
    }
}
