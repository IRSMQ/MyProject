using System;
using System.Collections.Generic;

namespace Test26.Models;

public partial class TaskUser : ISoftDeletable
{
    public Guid TaskId { get; set; }

    public Guid UserId { get; set; }

    public bool SoftDelete { get; set; }

    public virtual TaskManagement Task { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
