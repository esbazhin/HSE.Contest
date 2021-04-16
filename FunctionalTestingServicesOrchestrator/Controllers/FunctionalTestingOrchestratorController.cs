using Docker.DotNet;
using Docker.DotNet.Models;
using HSE.Contest.ClassLibrary;
using HSE.Contest.ClassLibrary.Communication.Requests;
using HSE.Contest.ClassLibrary.Communication.Responses;
using HSE.Contest.ClassLibrary.DbClasses;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace FunctionalTestingServicesOrchestrator.Controllers
{
    [Route("[controller]/[action]")]
    [ApiController]
    public class FunctionalTestingOrchestratorController : ControllerBase
    {
        private readonly TestingSystemConfig _config;
        private readonly HSEContestDbContext _db;
        public FunctionalTestingOrchestratorController(HSEContestDbContext db, TestingSystemConfig config)
        {
            _db = db;
            _config = config;
        }

        [HttpPost]
        public async Task<TestResponse> TestProject([FromBody] TestRequest request)
        {
            var solution = _db.Solutions.Find(request.SolutionId);
            if (solution is null || solution.File is null)
            {
                return new TestResponse
                {
                    OK = false,
                    Message = "no solution found",
                    Result = ResultCode.IE,
                    TestId = request.TestId,
                };
            }

            if (_config.FunctionalTesterImages.ContainsKey(solution.FrameworkType))
            {
                return await TestInNewContainer(_config.FunctionalTesterImages[solution.FrameworkType], request);
            }  
            else
            {
                return new TestResponse
                {
                    OK = false,
                    Message = "no framework found",
                    Result = ResultCode.IE,
                    TestId = request.TestId,
                };
            }
        }

        async Task<TestResponse> TestInNewContainer(ImageConfig imageConfig, TestRequest request)
        {
            try
            {
                DockerClient client = new DockerClientConfiguration().CreateClient();

                var createRes = await client.Containers.CreateContainerAsync(new CreateContainerParameters
                {
                    Image = imageConfig.Name,
                    ExposedPorts = new Dictionary<string, EmptyStruct>
                {
                    {
                        "80/tcp", default(EmptyStruct)
                    }
                },
                    HostConfig = new HostConfig
                    {
                        PublishAllPorts = true,
                        //Memory = 2097152
                    },
                    Env = new List<string> { "db_connection=" + _config.DatabaseInfo.GetConnectionStringFrom(null) }
                });

                var id = createRes.ID;

                var startRes = await client.Containers.StartContainerAsync(id, null);
                if (startRes)
                {
                    var inspectRes = await client.Containers.InspectContainerAsync(id);

                    if (inspectRes.State.Running)
                    {
                        var port = inspectRes.NetworkSettings.Ports["80/tcp"][0].HostPort;

                        var serviceName = inspectRes.Name;

                        var result = await StartTesting(port, imageConfig.TestActionLink, request);

                        await client.Containers.KillContainerAsync(id, new ContainerKillParameters());

                        await client.Containers.RemoveContainerAsync(id, new ContainerRemoveParameters());

                        return result;
                    }
                }

                return new TestResponse
                {
                    OK = false,
                    Message = "Couldn't start new functional testing container!",
                    Result = ResultCode.IE,
                    TestId = request.TestId,
                };
            }
            catch (Exception e)
            {
                return new TestResponse
                {
                    OK = false,
                    Message = "Error occured: " + e.Message + (e.InnerException is null ? "" : " Inner: " + e.InnerException.Message),
                    Result = ResultCode.IE,
                    TestId = request.TestId,
                };
            }
        }

        async Task<bool> CheckIfAlive(string port)
        {
            using var httpClient = new HttpClient();
            var link = "http://host.docker.internal:" + port + "/health";
            HttpResponseMessage response = await httpClient.GetAsync(link);

            if (response.IsSuccessStatusCode)
            {
                string apiResponse = await response.Content.ReadAsStringAsync();
                HealthResponse obj = JsonConvert.DeserializeObject<HealthResponse>(apiResponse);
                return obj.Status == "Healthy";
            }
            else
            {
                return false;
            }
        }

        async Task<TestResponse> StartTesting(string port, string link, TestRequest request)
        {
            var isAlive = await CheckIfAlive(port);

            if (isAlive)
            {
                using var httpClient = new HttpClient();
                using var form = JsonContent.Create(request);
                //var url = "http://host.docker.internal:" + port + "/FunctionalTester/TestProject";
                var url = "http://host.docker.internal:" + port + link;
                HttpResponseMessage response = await httpClient.PostAsync(url, form);
                string apiResponse = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {
                    return JsonConvert.DeserializeObject<TestResponse>(apiResponse);
                }
                else
                {
                    return new TestResponse
                    {
                        OK = false,
                        Message = "Created testing container replied with " + response.StatusCode,
                        Result = ResultCode.IE,
                        TestId = request.TestId,
                    };
                }
            }
            else
            {
                return new TestResponse
                {
                    OK = false,
                    Message = "Created testing container Is Dead!",
                    Result = ResultCode.IE,
                    TestId = request.TestId,
                };
            }
        }
    }
}
