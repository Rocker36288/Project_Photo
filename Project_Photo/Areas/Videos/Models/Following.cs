using System;
using System.Collections.Generic;

namespace Project_Photo.Areas.Videos.Models;

public partial class Following
{
    public long UserId { get; set; }

    public long ChannelId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Channel Channel { get; set; } = null!;
}
