using HSE.Contest.Areas.Administration.ViewModels;
using HSE.Contest.ClassLibrary;
using HSE.Contest.ClassLibrary.DbClasses;
using HSE.Contest.ClassLibrary.DbClasses.Administration;
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
    [Authorize(Roles = "admin")]
    [Area("Administration")]
    public class AdminController : Controller
    {
        private readonly TestingSystemConfig config;
        private readonly HSEContestDbContext _context;

        public AdminController()
        {
            string pathToConfig = "c:\\config\\config.json";
            config = JsonConvert.DeserializeObject<TestingSystemConfig>(System.IO.File.ReadAllText(pathToConfig));

            DbContextOptionsBuilder<HSEContestDbContext> options = new DbContextOptionsBuilder<HSEContestDbContext>();
            options.UseNpgsql(config.DatabaseInfo.GetConnectionStringFrom(config.FrontEnd));
            _context = new HSEContestDbContext(options.Options);
        }

        async Task<List<User>> GetAllUsers()
        {
            return await _context.Users.Include(c => c.Roles).ThenInclude(c => c.Role).ToListAsync();
        }

        async Task<User> GetUser(int id)
        {
            return await _context.Users.Include(c => c.Roles).ThenInclude(c => c.Role).FirstOrDefaultAsync(m => m.Id == id);
        }

        // GET: Administration/Users1
        public async Task<IActionResult> Index()
        {
            return View(await _context.Users.ToListAsync());
        }

        // GET: Administration/Users1/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await GetUser(id.Value);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // GET: Administration/Users1/Create
        public IActionResult Create()
        {
            return View(new UserCRUDViewModel { AllRoles = _context.Roles.ToListAsync().Result });
        }

        // POST: Administration/Users1/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Email,Password,FirstName,LastName")] User user, List<int> selectedRolesId)
        {
            if (ModelState.IsValid)
            {
                user.Roles.AddRange(selectedRolesId.Select(i => new UserRole { RoleId = i, UserId = user.Id }).ToList());
                _context.Add(user);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(user);
        }

        // GET: Administration/Users1/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await GetUser(id.Value);
            if (user == null)
            {
                return NotFound();
            }
            var allRoles = await _context.Roles.ToListAsync();
            return View(new UserCRUDViewModel { User = user, AllRoles = allRoles });
        }

        // POST: Administration/Users1/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([Bind("Id,Email,Password,FirstName,LastName")] User user, List<int> selectedRolesId)
        {
            if (ModelState.IsValid)
            {
                var user1 = await GetUser(user.Id);

                if (user1 == null)
                {
                    return NotFound();
                }

                user1.Email = user.Email;
                user1.FirstName = user.FirstName;
                user1.LastName = user.LastName;
                user1.Password = user.Password;

                user1.Roles.Clear();
                user1.Roles.AddRange(selectedRolesId.Select(i => new UserRole { RoleId = i, UserId = user.Id }).ToList());
                try
                {
                    _context.Update(user1);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UserExists(user.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(user);
        }

        async Task DeleteRoles(int id)
        {
            var user = await GetUser(id);
            user.Roles.Clear();
            await _context.SaveChangesAsync();
        }

        // GET: Administration/Users1/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await GetUser(id.Value);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // POST: Administration/Users1/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _context.Users.FindAsync(id);
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            if (User.FindFirst(c => c.Type == ClaimTypes.NameIdentifier).Value == id.ToString())
            {
                return RedirectToAction("Logout", "Users", new { area = "Administration" });
            }
            return RedirectToAction(nameof(Index));
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.Id == id);
        }
    }
}
