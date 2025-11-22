using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Project_Photo.Areas.Admin.ViewModels.PermissionCategory;
using Project_Photo.Models;

namespace Project_Photo.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class PermissionCategoryController : Controller
    {
        private readonly AaContext _context;
        private readonly ILogger<PermissionCategoryController> _logger;

        public PermissionCategoryController(AaContext context, ILogger<PermissionCategoryController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Admin/PermissionCategory
        public async Task<IActionResult> Index()
        {
            try
            {
                var categories = await _context.UserPermissionCategories
                    .OrderBy(c => c.CategoryId)
                    .ToListAsync();

                var viewModels = new List<PermissionCategoryListViewModel>();

                foreach (var category in categories)
                {
                    var permissionCount = await _context.UserPermissions
                        .CountAsync(p => p.CategoryId == category.CategoryId && p.IsActive == true);

                    viewModels.Add(new PermissionCategoryListViewModel
                    {
                        CategoryId = category.CategoryId,
                        CategoryCode = category.CategoryCode,
                        CategoryName = category.CategoryName,
                        CategoryDescription = category.CategoryDescription,
                        IsActive = category.IsActive,
                        CreatedAt = category.CreatedAt,
                        UpdatedAt = category.UpdatedAt,
                        PermissionCount = permissionCount
                    });
                }

                return View(viewModels);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得權限分類列表時發生錯誤");
                TempData["Error"] = "取得權限分類列表時發生錯誤";
                return View(new List<PermissionCategoryListViewModel>());
            }
        }

        // GET: Admin/PermissionCategory/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var category = await _context.UserPermissionCategories
                .FirstOrDefaultAsync(m => m.CategoryId == id);

                if (category == null)
                {
                    return NotFound();
                }

                var permissionsData = await _context.UserPermissions
                    .Where(p => p.CategoryId == id)
                    .Include(p => p.System)
                    .OrderBy(p => p.SystemId)
                    .ThenBy(p => p.PermissionId)
                    .ToListAsync();

                var permissions = permissionsData.Select(p => new CategoryPermissionInfo
                {
                    PermissionId = p.PermissionId,
                    PermissionCode = p.PermissionCode,
                    PermissionName = p.PermissionName,
                    PermissionDescription = p.PermissionDescription,
                    SystemName = p.System != null ? p.System.SystemName : null,
                    ParentPermissionId = p.ParentPermissionId,
                    ParentPermissionName = p.ParentPermissionId.HasValue
                        ? permissionsData.FirstOrDefault(parent => parent.PermissionId == p.ParentPermissionId.Value)?.PermissionName
                        : null,
                    IsActive = p.IsActive
                }).ToList();

                var viewModel = new PermissionCategoryDetailsViewModel
                {
                    CategoryId = category.CategoryId,
                    CategoryCode = category.CategoryCode,
                    CategoryName = category.CategoryName,
                    CategoryDescription = category.CategoryDescription,
                    IsActive = category.IsActive,
                    CreatedAt = category.CreatedAt,
                    UpdatedAt = category.UpdatedAt,
                    PermissionCount = permissions.Count,
                    Permissions = permissions
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得權限分類詳情時發生錯誤 CategoryId: {CategoryId}", id);
                TempData["Error"] = "取得權限分類詳情時發生錯誤";
                return RedirectToAction(nameof(Index));
            }

        }

        // GET: Admin/PermissionCategory/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/PermissionCategory/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserPermissionCategory model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // 檢查分類代碼是否已存在
                    var existingCategory = await _context.UserPermissionCategories
                        .FirstOrDefaultAsync(c => c.CategoryCode == model.CategoryCode);

                    if (existingCategory != null)
                    {
                        ModelState.AddModelError("CategoryCode", "分類代碼已存在");
                        return View(model);
                    }

                    model.CreatedAt = DateTime.Now;
                    model.UpdatedAt = DateTime.Now;

                    _context.UserPermissionCategories.Add(model);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("權限分類已建立 CategoryId: {CategoryId}, CategoryCode: {CategoryCode}",
                        model.CategoryId, model.CategoryCode);
                    TempData["Success"] = $"權限分類 {model.CategoryName} 建立成功";

                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "建立權限分類時發生錯誤");
                    ModelState.AddModelError("", "建立權限分類時發生錯誤");
                }
            }

            return View(model);
        }

        // GET: Admin/PermissionCategory/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var category = await _context.UserPermissionCategories.FindAsync(id);
                if (category == null)
                {
                    return NotFound();
                }
                return View(category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "載入編輯頁面時發生錯誤 CategoryId: {CategoryId}", id);
                TempData["Error"] = "載入編輯頁面時發生錯誤";
                return RedirectToAction(nameof(Index));
            }


        }

        // POST: Admin/PermissionCategory/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("CategoryId,CategoryCode,CategoryName,CategoryDescription,IsActive,CreatedAt,UpdatedAt")] UserPermissionCategory model)
        {
            if (id != model.CategoryId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var duplicateCategory = await _context.UserPermissionCategories
                        .Where(c => c.CategoryId != id && c.CategoryCode == model.CategoryCode)
                        .FirstOrDefaultAsync();

                    if (duplicateCategory != null)
                    {
                        ModelState.AddModelError("CategoryCode", "分類代碼已被其他分類使用");
                        return View(model);
                    }

                    var existingCategory = await _context.UserPermissionCategories.FindAsync(id);
                    if (existingCategory == null)
                    {
                        return NotFound();
                    }

                    existingCategory.CategoryCode = model.CategoryCode;
                    existingCategory.CategoryName = model.CategoryName;
                    existingCategory.CategoryDescription = model.CategoryDescription;
                    existingCategory.IsActive = model.IsActive;
                    existingCategory.UpdatedAt = DateTime.Now;

                    await _context.SaveChangesAsync();

                    _logger.LogInformation("權限分類已更新 CategoryId: {CategoryId}, CategoryCode: {CategoryCode}",
                        model.CategoryId, model.CategoryCode);
                    TempData["Success"] = $"權限分類 {model.CategoryName} 更新成功";

                    return RedirectToAction(nameof(Details), new { id = model.CategoryId });

                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UserPermissionCategoryExists(model.CategoryId))
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
                    _logger.LogError(ex, "更新權限分類時發生錯誤 CategoryId: {CategoryId}", id);
                    ModelState.AddModelError("", "更新權限分類時發生錯誤");
                }
            }

            return View(model);
        }

        // GET: Admin/PermissionCategory/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var category = await _context.UserPermissionCategories
                    .FirstOrDefaultAsync(c => c.CategoryId == id);

                if (category == null)
                {
                    return NotFound();
                }

                var permissionCount = await _context.UserPermissions
                    .CountAsync(p => p.CategoryId == id);

                var viewModel = new PermissionCategoryDeleteViewModel
                {
                    CategoryId = category.CategoryId,
                    CategoryCode = category.CategoryCode,
                    CategoryName = category.CategoryName,
                    CategoryDescription = category.CategoryDescription,
                    IsActive = category.IsActive,
                    CreatedAt = category.CreatedAt,
                    PermissionCount = permissionCount
                };
                
                return View(viewModel);
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, "載入刪除確認頁面時發生錯誤 CategoryId: {CategoryId}", id);
                TempData["Error"] = "載入刪除確認頁面時發生錯誤";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/PermissionCategory/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var category = await _context.UserPermissionCategories.FindAsync(id);
                if (category == null)
                {
                    return NotFound();
                }

                var hasPermissions = await _context.UserPermissions.AnyAsync(p => p.CategoryId == id);

                if (hasPermissions)
                {
                    TempData["Error"] = "無法刪除: 此權限分類仍有關聯的權限，請先移除相關權限";
                    return RedirectToAction(nameof(Delete), new { id });
                }

                _context.UserPermissionCategories.Remove(category);
                await _context.SaveChangesAsync();

                _logger.LogWarning("權限分類已刪除 CategoryId: {CategoryId}, CategoryCode: {CategoryCode}",
                    category.CategoryId, category.CategoryCode);
                TempData["Success"] = $"權限分類 {category.CategoryName} 已刪除";

                return RedirectToAction(nameof(Index));
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, "刪除權限分類時發生錯誤 CategoryId: {CategoryId}", id);
                TempData["Error"] = "刪除權限分類時發生錯誤";
                return RedirectToAction(nameof(Index));
            }
            
        }

        private bool UserPermissionCategoryExists(int id)
        {
            return _context.UserPermissionCategories.Any(e => e.CategoryId == id);
        }
    }
}