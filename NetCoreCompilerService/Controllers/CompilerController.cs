using HSE.Contest.ClassLibrary;
using HSE.Contest.ClassLibrary.Communication.Requests;
using HSE.Contest.ClassLibrary.Communication.Responses;
using HSE.Contest.ClassLibrary.DbClasses;
using HSE.Contest.ClassLibrary.DbClasses.Files;
using HSE.Contest.ClassLibrary.DbClasses.TestingSystem;
using HSE.Contest.ClassLibrary.TestsClasses.CodeStyleTest;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace NetCoreCompilerService.Controllers
{
    [Route("[controller]/[action]")]
    [ApiController]
    public class CompilerController : ControllerBase
    {
        string addFilesPath = "/home/NetCoreFiles";
        string rulesetFileName = "rules.ruleset";
        string stylecopFileName = "stylecop.json";

        HSEContestDbContext db;

        public CompilerController(HSEContestDbContext context)
        {
            db = context;
        }

        //[HttpPost]
        //public async Task<CompilerResponse> CompileProjectDebug([FromForm] IFormFile file, [FromForm] int taskId, [FromForm] string isNetCore)
        //{
        //    var dataBytes = await file.GetBytes();
        //    var req = new OldCompilerRequest
        //    {
        //        TaskId = taskId,
        //        File = dataBytes,
        //        IsNetCore = isNetCore == "true",
        //        ShouldUpdateFiles = false
        //    };

        //    return await CompileProject(req);
        //}

        [HttpPost]
        public TestResponse CompileProject([FromBody] TestRequest request)
        {
            try
            {
                var solution = db.Solutions.Find(request.SolutionId);

                if (solution is null)
                {
                    return new TestResponse
                    {
                        OK = false,
                        Message = "no solution found",
                        Result = ResultCode.IE,
                        TestId = request.TestId,
                    };
                }

                if (solution.File != null)
                {
                    string dirPath = "/home/solution";

                    if (Directory.Exists(dirPath))
                    {
                        Directory.Delete(dirPath, true);
                    }

                    var dir = Directory.CreateDirectory(dirPath);
                    string fullPath = dirPath + "/" + solution.File.Name;

                    // сохраняем файл в папку
                    System.IO.File.WriteAllBytes(fullPath, solution.File.Content);
                    ZipFile.ExtractToDirectory(fullPath, dirPath, true);

                    var pathToProj = FindProjectFile(dir);

                    CompilationResult result;

                    if (pathToProj == null)
                    {
                        result = new CompilationResult
                        {
                            SolutionId = solution.Id,
                            StError = "No project file found!",
                            ResultCode = ResultCode.CE,
                            FileId = null,
                        };

                        return WriteToDb(result, solution);
                    }

                    string pathToComp = dirPath + "/build";
                    if (Directory.Exists(pathToComp))
                    {
                        Directory.Delete(pathToComp, true);
                    }

                    bool didUpdate = UpdateRulesFiles(request.TestId);

                    var output = Compile(pathToProj, pathToComp);

                    if (output.Item1.Split("\n").FirstOrDefault(l => l.Contains("Error(s)")).Replace("Error(s)", "").Trim() != "0" || !Directory.Exists(pathToComp))
                    {
                        var lines = output.Item1.Split("\r\n");
                        var comp = new WarningsComparer();
                        var errors = lines.Where(l => l.Contains("error")).Select(w => new CodeStyleCommentary(w)).Distinct(comp).ToList();
                        var res = new CodeStyleTestResult
                        {
                            Results = new CodeStyleResults
                            {
                                Errors = errors
                            }
                        };

                        result = new CompilationResult
                        {
                            SolutionId = solution.Id,
                            StOutput = output.Item1,
                            StError = output.Item2,
                            ResultCode = ResultCode.CE,
                            FileId = null,
                            DidUpdateRules = didUpdate,
                        };

                        return WriteToDb(result, solution);
                    }
                    else
                    {
                        string compFileName = "compilation.zip";
                        string compilationPath = dirPath + "/" + compFileName;
                        ZipFile.CreateFromDirectory(pathToComp, compilationPath);

                        var dataBytes = System.IO.File.ReadAllBytes(compilationPath);

                        int fileId = db.UploadFile(compFileName, dataBytes);

                        result = new CompilationResult
                        {
                            SolutionId = solution.Id,
                            StOutput = output.Item1,
                            StError = output.Item2,
                            ResultCode = ResultCode.OK,
                            FileId = fileId,
                            DidUpdateRules = didUpdate,
                        };

                        return WriteToDb(result, solution);
                    }
                }
                else
                {
                    return new TestResponse
                    {
                        OK = false,
                        Message = "no solution file found",
                        Result = ResultCode.IE,
                        TestId = request.TestId,
                    };
                }
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

        TestResponse WriteToDb(CompilationResult res, Solution sol)
        {
            var x = db.CompilationResults.Add(res);
            var beforeState = x.State;
            int r = db.SaveChanges();
            var afterState = x.State;

            bool ok = false;
            if (beforeState == EntityState.Added && afterState == EntityState.Unchanged && r == 1)
            {
                sol.CompilationId = res.SolutionId;
                var x1 = db.Solutions.Update(sol);
                beforeState = x1.State;
                r = db.SaveChanges();
                afterState = x1.State;

                ok = beforeState == EntityState.Modified && afterState == EntityState.Unchanged && r == 1;
            }

            TestResponse response;

            if (ok)
            {
                response = new TestResponse
                {
                    OK = true,
                    Message = "success",
                    Result = res.ResultCode,
                };
            }
            else
            {
                response = new TestResponse
                {
                    OK = false,
                    Message = "can't write result to db",
                    Result = ResultCode.IE,
                };
            }

            return response;
        }

        bool UpdateRulesFiles(int testId)
        {
            var task = db.TaskTests.Find(testId);

            if(task is null)
            {
                return false; 
            }

            var data = JsonConvert.DeserializeObject<CodeStyleTestData>(task.TestData);

            if(!data.ShouldUpdate)
            {
                return true;
            }

            CodeStyleFiles files = db.CodeStyleFiles.Find(data.CodeStyleFilesId);

            if(files is null)
            {
                return false;
            }

            string rulesetFilePath = addFilesPath + "/" + rulesetFileName;
            string stylecopFilePath = addFilesPath + "/" + stylecopFileName;

            System.IO.File.WriteAllBytes(rulesetFilePath, files.RulesetFile);
            System.IO.File.WriteAllBytes(stylecopFilePath, files.StyleCopFile);

            return true;
        }

        (string, string) Compile(string pathToProj, string pathToComp)
        {
            var projFolder = (new FileInfo(pathToProj)).Directory;
            var addFilesFolder = new DirectoryInfo(addFilesPath);

            CloneDirectory(addFilesFolder, projFolder);

            ProcessStartInfo info = new ProcessStartInfo
            {
                FileName = "dotnet",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                Arguments = "build " + pathToProj + " -o " + pathToComp
                //Arguments = "--version"
                //UserName = userName,
                //Password = pswrd
            };
            Process pr = new Process
            {
                StartInfo = info
            };
            pr.Start();

            //StreamWriter sw = pr.StandardInput;
            //foreach (string inp in input)
            //    sw.WriteLine(inp);
            //sw.Close();

            string strOutput = pr.StandardOutput.ReadToEnd();
            strOutput = strOutput.TrimEnd();
            string err = pr.StandardError.ReadToEnd();
            err = err.TrimEnd();
            pr.WaitForExit();

            return (strOutput, err);
        }

        void CloneDirectory(DirectoryInfo root, DirectoryInfo dest)
        {
            foreach (var directory in root.GetDirectories())
            {
                string dirName = directory.Name;
                var newDir = new DirectoryInfo(Path.Combine(dest.FullName, dirName));
                if (!newDir.Exists)
                {
                    newDir.Create();
                }
                CloneDirectory(directory, newDir);
            }

            foreach (var file in root.GetFiles())
            {
                file.CopyTo(Path.Combine(dest.FullName, file.Name));
            }
        }

        string FindProjectFile(DirectoryInfo dir)
        {
            var f = dir.GetFiles().FirstOrDefault(f => f.Name.EndsWith(".csproj"));
            if (f == null)
            {
                string res = null;
                foreach (var subDir in dir.GetDirectories())
                {
                    res = FindProjectFile(subDir);
                    if (res != null)
                    {
                        break;
                    }
                }
                return res;
            }
            return f.FullName;
        }

        [HttpPost]
        public CompilationResponse CompileTaskProject([FromBody] CompilationRequest request)
        {
            try
            {               
                if (request.File != null)
                {
                    string dirPath = "/home/solution";

                    if (Directory.Exists(dirPath))
                    {
                        Directory.Delete(dirPath, true);
                    }

                    var dir = Directory.CreateDirectory(dirPath);
                    string fullPath = dirPath + "/" + "taskCompilation.zip";

                    // сохраняем файл в папку
                    System.IO.File.WriteAllBytes(fullPath, request.File);
                    ZipFile.ExtractToDirectory(fullPath, dirPath, true);

                    var pathToProj = FindProjectFile(dir);

                    CompilationResponse result;

                    if (pathToProj == null)
                    {
                        result = new CompilationResponse
                        {                           
                            Message = "No project file found!",
                            OK = false
                        };

                        return result;
                    }

                    string pathToComp = dirPath + "/build";
                    if (Directory.Exists(pathToComp))
                    {
                        Directory.Delete(pathToComp, true);
                    }                    

                    var output = Compile(pathToProj, pathToComp);

                    if (output.Item1.Split("\n").FirstOrDefault(l => l.Contains("Error(s)")).Replace("Error(s)", "").Trim() != "0" || !Directory.Exists(pathToComp))
                    {
                        var lines = output.Item1.Split("\r\n");
                        var comp = new WarningsComparer();
                        var errors = lines.Where(l => l.Contains("error")).Select(w => new CodeStyleCommentary(w)).Distinct(comp).ToList();
                        var res = new CodeStyleTestResult
                        {
                            Results = new CodeStyleResults
                            {
                                Errors = errors
                            }
                        };

                        result = new CompilationResponse
                        {
                            Message = output.Item2,
                            OK = false
                        };

                        return result;
                    }
                    else
                    {
                        string compFileName = "compilation.zip";
                        string compilationPath = dirPath + "/" + compFileName;
                        ZipFile.CreateFromDirectory(pathToComp, compilationPath);

                        var dataBytes = System.IO.File.ReadAllBytes(compilationPath);

                        result = new CompilationResponse
                        {
                            File = dataBytes,
                            OK = true,
                            Message = "succes"
                        };

                        return result;
                    }
                }
                else
                {
                    return new CompilationResponse
                    {
                        OK = false,
                        Message = "no file found",
                    };
                }
            }
            catch (Exception e)
            {
                return new CompilationResponse
                {
                    OK = false,
                    Message = "Error occured: " + e.Message + (e.InnerException is null ? "" : " Inner: " + e.InnerException.Message),
                };
            }
        }
    }
}
