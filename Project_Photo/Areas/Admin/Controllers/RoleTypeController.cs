using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Project_Photo.Areas.Admin.ViewModels.Role;
using Project_Photo.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Project_Photo.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class RoleTypeController : Controller
    {
        private readonly AaContext _context;
        private readonly ILogger<RoleTypeController> _logger;

        public RoleTypeController(AaContext context, ILogger<RoleTypeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Admin/UserRoleType
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var roleTypes = await _context.UserRoleTypes
                    .OrderBy(rt => rt.RoleTypeId)
                    .ToListAsync();

                var systems = await _context.UserSystemModules
                    .ToDictionaryAsync(s => s.SystemId, s => s.SystemName);

                var viewModels = new List<RoleTypeListViewModel>();

                foreach (var roleType in roleTypes)
                {
                    var userCount = await _context.UserRoles
                        .CountAsync(ur => ur.RoleTypeId == roleType.RoleTypeId && ur.IsActive == true);

                    viewModels.Add(new RoleTypeListViewModel
                    {
                        RoleTypeId = roleType.RoleTypeId,
                        RoleCode = roleType.RoleCode,
                        RoleName = roleType.RoleName,
                        RoleDescription = roleType.RoleDescription,
                        RoleLevel = roleType.RoleLevel,
                        SystemId = roleType.SystemId,
                        SystemName = roleType.SystemId.HasValue && systems.ContainsKey(roleType.SystemId.Value)
                            ? systems[roleType.SystemId.Value] 
                            : null,
                        IsActive = roleType.IsActive,
                        CreatedAt = roleType.CreatedAt,
                        UpdatedAt = roleType.UpdatedAt,
                        UserCount = userCount
                    });
                }

                return View(viewModels);
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, "取得角色類型列表時發生錯誤");
                TempData["Error"] = "取得角色類型列表時發生錯誤";
                return View(new List<RoleTypeListViewModel>());
            }
        }

        // GET: Admin/UserRoleType/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var roleType = await _context.UserRoleTypes
                    .FirstOrDefaultAsync(rt => rt.RoleTypeId == id);

                if (roleType == null)
                {
                    return NotFound();
                }

                var system = roleType.SystemId.HasValue
                    ? await _context.UserSystemModules.FirstOrDefaultAsync(s => s.SystemId ==roleType.SystemId.Value) 
                    : null;

                var users = await _context.UserRoles
                    .Where(ur => ur.RoleTypeId == id)
                    .Include(ur => ur.User)
                        .ThenInclude(u => u.UserProfile)
                    .OrderByDescending(ur => ur.AssignedAt)
                    .Select(ur => new RoleUserInfo
                    {
                        UserRoleId = ur.UserRoleId,
                        UserId = ur.UserId,
                        Account = ur.User.Account,
                        Email = ur.User.Email,
                        DisplayName = ur.User.UserProfile != null ? ur.User.UserProfile.DisplayName : null,
                        IsActive = ur.IsActive,
                        AssignedAt = ur.AssignedAt,
                        ExpiredAt = ur.ExpiredAt
                    })
                    .ToListAsync();

                var viewModel = new RoleTypeDetailsViewModel
                {
                    RoleTypeId = roleType.RoleTypeId,
                    RoleCode = roleType.RoleCode,
                    RoleName = roleType.RoleName,
                    RoleDescription = roleType.RoleDescription,
                    RoleLevel = roleType.RoleLevel,
                    SystemId = roleType.SystemId,
                    SystemName = system != null ? system.SystemName : null,
                    IsActive = roleType.IsActive,
                    CreatedAt = roleType.CreatedAt,
                    UpdatedAt = roleType.UpdatedAt,
                    UserCount = users.Count,
                    Users = users
                };

                return View(viewModel);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得角色類型詳情時發生錯誤 RoleTypeId: {RoleTypeId}", id);
                TempData["Error"] = "取得角色類型詳情時發生錯誤";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Admin/UserRoleType/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            try
            {
                // 載入系統模組選單
                ViewBag.SystemList = await _context.UserSystemModules
                    .Where(s => s.IsActive == true)
                    .OrderBy(s => s.SystemName)
                    .Select(s => new { s.SystemId, s.SystemName })
                    .ToListAsync();

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "載入新增頁面時發生錯誤");
                TempData["Error"] = "載入新增頁面時發生錯誤";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/UserRoleType/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("RoleTypeId,RoleCode,RoleName,RoleDescription,RoleLevel,SystemId,IsActive,CreatedAt,UpdatedAt")] UserRoleType model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var existingRoleType = await _context.UserRoleTypes
                        .FirstOrDefaultAsync(rt => rt.RoleCode == model.RoleCode);

                    if (existingRoleType != null)
                    {
                        ModelState.AddModelError("RoleCode", "角色代碼已存在");

                        ViewBag.SystemList = await _context.UserSystemModules
                            .Where(s => s.IsActive == true)
                            .OrderBy(s => s.SystemName)
                            .Select(s => new { s.SystemId, s.SystemName })
                            .ToListAsync();

                        return View(model);
                    }

                    model.CreatedAt = DateTime.Now;
                    model.UpdatedAt = DateTime.Now;

                    _context.UserRoleTypes.Add(model);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("角色類型已建立 RoleTypeId: {RoleTypeId}, RoleCode: {RoleCode}",
                        model.RoleTypeId, model.RoleCode);
                    TempData["Success"] = $"角色類型 {model.RoleName} 建立成功";

                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "建立角色類型時發生錯誤");
                    ModelState.AddModelError("", "建立角色類型時發生錯誤");
                }
            }

            ViewBag.SystemList = await _context.UserSystemModules
                .Where(s => s.IsActive == true)
                .OrderBy(s => s.SystemName)
                .Select(s => new { s.SystemId, s.SystemName })
                .ToListAsync();

            return View(model);
        }

        // GET: Admin/UserRoleType/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var roleType = await _context.UserRoleTypes.FindAsync(id);
                if (roleType == null)
                {
                    return NotFound();
                }

                // 載入系統模組選單
                ViewBag.SystemList = await _context.UserSystemModules
                    .Where(s => s.IsActive == true)
                    .OrderBy(s => s.SystemName)
                    .Select(s => new { s.SystemId, s.SystemName })
                    .ToListAsync();

                return View(roleType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "載入編輯頁面時發生錯誤 RoleTypeId: {RoleTypeId}", id);
                TempData["Error"] = "載入編輯頁面時發生錯誤";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/UserRoleType/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("RoleTypeId,RoleCode,RoleName,RoleDescription,RoleLevel,SystemId,IsActive,CreatedAt,UpdatedAt")] UserRoleType model)
        {
            if (id != model.RoleTypeId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // 檢查角色代碼是否被其他角色使用
                    var duplicateRoleType = await _context.UserRoleTypes
                        .Where(rt => rt.RoleTypeId != id && rt.RoleCode == model.RoleCode)
                        .FirstOrDefaultAsync();

                    if (duplicateRoleType != null)
                    {
                        ModelState.AddModelError("RoleCode", "角色代碼已被其他角色使用");

                        // 重新載入系統模組選單
                        ViewBag.SystemList = await _context.UserSystemModules
                            .Where(s => s.IsActive == true)
                            .OrderBy(s => s.SystemName)
                            .Select(s => new { s.SystemId, s.SystemName })
                            .ToListAsync();

                        return View(model);
                    }

                    var existingRoleType = await _context.UserRoleTypes.FindAsync(id);
                    if (existingRoleType == null)
                    {
                        return NotFound();
                    }

                    existingRoleType.RoleCode = model.RoleCode;
                    existingRoleType.RoleName = model.RoleName;
                    existingRoleType.RoleDescription = model.RoleDescription;
                    existingRoleType.RoleLevel = model.RoleLevel;
                    existingRoleType.SystemId = model.SystemId;
                    existingRoleType.IsActive = model.IsActive;
                    existingRoleType.UpdatedAt = DateTime.Now;

                    await _context.SaveChangesAsync();

                    _logger.LogInformation("角色類型已更新 RoleTypeId: {RoleTypeId}, RoleCode: {RoleCode}",
                        model.RoleTypeId, model.RoleCode);
                    TempData["Success"] = $"角色類型 {model.RoleName} 更新成功";

                    return RedirectToAction(nameof(Details), new { id = model.RoleTypeId });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UserRoleTypeExists(model.RoleTypeId))
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
                    _logger.LogError(ex, "更新角色類型時發生錯誤 RoleTypeId: {RoleTypeId}", id);
                    ModelState.AddModelError("", "更新角色類型時發生錯誤");
                }
            }
            // 重新載入系統模組選單
            ViewBag.SystemList = await _context.UserSystemModules
                .Where(s => s.IsActive == true)
                .OrderBy(s => s.SystemName)
                .Select(s => new { s.SystemId, s.SystemName })
                .ToListAsync();

            return View(model);
        }

        // GET: Admin/UserRoleType/Delete/5
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var roleType = await _context.UserRoleTypes
                    .FirstOrDefaultAsync(rt => rt.RoleTypeId == id);

                if (roleType == null)
                {
                    return NotFound();
                }

                // 取得系統名稱（如果有的話）
                var system = roleType.SystemId.HasValue
                    ? await _context.UserSystemModules.FirstOrDefaultAsync(s => s.SystemId == roleType.SystemId.Value)
                    : null;

                var userCount = await _context.UserRoles
                    .CountAsync(ur => ur.RoleTypeId == id);

                var viewModel = new RoleTypeDeleteViewModel
                {
                    RoleTypeId = roleType.RoleTypeId,
                    RoleCode = roleType.RoleCode,
                    RoleName = roleType.RoleName,
                    RoleDescription = roleType.RoleDescription,
                    RoleLevel = roleType.RoleLevel,
                    SystemId = roleType.SystemId,
                    SystemName = system != null ? system.SystemName : null,
                    IsActive = roleType.IsActive,
                    CreatedAt = roleType.CreatedAt,
                    UserCount = userCount
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "載入刪除確認頁面時發生錯誤 RoleTypeId: {RoleTypeId}", id);
                TempData["Error"] = "載入刪除確認頁面時發生錯誤";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/UserRoleType/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var roleType = await _context.UserRoleTypes.FindAsync(id);
                if (roleType == null)
                {
                    return NotFound();
                }

                // 檢查是否有用戶使用此角色
                var hasUsers = await _context.UserRoles.AnyAsync(ur => ur.RoleTypeId == id);

                if (hasUsers)
                {
                    TempData["Error"] = "無法刪除：此角色類型仍有用戶使用，請先移除相關用戶的角色";
                    return RedirectToAction(nameof(Delete), new { id });
                }

                _context.UserRoleTypes.Remove(roleType);
                await _context.SaveChangesAsync();

                _logger.LogWarning("角色類型已刪除 RoleTypeId: {RoleTypeId}, RoleCode: {RoleCode}",
                    roleType.RoleTypeId, roleType.RoleCode);
                TempData["Success"] = $"角色類型 {roleType.RoleName} 已刪除";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "刪除角色類型時發生錯誤 RoleTypeId: {RoleTypeId}", id);
                TempData["Error"] = "刪除角色類型時發生錯誤";
                return RedirectToAction(nameof(Index));
            }
        }

        private bool UserRoleTypeExists(int id)
        {
            return _context.UserRoleTypes.Any(e => e.RoleTypeId == id);
        }
    }
}
