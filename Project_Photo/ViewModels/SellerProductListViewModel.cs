namespace Project_Photo.ViewModels
{
    public class SellerProductListViewModel
    {
        public List<SellerProductViewModel> Products { get; set; }
        public int TotalProducts { get; set; }
        public int PageSize { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalProducts / PageSize);
        public string SearchQuery { get; set; }
        public string SelectedStatus { get; set; }
    }
}