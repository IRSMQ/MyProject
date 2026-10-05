using System;
using System.Collections.Generic;
using Test26.Models;

namespace Test26.DTOs;

public partial class TMADto
{
    public Guid ProjectId { get; set; }

    public Guid? StatusId { get; set; }

    public Guid? PriorityId { get; set; }

    public Guid? ParentId { get; set; }

    public string Title { get; set; } = null!;

    public string? Desc { get; set; }

    public DateTime? CreationDate { get; set; } = DateTime.Now;

    public DateTime? StartDate { get; set; } = DateTime.Now;

    public DateTime? DueDate { get; set; }
}
