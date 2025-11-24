namespace Project_Photo.ViewModels
{
    public class SellerProductViewModel
    {
        public long ProductId { get; set; }
        public string ProductName { get; set; }
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public string Status { get; set; }
        public string AuditStatus { get; set; }
        public string MainImageUrl { get; set; }
        public int Views { get; set; }
        public int SalesCount { get; set; }
    }
}