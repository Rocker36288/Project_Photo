
using Microsoft.EntityFrameworkCore;
using Project_Photo.Areas.Videos.Models;
using Project_Photo.Areas.Videos.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace Project_Photo.Services
{
    /// <summary>
    /// 影片分析數據服務
    /// </summary>
    public interface IVideoAnalyticsService
    {
        Task<ChartDataResponse> GetViewsAnalyticsAsync(int videoId, int days = 90);
        Task<ChartDataResponse> GetCommentsAnalyticsAsync(int videoId, int days = 90);
        Task<ChartDataResponse> GetLikesAnalyticsAsync(int videoId, int days = 90);
        Task<VideoAnalyticsData> GetAllAnalyticsAsync(int videoId, int days = 90);
    }

    public class VideoAnalyticsService : IVideoAnalyticsService
    {
        private readonly VideosDbContext _context;

        public VideoAnalyticsService(VideosDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// 獲取觀看分析數據
        /// </summary>
        public async Task<ChartDataResponse> GetViewsAnalyticsAsync(int videoId, int days = 90)
        {
            var endDate = DateTime.Today;
            var startDate = endDate.AddDays(-days + 1);

            // 查詢每日觀看數據
            var viewsData = await _context.Views
                .Where(v => v.VideoId == videoId &&
                           v.CreatedAt >= startDate &&
                           v.CreatedAt < endDate.AddDays(1))
                .GroupBy(v => v.CreatedAt.Date)
                .Select(g => new DailyMetric
                {
                    Date = g.Key,
                    Count = g.Count()
                })
                .OrderBy(x => x.Date)
                .ToListAsync();

            return BuildChartDataResponse(viewsData, startDate, endDate, "Views");
        }

        /// <summary>
        /// 獲取留言分析數據
        /// </summary>
        public async Task<ChartDataResponse> GetCommentsAnalyticsAsync(int videoId, int days = 90)
        {
            var endDate = DateTime.Today;
            var startDate = endDate.AddDays(-days + 1);

            // 查詢每日留言數據
            var commentsData = await _context.Comments
                .Where(c => c.VideoId == videoId &&
                           c.CreatedAt >= startDate &&
                           c.CreatedAt < endDate.AddDays(1))
                .GroupBy(c => c.CreatedAt.Date)
                .Select(g => new DailyMetric
                {
                    Date = g.Key,
                    Count = g.Count()
                })
                .OrderBy(x => x.Date)
                .ToListAsync();

            return BuildChartDataResponse(commentsData, startDate, endDate, "Comments");
        }

        /// <summary>
        /// 獲取喜歡分析數據
        /// </summary>
        public async Task<ChartDataResponse> GetLikesAnalyticsAsync(int videoId, int days = 90)
        {
            var endDate = DateTime.Today;
            var startDate = endDate.AddDays(-days + 1);

            // 查詢每日喜歡數據
            var likesData = await _context.Likes
                .Where(l => l.VideoId == videoId &&
                           l.CreatedAt >= startDate &&
                           l.CreatedAt < endDate.AddDays(1))
                .GroupBy(l => l.CreatedAt.Date)
                .Select(g => new DailyMetric
                {
                    Date = g.Key,
                    Count = g.Count()
                })
                .OrderBy(x => x.Date)
                .ToListAsync();

            return BuildChartDataResponse(likesData, startDate, endDate, "Likes");
        }

        /// <summary>
        /// 獲取所有分析數據
        /// </summary>
        public async Task<VideoAnalyticsData> GetAllAnalyticsAsync(int videoId, int days = 90)
        {
            var viewsTask = GetViewsAnalyticsAsync(videoId, days);
            var commentsTask = GetCommentsAnalyticsAsync(videoId, days);
            var likesTask = GetLikesAnalyticsAsync(videoId, days);

            await Task.WhenAll(viewsTask, commentsTask, likesTask);

            return new VideoAnalyticsData
            {
                ViewsAnalytics = ConvertToAnalyticsList(await viewsTask),
                CommentsAnalytics = ConvertToAnalyticsList(await commentsTask),
                LikesAnalytics = ConvertToAnalyticsList(await likesTask)
            };
        }

        /// <summary>
        /// 建立圖表數據回應
        /// </summary>
        private ChartDataResponse BuildChartDataResponse(
            List<DailyMetric> data,
            DateTime startDate,
            DateTime endDate,
            string metricName)
        {
            var response = new ChartDataResponse
            {
                MetricName = metricName
            };

            var dataDict = data.ToDictionary(x => x.Date, x => x.Count);
            var totalCount = 0;

            // 填充所有日期，包括沒有數據的日期
            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                response.Dates.Add(date.ToString("yyyy-MM-dd"));

                var dailyCount = dataDict.ContainsKey(date) ? dataDict[date] : 0;
                response.DailyData.Add(dailyCount);

                totalCount += dailyCount;
                response.TotalData.Add(totalCount);
            }

            return response;
        }

        /// <summary>
        /// 轉換為分析列表格式
        /// </summary>
        private List<DailyAnalytics> ConvertToAnalyticsList(ChartDataResponse chartData)
        {
            var analyticsList = new List<DailyAnalytics>();

            for (int i = 0; i < chartData.Dates.Count; i++)
            {
                analyticsList.Add(new DailyAnalytics
                {
                    Date = DateTime.Parse(chartData.Dates[i]),
                    DailyCount = chartData.DailyData[i],
                    TotalCount = chartData.TotalData[i]
                });
            }

            return analyticsList;
        }
    }

    /// <summary>
    /// 內部使用的每日指標類別
    /// </summary>
    internal class DailyMetric
    {
        public DateTime Date { get; set; }
        public int Count { get; set; }
    }
}
