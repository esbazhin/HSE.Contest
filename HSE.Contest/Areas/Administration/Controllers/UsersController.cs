using HSE.Contest.Areas.Administration.ViewModels;
using HSE.Contest.ClassLibrary;
using HSE.Contest.ClassLibrary.DbClasses;
using HSE.Contest.ClassLibrary.DbClasses.Administration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace HSE.Contest.Areas.Administration.Controllers
{
    [Authorize]
    [Area("Administration")]
    public class UsersController : Controller
    {
        TestingSystemConfig config;
        HSEContestDbContext db;
        public UsersController()
        {
            string pathToConfig = "c:\\config\\config.json";
            config = JsonConvert.DeserializeObject<TestingSystemConfig>(System.IO.File.ReadAllText(pathToConfig));

            DbContextOptionsBuilder<HSEContestDbContext> options = new DbContextOptionsBuilder<HSEContestDbContext>();
            options.UseNpgsql(config.DatabaseInfo.GetConnectionStringFrom(config.TestingSystem));
            db = new HSEContestDbContext(options.Options);
        }

        [AllowAnonymous]
        public IActionResult Index()
        {
            if (User.Identity.IsAuthenticated)
            {
                var id = User.FindFirst(c => c.Type == ClaimTypes.NameIdentifier).Value;
                User user = db.Users.Include(c => c.Roles).ThenInclude(c => c.Role).FirstOrDefaultAsync(u => u.Id == int.Parse(id)).Result;
                if (user != null)
                {
                    return RedicrectAfterLogin(user);
                }
                else
                {
                    return RedirectToAction("Logout", "Users");
                }
            }
            else
            {
                return RedirectToAction("Login", "Users");
            }
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {

            if (ModelState.IsValid)
            {
                User user = await db.Users.Include(c => c.Roles).ThenInclude(c => c.Role).FirstOrDefaultAsync(u => u.Email == model.Email && u.Password == model.Password);
                if (user != null)
                {
                    await Authenticate(user); // аутентификация

                    return RedicrectAfterLogin(user);
                }
                ModelState.AddModelError("", "Некорректные логин и(или) пароль");
            }
            return View(model);
        }

        private async Task Authenticate(User user)
        {
            // создаем один claim
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.FirstName + " " + user.LastName),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
            };

            claims.AddRange(user.Roles.Select(r => new Claim(ClaimTypes.Role, r.Role.Name)));
            // создаем объект ClaimsIdentity
            ClaimsIdentity id = new ClaimsIdentity(claims, "ApplicationCookie");
            // установка аутентификационных куки
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(id));
        }

        private IActionResult RedicrectAfterLogin(User user)
        {
            if (user.Roles.Count == 1)
            {
                return GetRedirect(user.Roles.First().Role.Name);
            }
            else
            {
                return ChooseRole(user);
            }
        }

        IActionResult ChooseRole(User user)
        {
            if (user.Roles.Count != 0)
            {
                return View("ChooseRole", user.Roles.Select(r => new RedirectViewModel(r.Role.Name, GetRedirect(r.Role.Name))
                {
                    Role = r.Role.Name,
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
                    return RedirectToAction("Index", "Admin");
                case "student":
                    return RedirectToAction("Index", "Student", new { area = "TestingSystem" });
                case "professor":
                    return RedirectToAction("Index", "Professor", new { area = "TestingSystem" });
                default:
                    return RedirectToAction("NoRoleFound", "Users");
            }
        }

        public IActionResult NoRoleFound()
        {
            return MessageResult("Не найдена подходящая роль, обратитесь к администратору!");
        }

        private IActionResult MessageResult(string v)
        {
            return View("Message", v);
        }

        public IActionResult AccessDenied()
        {
            return MessageResult("Доступ запрещён!");
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Users");
        }
    }
}
