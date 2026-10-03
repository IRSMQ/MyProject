using System;
using System.Collections.Generic;

namespace Test26.Models;

public partial class Project
{
    public Guid ProjectId { get; set; } = Guid.NewGuid();

    public Guid? StatusId { get; set; }

    public Guid? ManagerId { get; set; }

    public Guid? PriorityId { get; set; }

    public string ProjectName { get; set; } = null!;

    public string? Desc { get; set; }

    public DateTime CreationDate { get; set; } = DateTime.Now;

    public DateTime? StartDate { get; set; }

    public DateTime? DueDate { get; set; }

    public DateTime? EndDate { get; set; }

    

    public virtual Status Status { get; set; } = null!;
    public virtual User Manager { get; set; } = null!;
    public virtual Priority Priority { get; set; } = null!;
    public virtual ICollection<TaskManagement> TaskManagements { get; set; } = new List<TaskManagement>();
}
