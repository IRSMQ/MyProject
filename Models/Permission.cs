using System;
using System.Collections.Generic;

namespace Test26.Models;

public partial class Permission : ISoftDeletable
{
    public Guid PermissionId { get; set; }

    public string PermissionName { get; set; } = null!;

    public bool IsDeleted { get; set; }
}
