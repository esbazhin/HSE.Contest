using System.Collections.Generic;

namespace HSE.Contest.ClassLibrary.TestsClasses.FunctionalTest
{
    public class FunctionalTestData
    {
        public List<FunctionalTest> FunctionalTests { get; set; }
    }

    public class FunctionalTest
    {
        public string Name { get; set; }
        public string Input { get; set; }
        public string Output { get; set; }
        public int Key { get; set; }
        public int TimeLimit { get; set; }
    }
}
