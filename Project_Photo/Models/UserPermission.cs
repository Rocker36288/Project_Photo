using System;
using System.Collections.Generic;

namespace Project_Photo.Models;

public partial class UserPermission
{
    public int PermissionId { get; set; }

    public string PermissionCode { get; set; } = null!;

    public string PermissionName { get; set; } = null!;

    public string? PermissionDescription { get; set; }

    public int CategoryId { get; set; }

    public int SystemId { get; set; }

    public int? ParentPermissionId { get; set; }

    public bool IsActive { get; set; }

    public virtual UserPermissionCategory Category { get; set; } = null!;
}
