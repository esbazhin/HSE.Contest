using HSE.Contest.ClassLibrary;
using HSE.Contest.ClassLibrary.DbClasses.Administration;
using HSE.Contest.ClassLibrary.DbClasses.TestingSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace HSE.Contest.Areas.TestingSystem.ViewModels
{
    public class TaskCRUDViewModel
    {
        public int Id { get; set; }
        public TaskViewModelNew Task { get; set; }
        public bool IsUpdate { get; set; }
        public List<TestType> TestTypes { get; set; }
        public List<GroupViewModel> Groups { get; set; }
    }

    public class TaskViewModelNew
    {
        [JsonConstructor]
        public TaskViewModelNew()
        { }

        public int Id { get; set; }
        public string Name { get; set; }
        public string Text { get; set; }
        public int GroupId { get; set; }
        public List<DateTime> Time { get; set; }
        public bool IsContest { get; set; }
        public int AttemptsNumber { get; set; }
        public List<TestViewModel> Tests { get; set; }

        public TaskViewModelNew(int groupId)
        {
            Name = "";
            Text = "";
            GroupId = groupId;
            Time = new List<DateTime> { DateTime.Now, DateTime.Now.AddDays(1) };
            IsContest = false;
            Tests = new List<TestViewModel>();
            AttemptsNumber = 1;
        }

        public TaskViewModelNew(StudentTask task)
        {
            Id = task.Id;
            Name = task.Name;
            Text = task.TaskText;
            GroupId = task.GroupId.Value;
            Time = new List<DateTime> { task.From, task.To };
            IsContest = task.IsContest;
            AttemptsNumber = task.NumberOfAttempts;

            Tests = task.Tests.Select(t => new TestViewModel(t, task.Tests.IndexOf(t))).ToList();
        }
    }

    public class TestViewModel
    {
        [JsonConstructor]
        public TestViewModel()
        { }

        public int Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public int Weight { get; set; }
        public bool Block { get; set; }
        public int Key { get; set; }
        public bool IsNew { get; set; }
        public string Data { get; set; }

        public TestViewModel(TaskTest test, int key)
        {
            Id = test.Id;
            Type = test.TestType;
            Name = TestsNamesConverter.ConvertTypeToName(test.TestType);
            Weight = (int)(test.Weight * 100);
            Block = test.Block;
            Key = key;
            IsNew = false;
            if (!string.IsNullOrEmpty(test.TestData))
            {
                Data = new string(test.TestData.Take(30).ToArray());
            }
            else
            {
                Data = "empty";
            }
        }
    }

    public class TestType
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public bool Disabled { get; set; } 

        public TestType(string type)
        {
            Type = type;
            Disabled = false;
            Name = TestsNamesConverter.ConvertTypeToName(type);
        }
    }

    public class GroupViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public GroupViewModel(Group gr)
        {
            Id = gr.Id;
            Name = gr.Name;
        }
    }
}
