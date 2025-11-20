using System;
using System.Collections.Generic;

namespace Project_Photo.Models;

public partial class UserRoleType
{
    public int RoleTypeId { get; set; }

    public string RoleCode { get; set; } = null!;

    public string RoleName { get; set; } = null!;

    public string? RoleDescription { get; set; }

    public int RoleLevel { get; set; }

    public int? SystemId { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
