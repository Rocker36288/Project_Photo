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
using Project_Photo.Services;
using User = Project_Photo.Areas.Videos.Models.User;
using Video = Project_Photo.Areas.Videos.Models.Video;

namespace Project_Photo.Areas.Videos.Controllers
{
    [Area("Videos")]
    public class VideosController : Controller
    {

        private readonly VideosDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IVideoDeleteService _deleteService;
        //private readonly ILogger<VideoController> _logger;
        private readonly IVideoAnalyticsService _analyticsService;

        public VideosController(VideosDbContext context, IWebHostEnvironment env, IVideoDeleteService deleteService,IVideoAnalyticsService analyticsService)
        {
            _deleteService = deleteService;
            _context = context;
            _env = env;
            _analyticsService = analyticsService; 
        }

        //Get
        public async Task<IActionResult> Index(
            string searchTerm = "",
            string searchBy = "title",
            string sortBy = "date",
            string sortOrder = "desc",
            int page = 1)
        {
            const int pageSize = 30;

            // 基礎查詢
            var query = _context.Videos
                .Include(v => v.Channel)
                .AsQueryable();

            // 搜尋條件
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                switch (searchBy.ToLower())
                {
                    case "title":
                        query = query.Where(v => v.Title.Contains(searchTerm));
                        break;
                    case "username":
                        query = query.Where(v => v.Channel.ChannelName.Contains(searchTerm));
                        break;
                    case "date":
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

            // 先投影包含統計的資料
            var projectedQuery = query.Select(v => new VideoViewModel
            {
                Video = v,
                ViewCount = _context.Views.Count(x => x.VideoId == v.VideoId),
                LikeCount = _context.Likes.Count(x => x.VideoId == v.VideoId),
                CommentCount = _context.Comments.Count(x => x.VideoId == v.VideoId)
            });

            // 在投影後排序（這樣可以對統計數據排序）
            projectedQuery = sortBy.ToLower() switch
            {
                "publisher" => sortOrder == "asc"
                    ? projectedQuery.OrderBy(v => v.Video.Channel.ChannelName)
                    : projectedQuery.OrderByDescending(v => v.Video.Channel.ChannelName),
                "views" => sortOrder == "asc"
                    ? projectedQuery.OrderBy(v => v.ViewCount)
                    : projectedQuery.OrderByDescending(v => v.ViewCount),
                "likes" => sortOrder == "asc"
                    ? projectedQuery.OrderBy(v => v.LikeCount)
                    : projectedQuery.OrderByDescending(v => v.LikeCount),
                "comments" => sortOrder == "asc"
                    ? projectedQuery.OrderBy(v => v.CommentCount)
                    : projectedQuery.OrderByDescending(v => v.CommentCount),
                _ => sortOrder == "asc"
                    ? projectedQuery.OrderBy(v => v.Video.CreatedAt)
                    : projectedQuery.OrderByDescending(v => v.Video.CreatedAt)
            };

            // 分頁
            var videos = await projectedQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
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

        //GET /api/videos/analytics/views/{videoId}?days=90
        //GET /api/videos/analytics/comments/{videoId}? days = 90
        //GET /api/videos/analytics/likes/{videoId}? days = 90
        //GET /api/videos/analytics/all/{videoId}? days = 90
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

            // 獲取統計數據
            var viewCount = await _context.Views
                .CountAsync(v => v.VideoId == id);

            var commentCount = await _context.Comments
                .CountAsync(c => c.VideoId == id);

            var likeCount = await _context.Likes
                .CountAsync(l => l.VideoId == id);

            // 獲取用戶資訊（如果需要）
            var user = await _context.Channels
                .Where(c => c.ChannelId == video.ChannelId)
                .Select(c => new User { UserId = c.ChannelId })
                .FirstOrDefaultAsync();

            // 獲取分析數據（預設 90 天）
            VideoAnalyticsData? analyticsData = null;
            try
            {
                analyticsData = await _analyticsService.GetAllAnalyticsAsync(video.VideoId, days: 90);
            }
            catch
            {
                // 如果獲取分析數據失敗，繼續顯示頁面但不包含圖表數據
                analyticsData = null;
            }

            var viewModel = new VideoViewModel
            {
                Video = video,
                User = user ?? new User(),
                ViewCount = viewCount,
                CommentCount = commentCount,
                LikeCount = likeCount,
                ReportCount = 0, // 如果有舉報表可以在此查詢
                AnalyticsData = analyticsData
            };

            return View(viewModel);
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

        // STEP 2: 上傳影片檔 - 完整改進版本
        [HttpPost]
        [RequestSizeLimit(500_000_000)]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> UploadFile(int videoId, IFormFile videoFile)
        {
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
                var ext = Path.GetExtension(videoFile.FileName);
                var fileGuid = Guid.NewGuid().ToString();
                var fileName = $"{fileGuid}{ext}";

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

                // 取得影片資訊並生成縮圖
                string thumbnailError = null;
                int videoDuration = 0;
                string videoResolution = "";

                try
                {
                    // FFmpeg 放在 wwwroot 底下
                    var ffmpegDir = Path.Combine(wwwrootPath, "FFmpeg");

                    var ffmpegExe = Path.Combine(ffmpegDir, "ffmpeg.exe");
                    var ffprobeExe = Path.Combine(ffmpegDir, "ffprobe.exe");

                    if (!System.IO.File.Exists(ffmpegExe))
                        throw new Exception($"FFmpeg not found at: {ffmpegExe}");
                    if (!System.IO.File.Exists(ffprobeExe))
                        throw new Exception($"FFprobe not found at: {ffprobeExe}");

                    Xabe.FFmpeg.FFmpeg.SetExecutablesPath(ffmpegDir);

                    // 步驟 1: 使用 FFprobe 取得影片資訊
                    Console.WriteLine("=== Getting video info with FFprobe ===");
                    var videoInfo = await GetVideoInfoWithFFprobe(ffprobeExe, savePath);

                    videoDuration = videoInfo.Duration;
                    videoResolution = videoInfo.Resolution;

                    Console.WriteLine($"Video Duration: {videoDuration} seconds");
                    Console.WriteLine($"Video Resolution: {videoResolution}");

                    // 步驟 2: 生成縮圖
                    var thumbnailDir = Path.Combine(wwwrootPath, "images", "videos");
                    Directory.CreateDirectory(thumbnailDir);
                    var thumbnailFilePath = Path.Combine(thumbnailDir, $"{fileGuid}.jpg");

                    var seekTime = videoDuration > 2
                        ? TimeSpan.FromSeconds(1)
                        : TimeSpan.FromSeconds(0);

                    Console.WriteLine($"=== Generating thumbnail at {seekTime.TotalSeconds}s ===");

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
                        video.Duration = videoDuration;
                        video.Resolution = videoResolution;
                        Console.WriteLine("Thumbnail generated successfully");
                    }
                    else
                    {
                        throw new Exception("Thumbnail file not created or empty");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"=== Thumbnail/Info Error ===");
                    Console.WriteLine($"Error: {ex.Message}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                    thumbnailError = "影片資訊或縮圖生成失敗,但不影響影片上傳";
                    video.ThumbnailUrl = "";
                    video.Duration = videoDuration; // 即使縮圖失敗也保存時長
                    video.Resolution = videoResolution;
                }

                await _context.SaveChangesAsync();
                Console.WriteLine("Database updated successfully");

                return Ok(new
                {
                    success = true,
                    filePath = video.VideoUrl,
                    thumbnail = video.ThumbnailUrl,
                    duration = video.Duration,
                    resolution = video.Resolution,
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

        // 輔助方法: 使用 FFprobe 取得影片資訊
        private async Task<(int Duration, string Resolution)> GetVideoInfoWithFFprobe(string ffprobePath, string videoPath)
        {
            try
            {
                Console.WriteLine($"FFprobe path: {ffprobePath}");
                Console.WriteLine($"Video path: {videoPath}");

                // 使用更簡單可靠的 FFprobe 命令
                var process = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = ffprobePath,
                        // 使用 JSON 格式輸出,更容易解析
                        Arguments = $"-v quiet -print_format json -show_format -show_streams \"{videoPath}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                Console.WriteLine($"FFprobe exit code: {process.ExitCode}");

                if (!string.IsNullOrEmpty(error))
                    Console.WriteLine($"FFprobe stderr: {error}");

                if (process.ExitCode != 0)
                    throw new Exception($"FFprobe failed with exit code {process.ExitCode}");

                // 解析 JSON 輸出
                using (var doc = System.Text.Json.JsonDocument.Parse(output))
                {
                    var root = doc.RootElement;

                    int width = 0, height = 0;
                    double duration = 0;

                    // 從 streams 中找影片流
                    if (root.TryGetProperty("streams", out var streams))
                    {
                        foreach (var stream in streams.EnumerateArray())
                        {
                            if (stream.TryGetProperty("codec_type", out var codecType) &&
                                codecType.GetString() == "video")
                            {
                                if (stream.TryGetProperty("width", out var w))
                                    width = w.GetInt32();
                                if (stream.TryGetProperty("height", out var h))
                                    height = h.GetInt32();

                                // 嘗試從 stream 取得 duration
                                if (stream.TryGetProperty("duration", out var d))
                                {
                                    if (d.ValueKind == System.Text.Json.JsonValueKind.String)
                                        double.TryParse(d.GetString(), out duration);
                                    else if (d.ValueKind == System.Text.Json.JsonValueKind.Number)
                                        duration = d.GetDouble();
                                }
                                break;
                            }
                        }
                    }

                    // 如果 stream 沒有 duration,從 format 取得
                    if (duration == 0 && root.TryGetProperty("format", out var format))
                    {
                        if (format.TryGetProperty("duration", out var d))
                        {
                            if (d.ValueKind == System.Text.Json.JsonValueKind.String)
                                double.TryParse(d.GetString(), out duration);
                            else if (d.ValueKind == System.Text.Json.JsonValueKind.Number)
                                duration = d.GetDouble();
                        }
                    }

                    string resolution = (width > 0 && height > 0) ? $"{width}x{height}" : "";
                    int durationSeconds = (int)Math.Round(duration);

                    Console.WriteLine($"=== Parsed Video Info ===");
                    Console.WriteLine($"Width: {width}, Height: {height}");
                    Console.WriteLine($"Duration: {durationSeconds} seconds ({duration} raw)");
                    Console.WriteLine($"Resolution: {resolution}");

                    return (durationSeconds, resolution);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== GetVideoInfoWithFFprobe Error ===");
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return (0, "");
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
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id,
            [Bind("VideoId,Title,Description,ProcessStatus,PrivacyStatus,ThumbnailUrl")] Video model,
            IFormFile? ThumbnailFile)
        {
            if (id != model.VideoId)
            {
                return NotFound();
            }

            // 1. 從資料庫讀取原始實體，包含所有唯讀資訊
            var originalVideo = await _context.Videos
                                             .AsNoTracking()
                                             .FirstOrDefaultAsync(v => v.VideoId == id);

            if (originalVideo == null)
            {
                return NotFound();
            }

            // =======================================================================
            // ** 關鍵修復：將唯讀屬性複製邏輯移到 ModelState.IsValid 之前！ **
            // 這樣，如果 ModelState 檢查失敗，返回 View 時 model 已經被正確修復。
            // =======================================================================

            // 2. 將原始實體的唯讀屬性複製回提交的 model (修復 Model Binder 帶來的空值)
            model.VideoUrl = originalVideo.VideoUrl;
            model.Duration = originalVideo.Duration;
            model.Resolution = originalVideo.Resolution;
            model.FileSize = originalVideo.FileSize;
            model.CreatedAt = originalVideo.CreatedAt;

            // 確保 ThumbnailUrl 在沒有新檔案時仍是舊值
            if (string.IsNullOrEmpty(model.ThumbnailUrl))
            {
                model.ThumbnailUrl = originalVideo.ThumbnailUrl;
            }

            // =======================================================================

            // 3. 現在執行 ModelState.IsValid 檢查
            if (ModelState.IsValid)
            {
                try
                {
                    // 4. 處理新縮圖上傳 (邏輯不變)
                    if (ThumbnailFile != null && ThumbnailFile.Length > 0)
                    {
                        // ... 檔案驗證和覆蓋儲存邏輯 ...
                        var filePath = Path.Combine("wwwroot", originalVideo.ThumbnailUrl.TrimStart('/'));
                        Directory.CreateDirectory(Path.GetDirectoryName(filePath));

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await ThumbnailFile.CopyToAsync(stream);
                        }
                    }

                    // 5. 更新修改時間並存檔
                    model.UpdateAt = DateTime.Now;

                    _context.Update(model);
                    await _context.SaveChangesAsync();

                    return RedirectToAction(nameof(Details), new { id = model.VideoId });
                }
                catch (DbUpdateConcurrencyException)
                {
                    // ... (並行存取錯誤處理邏輯) ...
                    if (!_context.Videos.Any(e => e.VideoId == id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error updating video: {ex.Message}");
                    // 執行 return View(model);
                }
            }

            // 6. 如果 ModelState.IsValid 失敗或 Try/Catch 捕獲到錯誤，則返回 View。
            // 此時 model 已經包含正確的唯讀資訊。
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
