using Project_Photo.Data;
using Project_Photo.Areas.Videos.Models;

namespace Project_Photo.Services
{
    public interface IChannelService
    {
        // 定義創建頻道的方法，傳入新使用者的 ID 和名稱
        Task CreateDefaultChannelForUser(long userId, string username);
    }
    public class ChannelService : IChannelService
    {
        private readonly VideosDbContext _context;

        public ChannelService(VideosDbContext context)
        {
            _context = context;
        }

        public async Task CreateDefaultChannelForUser(long userId, string username)
        {
            // 1. 建立新的 Channel 實例
            var newChannel = new Channel
            {
                // 將 ChannelId 設為傳入的 UserId
                ChannelId = userId,
                ChannelName = $"{username}'s Channel",
                Description = $"歡迎來到 {username} 的頻道！",
                CreatedAt = DateTime.UtcNow,
                UpdateAt = DateTime.UtcNow,
            };

            // 2. 儲存 Channel 到資料庫
            _context.Channels.Add(newChannel);

            // 由於 User 已經在 Controller 中儲存過，這裡只需要儲存 Channel 即可。
            // 如果這個 Service 負責的任務比較單一，可以只在這裡 SaveChangesAsync。
            await _context.SaveChangesAsync();
        }
    }
}
