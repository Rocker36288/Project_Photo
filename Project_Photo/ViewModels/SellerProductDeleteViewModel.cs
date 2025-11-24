namespace Project_Photo.ViewModels
{
    public class SellerProductDeleteViewModel
    {
        public long ProductId { get; set; } // 必須是 long，與 Model 一致
        public string Name { get; set; }
        // public string MainImageUrl { get; set; } // 可選
    }
}
