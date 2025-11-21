using System;
using System.Collections.Generic;

namespace Project_Photo.Areas.Videos.Models;

public partial class View
{
    public int VideoId { get; set; }

    public long UserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdateAt { get; set; }
}
