using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations; // 引入此命名空間
using System.ComponentModel.DataAnnotations.Schema; // 引入此命名空間

namespace Project_Photo.Areas.Videos.Models;

// ✨ 強制映射到 [Video].[Channels] 表格 ✨
[Table("Channels", Schema = "Video")]
public partial class Channel
{
    // ✨ [Key] 標記為主鍵，[Column("ChannelId")] 強制欄位名稱
    [Key]
    [Column("ChannelId")]
    public long ChannelId { get; set; }

    [Column("ChannelName")]
    [StringLength(50)] // 這裡假設您想在 Model 層次定義最大長度
    public string ChannelName { get; set; } = null!;

    [StringLength(500)]
    public string? Description { get; set; }

    // 這些屬性名稱通常會與資料庫欄位名稱匹配，不需要 Column 特性，但保留它更安全
    public DateTime CreatedAt { get; set; }

    public DateTime UpdateAt { get; set; }

    // 導覽屬性 (如果它們在 Model 中定義)
    // public virtual User User { get; set; } // 如果您在此處定義了 User
}
