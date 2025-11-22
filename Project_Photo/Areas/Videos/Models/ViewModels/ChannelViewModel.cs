using Project_Photo.Models;

namespace Project_Photo.Areas.Videos.Models.ViewModels
{
    public class ChannelViewModel
    {
        public Video Video { get; set; }
        public User User { get; set; }
        public Channel Channel{ get; set; }
        public int FollowerCount { get; set; }
        public int CommentCount { get; set; }
        public int LikeCount { get; set; }
        public int ReportCount { get; set; }
    }
}
