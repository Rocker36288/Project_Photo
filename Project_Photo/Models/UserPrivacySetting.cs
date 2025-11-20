using System;
using System.Collections.Generic;

namespace Project_Photo.Models;

public partial class UserPrivacySetting
{
    public long PrivacyId { get; set; }

    public long UserId { get; set; }

    public string FieldName { get; set; } = null!;

    public string Visibility { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
