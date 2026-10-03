using System;
using System.Collections.Generic;

namespace Test26.Models;

public partial class Status
{
    public Guid StatusId { get; set; } = Guid.NewGuid();

    public string StatusName { get; set; } = null!;

    public virtual ICollection<Project> Projects { get; set; } = new List<Project>();

    public virtual ICollection<TaskManagement> TaskManagements { get; set; } = new List<TaskManagement>();
}
