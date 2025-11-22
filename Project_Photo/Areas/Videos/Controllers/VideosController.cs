using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Project_Photo.Areas.Videos.Models;
using Project_Photo.Areas.Videos.Models.ViewModels;
using Project_Photo.Areas.Videos.Services;
using Project_Photo.Models;

namespace Project_Photo.Areas.Videos.Controllers
{
    [Area("Videos")]
    public class VideosController : Controller
    {

        private readonly VideosDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IVideoDeleteService _deleteService;
        //private readonly ILogger<VideoController> _logger;

        public VideosController(VideosDbContext context, IWebHostEnvironment env, IVideoDeleteService deleteService)
        {
            _deleteService = deleteService;
            _context = context;
            _env = env;
        }

        
        // GET: Videos/Videos
        public async Task<IActionResult> Index(
            string searchTerm = "",
            string searchBy = "title",
            string sortBy = "date",
            string sortOrder = "desc",
            int page = 1)
        {
            const int pageSize = 30;

            // 基礎查詢
            var query = _context.Videos.AsQueryable();

            // 搜尋條件
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                switch (searchBy.ToLower())
                {
                    case "title":
                        query = query.Where(v => v.Title.Contains(searchTerm));
                        break;
                    case "username":
                        // 假設 Video 有 User 導航屬性
                        //query = query.Where(v => v.ChannelId.UserName.Contains(searchTerm));
                        break;
                    case "date":
                        // 嘗試解析日期搜尋
                        if (DateTime.TryParse(searchTerm, out var searchDate))
                        {
                            query = query.Where(v => v.CreatedAt.Date == searchDate.Date);
                        }
                        break;
                }
            }

            // 計算總數
            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            // 排序
            query = sortBy.ToLower() switch
            {
                "views" => sortOrder == "asc"
                    ? query.OrderBy(v => _context.Views.Count(x => x.VideoId == v.VideoId))
                    : query.OrderByDescending(v => _context.Views.Count(x => x.VideoId == v.VideoId)),
                "likes" => sortOrder == "asc"
                    ? query.OrderBy(v => _context.Likes.Count(x => x.VideoId == v.VideoId))
                    : query.OrderByDescending(v => _context.Likes.Count(x => x.VideoId == v.VideoId)),
                "comments" => sortOrder == "asc"
                    ? query.OrderBy(v => _context.Comments.Count(x => x.VideoId == v.VideoId))
                    : query.OrderByDescending(v => _context.Comments.Count(x => x.VideoId == v.VideoId)),
                _ => sortOrder == "asc" // date (預設)
                    ? query.OrderBy(v => v.CreatedAt)
                    : query.OrderByDescending(v => v.CreatedAt)
            };

            // 分頁
            var videos = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(v => new VideoViewModel
                {
                    Video = v,
                    ViewCount = _context.Views.Count(x => x.VideoId == v.VideoId),
                    LikeCount = _context.Likes.Count(x => x.VideoId == v.VideoId),
                    CommentCount = _context.Comments.Count(x => x.VideoId == v.VideoId),
                })
                .ToListAsync();

            var model = new VideoListViewModel
            {
                Videos = videos,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalItems = totalItems,
                PageSize = pageSize,
                SearchTerm = searchTerm,
                SearchBy = searchBy,
                SortBy = sortBy,
                SortOrder = sortOrder
            };

            return View(model);
        }


        [HttpGet]
        // GET: Videos/Videos/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var video = await _context.Videos
                .FirstOrDefaultAsync(m => m.VideoId == id);
            if (video == null)
            {
                return NotFound();
            }

            var model = await _context.Videos
               .Where(v => v.VideoId == id)
               .Select(v => new VideoViewModel
               {
                   Video = v,

                   // 這些你以後會做到，先預留
                   ViewCount = _context.Views.Count(x => x.VideoId == v.VideoId),
                   LikeCount = _context.Likes.Count(x => x.VideoId == v.VideoId),
                   CommentCount = _context.Comments.Count(x => x.VideoId == v.VideoId),

                   // 如果未來有 Report 表
                   // ReportCount = _context.Reports.Count(x => x.VideoId == v.VideoId),
               })
                .FirstOrDefaultAsync();

