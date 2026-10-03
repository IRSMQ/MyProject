using System;
using System.Collections.Generic;

namespace Test26.DTOs;

public partial class UserSignupDto
{
    public Guid RoleId { get; set; }
    public string UserFullName { get; set; } = null!;
    public string Username { get; set; } = null!;
    public string? UserEmail { get; set; }
    public string? UserPhone { get; set; }
    public string UserPassword { get; set; } = null!;
    public bool UserStatus { get; set; } = true;
}
