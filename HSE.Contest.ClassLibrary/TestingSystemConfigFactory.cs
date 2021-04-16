using Newtonsoft.Json;

namespace HSE.Contest.ClassLibrary
{
    public class TestingSystemConfigFactory
    {
        public TestingSystemConfig CreateApplicationConfig()
        {
            string pathToConfig = "c:\\config\\config.json";
            var config = JsonConvert.DeserializeObject<TestingSystemConfig>(System.IO.File.ReadAllText(pathToConfig));
           
            return config;
        }
    }
}
