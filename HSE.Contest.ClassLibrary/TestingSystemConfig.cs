using System.Collections.Generic;

namespace HSE.Contest.ClassLibrary
{
    public class TestingSystemConfig
    {
        public DbConfig DatabaseInfo { get; set; }
        public ServiceConfig TestingSystem { get; set; }
        public ServiceConfig CompilerServicesOrchestrator { get; set; }
        public Dictionary<string, ServiceConfig> Tests { get; set; }
        //public ServiceConfig FunctionalTestingServicesOrchestrator { get; set; }
        //public ServiceConfig CodeStyleTesterService { get; set; }
        //public ServiceConfig ReflectionTesterService { get; set; }
        //public ServiceConfig FileManagerService { get; set; }

        public Dictionary<string, ImageConfig> CompilerImages { get; set; }
        public Dictionary<string, ImageConfig> FunctionalTesterImages { get; set; }
        //public ImagesConfig CompilerImages { get; set; }
        //public ImagesConfig FunctionalTesterImages { get; set; }       
    }

    public class ServiceConfig
    {
        public string Host { private get;  set; }
        public string Port { get; set; }
        public string ActionLink { get; set; }
        public bool InDocker { get; set; }

        public string GetHost(ServiceConfig service)
        {
            var host = Host;
            if ((service is null ||service.InDocker) && Host == "localhost")
            {
                host = "host.docker.internal";
            }

            return host;
        }

        public string GetHostLinkFrom(ServiceConfig service)
        {            
            return "http://" + GetHost(service) + ":" + Port;
        }

        public string GetFullLinkFrom(ServiceConfig service)
        {
            return GetHostLinkFrom(service) + ActionLink;
        }
    }

    public class DbConfig : ServiceConfig
    {
        public string DatabaseName { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }

        public string GetConnectionStringFrom(ServiceConfig service)
        {
            return "Host=" + GetHost(service) + ";Port=" + Port + ";Database=" + DatabaseName + ";Username=" + Username + ";Password=" + Password;
        }
    }

    //public class ImagesConfig
    //{
    //    public string NetCore { get; set; }
    //    public string NetFramework { get; set; }
    //}

    public class ImageConfig
    {
        public string Name { get; set; }
        public string ActionLink { get; set; }
    }
}
