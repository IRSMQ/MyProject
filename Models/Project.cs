using System;
using System.Collections.Generic;

namespace Test26.Models;

public partial class Project : ISoftDeletable
{
    public Guid ProjectId { get; set; }

    public Guid? StatusId { get; set; }

    public Guid? ManagerId { get; set; }

    public Guid? PriorityId { get; set; }

    public string ProjectName { get; set; } = null!;

    public string? Desc { get; set; }

    public DateTime CreationDate { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? DueDate { get; set; }

    public DateTime? EndDate { get; set; }

    public bool SoftDelete { get; set; }

    public virtual User? Manager { get; set; }

    public virtual Priority? Priority { get; set; }

    public virtual Status? Status { get; set; }

    public virtual ICollection<TaskManagement> TaskManagements { get; set; } = new List<TaskManagement>();
}
