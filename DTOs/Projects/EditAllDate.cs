namespace Test26.DTOs;

public class EditAllDate
{
    public Guid ID { get; set; }
    public bool NullDate { get; set; } = false;
    public DateTime? CreationDate { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? EndDate { get; set; }
}