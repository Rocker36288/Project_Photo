using Project_Photo.Models;

namespace Project_Photo.Areas.Videos.Models.ViewModels
{
    public class ChannelViewModel
    {
        public Channel Channel { get; set; }
        public User User { get; set; }
        public List<Video> Videos { get; set; } = new List<Video>();
        public List<VideoViewModel> VideoViewModel { get; set; } // 新增：包含統計數據
        public int FollowerCount { get; set; }
        public int VideoCount { get; set; }
        public int ReportCount { get; set; }





        // 🔧 改為影片列表（原本是單一 Video）


        // 分頁相關
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalItems { get; set; }
        public int PageSize { get; set; }

        // 搜尋排序相關
        public string SearchTerm { get; set; }
        public string SearchBy { get; set; }
        public string SortBy { get; set; }
        public string SortOrder { get; set; }
       
    }

}


