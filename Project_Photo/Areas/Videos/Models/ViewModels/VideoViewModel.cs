namespace Project_Photo.Areas.Videos.Models.ViewModels
{
    public class VideoViewModel
    {
        public Video Video { get; set; }
        public int ViewCount { get; set; }
        public int CommentCount { get; set; }
        public int LikeCount { get; set; }
        public int ReportCount { get; set; }
    }
}
