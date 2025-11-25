using System;
using System.Collections.Generic;

namespace Project_Photo.Areas.Videos.Models;

public partial class Like
{
    public long UserId { get; set; }

    public int VideoId { get; set; }

    public DateTime CreatedAt { get; set; }
}
