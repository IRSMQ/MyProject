namespace Test26.DTOs;

public class ProjectDto
{
    public Guid ProjectId { get; set; }
    public Guid? StatusId { get; set; }
    public Guid? ManagerId { get; set; }
    public Guid? PriorityId { get; set; }
    public string ProjectName { get; set; } = null!;
    public string? Description { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime? CreationDate { get; set; } = DateTime.Now;
    public string? StatusName { get; set; } = null!;
    public string? ManagerName { get; set; } = null!;
    public string? PriorityName { get; set; } = null!;
}