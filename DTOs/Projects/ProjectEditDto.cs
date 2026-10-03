using Test26.Models;

namespace Test26.DTOs;

public class ProjectEditDto
{
    public Guid ProjectDtoId { get; set; }
    public Guid? StatusId { get; set; }
    public Guid? ManagerId { get; set; }
    public Guid? PriorityId { get; set; }
    public string? Title { get; set; } = null!;
    public string? Description { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime? CreationDate { get; set; } = DateTime.Now;
}