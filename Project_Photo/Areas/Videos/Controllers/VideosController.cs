using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Project_Photo.Areas.Videos.Models;

namespace Project_Photo.Areas.Videos.Controllers
{
    [Area("Videos")]
    public class VideosController : Controller
    {
        private readonly VideosDbContext _context;
        private readonly IWebHostEnvironment _env;

        public VideosController(VideosDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // GET: Videos/Videos
        public async Task<IActionResult> Index()
        {
            return View(await _context.Videos.ToListAsync());
        }

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

            return View(video);
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

        // STEP 3：更新影片資訊
        [HttpPost]
        public async Task<IActionResult> UpdateInfo([FromBody] VideoUpdateModel model)
        {
            Console.WriteLine($"UpdateInfo - VideoId: {model.VideoId}, Title: {model.Title}");

            var video = await _context.Videos.FindAsync(model.VideoId);
            if (video == null)
            {
                Console.WriteLine($"Video not found: {model.VideoId}");
                return NotFound();
            }

            video.Title = model.Title ?? "";
            video.Description = model.Description ?? "";
            video.UpdateAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return Ok(new { success = true });
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



        public class VideoUpdateModel
        {
            public int VideoId { get; set; }
            public string Title { get; set; }
            public string Description { get; set; }
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
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("VideoId,ChannelId,Title,Description,VideoUrl,ThumbnailUrl,Duration,Resolution,FileSize,ProcessStatus,PrivacyStatus,CreatedAt,UpdateAt")] Video video)
        {
            if (id != video.VideoId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(video);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!VideoExists(video.VideoId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(video);
        }

        // GET: Videos/Videos/Delete/5
        public async Task<IActionResult> Delete(int? id)
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

            return View(video);
        }

        // POST: Videos/Videos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var video = await _context.Videos.FindAsync(id);
            if (video != null)
            {
                _context.Videos.Remove(video);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool VideoExists(int id)
        {
            return _context.Videos.Any(e => e.VideoId == id);
        }
    }
}
