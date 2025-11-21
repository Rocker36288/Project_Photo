using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Project_Photo.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Project_Photo.Areas.Admin.Controllers
{
    [Area("Admin")]
    //[SuperAdminAuthorize]
    public class DashboardController : Controller
    {
        private readonly AaContext _context;
        private readonly ILogger<UserManagementController> _logger;

        public DashboardController(AaContext context, ILogger<UserManagementController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Admin/Dashboard
        public async Task<IActionResult> Index()
        {
            string displayName = HttpContext.Session.GetString("DisplayName") ?? "管理員";

            ViewBag.DisplayName = displayName;

            // 統計數據
            var totalUsers = await _context.Users.CountAsync(u => u.IsDeleted == false);
            var activeUsers = await _context.Users.CountAsync(u => u.AccountStatus == "Active" && u.IsDeleted == false);
            var inactiveUsers = await _context.Users.CountAsync(u => u.AccountStatus == "Inactive" && u.IsDeleted == false);
            var suspendedUsers = await _context.Users.CountAsync(u => u.AccountStatus == "Suspended" && u.IsDeleted == false);

            var activeSessions = await _context.UserSessions.CountAsync(s => s.IsActive == true);

            // 最近註冊的用戶（最近7天）
            var recentUsers = await _context.Users
                .Where(u => u.IsDeleted == false && u.CreatedAt >= DateTime.Now.AddDays(-7))
                .OrderBy(u => u.UserId)
                //.Take(10)
                .ToListAsync();

            // 系統統計
            var systemStats = await _context.UserSystemModules
                .Where(s => s.IsActive == true)
                .Select(s => new
                {
                    s.SystemName,
                    s.SystemCode,
                    UserCount = _context.UserRoles
                        .Count(ur => ur.RoleType.SystemId == s.SystemId && ur.IsActive == true)
                }).ToListAsync();

            ViewBag.TotalUsers = totalUsers;
            ViewBag.ActiveUsers = activeUsers;
            ViewBag.InactiveUsers = inactiveUsers;
            ViewBag.SuspendedUsers = suspendedUsers;
            ViewBag.ActiveSessions = activeSessions;
            ViewBag.RecentUsers = recentUsers;
            ViewBag.SystemStats = systemStats;

            return View();
        }

        // GET: Admin/Dashboard/Details/5
        public async Task<IActionResult> Details(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(m => m.UserId == id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // GET: Admin/Dashboard/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/Dashboard/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("UserId,Account,Email,Phone,Password,AccountType,AccountStatus,RegistrationSource,IsDeleted,DeletedAt,CreatedAt,UpdatedAt")] User user)
        {
            if (ModelState.IsValid)
            {
                _context.Add(user);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(user);
        }

        // GET: Admin/Dashboard/Edit/5
        public async Task<IActionResult> Edit(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            return View(user);
        }

        // POST: Admin/Dashboard/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, [Bind("UserId,Account,Email,Phone,Password,AccountType,AccountStatus,RegistrationSource,IsDeleted,DeletedAt,CreatedAt,UpdatedAt")] User user)
        {
            if (id != user.UserId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(user);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UserExists(user.UserId))
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

        // GET: Admin/Dashboard/Delete/5
        public async Task<IActionResult> Delete(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(m => m.UserId == id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // POST: Admin/Dashboard/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                _context.Users.Remove(user);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool UserExists(long id)
        {
            return _context.Users.Any(e => e.UserId == id);
        }
    }
}
