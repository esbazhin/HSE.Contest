using HSE.Contest.ClassLibrary;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Threading.Tasks;

namespace FileManagerService.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class FileManagerController : ControllerBase
    {
        string dir = "/home/files";
        string codestyleFiles = "/codestyle/";
        string rulesetFile = "rules.ruleset";
        string stylecopFile = "stylecop.json";

        [HttpGet]
        public CodeStyleFiles GetCodeStyleFiles()
        {
            return new CodeStyleFiles
            {
                RulesetFile = System.IO.File.ReadAllBytes(dir + codestyleFiles + rulesetFile),
                StyleCopFile = System.IO.File.ReadAllBytes(dir + codestyleFiles + stylecopFile),
            };
        }

        [HttpPost]
        public async Task<Response> UpdateCodeStyleFiles([FromForm] IFormFile stylecop, [FromForm] IFormFile ruleset)
        {
            if (stylecop is null || ruleset is null)
            {
                return new Response
                {
                    OK = false,
                    Message = "codestyle files are null!"
                };
            }

            var rulesetPath = dir + codestyleFiles + rulesetFile;
            var stylecopPath = dir + codestyleFiles + stylecopFile;

            // сохраняем файл в папку
            using (var fileStream = new FileStream(rulesetPath, FileMode.Create))
            {
                await ruleset.CopyToAsync(fileStream);
            }

            using (var fileStream = new FileStream(stylecopPath, FileMode.Create))
            {
                await stylecop.CopyToAsync(fileStream);
            }

            return new Response
            {
                OK = true,
                Message = "codestyle files are updated!"
            };
        }
    }
}
