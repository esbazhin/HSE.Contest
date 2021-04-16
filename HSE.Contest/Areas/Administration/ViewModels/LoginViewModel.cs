using System.ComponentModel.DataAnnotations;

namespace HSE.Contest.Areas.Administration.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Не указан Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Не указан пароль")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        public string ReturnUrl { get; set; }

        public bool RememberMe { get; set; }
    }

    public class NewLoginViewModel
    {     
        public string Login { get; set; }       
        public string Password { get; set; }
    }
}
