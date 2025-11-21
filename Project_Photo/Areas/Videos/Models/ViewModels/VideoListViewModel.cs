namespace Project_Photo.Areas.Videos.Models.ViewModels
{
    public class VideoListViewModel
    {
        public List<VideoViewModel> Videos { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalItems { get; set; }
        public int PageSize { get; set; }

        // 搜尋和排序參數
        public string SearchTerm { get; set; }
        public string SearchBy { get; set; } // "title", "username", "date"
        public string SortBy { get; set; } // "date", "views", "likes", "comments"
        public string SortOrder { get; set; } // "asc", "desc"
    }
}
