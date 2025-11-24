// /ViewModels/SellerProductEditViewModel.cs (已優化以處理多圖和多規格)

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace Project_Photo.ViewModels
{
    public class SellerProductEditViewModel
    {
        public long ProductId { get; set; }

        // 商品資訊
        [Display(Name = "商品名稱")]
        [Required(ErrorMessage = "請輸入商品名稱")]
        public string Name { get; set; }

        [Display(Name = "商品描述")]
        [Required(ErrorMessage = "請輸入商品描述")]
        public string Description { get; set; }

        [Display(Name = "價格")]
        [Required(ErrorMessage = "請輸入價格")]
        [Range(0.01, 1000000, ErrorMessage = "價格必須大於0")]
        public decimal Price { get; set; }

        [Display(Name = "分類")]
        [Required(ErrorMessage = "請選擇分類")]
        public int CategoryId { get; set; }

        [Display(Name = "主圖庫存")]
        [Required(ErrorMessage = "請輸入庫存")]
        public int StockQuantity { get; set; }

        public string Status { get; set; } = "待審核";

        // ====== 圖片處理：處理多張圖片上傳 ======
        [Display(Name = "商品圖片 (最多9張)")]
        // 接收多個 IFormFile (用於多圖上傳)
        [ValidateNever]
        public List<IFormFile> UploadedImages { get; set; }

        // 圖片處理：用於顯示和傳輸現有圖片的狀態 (必須初始化)
        public List<ProductImageViewModel> ExistingImages { get; set; } = new List<ProductImageViewModel>();

        // ====== 規格處理：用於動態規格新增/編輯 ======
        public List<ProductSpecViewModel> Specs { get; set; } = new List<ProductSpecViewModel>();

        // 分類選單數據 (用於下拉選單)
        public IEnumerable<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> CategoryList { get; set; }

        public string AuditStatus { get; set; }

    }

    public class ProductSpecViewModel
    {
        public int? ProductSpecificationId { get; set; }
        public string SpecName { get; set; } // 規格名稱 (如：顏色)
        public string SpecValue { get; set; } // 規格值 (如：紅色/藍色)

        [Display(Name = "價格調整")]
        public decimal PriceAdjustment { get; set; } = 0; // 價格調整，預設為 0

        [Display(Name = "規格庫存")]
        public int Stock { get; set; }

    }
}