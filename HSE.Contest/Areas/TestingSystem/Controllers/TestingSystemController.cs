using HSE.Contest.ClassLibrary;
using HSE.Contest.ClassLibrary.DbClasses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.IO;
using System.Linq;
using System.Security.Claims;

namespace HSE.Contest.Areas.TestingSystem.Controllers
{
    public class TestingSystemController : Controller
    {
        protected readonly HSEContestDbContext db;
        protected readonly TestingSystemConfig config;

        public TestingSystemController()
        {
            string pathToConfig = "c:\\config\\config.json";
            config = JsonConvert.DeserializeObject<TestingSystemConfig>(System.IO.File.ReadAllText(pathToConfig));

            DbContextOptionsBuilder<HSEContestDbContext> options = new DbContextOptionsBuilder<HSEContestDbContext>();
            options.UseNpgsql(config.DatabaseInfo.GetConnectionStringFrom(config.TestingSystem));
            db = new HSEContestDbContext(options.Options);
        }


        protected int GetId()
        {
            return int.Parse(User.FindFirst(c => c.Type == ClaimTypes.NameIdentifier).Value);
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
