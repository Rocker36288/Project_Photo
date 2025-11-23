using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project_Photo.Data;
using Project_Photo.Models;
using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;


namespace Project_Photo.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class PhotoSubscriptionPlanController : Controller
    {
        private readonly AaContext _context;
        private readonly ILogger<UserManagementController> _logger;

        public PhotoSubscriptionPlanController(AaContext context, ILogger<UserManagementController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: PhotoSubscription/Index
        [HttpGet]
        public async Task<IActionResult> Index(string searchTerm, int? planLevel, bool? isActive, int page = 1, int pageSize = 10)
        {
            try
            {
                _logger.LogInformation("查詢訂閱方案列表 - 搜尋條件: {SearchTerm}, 等級: {PlanLevel}, 狀態: {IsActive}, 頁碼: {Page}",
                    searchTerm, planLevel, isActive, page);

                var query = _context.PhotoSubscriptionPlans
                    .Include(p => p.System)
                    .AsQueryable();

                // 搜尋條件
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    query = query.Where(p =>
                        p.PlanName.Contains(searchTerm) ||
                        p.PlanCode.Contains(searchTerm) ||
                        p.PlanDescription.Contains(searchTerm));
                }

                // 方案等級篩選
                if (planLevel.HasValue)
                {
                    query = query.Where(p => p.PlanLevel == planLevel.Value);
                }

                // 啟用狀態篩選
                if (isActive.HasValue)
                {
                    query = query.Where(p => p.IsActive == isActive.Value);
                }

                query = query.OrderBy(p => p.PlanLevel).ThenBy(p => p.PlanName);

                var totalCount = await query.CountAsync();

                var plans = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // 傳遞分頁資訊到 ViewBag
                ViewBag.CurrentPage = page;
                ViewBag.PageSize = pageSize;
                ViewBag.TotalCount = totalCount;
                ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
                ViewBag.SearchTerm = searchTerm;
                ViewBag.PlanLevel = planLevel;
                ViewBag.IsActive = isActive;

                _logger.LogInformation("成功查詢訂閱方案列表 - 共 {TotalCount} 筆資料", totalCount);

                return View(plans);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查詢訂閱方案列表時發生錯誤");
                TempData["Error"] = "查詢訂閱方案時發生錯誤";
                return View(new List<PhotoSubscriptionPlan>());
            }
            
        }

        // GET: PhotoSubscription/PlanDetails/5
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var plan = await _context.PhotoSubscriptionPlans
                .Include(p => p.System)
                .FirstOrDefaultAsync(p => p.PlanId == id);

                if (plan == null)
                {
                    _logger.LogWarning("找不到訂閱方案 - PlanId: {PlanId}", id);
                    TempData["Error"] = "找不到指定的訂閱方案";
                    return RedirectToAction(nameof(Index));
                }

                _logger.LogInformation("成功查詢訂閱方案詳細資訊 - PlanId: {PlanId}, PlanName: {PlanName}", id, plan.PlanName);

                return View(plan);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查詢訂閱方案詳細資訊時發生錯誤 - PlanId: {PlanId}", id);
                TempData["Error"] = "查詢訂閱方案詳細資訊時發生錯誤";
                return RedirectToAction(nameof(Index));
            }
            
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            try
            {
                _logger.LogInformation("開啟新增訂月方案頁面");

                ViewBag.Systems = await _context.UserSystemModules
                    .Where(s => s.IsActive)
                    .OrderBy(s => s.SystemName)
                    .ToArrayAsync();

                return View();
            } 
            catch (Exception ex)
            {
                _logger.LogError(ex, "載入新增訂閱方案頁面時發生錯誤");
                TempData["Error"] = "載入頁面時發生錯誤";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PhotoSubscriptionPlan model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    _logger.LogInformation("開始新增訂閱方案 - PlanCode: {PlanCode}, PlanName: {PlanName}",
                        model.PlanCode, model.PlanName);

                    // 檢查方案代碼是否重複
                    var existingPlan = await _context.PhotoSubscriptionPlans
                        .FirstOrDefaultAsync(p => p.PlanCode == model.PlanCode);


                    if (existingPlan != null)
                    {
                        _logger.LogWarning("方案代碼已存在 - PlanCode: {PlanCode}", model.PlanCode);
                        ModelState.AddModelError("PlanCode", "此方案代碼已存在");
                        ViewBag.Systems = await _context.UserSystemModules
                            .Where(s => s.IsActive)
                            .OrderBy(s => s.SystemName)
                            .ToListAsync();
                        return View(model);
                    }

                    model.CreatedAt = DateTime.Now;
                    model.UpdateAt = DateTime.Now;

                    _context.PhotoSubscriptionPlans.Add(model);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("成功新增訂閱方案 - PlanId: {PlanId}, PlanCode: {PlanCode}, PlanName: {PlanName}",
                        model.PlanId, model.PlanCode, model.PlanName);

                    TempData["Success"] = "訂閱方案新增成功";
                    return RedirectToAction(nameof(Index));
                }

                _logger.LogWarning("新增訂閱方案驗證失敗 - ModelState 錯誤: {Errors}",
                    string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));

                ViewBag.Systems = await _context.UserSystemModules
                    .Where(s => s.IsActive)
                    .OrderBy(s => s.SystemName)
                    .ToListAsync();

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "新增訂閱方案時發生錯誤 - PlanCode: {PlanCode}", model?.PlanCode);
                TempData["Error"] = "新增訂閱方案時發生錯誤";

                ViewBag.Systems = await _context.UserSystemModules
                    .Where(s => s.IsActive)
                    .OrderBy(s => s.SystemName)
                    .ToListAsync();

                return View(model);
            }
        }

        // GET: Admin/PhotoSubscriptionPlan/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                _logger.LogInformation("開啟編輯訂閱方案頁面 - PlanId: {PlanId}", id);

                var plan = await _context.PhotoSubscriptionPlans
                    .FirstOrDefaultAsync(p => p.PlanId == id);

                if (plan == null)
                {
                    _logger.LogWarning("找不到訂閱方案 - PlanId: {PlanId}", id);
                    TempData["Error"] = "找不到指定的訂閱方案";
                    return RedirectToAction(nameof(Index));
                }

                // 載入系統模組列表供選擇
                ViewBag.Systems = await _context.UserSystemModules
                    .Where(s => s.IsActive)
                    .OrderBy(s => s.SystemName)
                    .ToListAsync();

                _logger.LogInformation("成功載入編輯訂閱方案頁面 - PlanId: {PlanId}, PlanName: {PlanName}",
                    id, plan.PlanName);

                return View(plan);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "載入編輯訂閱方案頁面時發生錯誤 - PlanId: {PlanId}", id);
                TempData["Error"] = "載入頁面時發生錯誤";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/PhotoSubscriptionPlan/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, PhotoSubscriptionPlan model)
        {
            try
            {
                if (id != model.PlanId)
                {
                    _logger.LogWarning("編輯訂閱方案 ID 不符 - URL PlanId: {UrlId}, Model PlanId: {ModelId}",
                        id, model.PlanId);
                    TempData["Error"] = "方案 ID 不符";
                    return RedirectToAction(nameof(Index));
                }

                if (ModelState.IsValid)
                {
                    _logger.LogInformation("開始更新訂閱方案 - PlanId: {PlanId}, PlanCode: {PlanCode}, PlanName: {PlanName}",
                        model.PlanId, model.PlanCode, model.PlanName);

                    var existingPlan = await _context.PhotoSubscriptionPlans
                        .AsNoTracking()
                        .FirstOrDefaultAsync(p => p.PlanId == id);

                    if (existingPlan == null)
                    {
                        _logger.LogWarning("找不到要更新的訂閱方案 - PlanId: {PlanId}", id);
                        TempData["Error"] = "找不到指定的訂閱方案";
                        return RedirectToAction(nameof(Index));
                    }

                    // 檢查方案代碼是否與其他方案重複
                    var duplicatePlan = await _context.PhotoSubscriptionPlans
                        .FirstOrDefaultAsync(p => p.PlanCode == model.PlanCode && p.PlanId != id);

                    if (duplicatePlan != null)
                    {
                        _logger.LogWarning("方案代碼已存在於其他方案 - PlanCode: {PlanCode}, ExistingPlanId: {ExistingPlanId}",
                            model.PlanCode, duplicatePlan.PlanId);
                        ModelState.AddModelError("PlanCode", "此方案代碼已被其他方案使用");

                        ViewBag.Systems = await _context.UserSystemModules
                            .Where(s => s.IsActive)
                            .OrderBy(s => s.SystemName)
                            .ToListAsync();

                        return View(model);
                    }

                    // 保留原始的建立時間
                    model.CreatedAt = existingPlan.CreatedAt;
                    model.UpdateAt = DateTime.Now;

                    _context.PhotoSubscriptionPlans.Update(model);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("成功更新訂閱方案 - PlanId: {PlanId}, PlanCode: {PlanCode}, PlanName: {PlanName}",
                        model.PlanId, model.PlanCode, model.PlanName);

                    TempData["Success"] = "訂閱方案更新成功";
                    return RedirectToAction(nameof(Details), new { id = model.PlanId });
                }

                _logger.LogWarning("更新訂閱方案驗證失敗 - PlanId: {PlanId}, ModelState 錯誤: {Errors}",
                    id, string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));

                ViewBag.Systems = await _context.UserSystemModules
                    .Where(s => s.IsActive)
                    .OrderBy(s => s.SystemName)
                    .ToListAsync();

                return View(model);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "更新訂閱方案時發生並發衝突 - PlanId: {PlanId}", id);

                if (!await _context.PhotoSubscriptionPlans.AnyAsync(p => p.PlanId == id))
                {
                    TempData["Error"] = "訂閱方案已被刪除";
                }
                else
                {
                    TempData["Error"] = "更新失敗，資料已被其他使用者修改，請重新整理後再試";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新訂閱方案時發生錯誤 - PlanId: {PlanId}", id);
                TempData["Error"] = "更新訂閱方案時發生錯誤";

                ViewBag.Systems = await _context.UserSystemModules
                    .Where(s => s.IsActive)
                    .OrderBy(s => s.SystemName)
                    .ToListAsync();

                return View(model);
            }
        }

        // POST: Admin/PhotoSubscriptionPlan/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                _logger.LogInformation("開始刪除訂閱方案 - PlanId: {PlanId}", id);

                var plan = await _context.PhotoSubscriptionPlans.FindAsync(id);
                if (plan == null)
                {
                    _logger.LogWarning("找不到要刪除的訂閱方案 - PlanId: {PlanId}", id);
                    TempData["Error"] = "找不到指定的訂閱方案";
                    return RedirectToAction(nameof(Index));
                }

                // 檢查是否有用戶訂閱此方案
                var hasSubscriptions = await _context.PhotoUserSubscriptions
                    .AnyAsync(s => s.PlanId == id);

                if (hasSubscriptions)
                {
                    _logger.LogWarning("無法刪除訂閱方案，已有用戶訂閱 - PlanId: {PlanId}, PlanName: {PlanName}",
                        id, plan.PlanName);
                    TempData["Error"] = "此方案已有用戶訂閱，無法刪除";
                    return RedirectToAction(nameof(Details), new { id });
                }

                // 檢查是否有配額設定
                var hasQuotas = await _context.PhotoSubscriptionQuota
                    .AnyAsync(q => q.PlanId == id);

                if (hasQuotas)
                {
                    _logger.LogInformation("刪除方案前先刪除配額設定 - PlanId: {PlanId}", id);
                    var quotas = await _context.PhotoSubscriptionQuota
                        .Where(q => q.PlanId == id)
                        .ToListAsync();
                    _context.PhotoSubscriptionQuota.RemoveRange(quotas);
                }

                var planCode = plan.PlanCode;
                var planName = plan.PlanName;

                _context.PhotoSubscriptionPlans.Remove(plan);
                await _context.SaveChangesAsync();

                _logger.LogInformation("成功刪除訂閱方案 - PlanId: {PlanId}, PlanCode: {PlanCode}, PlanName: {PlanName}",
                    id, planCode, planName);

                TempData["Success"] = $"訂閱方案「{planName}」已成功刪除";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "刪除訂閱方案時發生錯誤 - PlanId: {PlanId}", id);
                TempData["Error"] = "刪除訂閱方案時發生錯誤";
                return RedirectToAction(nameof(Index));
            }
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            try
            {
                _logger.LogInformation("切換訂閱方案啟用狀態 - PlanId: {PlanId}", id);

                var plan = await _context.PhotoSubscriptionPlans.FindAsync(id);
                if (plan == null)
                {
                    _logger.LogWarning("找不到訂閱方案 - PlanId: {PlanId}", id);
                    TempData["Error"] = "找不到指定的訂閱方案";
                    return RedirectToAction(nameof(Index));
                }

                var oldStatus = plan.IsActive;
                plan.IsActive = !plan.IsActive;
                plan.UpdateAt = DateTime.Now;
                await _context.SaveChangesAsync();

                _logger.LogInformation("成功切換訂閱方案啟用狀態 - PlanId: {PlanId}, PlanName: {PlanName}, 從 {OldStatus} 變更為 {NewStatus}",
                    id, plan.PlanName, oldStatus, plan.IsActive);

                TempData["Success"] = $"訂閱方案已{(plan.IsActive ? "啟用" : "停用")}";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "切換訂閱方案啟用狀態時發生錯誤 - PlanId: {PlanId}", id);
                TempData["Error"] = "切換狀態時發生錯誤";
                return RedirectToAction(nameof(Index));
            }
            
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TogglePublic(int id)
        {
            try
            {
                _logger.LogInformation("切換訂閱方案公開狀態 - PlanId: {PlanId}", id);

                var plan = await _context.PhotoSubscriptionPlans.FindAsync(id);
                if (plan == null)
                {
                    _logger.LogWarning("找不到訂閱方案 - PlanId: {PlanId}", id);
                    TempData["Error"] = "找不到指定的訂閱方案";
                    return RedirectToAction(nameof(Index));
                }

                var oldStatus = plan.IsPublic;
                plan.IsPublic = !plan.IsPublic;
                plan.UpdateAt = DateTime.Now;
                await _context.SaveChangesAsync();

                _logger.LogInformation("成功切換訂閱方案公開狀態 - PlanId: {PlanId}, PlanName: {PlanName}, 從 {OldStatus} 變更為 {NewStatus}",
                    id, plan.PlanName, oldStatus, plan.IsPublic);

                TempData["Success"] = $"訂閱方案已設為{(plan.IsPublic ? "公開" : "不公開")}";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "切換訂閱方案公開狀態時發生錯誤 - PlanId: {PlanId}", id);
                TempData["Error"] = "切換狀態時發生錯誤";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
