using HSE.Contest.ClassLibrary;
using HSE.Contest.ClassLibrary.DbClasses;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Linq;

namespace HSE.Contest.Areas.TestingSystem.Controllers
{
    public class TestingSystemController : Controller
    {
        protected readonly HSEContestDbContext _db;
        protected readonly TestingSystemConfig _config;
        protected readonly string _pathToConfigDir;

        public TestingSystemController(HSEContestDbContext db, TestingSystemConfig config)
        {
            _pathToConfigDir = "c:\\config";
            _db = db;
            _config = config;
        }

        protected string FindProjectFile(DirectoryInfo dir)
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
    }
}
