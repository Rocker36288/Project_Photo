using System;
using System.Collections.Generic;

namespace Project_Photo.Models;

public partial class UserProfile
{
    public long UserId { get; set; }

    public string? DisplayName { get; set; }

    public string? Avatar { get; set; }

    public string? CoverImage { get; set; }

    public string? Bio { get; set; }

    public string? Website { get; set; }

    public string? Location { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
