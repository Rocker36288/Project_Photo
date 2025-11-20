using System;
using System.Collections.Generic;

namespace Project_Photo.Models;

public partial class UserRole
{
    public long UserRoleId { get; set; }

    public long UserId { get; set; }

    public int RoleTypeId { get; set; }

    public long? AssignedBy { get; set; }

    public DateTime AssignedAt { get; set; }

    public bool IsActive { get; set; }

    public DateTime? ExpiredAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual UserRoleType RoleType { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
