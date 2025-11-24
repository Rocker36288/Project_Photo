using System;
using System.Collections.Generic;

namespace Project_Photo.Areas.Videos.Models;

public partial class Video
{
    public virtual Channel Channel { get; set; }
}
