namespace Test26.DTOs;

public class TaskUserDto
{
    public Guid TaskUserId { get; set; }
    public Guid TaskId { get; set; }
    public Guid UserId { get; set; }
    public string? TaskName { get; set; }
    public string? UserName { get; set; }
}