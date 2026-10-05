using System;
using System.Collections.Generic;

namespace Test26.Models;

public partial class Priority : ISoftDeletable
{
    public Guid PriorityId { get; set; }

    public string PriorityName { get; set; } = null!;

    public bool SoftDelete { get; set; }

    public virtual ICollection<Project> Projects { get; set; } = new List<Project>();

    public virtual ICollection<TaskManagement> TaskManagements { get; set; } = new List<TaskManagement>();
}
