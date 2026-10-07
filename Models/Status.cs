using System;
using System.Collections.Generic;

namespace Test26.Models;

public partial class Status : ISoftDeletable
{
    public Guid StatusId { get; set; }

    public string StatusName { get; set; } = null!;

    public bool IsDeleted { get; set; }

    public virtual ICollection<Project> Projects { get; set; } = new List<Project>();

    public virtual ICollection<TaskManagement> TaskManagements { get; set; } = new List<TaskManagement>();
}
