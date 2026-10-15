namespace Test26.DTOs;

public class UserPermissionDto
{
    public Guid? UserPermissionId { get; set; }
    public Guid? UserId { get; set; }
    public Guid? PermissionId { get; set; }
    public string? UserName { get; set; }
    public string? PermissionName { get; set; }
}