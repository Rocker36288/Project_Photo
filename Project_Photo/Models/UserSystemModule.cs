using System;
using System.Collections.Generic;

namespace Project_Photo.Models;

public partial class UserSystemModule
{
    public int SystemId { get; set; }

    public string SystemCode { get; set; } = null!;

    public string SystemName { get; set; } = null!;

    public string? SystemDescription { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
