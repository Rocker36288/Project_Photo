using System;
using System.Collections.Generic;

namespace Project_Photo.Areas.Videos.Models;

public partial class CommentLike
{
    public long UserId { get; set; }

    public int CommentId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Comment Comment { get; set; } = null!;
}
