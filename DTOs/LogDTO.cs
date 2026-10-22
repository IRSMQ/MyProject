namespace Test26.DTOs;

public class LogDto
{
    public Guid logId { get; set; }
    public Guid relatedId { get; set; }
    public Guid? userId { get; set; }
    public string logType { get; set; } = null!;
    public string? relatedTableName { get; set; }
    public DateTime Date { get; set; }
}