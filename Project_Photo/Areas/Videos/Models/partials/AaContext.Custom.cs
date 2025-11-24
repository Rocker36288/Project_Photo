// AaContext.Custom.cs

using Microsoft.EntityFrameworkCore;
using Project_Photo.Areas.Videos.Models; // 引入 Channel 實體所在的命名空間

namespace Project_Photo.Models
{
    public partial class AaContext
    {
        // 實作 OnModelCreatingPartial 方法來新增跨 Context 的映射關係
        partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
        {
            // ----------------------------------------------------
            // 關鍵修正：告訴 AaContext Channel 實體所在的 Schema 和 Table
            // 這是為了防止 AaContext 在查詢 User 時，因為看到了 User.Channel 導覽屬性，
            // 而錯誤地將 Channel 欄位（如 ChannelId）包含在 User 表的 SELECT 語句中。
            modelBuilder.Entity<Channel>(entity =>
            {
                // 1. 強制映射 Channel 的位置
                entity.ToTable("Channels", "Video");

                // 2. 關係配置：User (AaContext) 和 Channel (VideosDbContext) 的共享主鍵
                entity.HasOne<User>()
                    .WithOne()
                    .HasForeignKey<Channel>(c => c.ChannelId);

                // 3. 確保 ChannelId 的值不是自動生成的
                entity.Property(e => e.ChannelId).ValueGeneratedNever();
            });
            // ----------------------------------------------------
        }
    }
}
