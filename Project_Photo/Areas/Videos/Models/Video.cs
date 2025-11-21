using System;
using System.Collections.Generic;

namespace Project_Photo.Areas.Videos.Models;

public partial class Video
{
    public int VideoId { get; set; }

    public long ChannelId { get; set; }

    public string? Title { get; set; }

    public string? Description { get; set; }

    public string? VideoUrl { get; set; }

    public string? ThumbnailUrl { get; set; }

    public int Duration { get; set; }

    public string? Resolution { get; set; }

    public long? FileSize { get; set; }

    public string ProcessStatus { get; set; } = null!;

    public string PrivacyStatus { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdateAt { get; set; }
}
