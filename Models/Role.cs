using System;
using System.Collections.Generic;

namespace Test26.Models;

public partial class Role
{
    public Guid RoleId { get; set; }

    public string RoleName { get; set; } = null!;

    public bool IsDeleted { get; set; }

    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();

    public virtual ICollection<Users> Users { get; set; } = new List<Users>();
}
