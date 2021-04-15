using HSE.Contest.ClassLibrary.DbClasses.Files;
using Newtonsoft.Json;

namespace HSE.Contest.Areas.Administration.ViewModels
{
    public class CodeStyleCRUDViewModel
    {
        public bool IsUpdate { get; set; }
        public CodeStyleFilesViewModel CodeStyleFiles { get; set; }
    }

    public class CodeStyleFilesViewModel
    {
        public string Name { get; set; }
        public string StyleCop { get; set; }
        public string RuleSet { get; set; }
        public int Id { get; set; }

        [JsonConstructor]
        public CodeStyleFilesViewModel()
        { 
        }

        public CodeStyleFilesViewModel(CodeStyleFiles codeStyleFiles)
        {
            if (codeStyleFiles is null)
            {
                Name = "new rules";
                Id = -1;
            }
            else
            {
                Name = codeStyleFiles.Name;
                Id = codeStyleFiles.Id;
                StyleCop = System.Text.Encoding.UTF8.GetString(codeStyleFiles.StyleCopFile);
                RuleSet = System.Text.Encoding.UTF8.GetString(codeStyleFiles.RulesetFile);
            }
        }
    }
}
