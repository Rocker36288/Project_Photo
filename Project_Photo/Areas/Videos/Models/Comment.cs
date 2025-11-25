using System;
using System.Collections.Generic;

namespace Project_Photo.Areas.Videos.Models;

public partial class Comment
{
    public int CommentId { get; set; }

    public int VideoId { get; set; }

    public long UserId { get; set; }

    public string CommenContent { get; set; } = null!;

    public int? FatherId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdateAt { get; set; }

    public virtual Video Video { get; set; } = null!;
}
