namespace Test26.DTOs;

public class ChangeLogDto
{
    public Guid changeLogId { get; set; }
    public Guid logId { get; set; }
    public string? columnName { get; set;}
    public string? oldValue { get; set; }
    public string? newValue { get; set; }
}