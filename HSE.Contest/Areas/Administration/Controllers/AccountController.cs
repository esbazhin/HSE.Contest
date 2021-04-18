using HSE.Contest.Areas.Administration.ViewModels;
using HSE.Contest.ClassLibrary.DbClasses.Administration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HSE.Contest.Areas.Administration.Controllers
{
    [Authorize]
    [Area("Administration")]
    public class AccountController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        public AccountController(UserManager<User> userManager, SignInManager<User> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            if (User.Identity.IsAuthenticated)
            {
                var user = await _userManager.GetUserAsync(HttpContext.User);                
                if (user != null)
                {
                    return await RedicrectAfterLogin(user);
                }
                else
                {
                    return RedirectToAction("Logout", "Account");
                }
            }
            else
            {
                return RedirectToAction("Login", "Account");
            }
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result =
                    await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, false);

                if (result.Succeeded)
                {
                    // проверяем, принадлежит ли URL приложению
                    if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                    {
                        return Redirect(model.ReturnUrl);
                    }
                    else
                    {
                        var user = await _userManager.FindByEmailAsync(model.Email);
                        return await RedicrectAfterLogin(user);
                    }
                }
                else
                {
                    ModelState.AddModelError("", "Неправильный логин и (или) пароль");
                }
            }
            return View(model);
        }            

        private async Task<IActionResult> RedicrectAfterLogin(User user)
        {           
            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Count == 1)
            {
                return GetRedirect(roles[0]);
            }
            else
            {
                return ChooseRole(roles);
            }
        }

        IActionResult ChooseRole(IList<string> roles)
        {
            if (roles.Count != 0)
            {
                return View("ChooseRole", roles.Select(r => new RedirectViewModel(r, GetRedirect(r))
                {
                    Role = r,
                }).ToList());
            }
            else
            {
                return RedirectToAction("NoRoleFound", "Users");
            }
        }

        RedirectToActionResult GetRedirect(string role)
        {
            switch (role)
            {
                case "admin":
                    return RedirectToAction("Index", "Users");
                case "student":
                    return RedirectToAction("Index", "Student", new { area = "TestingSystem" });
                case "professor":
                    return RedirectToAction("Index", "Professor", new { area = "TestingSystem" });
                default:
                    return RedirectToAction("NoRoleFound", "Account");
            }
        }

        public IActionResult NoRoleFound()
        {
            return MessageResult("Не найдена подходящая роль, обратитесь к администратору!");
        }

        private IActionResult MessageResult(string v, string url = null)
        {
            return View("Message", (v, url));
        }

        [AllowAnonymous]
        public IActionResult AccessDenied(string ReturnUrl)
        {
            return MessageResult("Доступ запрещён!", ReturnUrl);
        }

        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login", "Account");
        }
    }
}
