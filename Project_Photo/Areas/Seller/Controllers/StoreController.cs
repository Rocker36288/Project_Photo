using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project_Photo.Models;
using System.Linq;
using System.Threading.Tasks;

namespace Project_Photo.Areas.Seller.Controllers
{
    [Area("Seller")]
    public class StoreController : Controller
    {
        private readonly AAContext _db;
        private readonly ILogger<StoreController> _logger;

        public StoreController(AAContext db, ILogger<StoreController> logger)
        {
            _db = db;
            _logger = logger;
        }

        // 賣場首頁
        public async Task<IActionResult> Index(long? userId)
        {
            long sellerId;

            // ✅ 優先從 Session 取得登入的會員 ID（使用 UserId）
            var sessionUserId = HttpContext.Session.GetInt32("UserId");

            if (sessionUserId.HasValue)
            {
                // 從 Session 取得（已登入用戶查看自己的賣場）
                sellerId = sessionUserId.Value;
                _logger.LogInformation($"從 Session 讀取到 UserId: {sellerId}");
            }
            else if (userId.HasValue)
            {
                // 從參數取得（用於查看其他賣家的賣場）
                sellerId = userId.Value;
                _logger.LogInformation($"從參數讀取到 UserId: {sellerId}");
            }
            else
            {
                // 未登入且沒有指定賣家，導向登入頁
                _logger.LogWarning("未登入且無指定賣家，導向登入頁");
                TempData["ErrorMessage"] = "請先登入";
                return RedirectToAction("Login", "UserSessions", new { area = "" });
            }

            // 獲取賣家資訊（包含 UserProfile）
            var seller = await _db.Users
                .Include(u => u.UserProfile)
                .FirstOrDefaultAsync(u => u.UserId == sellerId);

            if (seller == null)
            {
                _logger.LogWarning($"找不到賣家資料: UserId={sellerId}");
                TempData["ErrorMessage"] = "找不到賣家資料";
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            // 獲取賣家的商品（只顯示上架中的）
            var products = await _db.Products
                .Where(p => p.UserId == sellerId && p.Status == "上架中")
                .Include(p => p.ProductSpecifications)
                .Include(p => p.ProductImages)
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new
                {
                    p.ProductId,
                    p.ProductName,
                    Price = p.ProductSpecifications.Any()
                        ? p.ProductSpecifications.Min(s => s.Price)
                        : 0,
                    StockQuantity = p.ProductSpecifications.Sum(s => s.StockQuantity),
                    MainImageUrl = p.ProductImages
                        .OrderByDescending(img => img.IsMainImage)
                        .ThenBy(img => img.DisplayOrder)
                        .Select(img => img.ImageUrl)
                        .FirstOrDefault()
                })
                .ToListAsync();

            // 傳遞資料到 View
            ViewBag.SellerAccount = seller.Account ?? "賣家";
            ViewBag.SellerName = seller.UserProfile?.DisplayName ?? seller.Account ?? "賣家";

            // ✅ 取得頭像（統一路徑格式）
            var avatarFileName = seller.UserProfile?.Avatar;
            ViewBag.SellerAvatar = !string.IsNullOrEmpty(avatarFileName)
                ? $"/img/headphoto/{avatarFileName}"
                : "/img/headphoto/default.jpg";

            ViewBag.TotalProducts = products.Count;
            ViewBag.Products = products;

            // ✅ 判斷是否為自己的賣場
            ViewBag.IsOwnStore = (sessionUserId.HasValue && sessionUserId.Value == sellerId);

            _logger.LogInformation($"賣場頁面載入成功 - 賣家: {ViewBag.SellerAccount}, 商品數: {products.Count}, 是否本人: {ViewBag.IsOwnStore}");

            return View();
        }
    }
}