using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Project_Photo.Areas.Videos.Models;
using Project_Photo.Areas.Videos.Models.ViewModels;
using Project_Photo.Models;
using Project_Photo.Services; // 確保引用了服務所在的命名空間

namespace Project_Photo.Areas.Videos.Controllers
{
    [Area("Videos")]
    public class ChannelController : Controller
    {
        private readonly IChannelService _channelService;
        private readonly VideosDbContext _videosContext; // 用於 Channel 相關操作
        private readonly AaContext _aaContext;       // 用於 User 相關操作 (假設您的 Context 名稱是 AaContext)

        // 💡 建構函式注入
        public ChannelController(IChannelService channelService, VideosDbContext videosContext, AaContext aaContext)
        {
            _channelService = channelService;
            _videosContext = videosContext;
            _aaContext = aaContext;
        }


        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // 獲取所有頻道資料
            var channels = await _videosContext.Channels
                .OrderByDescending(c => c.CreatedAt) // 依創建時間排序
                .ToListAsync();

            // 將資料傳遞給 View
            return View("Index", channels); // 假設您的 View 命名為 Index.cshtml
        }




        [HttpGet]
        public async Task<IActionResult> Detail(int id)
        {
            // ✅ 步驟 1: 從 Videos Context 取得 Channel 資料
            var channel = await _videosContext.Channels
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.ChannelId == id);

            if (channel == null)
            {
                return NotFound($"找不到 ChannelId = {id} 的頻道");
            }

            // ✅ 步驟 2: 取得用戶資料（不載入導覽屬性）
            var user = await _aaContext.Users
                .Where(u => u.UserId == channel.ChannelId)
                .Select(u => new User
                {
                    UserId = u.UserId,
                    Account = u.Account
                })
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return NotFound($"找不到 UserId = {channel.ChannelId} 的用戶資料（頻道擁有者）");
            }

            // ✅ 步驟 3: 計算相關統計數據
            int followerCount = await _videosContext.Followings
                .CountAsync(f => f.ChannelId == id);

            // 🔧 取得頻道的所有影片（按上傳時間倒序排列）
            var videos = await _videosContext.Videos
                .Where(v => v.ChannelId == id)
                .OrderByDescending(v => v.CreatedAt)
                .ToListAsync();

            // 計算影片總數
            int videoCount = videos.Count;

            // ✅ 步驟 4: 建立 ViewModel
            var viewModel = new ChannelViewModel
            {
                Channel = channel,
                User = user,
                Videos = videos, // 🔧 改為傳入影片列表
                FollowerCount = followerCount,
                VideoCount = videoCount
            };

            return View(viewModel);
        }



        // 新增：用於後台批量初始化頻道的 Action
        [HttpPost]
        public async Task<IActionResult> InitializeChannels()
        {
            List<long> existingChannelIds = new List<long>();
            try
            {
                // 步驟 1 保持不變 (因為您說這一步現在是成功的)
                existingChannelIds = await _videosContext.Channels
                    .Select(c => c.ChannelId)
                    .ToListAsync();
            }
            catch (Microsoft.Data.SqlClient.SqlException ex)
            {
                // 捕捉並返回錯誤
                return Json(new { Success = false, Message = $"步驟 1 查詢 Channels 失敗。錯誤：{ex.Message}" });
            }

            // -------------------------------------------------------------------
            // ✨ 步驟 2 修正：使用明確投影 (Select) 來避免模型混淆 ✨
            // 我們只查詢 UserId 和 Account，強制 EF Core 忽略任何導覽屬性
            var usersDataWithoutChannel = await _aaContext.Users
                .Where(u => !existingChannelIds.Contains(u.UserId))
                .Select(u => new { u.UserId, u.Account }) // 僅選擇這兩個欄位
                .ToListAsync(); // 執行 AaContext 的查詢
                                // -------------------------------------------------------------------

            if (!usersDataWithoutChannel.Any())
            {
                return Json(new { Success = true, Count = 0, Message = "所有用戶的頻道都已存在。" });
            }

            int createdCount = 0;

            // ✨ 步驟 3 修正：新增 try-catch 塊來捕獲服務層的錯誤 ✨
            try
            {
                foreach (var userData in usersDataWithoutChannel)
                {
                    // 如果服務在這裡失敗，它會被捕獲
                    await _channelService.CreateDefaultChannelForUser(userData.UserId, userData.Account);
                    createdCount++;
                }
            }
            catch (Exception ex)
            {
                // 捕獲所有其他錯誤（例如 DbContext SaveChanges 失敗等）
                // 📢 返回一個明確的錯誤訊息，而不是讓控制器返回 HTTP 500
                return Json(new { Success = false, Message = $"服務層創建頻道失敗。錯誤詳情：{ex.Message}。內層錯誤：{ex.InnerException?.Message}" });
            }

            // 成功響應
            return Json(new
            {
                Success = true,
                Count = createdCount,
                Message = $"成功為 {createdCount} 位用戶創建了新頻道。"
            });
        }

        [HttpGet]
        // GET: Edit
        public async Task<IActionResult> Edit(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var Channels = await _videosContext.Channels.FindAsync(id);
            if (Channels == null)
            {
                return NotFound();
            }
            return View(Channels);
        }

        // 假設您的控制器或 PageModel 的 POST 處理方法
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, [Bind("ChannelId,ChannelName,Description,CreatedAt,UpdateAt")] Channel channel)
        {
            if (id != channel.ChannelId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // 關鍵步驟：在儲存之前，強制設定 UpdateAt 為當前時間
                    channel.UpdateAt = DateTime.Now;

                    // 如果您的 Entity Framework Core 追蹤了實體，並且您不希望覆蓋 CreatedAt，請確保該欄位不被更新
                    // 您可能需要先從資料庫獲取現有的 CreatedAt，然後再更新其他欄位。

                    // 範例：先從資料庫讀取舊資料，再更新可修改的欄位，並設定 UpdateAt
                    var channelToUpdate = await _videosContext.Channels.FindAsync(id);
                    if (channelToUpdate == null) return NotFound();

                    channelToUpdate.ChannelName = channel.ChannelName;
                    channelToUpdate.Description = channel.Description;
                    channelToUpdate.UpdateAt = DateTime.Now; // 設定系統時間

                    _videosContext.Update(channelToUpdate);
                    await _videosContext.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ChannelExists(channel.ChannelId))
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
            return View(channel);
        }
        private bool ChannelExists(long id)
        {
            // 使用 Any() 方法查詢資料庫中是否有 ChannelId 與傳入 ID 相符的記錄
            // 這是一種高效的檢查存在性的方式。
            return _videosContext.Channels.Any(e => e.ChannelId == id);
        }
    }
}
