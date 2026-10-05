using System;
using System.Collections.Generic;
using Test26.Models;

namespace Test26.DTOs;

public partial class TaskManagementDto
{
    public Guid TaskManagementId { get; set; }
    public Guid ProjectId { get; set; }
    public Guid? StatusId { get; set; }
    public Guid? PriorityId { get; set; }
    public Guid? ParentId { get; set; }

    public string Title { get; set; } = null!;
    public string ProjectName { get; set; } = null!;
    public string? UserFullName { get; set; }
    public string? StatusName { get; set; }
    public string? PriorityName { get; set; }
    public string? ParentName { get; set; }

    public string? Desc { get; set; }
    public DateTime? CreationDate { get; set; } = DateTime.Now;
    public DateTime? StartDate { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? CompletionDate { get; set; }
}
