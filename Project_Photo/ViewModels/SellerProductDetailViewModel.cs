using System.Collections.Generic;

namespace Project_Photo.ViewModels
{
    // 用來在 Details / Partial 中傳遞資料，屬性名稱要和 View 與 Controller 使用的一致
    public class SellerProductDetailViewModel
    {
        public long ProductId { get; set; }
        public string ProductName { get; set; }     // 必須有
        public string Description { get; set; }

        // 圖片 URL 清單（ProductImage.ImageUrl）
        public List<string> ImageUrls { get; set; } = new List<string>();

        // 若只需要顯示一筆價格/庫存，這兩個欄位就夠
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }

        // 若未來要支援多規格可以改成 List<ProductSpecDto>
    }

    // 若要用多規格，可保留此 DTO（目前也定義以避免 CS0246）
    public class ProductSpecDto
    {
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public string SpecText { get; set; } // 可選：名稱/選項顯示
    }
}
