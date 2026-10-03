using System;
using System.Collections.Generic;

namespace Test26.Models;

public partial class Permission
{
    public Guid PermissionId { get; set; } = Guid.NewGuid();

    public string PermissionName { get; set; } = null!;
}
