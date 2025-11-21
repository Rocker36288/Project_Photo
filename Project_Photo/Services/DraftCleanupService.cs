namespace Project_Photo.services;

using Microsoft.Extensions.Hosting;

using Microsoft.Extensions.DependencyInjection;

using Microsoft.Extensions.Logging;

using System;

using System.IO;

using System.Linq;

using System.Threading;

using System.Threading.Tasks;
using Project_Photo.Areas.Videos.Models;



// 背景服務：定期清理過期的草稿

public class DraftCleanupService : BackgroundService

{

    private readonly IServiceProvider _serviceProvider;

    private readonly ILogger<DraftCleanupService> _logger;

    private readonly TimeSpan _cleanupInterval = TimeSpan.FromSeconds(10); // 每 6 小時執行一次

    private readonly TimeSpan _draftExpiration = TimeSpan.FromHours(24); // 草稿過期時間 24 小時



    public DraftCleanupService(

        IServiceProvider serviceProvider,

        ILogger<DraftCleanupService> logger)

    {

        _serviceProvider = serviceProvider;

        _logger = logger;

    }



    protected override async Task ExecuteAsync(CancellationToken stoppingToken)

    {

        _logger.LogInformation("草稿清理服務已啟動");



        while (!stoppingToken.IsCancellationRequested)

        {

            try

            {

                await CleanupExpiredDrafts();

            }

            catch (Exception ex)

            {

                _logger.LogError(ex, "清理草稿時發生錯誤");

            }



            // 等待下一次執行

            await Task.Delay(_cleanupInterval, stoppingToken);

        }



        _logger.LogInformation("草稿清理服務已停止");

    }



    private async Task CleanupExpiredDrafts()

    {

        using var scope = _serviceProvider.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<VideosDbContext>(); // 替換為你的 DbContext



        var expirationTime = DateTime.Now.Add(-_draftExpiration);



        // 查找過期的草稿

        var expiredDrafts = context.Videos
    .Where(v => v.ProcessStatus != "published")
    .ToList();



        if (expiredDrafts.Count == 0)

        {

            _logger.LogInformation("沒有需要清理的過期草稿");

            return;

        }



        _logger.LogInformation($"找到 {expiredDrafts.Count} 個過期草稿，準備清理");



        int deletedCount = 0;

        int errorCount = 0;



        foreach (var draft in expiredDrafts)

        {

            try

            {

                // 刪除影片檔案

                if (!string.IsNullOrEmpty(draft.VideoUrl))

                {

                    var videoPath = Path.Combine(

                        Directory.GetCurrentDirectory(),

                        "wwwroot",

                        draft.VideoUrl.TrimStart('/')

                    );



                    if (File.Exists(videoPath))

                    {

                        File.Delete(videoPath);

                        _logger.LogInformation($"已刪除影片檔案: {videoPath}");

                    }

                }



                // 刪除縮圖檔案

                if (!string.IsNullOrEmpty(draft.ThumbnailUrl))

                {

                    var thumbnailPath = Path.Combine(

                        Directory.GetCurrentDirectory(),

                        "wwwroot",

                        draft.ThumbnailUrl.TrimStart('/')

                    );



                    if (File.Exists(thumbnailPath))

                    {

                        File.Delete(thumbnailPath);

                        _logger.LogInformation($"已刪除縮圖檔案: {thumbnailPath}");

                    }

                }



                // 從資料庫刪除

                context.Videos.Remove(draft);

                deletedCount++;



                _logger.LogInformation(

                    $"已清理過期草稿 - VideoId: {draft.VideoId}, " +

                    $"建立時間: {draft.CreatedAt}, 最後更新: {draft.UpdateAt}"

                );

            }

            catch (Exception ex)

            {

                errorCount++;

                _logger.LogError(ex, $"清理草稿失敗 - VideoId: {draft.VideoId}");

            }

        }



        await context.SaveChangesAsync();



        _logger.LogInformation(

            $"草稿清理完成 - 成功: {deletedCount}, 失敗: {errorCount}"

        );

    }

} 




// 在 Program.cs 或 Startup.cs 中註冊服務

// builder.Services.AddHostedService<DraftCleanupService>();


