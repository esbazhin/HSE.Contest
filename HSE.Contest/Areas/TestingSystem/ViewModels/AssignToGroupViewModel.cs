using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;

namespace HSE.Contest.Areas.TestingSystem.ViewModels
{
    public class AssignToGroupViewModel
    {
        public int TaskId { get; set; }
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public int GroupId { get; set; }
        public List<SelectListItem> Groups { get; set; }
    }
}
