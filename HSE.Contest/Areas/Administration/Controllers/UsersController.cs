using HSE.Contest.Areas.Administration.ViewModels;
using HSE.Contest.ClassLibrary.DbClasses;
using HSE.Contest.ClassLibrary.DbClasses.Administration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HSE.Contest.Areas.Administration.Controllers
{
    [Authorize(Roles = "admin")]
    [Area("Administration")]
    public class UsersController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly HSEContestDbContext _db;

        public UsersController(UserManager<User> userManager, RoleManager<IdentityRole> roleManager, HSEContestDbContext db)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _db = db;
        }

        public IActionResult Index()
        {
            return RedirectToAction("AllUsers");
        }

        public async Task<IActionResult> AllUsers()
        {
            var allUsers = await _userManager.Users.ToListAsync();
            var users = new List<UserPreViewModel>();

            foreach (var u in allUsers)
            {
                var roles = await _userManager.GetRolesAsync(u);
                users.Add(new UserPreViewModel
                {
                    Id = u.Id,
                    FullName = u.FirstName + " " + u.LastName,
                    Email = u.Email,
                    Roles = string.Join(",", roles),
                    Groups = string.Join(",", u.Groups.Select(g => g.Group.Name)),
                });
            }

            return View(users);
        }

        public async Task<IActionResult> DeleteUser(string id)
        {
            User user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                await _userManager.DeleteAsync(user);
            }
            return RedirectToAction("AllUsers");
        }

        public async Task<IActionResult> CreateNewUser()
        {
            var allRoles = await _roleManager.Roles.Select(r => new TransferViewModel(r)).ToListAsync();
            var allGroups = await _db.Groups.Select(g => new TransferViewModel(g)).ToListAsync();

            return View(new UserCRUDViewModel { AllRoles = allRoles, AllGroups = allGroups, User = new UserViewModel(null, null), IsUpdate = false });
        }

        public async Task<IActionResult> ChangeUser(string id)
        {
            User user = await _userManager.FindByIdAsync(id);
            if (user is null)
            {
                return NotFound();
            }

            var allRoles = await _roleManager.Roles.Select(r => new TransferViewModel(r)).ToListAsync();
            var allGroups = await _db.Groups.Select(g => new TransferViewModel(g)).ToListAsync();

            var selRoles = await _userManager.GetRolesAsync(user);
            return View("CreateNewUser", new UserCRUDViewModel { AllRoles = allRoles, AllGroups = allGroups, User = new UserViewModel(user, selRoles.ToList()), IsUpdate = true });
        }

        public async Task<IActionResult> UpdateUser(string json)
        {
            UserViewModel userRecord = JsonConvert.DeserializeObject<UserViewModel>(json);

            User user = await _userManager.FindByIdAsync(userRecord.Id);

            if (user is null)
            {
                return Content("error");
            }

            _db.UserGroups.RemoveRange(user.Groups);

            user.FirstName = userRecord.FirstName;
            user.LastName = userRecord.LastName;
            user.Email = userRecord.Email;
            user.UserName = userRecord.Email;

            user.Groups = userRecord.SelectedGroups.Select(g => new UserGroup { GroupId = int.Parse(g), UserId = userRecord.Id }).ToList();

            var result = await _userManager.UpdateAsync(user);

            bool ok = result.Succeeded;

            if (ok)
            {
                var allRoles = await _userManager.GetRolesAsync(user);

                var res = await _userManager.RemoveFromRolesAsync(user, allRoles);
                ok = res.Succeeded;

                if (ok)
                {
                    var res1 = await _userManager.AddToRolesAsync(user, userRecord.SelectedRoles);

                    ok = res1.Succeeded;

                    if(ok)
                    {
                        var response1 = new
                        {
                            status = "success",
                            data = "/Administration/Users/ChangeUser?id=" + user.Id
                        };

                        return Json(response1);
                    }
                }

                var response2 = new
                {
                    status = "error",
                    data = string.Join(",", res.Errors.Select(e => e.Description))
                };

                return Json(response2);
            }

            var response3 = new
            {
                status = "error",
                data = string.Join(",", result.Errors.Select(e => e.Description))
            };

            return Json(response3);
        }

        public async Task<IActionResult> PostNewUser(string json)
        {
            UserViewModel userRecord = JsonConvert.DeserializeObject<UserViewModel>(json);
            User newUser = new User
            {
                FirstName = userRecord.FirstName,
                LastName = userRecord.LastName,
                Email = userRecord.Email,
                UserName = userRecord.Email,
                Groups = userRecord.SelectedGroups.Select(g => new UserGroup { GroupId = int.Parse(g), UserId = userRecord.Id }).ToList()
            };

            var result = await _userManager.CreateAsync(newUser, userRecord.Password);

            bool ok = result.Succeeded;

            if (ok)
            {
                var res1 = await _userManager.AddToRolesAsync(newUser, userRecord.SelectedRoles);

                var response1 = new
                {
                    status = "success",
                    data = "/Administration/Users/ChangeUser?id=" + newUser.Id
                };

                return Json(response1);
            }

            var response2 = new
            {
                status = "error",
                data = string.Join(",", result.Errors.Select(e => e.Description))
            };

            return Json(response2);
        }

        [HttpGet]
        public IActionResult ChangePassword(string id)
        {
            return View(id);
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(string id, string newPassword)
        {
            User user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                var _passwordValidator =
                    HttpContext.RequestServices.GetService(typeof(IPasswordValidator<User>)) as IPasswordValidator<User>;
                var _passwordHasher =
                    HttpContext.RequestServices.GetService(typeof(IPasswordHasher<User>)) as IPasswordHasher<User>;

                IdentityResult result =
                    await _passwordValidator.ValidateAsync(_userManager, user, newPassword);
                if (result.Succeeded)
                {
                    user.PasswordHash = _passwordHasher.HashPassword(user, newPassword);
                    await _userManager.UpdateAsync(user);
                    var response1 = new
                    {
                        status = "success",
                        data = "/Administration/Users"
                    };

                    return Json(response1);
                }
                else
                {
                    var response1 = new
                    {
                        status = "error",
                        data = string.Join(",", result.Errors.Select(e => e.Description))
                    };

                    return Json(response1);
                }
            }
            else
            {
                var response1 = new
                {
                    status = "error",
                    data = "Пользователь не найден"
                };

                return Json(response1);
            }
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> ChangeMyPassword([FromServices] IHttpContextAccessor httpContextAccessor)
        {
            string id = _userManager.GetUserId(httpContextAccessor.HttpContext.User);

            User user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            ChangePasswordViewModel model = new ChangePasswordViewModel { Id = user.Id, Email = user.Email, BackLink = httpContextAccessor.HttpContext.Request.Headers["Referer"].ToString() };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ChangeMyPassword(ChangePasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                User user = await _userManager.FindByIdAsync(model.Id);
                if (user != null)
                {
                    IdentityResult result =
                        await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
                    if (result.Succeeded)
                    {
                        return Redirect(model.BackLink);
                    }
                    else
                    {
                        foreach (var error in result.Errors)
                        {
                            ModelState.AddModelError(string.Empty, error.Description);
                        }
                    }
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Пользователь не найден");
                }
            }
            return View(model);
        }
    }
}
