using System;
using System.ComponentModel.DataAnnotations;
using Project_Photo.Areas.Videos.Models;

namespace Project_Photo.Areas.Videos.ViewModels
{
    /// <summary>
    /// 影片編輯的 ViewModel
    /// 用於接收表單資料並進行驗證
    /// </summary>
    public class EditVideoViewModel
    {
        // ========================================
        // 主鍵與關聯
        // ========================================

        public int VideoId { get; set; }

        [Display(Name = "頻道 ID")]
        public long ChannelId { get; set; }

        // ========================================
        // 可編輯欄位
        // ========================================

        [Required(ErrorMessage = "標題為必填欄位")]
        [StringLength(200, ErrorMessage = "標題長度不能超過 200 個字元")]
        [Display(Name = "標題")]
        public string Title { get; set; }

        [StringLength(2000, ErrorMessage = "說明長度不能超過 2000 個字元")]
        [Display(Name = "說明")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "請選擇處理狀態")]
        [Display(Name = "處理狀態")]
        public string ProcessStatus { get; set; }

        [Required(ErrorMessage = "請選擇隱私狀態")]
        [Display(Name = "隱私狀態")]
        public string PrivacyStatus { get; set; }

        [Display(Name = "縮圖 URL")]
        public string? ThumbnailUrl { get; set; }

        // ========================================
        // 唯讀欄位（用於顯示，不會被更新）
        // ========================================

        [Display(Name = "影片 URL")]
        public string? VideoUrl { get; set; }

        [Display(Name = "時長（秒）")]
        public int? Duration { get; set; }

        [Display(Name = "解析度")]
        public string? Resolution { get; set; }

        [Display(Name = "檔案大小（位元組）")]
        public long? FileSize { get; set; }

        [Display(Name = "建立日期")]
        public DateTime CreatedAt { get; set; }

        // ========================================
        // 輔助屬性
        // ========================================

        /// <summary>
        /// 格式化的時長顯示
        /// </summary>
        public string FormattedDuration
        {
            get
            {
                if (!Duration.HasValue) return "N/A";
                var timeSpan = TimeSpan.FromSeconds(Duration.Value);
                return timeSpan.ToString(@"mm\:ss");
            }
        }

        /// <summary>
        /// 格式化的檔案大小顯示
        /// </summary>
        public string FormattedFileSize
        {
            get
            {
                if (!FileSize.HasValue || FileSize.Value == 0) return "N/A";

                long size = FileSize.Value;

                if (size >= 1024 * 1024 * 1024)
                    return $"{(size / (1024.0 * 1024 * 1024)):0.##} GB";

                if (size >= 1024 * 1024)
                    return $"{(size / (1024.0 * 1024)):0.##} MB";

                if (size >= 1024)
                    return $"{(size / 1024.0):0.##} KB";

                return $"{size} B";
            }
        }

        /// <summary>
        /// 從 Video 實體建立 ViewModel
        /// </summary>
        public static EditVideoViewModel FromVideo(Video video)
        {
            return new EditVideoViewModel
            {
                VideoId = video.VideoId,
                ChannelId = video.ChannelId,
                Title = video.Title,
                Description = video.Description,
                ProcessStatus = video.ProcessStatus,
                PrivacyStatus = video.PrivacyStatus,
                ThumbnailUrl = video.ThumbnailUrl,
                VideoUrl = video.VideoUrl,
                Duration = video.Duration,
                Resolution = video.Resolution,
                FileSize = video.FileSize,
                CreatedAt = video.CreatedAt
            };
        }

        /// <summary>
        /// 將 ViewModel 資料套用到 Video 實體
        /// </summary>
        public void ApplyToVideo(Video video)
        {
            video.Title = this.Title?.Trim();
            video.Description = this.Description?.Trim();
            video.ProcessStatus = this.ProcessStatus;
            video.PrivacyStatus = this.PrivacyStatus;
            video.UpdateAt = DateTime.Now;

            // ThumbnailUrl 由 Controller 處理（如果有上傳新檔案）
            if (!string.IsNullOrEmpty(this.ThumbnailUrl))
            {
                video.ThumbnailUrl = this.ThumbnailUrl;
            }
        }
    }
}

// ========================================
// Video Model (參考)
// ========================================
/*
namespace Project_Photo.Areas.Videos.Models
{
    public class Video
    {
        public int VideoId { get; set; }
        public int ChannelId { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }
        public string? VideoUrl { get; set; }
        public string? ThumbnailUrl { get; set; }
        public int? Duration { get; set; }
        public string? Resolution { get; set; }
        public long? FileSize { get; set; }
        public string ProcessStatus { get; set; }
        public string PrivacyStatus { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdateAt { get; set; }
    }
}
*/
