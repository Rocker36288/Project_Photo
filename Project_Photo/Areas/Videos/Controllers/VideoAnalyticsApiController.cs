using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Project_Photo.Areas.Videos.Models.ViewModels;
using Project_Photo.Areas.Videos.Services;
using Project_Photo.Services;

namespace Project_Photo.Areas.Videos.Controllers
{
    [Area("Videos")]
    [Route("api/videos/analytics")]
    [ApiController]
    public class VideoAnalyticsApiController : ControllerBase
    {
        private readonly IVideoAnalyticsService _analyticsService;

        public VideoAnalyticsApiController(IVideoAnalyticsService analyticsService)
        {
            _analyticsService = analyticsService;
        }

        /// <summary>
        /// 獲取觀看數據
        /// GET: api/videos/analytics/views/123?days=90
        /// </summary>
        [HttpGet("views/{videoId}")]
        public async Task<ActionResult<ChartDataResponse>> GetViewsAnalytics(
            int videoId,
            [FromQuery] int days = 90)
        {
            if (videoId <= 0)
                return BadRequest(new { message = "Invalid video ID" });

            if (days < 1 || days > 365)
                return BadRequest(new { message = "Days must be between 1 and 365" });

            try
            {
                var data = await _analyticsService.GetViewsAnalyticsAsync(videoId, days);
                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving views analytics", error = ex.Message });
            }
        }

        /// <summary>
        /// 獲取留言數據
        /// GET: api/videos/analytics/comments/123?days=90
        /// </summary>
        [HttpGet("comments/{videoId}")]
        public async Task<ActionResult<ChartDataResponse>> GetCommentsAnalytics(
            int videoId,
            [FromQuery] int days = 90)
        {
            if (videoId <= 0)
                return BadRequest(new { message = "Invalid video ID" });

            if (days < 1 || days > 365)
                return BadRequest(new { message = "Days must be between 1 and 365" });

            try
            {
                var data = await _analyticsService.GetCommentsAnalyticsAsync(videoId, days);
                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving comments analytics", error = ex.Message });
            }
        }

        /// <summary>
        /// 獲取喜歡數據
        /// GET: api/videos/analytics/likes/123?days=90
        /// </summary>
        [HttpGet("likes/{videoId}")]
        public async Task<ActionResult<ChartDataResponse>> GetLikesAnalytics(
            int videoId,
            [FromQuery] int days = 90)
        {
            if (videoId <= 0)
                return BadRequest(new { message = "Invalid video ID" });

            if (days < 1 || days > 365)
                return BadRequest(new { message = "Days must be between 1 and 365" });

            try
            {
                var data = await _analyticsService.GetLikesAnalyticsAsync(videoId, days);
                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving likes analytics", error = ex.Message });
            }
        }

        /// <summary>
        /// 獲取所有分析數據
        /// GET: api/videos/analytics/all/123?days=90
        /// </summary>
        [HttpGet("all/{videoId}")]
        public async Task<ActionResult<VideoAnalyticsData>> GetAllAnalytics(
            int videoId,
            [FromQuery] int days = 90)
        {
            if (videoId <= 0)
                return BadRequest(new { message = "Invalid video ID" });

            if (days < 1 || days > 365)
                return BadRequest(new { message = "Days must be between 1 and 365" });

            try
            {
                var data = await _analyticsService.GetAllAnalyticsAsync(videoId, days);
                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving analytics", error = ex.Message });
            }
        }
    }
}
