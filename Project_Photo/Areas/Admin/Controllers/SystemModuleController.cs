using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Project_Photo.Areas.Admin.ViewModels.SystemModule;
using Project_Photo.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Permissions;
using System.Threading.Tasks;

namespace Project_Photo.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class SystemModuleController : Controller
    {
        private readonly AaContext _context;
        private readonly ILogger<SystemModuleController> _logger;

        public SystemModuleController(AaContext context, ILogger<SystemModuleController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Admin/SystemModule
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var systemModules = await _context.UserSystemModules
                    .OrderBy(s => s.SystemId)
                    .ToListAsync();

                var viewModels = new List<SystemModuleListViewModel>();

                foreach (var system in systemModules)
                {
                    var roleTypeCount = await _context.UserRoleTypes
                        .CountAsync(rt => rt.SystemId == system.SystemId && rt.IsActive == true);

                    var permissionCount = await _context.UserPermissions
                        .CountAsync(p => p.SystemId == system.SystemId && p.IsActive == true);

                    var activeUserCount = await _context.UserRoles
                        .Where(ur => ur.RoleType.SystemId == system.SystemId && ur.IsActive == true)
                        .Select(ur => ur.UserId)
                        .Distinct()
                        .CountAsync();

                    viewModels.Add(new SystemModuleListViewModel
                    {
                        SystemId = system.SystemId,
                        SystemCode = system.SystemCode,
                        SystemName = system.SystemName,
                        SystemDescription = system.SystemDescription,
                        IsActive = system.IsActive,
                        CreatedAt = system.CreatedAt,
                        UpdatedAt = system.UpdatedAt,
                        RoleTypeCount = roleTypeCount,
                        PermissionCount = permissionCount,
                        ActiveUserCount = activeUserCount
                    });

                }

                return View(viewModels);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得系統模組列表時發生錯誤");
                TempData["Error"] = "取得系統模組列表時發生錯誤";
                return View(new List<SystemModuleListViewModel>());
            }
        }

        // GET: Admin/SystemModule/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            try
            {
                var system = await _context.UserSystemModules
                    .FirstOrDefaultAsync(m => m.SystemId == id);

                if (system == null)
                {
                    return NotFound();
                }

                var roleTypes = await _context.UserRoleTypes
                    .Where(rt => rt.SystemId == id)
                    .OrderBy(rt => rt.RoleLevel)
                    .Select(rt => new RoleTypeInfo
                    {
                        RoleTypeId = rt.RoleTypeId,
                        RoleCode = rt.RoleCode,
                        RoleName = rt.RoleName,
                        RoleDescription = rt.RoleDescription,
                        RoleLevel = rt.RoleLevel,
                        IsActive = rt.IsActive,
                        CreatedAt = rt.CreatedAt
                    })
                    .ToListAsync();

                var permissionsData = await _context.UserPermissions
                    .Where(p => p.SystemId == id)
                    .OrderBy(p => p.CategoryId)
                    .ThenBy(p => p.PermissionId)
                    .ToArrayAsync();

                var permissions = permissionsData.Select(p => new PermissionInfo
                {
                    PermissionId = p.PermissionId,
                    PermissionCode = p.PermissionCode,
                    PermissionName = p.PermissionName,
                    PermissionDescription = p.PermissionDescription,
                    CategoryName = p.Category != null ? p.Category.CategoryName : null,
                    ParentPermissionId = p.ParentPermissionId,
                    ParentPermissionName = p.ParentPermissionId.HasValue
                        ? permissionsData.FirstOrDefault(parent => parent.PermissionId == p.ParentPermissionId.Value)?.PermissionName
                        : null,
                    IsActive = p.IsActive
                }).ToList();

                var roleTypeCount = roleTypes.Count;
                var permissionCount = permissions.Count;
                var activeUserCount = await _context.UserRoles
                    .Where(ur => ur.RoleType.SystemId == id && ur.IsActive == true)
                    .Select(ur => ur.UserId)
                    .Distinct()
                    .CountAsync();

                var viewModel = new SystemModuleDetailsViewModel
                {
                    SystemId = system.SystemId,
                    SystemCode = system.SystemCode,
                    SystemName = system.SystemName,
                    SystemDescription = system.SystemDescription,
                    IsActive = system.IsActive,
                    CreatedAt = system.CreatedAt,
                    UpdatedAt = system.UpdatedAt,
                    RoleTypeCount = roleTypeCount,
                    PermissionCount = permissionCount,
                    ActiveUserCount = activeUserCount,
                    RoleTypes = roleTypes,
                    Permissions = permissions
                };


                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得系統模組詳情時發生錯誤 SystemId: {SystemId}", id);
                TempData["Error"] = "取得系統模組詳情時發生錯誤";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Admin/SystemModule/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/SystemModule/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserSystemModule model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var existingSystem = await _context.UserSystemModules
                        .FirstOrDefaultAsync(s => s.SystemCode == model.SystemCode);

                    if (existingSystem == null)
                    {
                        ModelState.AddModelError("SystemCode", "系統代碼已存在");
                        return View(model);
                    }

                    model.CreatedAt = DateTime.Now;
                    model.UpdatedAt = DateTime.Now;

                    _context.UserSystemModules.Add(model);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("系統模組已建立 SystemId: {SystemId}, SystemCode: {SystemCode}",
                        model.SystemId, model.SystemCode);
                    TempData["Success"] = $"系統模組 {model.SystemName} 建立成功";

                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "建立系統模組時發生錯誤");
                    ModelState.AddModelError("", "建立系統模組時發生錯誤");
                }
            }

            return View(model);
        }

        // GET: Admin/SystemModule/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            try
            {
                var system = await _context.UserSystemModules.FindAsync(id);
                if (system == null)
                {
                    return NotFound();
                }

                return View(system);
            } catch (Exception ex)
            {
                _logger.LogError(ex, "載入編輯頁面時發生錯誤 SystemId: {SystemId}", id);
                TempData["Error"] = "載入編輯頁面時發生錯誤";
                return RedirectToAction(nameof(Index));
            }
            
        }

        // POST: Admin/SystemModule/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, UserSystemModule model)
        {
            if (id != model.SystemId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var duplicateSystem = await _context.UserSystemModules
                        .Where(s => s.SystemId != id && s.SystemCode == model.SystemCode)
                        .FirstOrDefaultAsync();

                    if (duplicateSystem != null)
                    {
                        ModelState.AddModelError("SystemCode", "系統代碼已被其他系統使用");
                        return View(model);
                    }

                    var existingSystem = await _context.UserSystemModules.FindAsync(id);
                    if (existingSystem == null)
                    {
                        return NotFound();
                    }

                    existingSystem.SystemCode = model.SystemCode;
                    existingSystem.SystemName = model.SystemName;
                    existingSystem.SystemDescription = model.SystemDescription;
                    existingSystem.IsActive = model.IsActive;
                    existingSystem.UpdatedAt = DateTime.Now;

                    await _context.SaveChangesAsync();

                    _logger.LogInformation("系統模組已更新 SystemId: {SystemId}, SystemCode: {SystemCode}",
                        model.SystemId, model.SystemCode);
                    TempData["Success"] = $"系統模組 {model.SystemName} 更新成功";

                    return RedirectToAction(nameof(Details), new { id = model.SystemId });

                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UserSystemModuleExists(model.SystemId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "更新系統模組時發生錯誤 SystemId: {SystemId}", id);
                    ModelState.AddModelError("", "更新系統模組時發生錯誤");
                }
            }

            return View(model);
        }

        // GET: Admin/SystemModule/Delete/5
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            try
            {
                var system = await _context.UserSystemModules
                .FirstOrDefaultAsync(m => m.SystemId == id);

                if (system == null)
                {
                    return NotFound();
                }

                var roleTypeCount = await _context.UserRoleTypes
                    .CountAsync(rt => rt.SystemId == id);

                var permissionCount = await _context.UserPermissions
                    .CountAsync(p => p.SystemId == id);

                var affectedUserCount = await _context.UserRoles
                    .Where(ur => ur.RoleType.SystemId == id && ur.IsActive == true)
                    .Select(ur => ur.UserId)
                    .Distinct()
                    .CountAsync();

                var viewModel = new SystemModuleDeleteViewModel
                {
                    SystemId = system.SystemId,
                    SystemCode = system.SystemCode,
                    SystemName = system.SystemName,
                    SystemDescription = system.SystemDescription,
                    IsActive = system.IsActive,
                    CreatedAt = system.CreatedAt,
                    RoleTypeCount = roleTypeCount,
                    PermissionCount = permissionCount,
                    AffectedUserCount = affectedUserCount
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "載入刪除確認頁面時發生錯誤 SystemId: {SystemId}", id);
                TempData["Error"] = "載入刪除確認頁面時發生錯誤";
                return RedirectToAction(nameof(Index));
            }
            
        }

        // POST: Admin/SystemModule/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var system = await _context.UserSystemModules.FindAsync(id);

                if (system == null)
                {
                    return NotFound();
                }

                var hasRoleTypes = await _context.UserRoleTypes.AnyAsync(rt => rt.SystemId == id);
                var hasPermissions = await _context.UserPermissions.AnyAsync(p => p.SystemId == id);

                if (hasRoleTypes || hasPermissions)
                {
                    TempData["Error"] = "無法刪除:此系統模組仍有關聯的角色類型或權限,請先移除相關資料";
                    return RedirectToAction(nameof(Delete), new { id });
                }

                _context.UserSystemModules.Remove(system);
                await _context.SaveChangesAsync();

                _logger.LogWarning("系統模組已刪除 SystemId: {SystemId}, SystemCode: {SystemCode}",
                    system.SystemId, system.SystemCode);
                TempData["Success"] = $"系統模組 {system.SystemName} 已刪除";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "刪除系統模組時發生錯誤 SystemId: {SystemId}", id);
                TempData["Error"] = "刪除系統模組時發生錯誤";
                return RedirectToAction(nameof(Index));
            }
            
        }

        private bool UserSystemModuleExists(int id)
        {
            return _context.UserSystemModules.Any(e => e.SystemId == id);
        }
    }
}
