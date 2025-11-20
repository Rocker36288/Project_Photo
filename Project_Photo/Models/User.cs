using System;
using System.Collections.Generic;

namespace Project_Photo.Models;

public partial class User
{
    public long UserId { get; set; }

    public string Account { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? Phone { get; set; }

    public string Password { get; set; } = null!;

    public string AccountType { get; set; } = null!;

    public string AccountStatus { get; set; } = null!;

    public string RegistrationSource { get; set; } = null!;

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<UserPrivacySetting> UserPrivacySettings { get; set; } = new List<UserPrivacySetting>();

    public virtual UserProfile? UserProfile { get; set; }

    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    public virtual ICollection<UserSession> UserSessions { get; set; } = new List<UserSession>();
}
