using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Project_Photo.Models;
using Project_Photo.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Project_Photo.Areas.Seller.Controllers
{
    [Area("Seller")]
    public class MyProductsController : Controller
    {
        private readonly AAContext _db;
        private readonly IWebHostEnvironment _hostingEnvironment;

        public MyProductsController(AAContext db, IWebHostEnvironment hostingEnvironment)
        {
            _db = db;
            _hostingEnvironment = hostingEnvironment;
        }

        // ======================================
        // A. Index (商品列表) - 【最終修正：移除 Include，使用子查詢獲取價格庫存】
        // ======================================

        public async Task<IActionResult> Index(int page = 1, string search = "", string status = "全部")
        {
            long currentUserId = 9;
            int pageSize = 5;

            var query = _db.Products
                .Where(p => p.UserId == currentUserId)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(p => p.ProductName.Contains(search));
            }
            if (status != "全部" && !string.IsNullOrEmpty(status))
            {
                query = query.Where(p => p.Status == status);
            }

            var totalProducts = await query.CountAsync();
            var skipCount = (page - 1) * pageSize;

            var productViewModels = await query.OrderByDescending(p => p.CreatedAt)
                .Skip(skipCount)
                .Take(pageSize)
                .Select(p => new SellerProductViewModel
                {
                    ProductId = p.ProductId,
                    ProductName = p.ProductName,
                    Status = p.Status,
                    AuditStatus = p.AuditStatus,

                    Price = p.ProductSpecifications.Any()
                        ? p.ProductSpecifications.Min(s => s.Price)
                        : 0,

                    StockQuantity = p.ProductSpecifications.Sum(s => s.StockQuantity),

                    // **修正：如果沒有圖片，直接設為 null，不使用 default.png**
                    MainImageUrl = p.ProductImages
                        .OrderByDescending(img => img.IsMainImage)
                        .ThenBy(img => img.DisplayOrder)
                        .Select(img => img.ImageUrl)
                        .FirstOrDefault(), // 移除 ?? "/img/default.png"

                    Views = (int)(p.ProductId * 100 / 10),
                    SalesCount = (int)(p.ProductId * 5)
                })
                .ToListAsync();

            var viewModel = new SellerProductListViewModel
            {
                Products = productViewModels,
                TotalProducts = totalProducts,
                PageSize = pageSize,
                CurrentPage = page,
                SearchQuery = search,
                SelectedStatus = status
            };

            return View(viewModel);
        }

        // ======================================
        // B. 輔助方法：獲取分類列表 - 修正 DbSet 命名為複數
        // ======================================

        private IEnumerable<SelectListItem> GetCategoryList()
        {
            try
            {
                // 修正 CS1061: 假設 DbSet 名稱為 SellerCategories (複數)
                return _db.SellerCategories
                          .Select(c => new SelectListItem
                          {
                              Value = c.SellerCategoryId.ToString(),
                              Text = c.CategoryName
                          })
                          .ToList();
            }
            catch
            {
                return new List<SelectListItem>();
            }
        }

        // ======================================
        // C. Create (GET) - 保持不變
        // ======================================

        public IActionResult Create()
        {
            var viewModel = new SellerProductEditViewModel
            {
                // 載入分類列表
                CategoryList = GetCategoryList(),
                // 修正 ArgumentOutOfRangeException：初始化 Specs 列表
                Specs = new List<ProductSpecViewModel>()
            };
            return View(viewModel);
        }

        // ======================================
        // B. Create (POST) - 修正圖片上傳邏輯
        // ======================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SellerProductEditViewModel model, string action)
        {
            System.Diagnostics.Debug.WriteLine("=== Create POST 開始 ===");
            System.Diagnostics.Debug.WriteLine($"Action: {action}");
            System.Diagnostics.Debug.WriteLine($"Name: {model.Name}");
            System.Diagnostics.Debug.WriteLine($"UploadedImages: {model.UploadedImages?.Count ?? 0}");

            // 移除不需要驗證的欄位
            var keysToRemove = new[] { "CategoryList", "ExistingImages", "ProductId", "AuditStatus", "Status" };
            foreach (var key in keysToRemove)
            {
                ModelState.Remove(key);
            }

            if (model.UploadedImages == null || model.UploadedImages.Count == 0)
            {
                ModelState.Remove("UploadedImages");
            }

            var specsKeys = ModelState.Keys.Where(k => k.StartsWith("Specs")).ToList();
            foreach (var key in specsKeys)
            {
                ModelState.Remove(key);
            }

            // 手動驗證
            bool hasErrors = false;

            if (string.IsNullOrWhiteSpace(model.Name))
            {
                ModelState.AddModelError("Name", "商品名稱為必填。");
                hasErrors = true;
            }
            if (model.CategoryId <= 0)
            {
                ModelState.AddModelError("CategoryId", "請選擇商品分類。");
                hasErrors = true;
            }
            if (model.Price <= 0)
            {
                ModelState.AddModelError("Price", "基礎價格必須大於零。");
                hasErrors = true;
            }
            if (model.StockQuantity <= 0)
            {
                ModelState.AddModelError("StockQuantity", "基礎庫存必須大於零。");
                hasErrors = true;
            }

            if (hasErrors || !ModelState.IsValid)
            {
                System.Diagnostics.Debug.WriteLine("=== 驗證失敗 ===");
                foreach (var key in ModelState.Keys)
                {
                    var errors = ModelState[key].Errors;
                    if (errors.Count > 0)
                    {
                        foreach (var error in errors)
                        {
                            System.Diagnostics.Debug.WriteLine($"{key}: {error.ErrorMessage}");
                        }
                    }
                }

                model.CategoryList = GetCategoryList();
                return View(model);
            }

            System.Diagnostics.Debug.WriteLine("=== 驗證通過 ===");

            string statusToSet = action == "publish" ? "上架中" : "下架中";
            long currentUserId = 9;

            try
            {
                // 1. 建立商品主檔
                var product = new Product
                {
                    ProductName = model.Name,
                    Description = model.Description,
                    Status = statusToSet,
                    AuditStatus = "通過",
                    UserId = currentUserId,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };
                _db.Products.Add(product);

                System.Diagnostics.Debug.WriteLine("=== 第一次儲存：建立商品 ===");
                await _db.SaveChangesAsync();
                System.Diagnostics.Debug.WriteLine($"=== ProductId: {product.ProductId} ===");

                // 2. 處理圖片上傳（增強錯誤處理）
                if (model.UploadedImages != null && model.UploadedImages.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"=== 開始上傳 {model.UploadedImages.Count} 張圖片 ===");

                    // **檢查 WebRootPath 是否存在**
                    if (string.IsNullOrEmpty(_hostingEnvironment.WebRootPath))
                    {
                        System.Diagnostics.Debug.WriteLine("!!! WebRootPath 為 null !!!");
                        throw new Exception("無法取得 WebRootPath，請確認專案設定");
                    }

                    string uploadsFolder = Path.Combine(_hostingEnvironment.WebRootPath, "img", "products");
                    System.Diagnostics.Debug.WriteLine($"圖片儲存路徑: {uploadsFolder}");

                    // **建立資料夾（如果不存在）**
                    if (!Directory.Exists(uploadsFolder))
                    {
                        System.Diagnostics.Debug.WriteLine("資料夾不存在，正在建立...");
                        try
                        {
                            Directory.CreateDirectory(uploadsFolder);
                            System.Diagnostics.Debug.WriteLine("資料夾建立成功");
                        }
                        catch (Exception dirEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"!!! 建立資料夾失敗: {dirEx.Message}");
                            throw new Exception($"無法建立圖片資料夾: {dirEx.Message}");
                        }
                    }

                    for (int i = 0; i < model.UploadedImages.Count; i++)
                    {
                        var file = model.UploadedImages[i];

                        System.Diagnostics.Debug.WriteLine($"處理第 {i + 1} 張圖片: {file.FileName}, 大小: {file.Length} bytes");

                        // **檢查檔案是否有效**
                        if (file.Length == 0)
                        {
                            System.Diagnostics.Debug.WriteLine($"!!! 警告：第 {i + 1} 張圖片大小為 0");
                            continue;
                        }

                        string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        System.Diagnostics.Debug.WriteLine($"完整檔案路徑: {filePath}");

                        try
                        {
                            using (var fileStream = new FileStream(filePath, FileMode.Create))
                            {
                                await file.CopyToAsync(fileStream);
                            }
                            System.Diagnostics.Debug.WriteLine($"圖片 {i + 1} 儲存成功");

                            // **驗證檔案是否真的存在**
                            if (System.IO.File.Exists(filePath))
                            {
                                var fileInfo = new FileInfo(filePath);
                                System.Diagnostics.Debug.WriteLine($"檔案存在，大小: {fileInfo.Length} bytes");
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine("!!! 警告：檔案儲存後無法找到");
                            }
                        }
                        catch (Exception fileEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"!!! 儲存圖片失敗: {fileEx.Message}");
                            throw new Exception($"儲存圖片失敗: {fileEx.Message}");
                        }

                        string imageUrlPath = $"/img/products/{uniqueFileName}";

                        var productImage = new ProductImage
                        {
                            ProductId = product.ProductId,
                            ImageUrl = imageUrlPath,
                            IsMainImage = (i == 0),
                            DisplayOrder = i + 1,
                            CreatedAt = DateTime.Now,
                            UpdatedAt = DateTime.Now
                        };
                        _db.ProductImages.Add(productImage);
                        System.Diagnostics.Debug.WriteLine($"圖片記錄已加入 DbContext: {imageUrlPath}");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("=== 沒有上傳圖片 ===");
                }

                // 3. 處理分類
                if (model.CategoryId > 0)
                {
                    _db.ProductSellerCategoryMappins.Add(new ProductSellerCategoryMappin
                    {
                        SellerCategoryId = model.CategoryId,
                        ProductId = product.ProductId
                    });
                }

                // 4. 處理規格
                var specsToAdd = new List<ProductSpecViewModel>();

                bool hasValidSpecs = model.Specs != null && model.Specs.Any(s =>
                    !string.IsNullOrWhiteSpace(s.SpecName) ||
                    !string.IsNullOrWhiteSpace(s.SpecValue) ||
                    s.Stock > 0);

                if (!hasValidSpecs)
                {
                    specsToAdd.Add(new ProductSpecViewModel
                    {
                        PriceAdjustment = 0,
                        Stock = model.StockQuantity
                    });
                }
                else
                {
                    specsToAdd.AddRange(model.Specs.Where(s => s.Stock > 0));
                }

                System.Diagnostics.Debug.WriteLine($"=== 建立 {specsToAdd.Count} 個規格 ===");

                foreach (var spec in specsToAdd)
                {
                    _db.ProductSpecifications.Add(new ProductSpecification
                    {
                        ProductId = product.ProductId,
                        Price = model.Price + spec.PriceAdjustment,
                        StockQuantity = spec.Stock,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    });
                }

                System.Diagnostics.Debug.WriteLine("=== 第二次儲存：圖片、分類、規格 ===");
                await _db.SaveChangesAsync();
                System.Diagnostics.Debug.WriteLine("=== 儲存成功！===");

                _db.ChangeTracker.Clear();

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                string innerMessage = ex.InnerException?.Message ?? ex.Message;
                System.Diagnostics.Debug.WriteLine($"=== 資料庫錯誤: {innerMessage} ===");

                ModelState.AddModelError("", "資料庫新增失敗: " + innerMessage);
                model.CategoryList = GetCategoryList();
                return View(model);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"=== 系統錯誤: {ex.Message} ===");
                System.Diagnostics.Debug.WriteLine($"堆疊: {ex.StackTrace}");

                ModelState.AddModelError("", "系統錯誤: " + ex.Message);
                model.CategoryList = GetCategoryList();
                return View(model);
            }
        }

        // ======================================
        // Edit (GET) - 最終修正版：解決所有編譯錯誤並載入數據
        // ======================================

        public async Task<IActionResult> Edit(long? id)
        {
            long currentUserId = 9;

            if (id == null)
            {
                return NotFound();
            }

            // 1. 載入商品，並預先載入相關數據
            var product = await _db.Products
                .Include(p => p.ProductSpecifications)
                .Include(p => p.ProductImages)
                .Include(p => p.ProductSellerCategoryMappins)
                .FirstOrDefaultAsync(p => p.ProductId == id && p.UserId == currentUserId);

            if (product == null)
            {
                return NotFound();
            }

            // 2. 計算基礎價格 (用於規格價差計算)
            // 假設規格表只儲存 Price 和 StockQuantity
            decimal basePrice = product.ProductSpecifications.Any() ? product.ProductSpecifications.Min(s => s.Price) : 0;

            // 3. 轉換為 View Model
            var viewModel = new SellerProductEditViewModel
            {
                ProductId = product.ProductId,
                Name = product.ProductName,
                Description = product.Description,
                Status = product.Status,
                // 修正 CS0117：假設 AuditStatus 屬性存在於 ViewModel 中
                AuditStatus = product.AuditStatus,

                // 載入基礎價格和總庫存
                Price = basePrice,
                StockQuantity = product.ProductSpecifications.Sum(s => s.StockQuantity),

                // 載入分類
                CategoryId = product.ProductSellerCategoryMappins.FirstOrDefault()?.SellerCategoryId ?? 0,
                CategoryList = GetCategoryList(),

                // 載入規格列表 (修正 CS1061)
                Specs = product.ProductSpecifications.Select(s => new ProductSpecViewModel
                {
                    // 載入規格數據 (僅使用 PriceAdjustment 和 Stock)
                    PriceAdjustment = s.Price - basePrice,
                    Stock = s.StockQuantity
                }).ToList(),

                // 【修正 CS0029】：將 List<string> 轉換為 List<ProductImageViewModel>
                // 這是解決類型不匹配問題的關鍵步驟。
                ExistingImages = product.ProductImages
                    .OrderBy(img => img.DisplayOrder)
                    .Select(img => new ProductImageViewModel
                    {
                        ImageUrl = img.ImageUrl,
                        IsMainImage = img.IsMainImage,
                        DisplayOrder = img.DisplayOrder,
                        // 如果 ProductImageViewModel 中包含 ID，建議也加入
                    })
                    .ToList()
            };

            // 如果規格列表為空，則手動添加一個空的規格，避免 View 崩潰
            if (viewModel.Specs == null || viewModel.Specs.Count == 0)
            {
                viewModel.Specs = new List<ProductSpecViewModel>();
            }

            return View(viewModel);
        }

        // ======================================
        // ======================================
        // F. Edit (POST) - 【最終修復：圖片非必填 + 資料庫更新失敗】
        // ======================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(SellerProductEditViewModel model, string action)
        {
            // **除錯日誌**
            System.Diagnostics.Debug.WriteLine($"=== Edit POST 開始 ===");
            System.Diagnostics.Debug.WriteLine($"ProductId: {model.ProductId}");
            System.Diagnostics.Debug.WriteLine($"Action: {action}");
            System.Diagnostics.Debug.WriteLine($"Name: {model.Name}");

            // --- 階段 1: 清理不需要驗證的欄位 ---
            var keysToRemove = new[] { "UploadedImages", "CategoryList", "ExistingImages", "Specs", "AuditStatus" };
            foreach (var key in keysToRemove)
            {
                if (ModelState.ContainsKey(key))
                {
                    ModelState.Remove(key);
                }
            }

            // 手動驗證必填欄位
            if (model.ProductId <= 0)
            {
                ModelState.AddModelError("", "無效的商品 ID。");
            }
            if (string.IsNullOrWhiteSpace(model.Name))
            {
                ModelState.AddModelError("Name", "商品名稱為必填。");
            }
            if (model.Price <= 0)
            {
                ModelState.AddModelError("Price", "基礎價格必須大於零。");
            }
            if (model.StockQuantity <= 0)
            {
                ModelState.AddModelError("StockQuantity", "基礎庫存必須大於零。");
            }

            // --- 階段 2: 驗證失敗處理 ---
            if (!ModelState.IsValid)
            {
                System.Diagnostics.Debug.WriteLine("=== ModelState 驗證失敗 ===");
                foreach (var key in ModelState.Keys)
                {
                    var errors = ModelState[key].Errors;
                    if (errors.Count > 0)
                    {
                        foreach (var error in errors)
                        {
                            System.Diagnostics.Debug.WriteLine($"{key}: {error.ErrorMessage}");
                        }
                    }
                }

                model.CategoryList = GetCategoryList();
                model.Specs = model.Specs ?? new List<ProductSpecViewModel>();

                // 重新載入現有圖片
                var productForImages = await _db.Products
                    .Include(p => p.ProductImages)
                    .FirstOrDefaultAsync(p => p.ProductId == model.ProductId);

                if (productForImages != null)
                {
                    model.ExistingImages = productForImages.ProductImages
                        .OrderBy(img => img.DisplayOrder)
                        .Select(img => new ProductImageViewModel
                        {
                            ImageUrl = img.ImageUrl,
                            IsMainImage = img.IsMainImage,
                            DisplayOrder = img.DisplayOrder
                        })
                        .ToList();
                }

                return View(model);
            }

            System.Diagnostics.Debug.WriteLine("=== ModelState 驗證通過 ===");

            // --- 階段 3: 資料庫更新 ---
            string statusToSet = action == "publish" ? "上架中" : "下架中";
            long currentUserId = 9;

            var productToUpdate = await _db.Products
                .Include(p => p.ProductImages)
                .Include(p => p.ProductSpecifications)
                .Include(p => p.ProductSellerCategoryMappins)
                .FirstOrDefaultAsync(p => p.ProductId == model.ProductId && p.UserId == currentUserId);

            if (productToUpdate == null)
            {
                System.Diagnostics.Debug.WriteLine("=== 找不到商品 ===");
                return NotFound();
            }

            try
            {
                System.Diagnostics.Debug.WriteLine("=== 開始更新商品 ===");

                // 1. 更新基本資訊
                productToUpdate.ProductName = model.Name;
                productToUpdate.Description = model.Description;
                productToUpdate.Status = statusToSet;
                productToUpdate.UpdatedAt = DateTime.Now;

                // 2. 更新規格
                _db.ProductSpecifications.RemoveRange(productToUpdate.ProductSpecifications);

                decimal basePrice = model.Price;
                var specsToAdd = new List<ProductSpecViewModel>();

                if (model.Specs == null || model.Specs.Count == 0)
                {
                    specsToAdd.Add(new ProductSpecViewModel { PriceAdjustment = 0, Stock = model.StockQuantity });
                }
                else
                {
                    specsToAdd.AddRange(model.Specs);
                }

                foreach (var specVm in specsToAdd)
                {
                    _db.ProductSpecifications.Add(new ProductSpecification
                    {
                        Price = basePrice + specVm.PriceAdjustment,
                        StockQuantity = specVm.Stock,
                        CreatedAt = productToUpdate.CreatedAt,
                        UpdatedAt = DateTime.Now,
                        ProductId = productToUpdate.ProductId
                    });
                }

                // 3. 更新分類
                _db.ProductSellerCategoryMappins.RemoveRange(productToUpdate.ProductSellerCategoryMappins);
                if (model.CategoryId > 0)
                {
                    _db.ProductSellerCategoryMappins.Add(new ProductSellerCategoryMappin
                    {
                        SellerCategoryId = model.CategoryId,
                        ProductId = productToUpdate.ProductId
                    });
                }

                // 4. 處理圖片（只在有上傳新圖片時）
                if (model.UploadedImages != null && model.UploadedImages.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"=== 處理 {model.UploadedImages.Count} 張新圖片 ===");

                    string uploadsFolder = Path.Combine(_hostingEnvironment.WebRootPath, "img", "products");

                    // 刪除舊圖片
                    foreach (var image in productToUpdate.ProductImages)
                    {
                        string fileName = Path.GetFileName(image.ImageUrl);
                        string filePath = Path.Combine(uploadsFolder, fileName);
                        if (System.IO.File.Exists(filePath))
                        {
                            System.IO.File.Delete(filePath);
                        }
                    }
                    _db.ProductImages.RemoveRange(productToUpdate.ProductImages);

                    // 上傳新圖片
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    for (int i = 0; i < model.UploadedImages.Count; i++)
                    {
                        var file = model.UploadedImages[i];
                        string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(fileStream);
                        }

                        string imageUrlPath = $"/img/products/{uniqueFileName}";

                        _db.ProductImages.Add(new ProductImage
                        {
                            ImageUrl = imageUrlPath,
                            IsMainImage = (i == 0),
                            DisplayOrder = i + 1,
                            CreatedAt = DateTime.Now,
                            UpdatedAt = DateTime.Now,
                            ProductId = productToUpdate.ProductId
                        });
                    }
                }

                // 5. 儲存變更
                System.Diagnostics.Debug.WriteLine("=== 準備儲存到資料庫 ===");
                await _db.SaveChangesAsync();
                System.Diagnostics.Debug.WriteLine("=== 儲存成功！===");

                _db.ChangeTracker.Clear();

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                string innerMessage = ex.InnerException?.Message ?? ex.Message;
                System.Diagnostics.Debug.WriteLine($"=== 資料庫更新失敗: {innerMessage} ===");

                ModelState.AddModelError("", "商品更新失敗: " + innerMessage);

                model.CategoryList = GetCategoryList();
                model.Specs = model.Specs ?? new List<ProductSpecViewModel>();

                return View(model);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"=== 發生錯誤: {ex.Message} ===");

                ModelState.AddModelError("", "發生錯誤: " + ex.Message);

                model.CategoryList = GetCategoryList();
                model.Specs = model.Specs ?? new List<ProductSpecViewModel>();

                return View(model);
            }
        }
        
        // ======================================
        // F. Delete (GET) - 顯示刪除確認頁面
        // ======================================

        [HttpGet]
        public async Task<IActionResult> Delete(long id)
        {
            var product = await _db.Products
                .FirstOrDefaultAsync(p => p.ProductId == id && p.UserId == 9);

            if (product == null)
            {
                return RedirectToAction(nameof(Index));
            }

            return View(product);
        }

        // ======================================
        // G. DeleteConfirmed (POST) - 執行刪除操作
        // ======================================

        [HttpPost, ActionName("Delete")] // 可以使用 ActionName 讓 URL 仍然顯示 /Delete
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(long id)
        {
            // 假設 currentUserId 仍然是 9
            long currentUserId = 9;

            // 1. 載入商品，並包含所有相關實體 (用於清理)
            // 我們只刪除屬於當前用戶的商品
            var productToDelete = await _db.Products
                .Include(p => p.ProductImages)
                .Include(p => p.ProductSpecifications)
                .Include(p => p.ProductSellerCategoryMappins) // 假設您的 DbSet 名稱正確
                .FirstOrDefaultAsync(p => p.ProductId == id && p.UserId == currentUserId);

            if (productToDelete == null)
            {
                // 找不到商品或不屬於當前用戶
                return RedirectToAction(nameof(Index));
            }

            try
            {
                // 2. 刪除實體圖片檔案
                string uploadsFolder = Path.Combine(_hostingEnvironment.WebRootPath, "img", "products");

                foreach (var image in productToDelete.ProductImages)
                {
                    // 從 URL 中解析出檔名
                    string fileName = Path.GetFileName(image.ImageUrl);
                    string filePath = Path.Combine(uploadsFolder, fileName);

                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }

                // 3. 刪除相關記錄 (如果資料庫沒有配置 Cascade Delete，則需要手動刪除)
                _db.ProductImages.RemoveRange(productToDelete.ProductImages);
                _db.ProductSpecifications.RemoveRange(productToDelete.ProductSpecifications);
                _db.ProductSellerCategoryMappins.RemoveRange(productToDelete.ProductSellerCategoryMappins);

                // 4. 刪除主要商品記錄
                _db.Products.Remove(productToDelete);

                // 5. 提交變更
                await _db.SaveChangesAsync();

                // 可選：成功訊息
                // TempData["SuccessMessage"] = "商品已成功刪除。"; 

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                // 錯誤處理：例如記錄日誌
                // TempData["ErrorMessage"] = "刪除商品失敗：" + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }
    }
}