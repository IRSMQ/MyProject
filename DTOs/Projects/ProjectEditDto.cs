using Test26.Models;

namespace Test26.DTOs;

public class ProjectEditDto
{
    public Guid ProjectDtoId { get; set; }
    public Guid? ManagerId { get; set; }
    public Guid? PriorityId { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
}