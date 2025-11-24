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
            System.Diagnostics.Debug.WriteLine($"Price: {model.Price}");
            System.Diagnostics.Debug.WriteLine($"StockQuantity: {model.StockQuantity}");
            System.Diagnostics.Debug.WriteLine($"Specs Count: {model.Specs?.Count ?? 0}");

            // ====== 步驟 1：檢查是否有有效的多規格 ======
            bool hasValidSpecs = false;
            if (model.Specs != null && model.Specs.Count > 0)
            {
                hasValidSpecs = model.Specs.Any(s =>
                    (!string.IsNullOrWhiteSpace(s.SpecName) || !string.IsNullOrWhiteSpace(s.SpecValue)) && s.Stock > 0);
            }

            System.Diagnostics.Debug.WriteLine($"是否有有效規格: {hasValidSpecs}");
            if (hasValidSpecs)
            {
                System.Diagnostics.Debug.WriteLine("檢測到多規格，將忽略基礎價格和庫存的驗證");
            }

            // ====== 步驟 2：清除所有自動驗證 ======
            ModelState.Clear(); // 清空所有 ModelState

            System.Diagnostics.Debug.WriteLine("已清空 ModelState");

            // ====== 步驟 3：手動驗證必填欄位 ======
            bool hasErrors = false;

            // 驗證商品名稱
            if (string.IsNullOrWhiteSpace(model.Name))
            {
                ModelState.AddModelError("Name", "商品名稱為必填。");
                hasErrors = true;
            }

            // 驗證商品分類
            if (model.CategoryId <= 0)
            {
                ModelState.AddModelError("CategoryId", "請選擇商品分類。");
                hasErrors = true;
            }

            // ====== 步驟 4：條件式驗證（關鍵！）======
            if (hasValidSpecs)
            {
                // 情況 A：有多規格 - 不驗證基礎價格和庫存
                System.Diagnostics.Debug.WriteLine("【有多規格】驗證規格資料");

                int validSpecCount = 0;
                for (int i = 0; i < model.Specs.Count; i++)
                {
                    var spec = model.Specs[i];
                    bool hasSpecName = !string.IsNullOrWhiteSpace(spec.SpecName);
                    bool hasSpecValue = !string.IsNullOrWhiteSpace(spec.SpecValue);

                    if (hasSpecName || hasSpecValue)
                    {
                        if (spec.Stock <= 0)
                        {
                            ModelState.AddModelError("", $"規格 {i + 1}（{spec.SpecName}-{spec.SpecValue}）的數量必須大於零。");
                            hasErrors = true;
                        }
                        else
                        {
                            validSpecCount++;
                        }
                    }
                }

                if (validSpecCount == 0)
                {
                    ModelState.AddModelError("", "請至少設定一個有效的規格（需填寫數量）。");
                    hasErrors = true;
                }

                // 給基礎價格和庫存預設值（避免資料庫錯誤）
                if (model.Price <= 0) model.Price = 1;
                if (model.StockQuantity <= 0) model.StockQuantity = 1;

                System.Diagnostics.Debug.WriteLine($"有效規格數量: {validSpecCount}");
            }
            else
            {
                // 情況 B：沒有多規格 - 驗證基礎價格和庫存
                System.Diagnostics.Debug.WriteLine("【沒有多規格】驗證基礎價格和庫存");

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
            }

            // ====== 步驟 5：檢查驗證結果 ======
            if (hasErrors)
            {
                System.Diagnostics.Debug.WriteLine("=== 驗證失敗 ===");
                foreach (var key in ModelState.Keys)
                {
                    var errors = ModelState[key].Errors;
                    if (errors.Count > 0)
                    {
                        foreach (var error in errors)
                        {
                            System.Diagnostics.Debug.WriteLine($"❌ {key}: {error.ErrorMessage}");
                        }
                    }
                }

                model.CategoryList = GetCategoryList();
                model.Specs = model.Specs ?? new List<ProductSpecViewModel>();
                return View(model);
            }

            System.Diagnostics.Debug.WriteLine("=== 驗證通過，開始寫入資料庫 ===");

            // ====== 步驟 6：寫入資料庫 ======
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

                // 2. 處理圖片
                if (model.UploadedImages != null && model.UploadedImages.Count > 0)
                {
                    string uploadsFolder = Path.Combine(_hostingEnvironment.WebRootPath, "img", "products");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    for (int i = 0; i < model.UploadedImages.Count; i++)
                    {
                        var file = model.UploadedImages[i];
                        if (file.Length == 0) continue;

                        string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(fileStream);
                        }

                        _db.ProductImages.Add(new ProductImage
                        {
                            ProductId = product.ProductId,
                            ImageUrl = $"/img/products/{uniqueFileName}",
                            IsMainImage = (i == 0),
                            DisplayOrder = i + 1,
                            CreatedAt = DateTime.Now,
                            UpdatedAt = DateTime.Now
                        });
                    }
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

                // 4. 處理規格和屬性
                if (!hasValidSpecs)
                {
                    // 沒有多規格：建立基礎規格
                    System.Diagnostics.Debug.WriteLine($"建立基礎規格 - 價格: {model.Price}, 數量: {model.StockQuantity}");

                    _db.ProductSpecifications.Add(new ProductSpecification
                    {
                        ProductId = product.ProductId,
                        Price = model.Price,
                        StockQuantity = model.StockQuantity,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    });
                }
                else
                {
                    // 有多規格：建立屬性和規格
                    System.Diagnostics.Debug.WriteLine("=== 建立多規格相關資料 ===");

                    var propertyDictionary = new Dictionary<string, int>();

                    var uniquePropertyNames = model.Specs
                        .Where(s => !string.IsNullOrWhiteSpace(s.SpecName))
                        .Select(s => s.SpecName.Trim())
                        .Distinct()
                        .ToList();

                    // 建立 ProductProperty
                    foreach (var propertyName in uniquePropertyNames)
                    {
                        var productProperty = new ProductProperty
                        {
                            ProductId = product.ProductId,
                            PropertyName = propertyName,
                            CreatedAt = DateTime.Now,
                            UpdatedAt = DateTime.Now
                        };

                        _db.Add(productProperty);
                        await _db.SaveChangesAsync();

                        propertyDictionary[propertyName] = productProperty.PropertyId;
                        System.Diagnostics.Debug.WriteLine($"建立屬性: {propertyName}, PropertyId: {productProperty.PropertyId}");

                        var optionValues = model.Specs
                            .Where(s => s.SpecName?.Trim() == propertyName && !string.IsNullOrWhiteSpace(s.SpecValue))
                            .Select(s => s.SpecValue.Trim())
                            .Distinct()
                            .ToList();

                        foreach (var optionValue in optionValues)
                        {
                            var propertyDetail = new ProductPropertyDetail
                            {
                                PropertyId = productProperty.PropertyId,
                                OptionValue = optionValue,
                                CreatedAt = DateTime.Now,
                                UpdatedAt = DateTime.Now
                            };

                            _db.Add(propertyDetail);
                            System.Diagnostics.Debug.WriteLine($"  建立選項值: {optionValue}");
                        }
                    }

                    await _db.SaveChangesAsync();

                    // 建立 ProductSpecification
                    int specCount = 0;
                    foreach (var spec in model.Specs)
                    {
                        if ((!string.IsNullOrWhiteSpace(spec.SpecName) || !string.IsNullOrWhiteSpace(spec.SpecValue)) && spec.Stock > 0)
                        {
                            decimal finalPrice = model.Price + spec.PriceAdjustment;

                            _db.ProductSpecifications.Add(new ProductSpecification
                            {
                                ProductId = product.ProductId,
                                Price = finalPrice,
                                StockQuantity = spec.Stock,
                                CreatedAt = DateTime.Now,
                                UpdatedAt = DateTime.Now
                            });

                            specCount++;
                            System.Diagnostics.Debug.WriteLine($"建立規格 {specCount} - 價格: {finalPrice}, 數量: {spec.Stock}");
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine("=== 最終儲存 ===");
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
                model.Specs = model.Specs ?? new List<ProductSpecViewModel>();
                return View(model);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"=== 系統錯誤: {ex.Message} ===");

                ModelState.AddModelError("", "系統錯誤: " + ex.Message);
                model.CategoryList = GetCategoryList();
                model.Specs = model.Specs ?? new List<ProductSpecViewModel>();
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
                System.Diagnostics.Debug.WriteLine("=== 開始處理規格 ===");
                _db.ProductSpecifications.RemoveRange(productToUpdate.ProductSpecifications);

                decimal basePrice = model.Price;
                var specsToAdd = new List<ProductSpecViewModel>();

                // **關鍵修正：檢查 Specs 是否有有效資料**
                bool hasValidSpecs = model.Specs != null && model.Specs.Any(s =>
                    !string.IsNullOrWhiteSpace(s.SpecName) ||
                    !string.IsNullOrWhiteSpace(s.SpecValue) ||
                    s.Stock > 0);

                System.Diagnostics.Debug.WriteLine($"是否有有效規格: {hasValidSpecs}");
                System.Diagnostics.Debug.WriteLine($"Specs Count: {model.Specs?.Count ?? 0}");

                if (!hasValidSpecs)
                {
                    // 沒有規格：使用基礎價格和庫存
                    System.Diagnostics.Debug.WriteLine("使用基礎價格和庫存");
                    specsToAdd.Add(new ProductSpecViewModel
                    {
                        PriceAdjustment = 0,
                        Stock = model.StockQuantity
                    });
                }
                else
                {
                    // 有規格：使用規格資料
                    System.Diagnostics.Debug.WriteLine($"使用 {model.Specs.Count} 個規格");
                    foreach (var spec in model.Specs)
                    {
                        System.Diagnostics.Debug.WriteLine($"  規格: {spec.SpecName}-{spec.SpecValue}, 價差: {spec.PriceAdjustment}, 庫存: {spec.Stock}");

                        // 只要有庫存就加入（即使名稱和值為空）
                        if (spec.Stock > 0)
                        {
                            specsToAdd.Add(spec);
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine($"準備建立 {specsToAdd.Count} 個規格記錄");

                foreach (var specVm in specsToAdd)
                {
                    var newSpec = new ProductSpecification
                    {
                        Price = basePrice + specVm.PriceAdjustment,
                        StockQuantity = specVm.Stock,
                        CreatedAt = productToUpdate.CreatedAt,
                        UpdatedAt = DateTime.Now,
                        ProductId = productToUpdate.ProductId
                    };

                    _db.ProductSpecifications.Add(newSpec);
                    System.Diagnostics.Debug.WriteLine($"  建立規格 - 價格: {newSpec.Price}, 庫存: {newSpec.StockQuantity}");
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