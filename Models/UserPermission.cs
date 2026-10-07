using System;
using System.Collections.Generic;

namespace Test26.Models;

public partial class UserPermission : ISoftDeletable
{
    public Guid UserId { get; set; }

    public Guid PermissionId { get; set; }

    public byte Int { get; set; }

    public bool IsDeleted { get; set; }

    public virtual Permission Permission { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
