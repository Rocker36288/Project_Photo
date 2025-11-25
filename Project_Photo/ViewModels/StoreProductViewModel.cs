// 檔案路徑：ViewModels/StoreProductViewModel.cs

namespace Project_Photo.ViewModels
{
    // 這裡必須是 public，以避免 CS0573 錯誤
    public class StoreProductViewModel
    {
        public long ProductId { get; set; }
        public string ProductName { get; set; }
        public decimal Price { get; set; }
        public string FirstImageUrl { get; set; }
    }
}