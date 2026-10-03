using System;
using System.Collections.Generic;

namespace Test26.Models;

public partial class Role
{
    public Guid RoleId { get; set; } = Guid.NewGuid();

    public string RoleName { get; set; } = null!;

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
