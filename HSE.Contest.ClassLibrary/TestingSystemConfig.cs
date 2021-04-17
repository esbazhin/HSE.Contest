using System.Collections.Generic;

namespace HSE.Contest.ClassLibrary
{
    public class TestingSystemConfig
    {
        public long MossId { get; set; }
        public DbConfig DatabaseInfo { get; set; }

        public RabbitMQConfig MessageQueueInfo { get; set; }
        public ContainerConfig FrontEnd { get; set; }
        public ContainerConfig TestingSystemWorker { get; set; }
        public ServiceConfig CompilerServicesOrchestrator { get; set; }
        public Dictionary<string, ServiceConfig> Tests { get; set; }
        public Dictionary<string, ImageConfig> CompilerImages { get; set; }
        public Dictionary<string, ImageConfig> FunctionalTesterImages { get; set; }     
    }

    public class ContainerConfig
    {
        public string Host { private get;  set; }
        public int Port { get; set; }
       
        public bool InDocker { get; set; }

        public string GetHost(ContainerConfig service)
        {
            var host = Host;
            if ((service is null ||service.InDocker) && Host == "localhost")
            {
                host = "host.docker.internal";
            }

            return host;
        }

        public string GetHostLinkFrom(ContainerConfig service)
        {            
            return "http://" + GetHost(service) + ":" + Port.ToString();
        }

        
    }

    public class ServiceConfig : ContainerConfig
    {
        public string TestActionLink { get; set; }
        public string TaskActionLink { get; set; }

        public string GetFullTestLinkFrom(ContainerConfig service)
        {
            return GetHostLinkFrom(service) + TestActionLink;
        }

        public string GetFullTaskLinkFrom(ContainerConfig service)
        {
            return GetHostLinkFrom(service) + TaskActionLink;
        }
    }

    public class DbConfig : ContainerConfig
    {
        public string DatabaseName { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }

        public string GetConnectionStringFrom(ContainerConfig service)
        {
            return "Host=" + GetHost(service) + ";Port=" + Port.ToString() + ";Database=" + DatabaseName + ";Username=" + Username + ";Password=" + Password;
        }
    }

    public class RabbitMQConfig : ContainerConfig
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string TestingQueueName { get; set; }
        public string PlagiarismQueueName { get; set; }
    }

    //public class ImagesConfig
    //{
    //    public string NetCore { get; set; }
    //    public string NetFramework { get; set; }
    //}

    public class ImageConfig
    {
        public string Name { get; set; }
        public string TestActionLink { get; set; }
        public string TaskActionLink { get; set; }
    }
}
