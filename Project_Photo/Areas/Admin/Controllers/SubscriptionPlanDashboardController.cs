using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project_Photo.Models;

namespace Project_Photo.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class SubscriptionPlanDashboardController : Controller
    {
        private readonly AaContext _context;
        private readonly ILogger<SubscriptionPlanDashboardController> _logger;

        public SubscriptionPlanDashboardController(AaContext context, ILogger<SubscriptionPlanDashboardController> logger) 
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                _logger.LogInformation("載入訂閱方案儀表板");

                // 統計資料
                var totalPlans = await _context.PhotoSubscriptionPlans.CountAsync();
                var activePlans = await _context.PhotoSubscriptionPlans.CountAsync(p => p.IsActive);
                var publicPlans = await _context.PhotoSubscriptionPlans.CountAsync(p => p.IsPublic);
                var totalSubscriptions = await _context.PhotoSubscriptionPlans.CountAsync();

                var totalQuotaTypes = await _context.PhotoQuotaTypes.CountAsync();
                var activeQuotaTypes = await _context.PhotoQuotaTypes.CountAsync(q => q.IsActive);

                // 各等級方案數量
                var plansByLevel = await _context.PhotoSubscriptionPlans
                    .GroupBy(p => p.PlanLevel)
                    .Select(g => new
                    {
                        Level = g.Key,
                        Count = g.Count()
                    })
                    .OrderBy(x => x.Level)
                    .ToListAsync();

                var recentPlans = await _context.PhotoSubscriptionPlans
                    .Include(p => p.System)
                    .OrderByDescending(p => p.UpdateAt)
                    .Take(5)
                    .ToListAsync();

                var systemStats = await _context.PhotoSubscriptionPlans
                    .Include(p => p.System)
                    .GroupBy(p => p.System)
                    .Select(g => new
                    {
                        SystemName = g.Key.SystemName,
                        SystemCode = g.Key.SystemCode,
                        Count = g.Count(),
                        ActiveCount = g.Count(p => p.IsActive)
                    })
                    .ToListAsync();

                // 傳遞資料到 ViewBag
                ViewBag.TotalPlans = totalPlans;
                ViewBag.ActivePlans = activePlans;
                ViewBag.PublicPlans = publicPlans;
                ViewBag.TotalSubscriptions = totalSubscriptions;
                ViewBag.PlansByLevel = plansByLevel;
                ViewBag.SystemStats = systemStats;

                ViewBag.TotalQuotaTypes = totalQuotaTypes;
                ViewBag.ActiveQuotaTypes = activeQuotaTypes;

                _logger.LogInformation("成功載入訂閱方案儀表板 - 總方案數: {TotalPlans}", totalPlans);

                return View(recentPlans);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "載入訂閱方案儀表板時發生錯誤");
                TempData["Error"] = "載入儀表板時發生錯誤";
                return View(new List<PhotoSubscriptionPlan>());
            }
        }
    }
}
