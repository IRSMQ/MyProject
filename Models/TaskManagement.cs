using System;
using System.Collections.Generic;

namespace Test26.Models;

public partial class TaskManagement
{
    public Guid TaskManagementId { get; set; } = Guid.NewGuid();

    public Guid ProjectId { get; set; }

    public Guid? UserId { get; set; }

    public Guid StatusId { get; set; }

    public Guid PriorityId { get; set; }

    public Guid? ParentId { get; set; }

    public string Title { get; set; } = null!;

    public string? Desc { get; set; }

    public DateTime CreationDate { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? DueDate { get; set; }

    public DateTime? CompletionDate { get; set; }

    public virtual TaskManagement? Parent { get; set; }

    public virtual ICollection<TaskManagement> Children { get; set; } = new List<TaskManagement>();

    public virtual Priority Priority { get; set; } = null!;

    public virtual Project Project { get; set; } = null!;

    public virtual Status Status { get; set; } = null!;

    public virtual User? User { get; set; }
}
