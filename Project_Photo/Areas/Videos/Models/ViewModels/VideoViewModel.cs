using Project_Photo.Models;

namespace Project_Photo.Areas.Videos.Models.ViewModels
{
    public class VideoViewModel
    {
        public Video Video { get; set; }
        public User User { get; set; }
        public int ViewCount { get; set; }
        public int CommentCount { get; set; }
        public int LikeCount { get; set; }
        public int ReportCount { get; set; }

        public VideoAnalyticsData? AnalyticsData { get; set; }
    }
    /// <summary>
    /// 影片分析數據容器
    /// </summary>
    public class VideoAnalyticsData
    {
        public List<DailyAnalytics> ViewsAnalytics { get; set; } = new();
        public List<DailyAnalytics> CommentsAnalytics { get; set; } = new();
        public List<DailyAnalytics> LikesAnalytics { get; set; } = new();
    }

    /// <summary>
    /// 每日分析數據
    /// </summary>
    public class DailyAnalytics
    {
        public DateTime Date { get; set; }
        public int DailyCount { get; set; }
        public int TotalCount { get; set; }
    }

    /// <summary>
    /// API 回應格式：圖表數據
    /// </summary>
    public class ChartDataResponse
    {
        public List<string> Dates { get; set; } = new();
        public List<int> DailyData { get; set; } = new();
        public List<int> TotalData { get; set; } = new();
        public string MetricName { get; set; } = string.Empty;
    }

    /// <summary>
    /// 分析查詢參數
    /// </summary>
    public class AnalyticsQueryParams
    {
        public int VideoId { get; set; }
        public int Days { get; set; } = 90; // 預設 90 天
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
