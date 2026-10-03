namespace Test26.DTOs;

public class CreateProject
{
    public Guid? StatusId { get; set; }
    public Guid? ManagerId { get; set; }
    public Guid? PriorityId { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? EndDate { get; set; }
}