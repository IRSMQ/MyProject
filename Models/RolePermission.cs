using System;
using System.Collections.Generic;

namespace Test26.Models;

public partial class RolePermission
{
    public Guid Rpid { get; set; }

    public Guid RoleId { get; set; }

    public Guid PermissionId { get; set; }

    public bool IsDeleted { get; set; }
}
