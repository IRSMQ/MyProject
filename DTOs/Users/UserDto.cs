using Test26.Models;

namespace Test26.DTOs;




public class UserDto
{
    public Guid UserId { get; set; }
    public Guid? RoleId { get; set; }
    public string UserFullName { get; set; } = null!;
    public string Username { get; set; } = null!;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public bool Status { get; set; } = true;
    public string? RoleName { get; set; }
}