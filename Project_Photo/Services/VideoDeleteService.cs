using Microsoft.EntityFrameworkCore;
using Project_Photo.Areas.Videos.Models;

namespace Project_Photo.Areas.Videos.Services
{
    /// <summary>
    /// 影片刪除服務 - 處理影片軟刪除及相關資源清理
    /// </summary>
    public interface IVideoDeleteService
    {
        Task<VideoDeleteResult> SoftDeleteVideoAsync(int videoId, long requestUserId);
        Task<VideoDeleteResult> HardDeleteVideoAsync(int videoId, long requestUserId);
    }

    public class VideoDeleteService : IVideoDeleteService
    {
        private readonly VideosDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<VideoDeleteService> _logger;

        public VideoDeleteService(
            VideosDbContext context,
            IWebHostEnvironment environment,
            ILogger<VideoDeleteService> logger)
        {
            _context = context;
            _environment = environment;
            _logger = logger;
        }

        /// <summary>
        /// 軟刪除影片 - 將 ProcessStatus 設為 Delete，並清理相關資源
        /// </summary>
        public async Task<VideoDeleteResult> SoftDeleteVideoAsync(int videoId, long requestUserId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. 查詢影片並驗證權限
                var video = await _context.Videos
                    .FirstOrDefaultAsync(v => v.VideoId == videoId);

                if (video == null)
                {
                    return VideoDeleteResult.NotFound("影片不存在");
                }

                // 檢查是否為頻道擁有者
                //if (video.ChannelId != requestUserId)
                //{
                //    return VideoDeleteResult.Forbidden("您沒有權限刪除此影片");
                //}

                // 檢查影片是否已被刪除
                if (video.ProcessStatus == "Delete")
                {
                    return VideoDeleteResult.AlreadyDeleted("影片已被刪除");
                }

                // 2. 刪除實體檔案 (影片和縮圖)
                var filesDeleted = await DeletePhysicalFilesAsync(video);

                // 3. 刪除關聯資料
                await DeleteRelatedDataAsync(videoId);

                // 4. 更新影片狀態為軟刪除
                video.ProcessStatus = "Delete";
                video.UpdateAt = DateTime.Now;

                // 清空檔案路徑 (可選，因為已經刪除實體檔案)
                video.VideoUrl = null;
                video.ThumbnailUrl = null;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation(
                    "影片 {VideoId} 已軟刪除，影片檔案已刪除: {VideoDeleted}，縮圖已刪除: {ThumbnailDeleted}",
                    videoId, filesDeleted.VideoDeleted, filesDeleted.ThumbnailDeleted);

                return VideoDeleteResult.Success(filesDeleted);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "刪除影片 {VideoId} 時發生錯誤", videoId);
                return VideoDeleteResult.Error($"刪除失敗: {ex.Message}");
            }
        }

        /// <summary>
        /// 硬刪除影片 - 完全移除資料庫記錄 (可選功能)
        /// </summary>
        public async Task<VideoDeleteResult> HardDeleteVideoAsync(int videoId, long requestUserId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var video = await _context.Videos
                    .FirstOrDefaultAsync(v => v.VideoId == videoId);

                if (video == null)
                {
                    return VideoDeleteResult.NotFound("影片不存在");
                }

                if (video.ChannelId != requestUserId)
                {
                    return VideoDeleteResult.Forbidden("您沒有權限刪除此影片");
                }

                // 刪除實體檔案
                var filesDeleted = await DeletePhysicalFilesAsync(video);

                // 刪除關聯資料
                await DeleteRelatedDataAsync(videoId);

                // 完全刪除影片記錄
                _context.Videos.Remove(video);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogWarning("影片 {VideoId} 已永久刪除", videoId);

                return VideoDeleteResult.Success(filesDeleted);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "永久刪除影片 {VideoId} 時發生錯誤", videoId);
                return VideoDeleteResult.Error($"刪除失敗: {ex.Message}");
            }
        }

        /// <summary>
        /// 刪除影片和縮圖的實體檔案
        /// </summary>
        private async Task<FileDeleteInfo> DeletePhysicalFilesAsync(Video video)
        {
            var result = new FileDeleteInfo();

            // 刪除影片檔案
            if (!string.IsNullOrEmpty(video.VideoUrl))
            {
                var videoPath = Path.Combine(_environment.WebRootPath, video.VideoUrl.TrimStart('/'));
                result.VideoDeleted = await DeleteFileIfExistsAsync(videoPath, "影片");
            }

            // 刪除縮圖檔案
            if (!string.IsNullOrEmpty(video.ThumbnailUrl))
            {
                var thumbnailPath = Path.Combine(_environment.WebRootPath, video.ThumbnailUrl.TrimStart('/'));
                result.ThumbnailDeleted = await DeleteFileIfExistsAsync(thumbnailPath, "縮圖");
            }

            return result;
        }

        /// <summary>
        /// 刪除單一檔案
        /// </summary>
        private async Task<bool> DeleteFileIfExistsAsync(string filePath, string fileType)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    await Task.Run(() => File.Delete(filePath));
                    _logger.LogInformation("{FileType} 檔案已刪除: {FilePath}", fileType, filePath);
                    return true;
                }
                else
                {
                    _logger.LogWarning("{FileType} 檔案不存在: {FilePath}", fileType, filePath);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "刪除 {FileType} 檔案失敗: {FilePath}", fileType, filePath);
                return false;
            }
        }

        /// <summary>
        /// 刪除影片的所有關聯資料 (留言、按讚、觀看記錄、留言按讚)
        /// </summary>
        private async Task DeleteRelatedDataAsync(int videoId)
        {
            // 1. 取得所有留言 ID (用於刪除留言按讚)
            var commentIds = await _context.Comments
                .Where(c => c.VideoId == videoId)
                .Select(c => c.CommentId)
                .ToListAsync();

            // 2. 刪除留言按讚 (CommentLikes)
            if (commentIds.Any())
            {
                var commentLikesToDelete = await _context.CommentLikes
                    .Where(cl => commentIds.Contains(cl.CommentId))
                    .ToListAsync();

                if (commentLikesToDelete.Any())
                {
                    _context.CommentLikes.RemoveRange(commentLikesToDelete);
                    _logger.LogInformation("刪除 {Count} 筆留言按讚記錄", commentLikesToDelete.Count);
                }
            }

            // 3. 刪除留言 (Comments)
            var commentsToDelete = await _context.Comments
                .Where(c => c.VideoId == videoId)
                .ToListAsync();

            if (commentsToDelete.Any())
            {
                _context.Comments.RemoveRange(commentsToDelete);
                _logger.LogInformation("刪除 {Count} 筆留言", commentsToDelete.Count);
            }

            // 4. 刪除影片按讚 (Likes)
            var likesToDelete = await _context.Likes
                .Where(l => l.VideoId == videoId)
                .ToListAsync();

            if (likesToDelete.Any())
            {
                _context.Likes.RemoveRange(likesToDelete);
                _logger.LogInformation("刪除 {Count} 筆按讚記錄", likesToDelete.Count);
            }

            // 5. 刪除觀看記錄 (Views)
            var viewsToDelete = await _context.Views
                .Where(v => v.VideoId == videoId)
                .ToListAsync();

            if (viewsToDelete.Any())
            {
                _context.Views.RemoveRange(viewsToDelete);
                _logger.LogInformation("刪除 {Count} 筆觀看記錄", viewsToDelete.Count);
            }

            await _context.SaveChangesAsync();
        }
    }

    #region 結果類別

    /// <summary>
    /// 影片刪除結果
    /// </summary>
    public class VideoDeleteResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
        public VideoDeleteStatus Status { get; set; }
        public FileDeleteInfo? FileInfo { get; set; }

        public static VideoDeleteResult Success(FileDeleteInfo fileInfo)
        {
            return new VideoDeleteResult
            {
                IsSuccess = true,
                Status = VideoDeleteStatus.Success,
                Message = "影片已成功刪除",
                FileInfo = fileInfo
            };
        }

        public static VideoDeleteResult NotFound(string message)
        {
            return new VideoDeleteResult
            {
                IsSuccess = false,
                Status = VideoDeleteStatus.NotFound,
                Message = message
            };
        }

        public static VideoDeleteResult Forbidden(string message)
        {
            return new VideoDeleteResult
            {
                IsSuccess = false,
                Status = VideoDeleteStatus.Forbidden,
                Message = message
            };
        }

        public static VideoDeleteResult AlreadyDeleted(string message)
        {
            return new VideoDeleteResult
            {
                IsSuccess = false,
                Status = VideoDeleteStatus.AlreadyDeleted,
                Message = message
            };
        }

        public static VideoDeleteResult Error(string message)
        {
            return new VideoDeleteResult
            {
                IsSuccess = false,
                Status = VideoDeleteStatus.Error,
                Message = message
            };
        }
    }

    public enum VideoDeleteStatus
    {
        Success,
        NotFound,
        Forbidden,
        AlreadyDeleted,
        Error
    }

    /// <summary>
    /// 檔案刪除資訊
    /// </summary>
    public class FileDeleteInfo
    {
        public bool VideoDeleted { get; set; }
        public bool ThumbnailDeleted { get; set; }
    }

    #endregion
}
