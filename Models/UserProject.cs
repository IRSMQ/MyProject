using System;
using System.Collections.Generic;

namespace Test26.Models;

public partial class UserProject : ISoftDeletable
{
    public Guid ProjectId { get; set; }

    public Guid UserId { get; set; }

    public bool IsDeleted { get; set; }

    public virtual Project Project { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
