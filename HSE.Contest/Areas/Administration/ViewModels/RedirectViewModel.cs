using Microsoft.AspNetCore.Mvc;

namespace HSE.Contest.Areas.Administration.ViewModels
{
    public class RedirectViewModel
    {
        public string Role { get; set; }
        public string Area { get; set; }
        public string Controller { get; set; }
        public string Action { get; set; }

        public RedirectViewModel(string role, RedirectToActionResult res)
        {
            Role = role;
            Area = res.RouteValues != null && res.RouteValues.ContainsKey("Area") ? (string)res.RouteValues["Area"] : null;
            Controller = res.ControllerName;
            Action = res.ActionName;
        }
    }
}
