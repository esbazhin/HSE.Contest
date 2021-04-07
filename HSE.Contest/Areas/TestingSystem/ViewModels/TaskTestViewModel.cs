using HSE.Contest.ClassLibrary;
using HSE.Contest.ClassLibrary.DbClasses.Files;
using HSE.Contest.ClassLibrary.DbClasses.TestingSystem;
using HSE.Contest.ClassLibrary.TestsClasses.CodeStyleTest;
using HSE.Contest.ClassLibrary.TestsClasses.FunctionalTest;
using HSE.Contest.ClassLibrary.TestsClasses.ReflectionTest;
using Newtonsoft.Json;

namespace HSE.Contest.Areas.TestingSystem.ViewModels
{
    public class TaskTestViewModel
    {
        public int Id { get; set; }
        public int TaskId { get; set; }
        public string Type { get; set; }
        public string Name { get; set; }
        public string TaskName { get; set; }
        public string GroupName { get; set; }
        public object Data { get; set; }
        public string[] FrameworkTypes { get; set; }
        public bool IsRaw { get; set; }
        public CodeStyleFilesViewModel[] CodeStyleFiles { get; set; }

        [JsonConstructor]
        public TaskTestViewModel()
        {
        }

        public TaskTestViewModel(TaskTest t, StudentTask task, string[] frameworkTypes, CodeStyleFilesViewModel[] codeStyleFiles)
        {
            CodeStyleFiles = codeStyleFiles;
            FrameworkTypes = frameworkTypes;
            TaskName = task.Name;
            GroupName = task.Group.Name;
            Id = t.Id;
            TaskId = t.TaskId;
            Type = t.TestType;

            Name = TestsNamesConverter.ConvertTypeToName(t.TestType);

            if (TestsNamesConverter.IsValidType(Type))
            {
                object obj = null;
                if (t.TestData != null)
                {
                    switch(Type)
                    {
                        case "reflectionTest":
                            obj = JsonConvert.DeserializeObject<ReflectionTestData>(t.TestData);
                            break;

                        case "functionalTest":
                            obj = JsonConvert.DeserializeObject<FunctionalTestData>(t.TestData);
                            break;

                        case "codeStyleTest":
                            obj = JsonConvert.DeserializeObject<CodeStyleTestData>(t.TestData);
                            break;
                    }
                }

                Data = obj;
                IsRaw = false;
            }
            else
            {
                Data = t.TestData;
                IsRaw = true;
            }
        }
    }

    public class CodeStyleFilesViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }

        [JsonConstructor]
        public CodeStyleFilesViewModel()
        { 
        }

        public CodeStyleFilesViewModel(CodeStyleFiles f)
        {
            Id = f.Id;
            Name = f.Name;
        }
    }
}
