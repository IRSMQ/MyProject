using System;
using System.Collections.Generic;

namespace Test26.Models;

public partial class Role : ISoftDeletable
{
    public Guid RoleId { get; set; }

    public string RoleName { get; set; } = null!;

    public bool SoftDelete { get; set; }

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
