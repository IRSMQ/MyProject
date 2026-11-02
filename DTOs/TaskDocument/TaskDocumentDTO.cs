namespace Test26.DTOs;

public class TaskDocumentDto
{
    public Guid? TaskDocumentId { get; set; }
    public Guid? TaskId { get; set; }
    public Guid? DocumentId { get; set; }
    public string? TaskTitle { get; set; }
    public string? DocumentName { get; set; }
}