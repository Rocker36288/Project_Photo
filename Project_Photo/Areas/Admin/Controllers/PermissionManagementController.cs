using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Project_Photo.Areas.Admin.ViewModels.PermissionManagement;
using Project_Photo.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Project_Photo.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class PermissionManagementController : Controller
    {
        private readonly AaContext _context;
        private readonly ILogger<PermissionManagementController> _logger;

        public PermissionManagementController(AaContext context, ILogger<PermissionManagementController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Admin/PermissionManagement
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var model = new PermissionManagementIndexViewModel();

                // 統計系統模組
                model.TotalSystemModules = await _context.UserSystemModules.CountAsync();
                model.ActiveSystemModules = await _context.UserSystemModules.CountAsync(s => s.IsActive == true);

                // 統計角色類型
                model.TotalRoleTypes = await _context.UserRoleTypes.CountAsync();
                model.ActiveRoleTypes = await _context.UserRoleTypes.CountAsync(r => r.IsActive == true);

                // 統計權限分類
                model.TotalCategories = await _context.UserPermissionCategories.CountAsync();
                model.ActiveCategories = await _context.UserPermissionCategories.CountAsync(c => c.IsActive == true);

                model.TotalPermissions = await _context.UserPermissions.CountAsync();
                model.ActivePermissions = await _context.UserPermissions.CountAsync(p => p.IsActive == true);

                // 最近7天更新的權限
                var sevenDaysAgo = DateTime.Now.AddDays(-7);
                model.RecentPermissions = await _context.UserPermissions
                    .Include(p => p.System)
                    .Include(p => p.Category)
                    .OrderByDescending(p => p.PermissionId)
                    .Select(p => new RecentPermissionInfo
                    {
                        PermissionId = p.PermissionId,
                        PermissionCode = p.PermissionCode,
                        PermissionName = p.PermissionName,
                        SystemName = p.System != null ? p.System.SystemName : "未分配",
                        CategoryName = p.Category != null ? p.Category.CategoryName : "未分類",
                        IsActive = p.IsActive,
                    })
                    .ToListAsync();

                var systems = await _context.UserSystemModules
                    .Where(s => s.IsActive == true)
                    .OrderBy(s => s.SystemId)
                    .ToListAsync();

                foreach (var system in systems)
                {
                    var permissionCount = await _context.UserPermissions
                        .CountAsync(p => p.SystemId == system.SystemId);

                    var activePermissionCount = await _context.UserPermissions
                        .CountAsync(p => p.SystemId == system.SystemId && p.IsActive == true);

                    model.SystemPermissionStats.Add(new SystemPermissionStatInfo
                    {
                        SystemId = system.SystemId,
                        SystemCode = system.SystemCode,
                        SystemName = system.SystemName,
                        PermissionCount = permissionCount,
                        ActivePermissionCount = activePermissionCount
                    });
                }

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得權限管理總覽時發生錯誤");
                TempData["Error"] = "取得權限管理總覽時發生錯誤";
                return View(new PermissionManagementIndexViewModel());
            }

        }
    }
}
