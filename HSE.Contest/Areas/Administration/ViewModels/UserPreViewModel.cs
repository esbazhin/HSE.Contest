namespace HSE.Contest.Areas.Administration.ViewModels
{
    public class UserPreViewModel
    {
        public string Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Roles { get; set; }
        public string Groups { get; set; }

        public string Description { get { return $"Email: {Email}, Roles: {Roles}, Groups: {Groups}"; } }
    }
}
