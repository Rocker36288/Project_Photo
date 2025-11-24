using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Project_Photo.Areas.Admin.ViewModels.Permission;
using Project_Photo.Models;

namespace Project_Photo.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class UserPermissionController : Controller
    {
        private readonly AAContext _context;
        private readonly ILogger<UserPermissionController> _logger;

        public UserPermissionController(AAContext context, ILogger<UserPermissionController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Admin/UserPermission
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var permissions = await _context.UserPermissions
                    .Include(p => p.Category)
                    .Include(p => p.System)
                    .OrderBy(p => p.PermissionId)
                    .ToListAsync();

                var viewModels = new List<UserPermissionListViewModel>();

                foreach (var permission in permissions )
                {
                    var parentPermissionName = permission.ParentPermissionId.HasValue
                        ? await _context.UserPermissions
                            .Where(p => p.PermissionId == permission.ParentPermissionId.Value)
                            .Select(p => p.PermissionName)
                            .FirstOrDefaultAsync()
                        : null;

                    var childPermissionCount = await _context.UserPermissions
                        .CountAsync(p => p.ParentPermissionId == permission.PermissionId);

                    var assignedRoleTypeCount = await _context.UserRolePermissions
                        .Where(rp => rp.PermissionId == permission.PermissionId)
                        .Select(rp => rp.RoleTypeId)
                        .Distinct()
                        .CountAsync();

                    viewModels.Add(new UserPermissionListViewModel
                    {
                        PermissionId = permission.PermissionId,
                        PermissionCode = permission.PermissionCode,
                        PermissionName = permission.PermissionName,
                        PermissionDescription = permission.PermissionDescription,
                        CategoryId = permission.CategoryId,
                        CategoryName = permission.Category?.CategoryName,
                        SystemId = permission.SystemId,
                        SystemName = permission.System?.SystemName,
                        ParentPermissionId = permission.ParentPermissionId,
                        ParentPermissionName = parentPermissionName,
                        IsActive = permission.IsActive,
                        ChildPermissionCount = childPermissionCount,
                        AssignedRoleTypeCount = assignedRoleTypeCount
                    });

                }

                return View(viewModels);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得權限列表時發生錯誤");
                TempData["Error"] = "取得權限列表時發生錯誤";
                return View(new List<UserPermissionListViewModel>());
            }

        }

        // GET: Admin/UserPermission/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var permission = await _context.UserPermissions
                    .Include(p => p.Category)
                    .Include(p => p.System)
                    .FirstOrDefaultAsync(p => p.PermissionId == id);

                if (permission == null)
                {
                    return NotFound();
                }

                var parentPermissionName = permission.ParentPermissionId.HasValue
                    ? await _context.UserPermissions
                        .Where(p => p.PermissionId == permission.ParentPermissionId.Value)
                        .Select(p => p.PermissionName)
                        .FirstOrDefaultAsync()
                    : null;

                var childPermissions = await _context.UserPermissions
                    .Where(p => p.ParentPermissionId == id)
                    .OrderBy(p => p.PermissionId)
                    .Select(p => new ChildPermissionInfo
                    {
                        PermissionId = p.PermissionId,
                        PermissionCode = p.PermissionCode,
                        PermissionName = p.PermissionName,
                        PermissionDescription = p.PermissionDescription,
                        IsActive = p.IsActive,
                    }).ToListAsync();

                var systems = await _context.UserSystemModules
                    .ToDictionaryAsync(s => s.SystemId, s => s.SystemName);

                var assignedRoleTypes = await _context.UserRolePermissions
                    .Where(rp => rp.PermissionId == id)
                    .Include(rp => rp.RoleType)
                    .Select(rp => new AssignedRoleTypeInfo
                    {
                        RoleTypeId = rp.RoleType.RoleTypeId,
                        RoleCode = rp.RoleType.RoleCode,
                        RoleName = rp.RoleType.RoleName,
                        RoleDescription = rp.RoleType.RoleDescription,
                        RoleLevel = rp.RoleType.RoleLevel,
                        SystemName = rp.RoleType.SystemId.HasValue && systems.ContainsKey(rp.RoleType.SystemId.Value)
                            ? systems[rp.RoleType.SystemId.Value]
                            : null,
                        IsActive = rp.RoleType.IsActive
                    })
                    .ToListAsync();

                var viewModel = new UserPermissionDetailsViewModel
                {
                    PermissionId = permission.PermissionId,
                    PermissionCode = permission.PermissionCode,
                    PermissionName = permission.PermissionName,
                    PermissionDescription = permission.PermissionDescription,
                    CategoryId = permission.CategoryId,
                    CategoryName = permission.Category?.CategoryName,
                    SystemId = permission.SystemId,
                    SystemName = permission.System?.SystemName,
                    ParentPermissionId = permission.ParentPermissionId,
                    ParentPermissionName = parentPermissionName,
                    IsActive = permission.IsActive,
                    ChildPermissions = childPermissions,
                    AssignedRoleTypes = assignedRoleTypes,
                    ChildPermissionCount = childPermissions.Count,
                    AssignedRoleTypeCount = assignedRoleTypes.Count
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得權限詳情時發生錯誤 PermissionId: {PermissionId}", id);
                TempData["Error"] = "取得權限詳情時發生錯誤";
                return RedirectToAction(nameof(Index));
            }

        }

        // GET: Admin/UserPermission/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            try
            {
                // 載入權限分類選單
                ViewBag.CategoryList = await _context.UserPermissionCategories
                    .Where(c => c.IsActive == true)
                    .OrderBy(c => c.CategoryName)
                    .Select(c => new { c.CategoryId, c.CategoryName })
                    .ToListAsync();

                // 載入系統模組選單
                ViewBag.SystemList = await _context.UserSystemModules
                    .Where(s => s.IsActive == true)
                    .OrderBy(s => s.SystemName)
                    .Select(s => new { s.SystemId, s.SystemName })
                    .ToListAsync();

                // 載入父權限選單（只顯示啟用中的權限）
                ViewBag.ParentPermissionList = await _context.UserPermissions
                    .Where(p => p.IsActive == true)
                    .OrderBy(p => p.PermissionName)
                    .Select(p => new { p.PermissionId, p.PermissionName })
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

        // POST: Admin/UserPermission/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("PermissionId,PermissionCode,PermissionName,PermissionDescription,CategoryId,SystemId,ParentPermissionId,IsActive")] UserPermission model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // 檢查權限代碼是否已存在
                    var existingPermission = await _context.UserPermissions
                        .FirstOrDefaultAsync(p => p.PermissionCode == model.PermissionCode);

                    if (existingPermission != null)
                    {
                        ModelState.AddModelError("PermissionCode", "權限代碼已存在");

                        // 重新載入下拉選單
                        await LoadDropdownLists();
                        return View(model);
                    }

                    // 建立新權限
                    var permission = new UserPermission
                    {
                        PermissionCode = model.PermissionCode,
                        PermissionName = model.PermissionName,
                        PermissionDescription = model.PermissionDescription,
                        CategoryId = model.CategoryId,
                        SystemId = model.SystemId,
                        ParentPermissionId = model.ParentPermissionId,
                        IsActive = model.IsActive
                    };

                    _context.UserPermissions.Add(permission);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("權限已建立 PermissionId: {PermissionId}, PermissionCode: {PermissionCode}",
                        permission.PermissionId, permission.PermissionCode);
                    TempData["Success"] = $"權限 {permission.PermissionName} 建立成功";

                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "建立權限時發生錯誤");
                    ModelState.AddModelError("", "建立權限時發生錯誤");
                }
            }

            // 重新載入下拉選單
            await LoadDropdownLists();
            return View(model);
        }

        // GET: Admin/UserPermission/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var permission = await _context.UserPermissions.FindAsync(id);
                if (permission == null)
                {
                    return NotFound();
                }

                // 計算關聯資料數量
                var childPermissionCount = await _context.UserPermissions
                    .CountAsync(p => p.ParentPermissionId == id);

                // 查詢擁有此權限的角色數量
                var assignedRoleTypeCount = await _context.UserRolePermissions
                    .Where(rp => rp.PermissionId == id)
                    .Select(rp => rp.RoleTypeId)
                    .Distinct()
                    .CountAsync();

                var viewModel = new UserPermissionEditViewModel
                {
                    PermissionId = permission.PermissionId,
                    PermissionCode = permission.PermissionCode,
                    PermissionName = permission.PermissionName,
                    PermissionDescription = permission.PermissionDescription,
                    CategoryId = permission.CategoryId,
                    SystemId = permission.SystemId,
                    ParentPermissionId = permission.ParentPermissionId,
                    IsActive = permission.IsActive,
                    ChildPermissionCount = childPermissionCount,
                    AssignedRoleTypeCount = assignedRoleTypeCount
                };

                // 載入下拉選單
                await LoadDropdownLists(id);

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "載入編輯頁面時發生錯誤 PermissionId: {PermissionId}", id);
                TempData["Error"] = "載入編輯頁面時發生錯誤";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/UserPermission/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("PermissionId,PermissionCode,PermissionName,PermissionDescription,CategoryId,SystemId,ParentPermissionId,IsActive")] UserPermission model)
        {
            if (id != model.PermissionId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // 檢查權限代碼是否被其他權限使用
                    var duplicatePermission = await _context.UserPermissions
                        .Where(p => p.PermissionId != id && p.PermissionCode == model.PermissionCode)
                        .FirstOrDefaultAsync();

                    if (duplicatePermission != null)
                    {
                        ModelState.AddModelError("PermissionCode", "權限代碼已被其他權限使用");

                        // 重新載入下拉選單
                        await LoadDropdownLists(id);
                        return View(model);
                    }

                    // 檢查是否會造成循環參照（父權限不能是自己或自己的子權限）
                    if (model.ParentPermissionId.HasValue)
                    {
                        if (model.ParentPermissionId.Value == id)
                        {
                            ModelState.AddModelError("ParentPermissionId", "父權限不能是自己");
                            await LoadDropdownLists(id);
                            return View(model);
                        }
                    }

                    var existingPermission = await _context.UserPermissions.FindAsync(id);
                    if (existingPermission == null)
                    {
                        return NotFound();
                    }

                    existingPermission.PermissionCode = model.PermissionCode;
                    existingPermission.PermissionName = model.PermissionName;
                    existingPermission.PermissionDescription = model.PermissionDescription;
                    existingPermission.CategoryId = model.CategoryId;
                    existingPermission.SystemId = model.SystemId;
                    existingPermission.ParentPermissionId = model.ParentPermissionId;
                    existingPermission.IsActive = model.IsActive;

                    await _context.SaveChangesAsync();

                    _logger.LogInformation("權限已更新 PermissionId: {PermissionId}, PermissionCode: {PermissionCode}",
                        model.PermissionId, model.PermissionCode);
                    TempData["Success"] = $"權限 {model.PermissionName} 更新成功";

                    return RedirectToAction(nameof(Details), new { id = model.PermissionId });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UserPermissionExists(model.PermissionId))
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
                    _logger.LogError(ex, "更新權限時發生錯誤 PermissionId: {PermissionId}", id);
                    ModelState.AddModelError("", "更新權限時發生錯誤");
                }
            }

            // 重新載入下拉選單
            await LoadDropdownLists(id);
            return View(model);
        }

        // GET: Admin/UserPermission/Delete/5
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var permission = await _context.UserPermissions
                    .Include(p => p.Category)
                    .Include(p => p.System)
                    .FirstOrDefaultAsync(p => p.PermissionId == id);

                if (permission == null)
                {
                    return NotFound();
                }

                // 取得父權限名稱
                var parentPermissionName = permission.ParentPermissionId.HasValue
                    ? await _context.UserPermissions
                        .Where(p => p.PermissionId == permission.ParentPermissionId.Value)
                        .Select(p => p.PermissionName)
                        .FirstOrDefaultAsync()
                    : null;

                // 計算關聯資料數量
                var childPermissionCount = await _context.UserPermissions
                    .CountAsync(p => p.ParentPermissionId == id);

                // 查詢擁有此權限的角色數量
                var assignedRoleTypeCount = await _context.UserRolePermissions
                    .Where(rp => rp.PermissionId == id)
                    .Select(rp => rp.RoleTypeId)
                    .Distinct()
                    .CountAsync();

                var viewModel = new UserPermissionDeleteViewModel
                {
                    PermissionId = permission.PermissionId,
                    PermissionCode = permission.PermissionCode,
                    PermissionName = permission.PermissionName,
                    PermissionDescription = permission.PermissionDescription,
                    CategoryId = permission.CategoryId,
                    CategoryName = permission.Category?.CategoryName,
                    SystemId = permission.SystemId,
                    SystemName = permission.System?.SystemName,
                    ParentPermissionId = permission.ParentPermissionId,
                    ParentPermissionName = parentPermissionName,
                    IsActive = permission.IsActive,
                    ChildPermissionCount = childPermissionCount,
                    AssignedRoleTypeCount = assignedRoleTypeCount
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "載入刪除確認頁面時發生錯誤 PermissionId: {PermissionId}", id);
                TempData["Error"] = "載入刪除確認頁面時發生錯誤";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/UserPermission/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var permission = await _context.UserPermissions.FindAsync(id);
                if (permission == null)
                {
                    return NotFound();
                }

                // 檢查是否有子權限
                var hasChildPermissions = await _context.UserPermissions
                    .AnyAsync(p => p.ParentPermissionId == id);

                if (hasChildPermissions)
                {
                    TempData["Error"] = "無法刪除：此權限仍有子權限，請先移除相關子權限";
                    return RedirectToAction(nameof(Delete), new { id });
                }

                _context.UserPermissions.Remove(permission);
                await _context.SaveChangesAsync();

                _logger.LogWarning("權限已刪除 PermissionId: {PermissionId}, PermissionCode: {PermissionCode}",
                    permission.PermissionId, permission.PermissionCode);
                TempData["Success"] = $"權限 {permission.PermissionName} 已刪除";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "刪除權限時發生錯誤 PermissionId: {PermissionId}", id);
                TempData["Error"] = "刪除權限時發生錯誤";
                return RedirectToAction(nameof(Index));
            }
        }

        private async Task LoadDropdownLists(int? excludePermissionId = null)
        {
            ViewBag.CategoryList = await _context.UserPermissionCategories
                .Where(c => c.IsActive == true)
                .OrderBy (c => c.CategoryName)
                .Select(c => new { c.CategoryId, c.CategoryName })
                .ToListAsync();

            // 載入系統模組選單
            ViewBag.SystemList = await _context.UserSystemModules
                .Where(s => s.IsActive == true)
                .OrderBy(s => s.SystemName)
                .Select(s => new { s.SystemId, s.SystemName })
                .ToListAsync();

            // 載入父權限選單（排除自己，避免循環參照）
            var parentPermissionQuery = _context.UserPermissions
                .Where(p => p.IsActive == true);

            if (excludePermissionId.HasValue)
            {
                parentPermissionQuery = parentPermissionQuery.Where(p => p.PermissionId != excludePermissionId.Value);
            }

            ViewBag.ParentPermissionList = await parentPermissionQuery
                .OrderBy(p => p.PermissionName)
                .Select(p => new { p.PermissionId, p.PermissionName })
                .ToListAsync();
        }

        private bool UserPermissionExists(int id)
        {
            return _context.UserPermissions.Any(e => e.PermissionId == id);
        }
    }
}
