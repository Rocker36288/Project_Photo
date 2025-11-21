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
}