            return View(model);
        }

        //VIDEO創立流程

        // STEP 1：建立草稿 - 移除 ValidateAntiForgeryToken 或改用其他方式
        [HttpPost]
        public async Task<IActionResult> CreateDraft(int channelId)
        {
            // 手動驗證 token（如果需要）
            // 或者在前端用 FormData 而非 JSON

            var video = new Video
            {
                ChannelId = channelId,
                Title = "",
                Description = "",
                VideoUrl = "",
                ThumbnailUrl = "",
                Duration = 0,
                Resolution = "",
                FileSize = 0,
                ProcessStatus = "uploading",
                PrivacyStatus = "private",
                CreatedAt = DateTime.Now,
                UpdateAt = DateTime.Now
            };

            _context.Videos.Add(video);
            await _context.SaveChangesAsync();

            return Json(new { videoId = video.VideoId });
        }

        // STEP 2：上傳影片檔 - 移除 ValidateAntiForgeryToken，改用 header 驗證
        [HttpPost]
        [RequestSizeLimit(500_000_000)]
        [DisableRequestSizeLimit] // 或使用這個
        public async Task<IActionResult> UploadFile(int videoId, IFormFile videoFile)
        {
            // 記錄請求信息以便調試
            Console.WriteLine($"UploadFile called - VideoId: {videoId}");
            Console.WriteLine($"File received: {videoFile?.FileName}, Size: {videoFile?.Length}");

            if (videoFile == null || videoFile.Length == 0)
            {
                Console.WriteLine("No file uploaded");
                return BadRequest(new { success = false, message = "No file uploaded" });
            }

            if (videoFile.Length > 500_000_000)
            {
                Console.WriteLine($"File too large: {videoFile.Length}");
                return BadRequest(new { success = false, message = "File too large" });
            }

            try
            {
                // 取得副檔名
                var ext = Path.GetExtension(videoFile.FileName);
                var fileGuid = Guid.NewGuid().ToString();
                var fileName = $"{fileGuid}{ext}";

                // 使用絕對路徑
                var wwwrootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var videosPath = Path.Combine(wwwrootPath, "videos");
                Directory.CreateDirectory(videosPath);

                var savePath = Path.Combine(videosPath, fileName);
                Console.WriteLine($"Saving to: {savePath}");

                // 儲存檔案
                using (var stream = new FileStream(savePath, FileMode.Create))
                {
                    await videoFile.CopyToAsync(stream);
                }

                Console.WriteLine("File saved successfully");

                // 更新資料庫
                var video = await _context.Videos.FindAsync(videoId);
                if (video == null)
                {
                    Console.WriteLine($"Video not found: {videoId}");
                    return NotFound(new { success = false, message = "Video not found" });
                }

                video.VideoUrl = $"/videos/{fileName}";
                video.FileSize = videoFile.Length;
                video.ProcessStatus = "uploaded";
                video.UpdateAt = DateTime.Now;

                // 生成縮圖
                string thumbnailError = null;
                try
                {
                    var appBaseDir = AppDomain.CurrentDomain.BaseDirectory;
                    var ffmpegDir = Path.Combine(appBaseDir, "FFmpeg");

                    Console.WriteLine($"FFmpeg directory: {ffmpegDir}");

                    var ffmpegExe = Path.Combine(ffmpegDir, "ffmpeg.exe");
                    if (!System.IO.File.Exists(ffmpegExe))
                        throw new Exception($"FFmpeg not found at: {ffmpegExe}");

                    Xabe.FFmpeg.FFmpeg.SetExecutablesPath(ffmpegDir);

                    // 改成 /images/videos/ 目錄
                    var thumbnailDir = Path.Combine(wwwrootPath, "images", "videos");
                    Directory.CreateDirectory(thumbnailDir);
                    var thumbnailFilePath = Path.Combine(thumbnailDir, $"{fileGuid}.jpg");

                    var mediaInfo = await Xabe.FFmpeg.FFmpeg.GetMediaInfo(savePath);
                    var videoStream = mediaInfo.VideoStreams.FirstOrDefault();

                    if (videoStream != null)
                    {
                        var videoDuration = videoStream.Duration;
                        var seekTime = videoDuration.TotalSeconds > 2
                            ? TimeSpan.FromSeconds(1)
                            : TimeSpan.FromSeconds(0);

                        var conversion = await Xabe.FFmpeg.FFmpeg.Conversions.FromSnippet.Snapshot(
                            savePath,
                            thumbnailFilePath,
                            seekTime
                        );

                        conversion.AddParameter("-vframes 1");
                        conversion.AddParameter("-q:v 2");

                        await conversion.Start();

                        if (System.IO.File.Exists(thumbnailFilePath) &&
                            new FileInfo(thumbnailFilePath).Length > 0)
                        {
                            video.ThumbnailUrl = $"/images/videos/{fileGuid}.jpg";
                            video.Duration = (int)videoDuration.TotalSeconds;
                            video.Resolution = $"{videoStream.Width}x{videoStream.Height}";
                        }
                        else
                        {
                            throw new Exception("Thumbnail file not created or empty");
                        }
                    }
                    else
                    {
                        throw new Exception("No video stream found");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Thumbnail error: {ex.Message}");
                    thumbnailError = "縮圖生成失敗，但不影響影片上傳";
                    video.ThumbnailUrl = "";
                }

                await _context.SaveChangesAsync();
                Console.WriteLine("Database updated successfully");

                return Ok(new
                {
                    success = true,
                    filePath = video.VideoUrl,
                    thumbnail = video.ThumbnailUrl,
                    thumbnailError = thumbnailError
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Upload error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // STEP 3：更新影片資訊 (修改版，加入 Privacy 支援)
        [HttpPost]
        public async Task<IActionResult> UpdateInfo([FromBody] VideoUpdateModel model)
        {
            Console.WriteLine($"UpdateInfo - VideoId: {model.VideoId}, Title: {model.Title}, Privacy: {model.Privacy}");

            var video = await _context.Videos.FindAsync(model.VideoId);
            if (video == null)
            {
                Console.WriteLine($"Video not found: {model.VideoId}");
                return NotFound();
            }

            video.Title = model.Title ?? "";
            video.Description = model.Description ?? "";

            // 更新隱私設定
            if (!string.IsNullOrEmpty(model.Privacy))
            {
                video.PrivacyStatus = model.Privacy; // 假設你的 Video 模型有 Privacy 屬性
            }

            video.UpdateAt = DateTime.Now;

            await _context.SaveChangesAsync();

            Console.WriteLine($"影片資訊已更新: {video.Title}");

            return Ok(new { success = true });
        }

        // STEP 3-1：上傳縮圖
        [HttpPost]
        public async Task<IActionResult> UploadThumbnail(int videoId, IFormFile thumbnail)
        {
            Console.WriteLine($"UploadThumbnail - VideoId: {videoId}");

            try
            {

                var video = await _context.Videos.FindAsync(videoId);
                if (video == null)
                {
                    Console.WriteLine($"Video not found: {videoId}");
                    return NotFound(new { success = false, message = "影片不存在" });
                }

                if (thumbnail == null || thumbnail.Length == 0)
                {
                    return BadRequest(new { success = false, message = "未提供縮圖檔案" });
                }

                // 驗證檔案類型
                var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png" };
                if (!allowedTypes.Contains(thumbnail.ContentType.ToLower()))
                {
                    return BadRequest(new { success = false, message = "不支援的圖片格式，請使用 JPG 或 PNG" });
                }

                // 驗證檔案大小 (最大 2MB)
                if (thumbnail.Length > 2 * 1024 * 1024)
                {
                    return BadRequest(new { success = false, message = "圖片大小不能超過 2MB" });
                }

                // 設定儲存路徑
                var uploadsFolder = Path.Combine(_env.WebRootPath, "images", "videos");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }


                // 刪除舊的縮圖
                if (!string.IsNullOrEmpty(video.ThumbnailUrl))
                {
                    var oldThumbnailPath = Path.Combine(_env.WebRootPath, video.ThumbnailUrl.TrimStart('/'));
                    if (System.IO.File.Exists(oldThumbnailPath))
                    {
                        try
                        {
                            System.IO.File.Delete(oldThumbnailPath);
                            Console.WriteLine($"已刪除舊縮圖: {oldThumbnailPath}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"刪除舊縮圖失敗: {ex.Message}");
                        }
                    }
                }

                // 生成新的檔案名稱
                var extension = Path.GetExtension(thumbnail.FileName);
                var fileName = $"thumb_{videoId}_{DateTime.Now:yyyyMMddHHmmss}{extension}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                // 儲存檔案
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await thumbnail.CopyToAsync(stream);
                }

                // 更新資料庫
                video.ThumbnailUrl = $"/images/videos/{fileName}";
                video.UpdateAt = DateTime.Now;
                await _context.SaveChangesAsync();

                Console.WriteLine($"縮圖上傳成功: {video.ThumbnailUrl}");

                return Ok(new
                {
                    success = true,
                    thumbnailUrl = video.ThumbnailUrl,
                    message = "縮圖上傳成功"
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"UploadThumbnail error: {ex.Message}");
                return StatusCode(500, new { success = false, message = $"上傳失敗: {ex.Message}" });
            }
        }


        // STEP 4：發佈影片
        [HttpPost]
        public async Task<IActionResult> Publish([FromBody] PublishModel model)
        {
            Console.WriteLine($"Publish - VideoId: {model.VideoId}");

            var video = await _context.Videos.FindAsync(model.VideoId);
            if (video == null)
            {
                Console.WriteLine($"Video not found: {model.VideoId}");
                return Json(new { success = false });
            }

            video.ProcessStatus = "published";
            video.UpdateAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                videoUrl = Url.Action("Details", "Videos", new { id = video.VideoId })
            });
        }

        // 刪除草稿（清理未完成的上傳）
        [HttpPost]
        public async Task<IActionResult> DeleteDraft([FromBody] DeleteDraftModel model)
        {
            Console.WriteLine($"DeleteDraft - VideoId: {model.VideoId}");

            try
            {
                var video = await _context.Videos.FindAsync(model.VideoId);
                if (video == null)
                {
                    Console.WriteLine($"Video not found: {model.VideoId}");
                    return NotFound(new { success = false, message = "Video not found" });
                }

                // 只能刪除未發佈的影片
                if (video.ProcessStatus == "published")
                {
                    Console.WriteLine($"Cannot delete published video: {model.VideoId}");
                    return BadRequest(new { success = false, message = "Cannot delete published video" });
                }

                // 刪除實體檔案
                if (!string.IsNullOrEmpty(video.VideoUrl))
                {
                    var videoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", video.VideoUrl.TrimStart('/'));
                    if (System.IO.File.Exists(videoPath))
                    {
                        System.IO.File.Delete(videoPath);
                        Console.WriteLine($"Deleted video file: {videoPath}");
                    }
                }

                // 刪除縮圖檔案
                if (!string.IsNullOrEmpty(video.ThumbnailUrl))
                {
                    var thumbnailPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", video.ThumbnailUrl.TrimStart('/'));
                    if (System.IO.File.Exists(thumbnailPath))
                    {
                        System.IO.File.Delete(thumbnailPath);
                        Console.WriteLine($"Deleted thumbnail file: {thumbnailPath}");
                    }
                }

                // 從資料庫刪除記錄
                _context.Videos.Remove(video);
                await _context.SaveChangesAsync();

                Console.WriteLine($"Draft deleted successfully: {model.VideoId}");
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DeleteDraft error: {ex.Message}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        public class DeleteDraftModel
        {
            public int VideoId { get; set; }
        }
        public class PublishModel
        {
            public int VideoId { get; set; }
        }



        // 更新 VideoUpdateModel (如果還沒有 Privacy 屬性)
        public class VideoUpdateModel
        {
            public int VideoId { get; set; }
            public string? Title { get; set; }
            public string? Description { get; set; }
            public string? Privacy { get; set; } // 新增
        }

        // 顯示 create 頁面
        public IActionResult Create()
        {
            return View();
        }

        // GET: Videos/Videos/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var video = await _context.Videos.FindAsync(id);
            if (video == null)
            {
                return NotFound();
            }
            return View(video);
        }

        // POST: Videos/Videos/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
       
        //修改
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Video model, IFormFile ThumbnailFile)
        {
            if (id != model.VideoId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // 處理縮圖替換
                    if (ThumbnailFile != null && ThumbnailFile.Length > 0)
                    {
                        // 驗證檔案大小 (5MB)
                        if (ThumbnailFile.Length > 5 * 1024 * 1024)
                        {
                            ModelState.AddModelError("ThumbnailFile", "File size exceeds 5MB limit.");
                            return View(model);
                        }

                        // 驗證檔案類型
                        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                        var extension = Path.GetExtension(ThumbnailFile.FileName).ToLowerInvariant();
                        if (!allowedExtensions.Contains(extension))
                        {
                            ModelState.AddModelError("ThumbnailFile", "Invalid file type.");
                            return View(model);
                        }

                        // 直接覆蓋原始檔案路徑
                        // 假設 model.ThumbnailUrl 是類似 "/images/video/thumbnail_123.jpg" 的格式
                        var filePath = Path.Combine("wwwroot", model.ThumbnailUrl.TrimStart('/'));

                        // 確保目錄存在
                        Directory.CreateDirectory(Path.GetDirectoryName(filePath));

                        // 覆蓋儲存檔案
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await ThumbnailFile.CopyToAsync(stream);
                        }

                        // ThumbnailUrl 保持不變，因為是覆蓋同一個檔案
                    }

                    model.UpdateAt = DateTime.Now;
                    _context.Update(model);
                    await _context.SaveChangesAsync();

                    return RedirectToAction(nameof(Details), new { id = model.VideoId });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error updating video: {ex.Message}");
                }
            }
            return View(model);
        }

        // <summary>
        /// 刪除影片 (軟刪除) - API 方式
        /// DELETE: Videos/Videos/DeleteVideo/5
        /// </summary>
        [HttpDelete("DeleteVideo/{videoId}")]
        [Route("Videos/Videos/DeleteVideo/{videoId}")]
        public async Task<IActionResult> DeleteVideo(int videoId)
        {
            try
            {
                //// 從 Claims 取得當前用戶 ID
                //var userId = GetCurrentUserId();
                //if (userId == null)
                //{
                //    return Unauthorized(new { message = "請先登入" });
                //}

                //var result = await _deleteService.SoftDeleteVideoAsync(videoId, userId.Value);

                long testUserId = 1;//測試用
                var result = await _deleteService.SoftDeleteVideoAsync(videoId, testUserId);

                return result.Status switch
                {
                    VideoDeleteStatus.Success => Ok(new
                    {
                        success = true,
                        message = result.Message,
                        data = new
                        {
                            videoDeleted = result.FileInfo?.VideoDeleted ?? false,
                            thumbnailDeleted = result.FileInfo?.ThumbnailDeleted ?? false
                        }
                    }),
                    VideoDeleteStatus.NotFound => NotFound(new { success = false, message = result.Message }),
                    VideoDeleteStatus.Forbidden => StatusCode(403, new { success = false, message = result.Message }),
                    VideoDeleteStatus.AlreadyDeleted => BadRequest(new { success = false, message = result.Message }),
                    _ => StatusCode(500, new { success = false, message = result.Message })
                };
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "刪除影片時發生未預期的錯誤");
                return StatusCode(500, new { success = false, message = "系統錯誤，請稍後再試" });
            }
        }

        /// <summary>
        /// 永久刪除影片 (硬刪除) - 管理員功能
        /// DELETE: Videos/Video/PermanentDelete/5
        /// </summary>
        [HttpDelete("PermanentDelete/{videoId}")]
        public async Task<IActionResult> PermanentDeleteVideo(int videoId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return Unauthorized(new { message = "請先登入" });
                }

                // 可以在這裡加入管理員權限檢查
                // if (!User.IsInRole("Admin")) { return Forbid(); }

                var result = await _deleteService.HardDeleteVideoAsync(videoId, userId.Value);

                return result.Status switch
                {
                    VideoDeleteStatus.Success => Ok(new
                    {
                        success = true,
                        message = "影片已永久刪除",
                        data = new
                        {
                            videoDeleted = result.FileInfo?.VideoDeleted ?? false,
                            thumbnailDeleted = result.FileInfo?.ThumbnailDeleted ?? false
                        }
                    }),
                    VideoDeleteStatus.NotFound => NotFound(new { success = false, message = result.Message }),
                    VideoDeleteStatus.Forbidden => StatusCode(403, new { success = false, message = result.Message }),
                    _ => StatusCode(500, new { success = false, message = result.Message })
                };
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "永久刪除影片時發生未預期的錯誤");
                return StatusCode(500, new { success = false, message = "系統錯誤，請稍後再試" });
            }
        }

        /// <summary>
        /// 取得當前登入用戶的 ID
        /// </summary>
        private long? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                           ?? User.FindFirst("UserId")?.Value;

            if (long.TryParse(userIdClaim, out long userId))
            {
                return userId;
            }
            return null;
        }

        private bool VideoExists(int id)
        {
            return _context.Videos.Any(e => e.VideoId == id);
        }
    }
}
