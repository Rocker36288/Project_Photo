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

    }
}
