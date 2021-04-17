using AngleSharp;
using AngleSharp.Dom;
using HSE.Contest.ClassLibrary;
using HSE.Contest.ClassLibrary.Communication.Requests;
using HSE.Contest.ClassLibrary.DbClasses;
using HSE.Contest.ClassLibrary.DbClasses.TestingSystem;
using HSE.Contest.ClassLibrary.RabbitMQ;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace PlagiarismCheckerWorkerService
{
    public class PlagiarismCheckerWorker : BackgroundService
    {
        private readonly IBus _busControl;
        private readonly TestingSystemConfig _config;
        private readonly HSEContestDbContext _db;

        public PlagiarismCheckerWorker()
        {
            string pathToConfig = "c:\\config\\config.json";
            _config = JsonConvert.DeserializeObject<TestingSystemConfig>(File.ReadAllText(pathToConfig));

            DbContextOptionsBuilder<HSEContestDbContext> options = new DbContextOptionsBuilder<HSEContestDbContext>();
            options.UseNpgsql(_config.DatabaseInfo.GetConnectionStringFrom(_config.FrontEnd));
            _db = new HSEContestDbContext(options.Options);

            _busControl = RabbitHutch.CreateBus(_config.MessageQueueInfo, _config.TestingSystemWorker);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await _busControl.ReceiveAsync<PlagiarismCheckRequest>(_config.MessageQueueInfo.PlagiarismQueueName, x => CheckSolutions(x).Start());
        }

        public async Task CheckSolutions(PlagiarismCheckRequest checkRequest)
        {
            try
            {
                int taskId = checkRequest.TaskId;

                var task = _db.StudentTasks.Find(taskId);

                while (DateTime.Now < task.To)
                {
                    await Task.Delay(5 * 60 * 1000);
                    _db.Entry(task).Reload();
                }

                var check = _db.PlagiarismChecks.Find(taskId);

                if (check.Settings.MakeCheck)
                {
                    var sourceFileList = new List<string>();
                    var studentSolutions = _db.StudentResults.Where(r => r.TaskId == taskId).Select(r => r.Solution).ToList();

                    var taskDir = new DirectoryInfo("C:\\tasks\\" + taskId.ToString());

                    if (taskDir.Exists)
                    {
                        taskDir.Delete(true);
                    }

                    taskDir.Create();

                    var saveDir = new DirectoryInfo("C:\\saving");

                    if (!saveDir.Exists)
                    {
                        saveDir.Create();
                    }

                    foreach (var sol in studentSolutions)
                    {
                        var curSaveDir = new DirectoryInfo(saveDir.FullName + "\\" + sol.Id.ToString());

                        if (curSaveDir.Exists)
                        {
                            curSaveDir.Delete(true);
                        }

                        curSaveDir.Create();

                        string fullPath = curSaveDir.FullName + "\\" + sol.File.Name;

                        File.WriteAllBytes(fullPath, sol.File.Content);
                        ZipFile.ExtractToDirectory(fullPath, curSaveDir.FullName, true);

                        var curSolDir = new DirectoryInfo(taskDir.FullName + "\\" + sol.Id.ToString());
                        if (curSolDir.Exists)
                        {
                            curSolDir.Delete(true);
                        }
                        curSolDir.Create();

                        var solFiles = curSaveDir.GetFiles("*.cs", SearchOption.AllDirectories).Where(p => !p.FullName.Contains("\\obj\\")).ToList();

                        foreach (var file in solFiles)
                        {
                            file.CopyTo(curSolDir.FullName + "//" + file.Name);
                        }

                        curSaveDir.Delete(true);
                    }

                    var request = new MossRequest
                    {
                        UserId = _config.MossId,
                        IsDirectoryMode = true,
                        IsBetaRequest = false,
                        Language = check.Settings.Language,
                        MaxMatches = check.Settings.MaxMatches
                    };

                    //request.BaseFile.AddRange(this.BaseFileList);                    

                    foreach (var solDir in taskDir.GetDirectories())
                    {
                        var curSolFiles = solDir.GetFiles("*.cs", SearchOption.AllDirectories).Select(f => f.FullName).Where(p => !p.Contains("\\obj\\")).ToList();
                        sourceFileList.AddRange(curSolFiles);
                    }

                    request.Files.AddRange(sourceFileList);

                    bool res = request.SendRequest(out string response);

                    check.Link = response;

                    if (res)
                    {
                        string html = new WebClient().DownloadString(response);

                        var config = Configuration.Default;

                        var context = BrowsingContext.New(config);

                        var document = context.OpenAsync(req => req.Content(html)).Result;

                        var allLines = document.All.Where(m => m.LocalName == "tr" && !m.Children.Any(c => c.LocalName == "th"));

                        foreach (var line in allLines)
                        {
                            var file1 = ParseUserAndPercent(line.Children[0]);
                            var file2 = ParseUserAndPercent(line.Children[1]);
                            var lMatched = int.Parse(line.Children[2].InnerHtml);

                            var solId1 = file1.Item1;
                            var percent1 = file1.Item2;

                            var solId2 = file2.Item1;
                            var percent2 = file2.Item2;

                            if (percent1 > check.Settings.MinPercent || percent2 > check.Settings.MinPercent)
                            {
                                var sol1 = _db.Solutions.Find(solId1);
                                var sol2 = _db.Solutions.Find(solId2);

                                var newResult = new PlagiarismResult
                                {
                                    SolutionId1 = sol1.Id,
                                    SolutionId2 = sol2.Id,
                                    Percent1 = percent1,
                                    Percent2 = percent2,
                                    LinesMatched = lMatched,
                                    TaskId = taskId,
                                };

                                _db.PlagiarismResults.Add(newResult);

                                sol1.PlagiarismDetected = true;
                                sol2.PlagiarismDetected = true;
                            }
                        }
                    }
                    _db.SaveChanges();
                }
            }
            catch
            {
                return;
            }
        }

        static (int, double) ParseUserAndPercent(IElement line)
        {
            var arr1 = line.Children[0].InnerHtml.Split(" ");
            var arr2 = arr1[0].Split("/");
            var solId = arr2[arr2.Length - 2];
            var percent = arr1[1].Replace("(", "").Replace(")", "").Replace("%", "");

            return (int.Parse(solId), int.Parse(percent) / 100.0);
        }
    }
}
