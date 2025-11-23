using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Project_Photo.Models;

namespace Project_Photo.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class QuotaTypesController : Controller
    {
        private readonly AaContext _context;

        public QuotaTypesController(AaContext context)
        {
            _context = context;
        }

        // GET: Admin/QuotaTypes
        [HttpGet]
        public async Task<IActionResult> Index(string searchKeyword, int? systemId, bool? isActive)
        {
            ViewBag.SearchKeyword = searchKeyword;
            ViewBag.SystemId = systemId;
            ViewBag.IsActive = isActive;

            ViewBag.Systems = await _context.UserSystemModules
                .Where(s => s.IsActive)
                .Select(s => new SelectListItem
                {
                    Value = s.SystemId.ToString(),
                    Text = s.SystemName
                })
                .ToListAsync();

            var query = _context.PhotoQuotaTypes
                .Include(q => q.System)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchKeyword))
            {
                query = query.Where(q =>
                    q.QuotaTypeCode.Contains(searchKeyword) ||
                    q.QuotaTypeName.Contains(searchKeyword) ||
                    q.Description.Contains(searchKeyword));
            }

            // 狀態篩選
            if (isActive.HasValue)
            {
                query = query.Where(q => q.IsActive == isActive.Value);
            }

            var quotaTypes = await query
                .OrderByDescending(q => q.CreatedAt)
                .ToListAsync();

            // 統計資料
            ViewBag.TotalCount = await _context.PhotoQuotaTypes.CountAsync();
            ViewBag.ActiveCount = await _context.PhotoQuotaTypes.CountAsync(q => q.IsActive);
            ViewBag.InactiveCount = await _context.PhotoQuotaTypes.CountAsync(q => !q.IsActive);

            return View(quotaTypes);
        }

        // GET: Admin/QuotaTypes/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var quotaType = await _context.PhotoQuotaTypes
                .Include(q => q.System)
                .FirstOrDefaultAsync(m => m.QuotaTypeId == id);

            if (quotaType == null)
            {
                return NotFound();
            }

            var relatedPlans = await _context.PhotoSubscriptionQuota
                .Where(sq => sq.QuotaTypeId == id)
                .Include(sq => sq.Plan)
                .Select(sq => sq.Plan)
                .Distinct()
                .ToListAsync();

            ViewBag.RelatedPlans = relatedPlans;

            return View(quotaType);
        }

        // GET: Admin/QuotaTypes/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadSelectLists();
            return View();
        }

        // POST: Admin/QuotaTypes/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("QuotaTypeId,QuotaTypeCode,QuotaTypeName,QuotaUnit,Description,SystemId,ResetPeriod,IsActive,CreatedAt,UpdatedAt")] PhotoQuotaType model)
        {
            if (ModelState.IsValid)
            {
                var exists = await _context.PhotoQuotaTypes
                    .AnyAsync(q => q.QuotaTypeCode == model.QuotaTypeCode);

                if (exists)
                {
                    ModelState.AddModelError("QuotaTypeCode", "此配額類型代碼已存在");
                    await LoadSelectLists();
                    return View(model);
                }

                model.CreatedAt = DateTime.Now;
                model.UpdatedAt = DateTime.Now;

                _context.Add(model);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "配額類型已成功建立";
                return RedirectToAction(nameof(Index));
            }
            await LoadSelectLists();
            return View(model);
        }

        // GET: Admin/QuotaTypes/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var quotaType = await _context.PhotoQuotaTypes.FindAsync(id);
            if (quotaType == null)
            {
                return NotFound();
            }

            await LoadSelectLists(quotaType.SystemId);
            return View(quotaType);
        }

        // POST: Admin/QuotaTypes/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("QuotaTypeId,QuotaTypeCode,QuotaTypeName,QuotaUnit,Description,SystemId,ResetPeriod,IsActive,CreatedAt,UpdatedAt")] PhotoQuotaType model)
        {
            if (id != model.QuotaTypeId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // 檢查代碼是否與其它記錄重複
                    var exists = await _context.PhotoQuotaTypes
                        .AnyAsync(q => q.QuotaTypeCode == model.QuotaTypeCode && q.QuotaTypeId != id);

                    if (exists)
                    {
                        ModelState.AddModelError("QuotaTypeCode", "此配額類型代碼已存在");
                        await LoadSelectLists(model.SystemId);
                        return View(model);
                    }

                    model.UpdatedAt = DateTime.Now;

                    _context.Update(model);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "配額類型已成功更新";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PhotoQuotaTypeExists(model.QuotaTypeId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            await LoadSelectLists(model.SystemId);
            return View(model);
        }

        // GET: Admin/QuotaTypes/Delete/5
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            var quotaType = await _context.PhotoQuotaTypes.FindAsync(id);
            if (quotaType == null)
            {
                return Json(new { success = false, message = "配額類型不存在" });
            }

            // 檢查是否有方案正在使用此配額類型
            var isUsed = await _context.PhotoSubscriptionQuota
                .AnyAsync(sq => sq.QuotaTypeId == id);

            if (isUsed)
            {
                return Json(new { success = false, message = "此配額類型正在被方案使用,無法刪除" });
            }

            try
            {
                _context.PhotoQuotaTypes.Remove(quotaType);
                await _context.SaveChangesAsync();
                return Json(new { success = false, message = "配額類型已成功刪除" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"刪除失敗:{ex.Message}" });
            }

        }

        // POST: Admin/QuotaTypes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var photoQuotaType = await _context.PhotoQuotaTypes.FindAsync(id);
            if (photoQuotaType != null)
            {
                _context.PhotoQuotaTypes.Remove(photoQuotaType);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PhotoQuotaTypeExists(int id)
        {
            return _context.PhotoQuotaTypes.Any(e => e.QuotaTypeId == id);
        }

        private async Task<IActionResult> ToggleStatus(int id)
        {
            var quotaType = await _context.PhotoQuotaTypes.FindAsync(id);
            if (quotaType == null)
            {
                return Json(new { success = false, message = "配額類型不存在" });
            }

            try
            {
                quotaType.IsActive = !quotaType.IsActive;
                quotaType.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();

                var status = quotaType.IsActive ? "啟用" : "停用";
                return Json(new { success = true, message = $"已{status}配額類型", isActive = quotaType.IsActive });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"操作失敗: {ex.Message}" });
            }
        }

        private async Task LoadSelectLists(int? selectedSystemId = null)
        {
            ViewBag.Systems = await _context.UserSystemModules
                .Where(s => s.IsActive)
                .Select(s => new SelectListItem
                {
                    Value = s.SystemId.ToString(),
                    Text = s.SystemName,
                    Selected = selectedSystemId.HasValue && s.SystemId == selectedSystemId.Value
                })
                .ToListAsync();

            ViewBag.QuotaUnits = new SelectList(new[]
            {
                new { Value = "GB", Text = "GB (Gigabyte)" },
                new { Value = "MB", Text = "MB (Megabyte)" },
                new { Value = "次", Text = "次" },
                new { Value = "張", Text = "張" },
                new { Value = "分鐘", Text = "分鐘" },
                new { Value = "小時", Text = "小時" },
                new { Value = "個", Text = "個" }
            }, "Value", "Text");

            // 重製週期下拉選單
            ViewBag.ResetPeriods = new SelectList(new[]
            {
                new { Value = "Month", Text = "每月重置" },
                new { Value = "Year", Text = "每年重置" },
                new { Value = "Never", Text = "永不重置" }
            }, "Value", "Text");
        }
    }
}
