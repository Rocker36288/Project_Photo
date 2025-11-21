using System;
using System.Collections.Generic;

namespace Project_Photo.Areas.Videos.Models;

public partial class Channel
{
    public long ChannelId { get; set; }

    public string ChannelName { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdateAt { get; set; }
}
