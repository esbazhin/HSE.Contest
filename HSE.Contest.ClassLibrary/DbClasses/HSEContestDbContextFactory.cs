using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace HSE.Contest.ClassLibrary.DbClasses
{
    public class HSEContestDbContextFactory
    {
        public HSEContestDbContext CreateApplicationDbContext()
        {
            string pathToConfig = "c:\\config\\config.json";
            var config = JsonConvert.DeserializeObject<TestingSystemConfig>(System.IO.File.ReadAllText(pathToConfig));

            DbContextOptionsBuilder<HSEContestDbContext> options = new DbContextOptionsBuilder<HSEContestDbContext>();
            options.UseNpgsql(config.DatabaseInfo.GetConnectionStringFrom(config.FrontEnd));
            var db = new HSEContestDbContext(options.Options);

            return db;
        }
    }
}
