using System;
using System.Collections.Generic;

namespace Test26.DTOs;

public partial class UserAddDto
{
    public Guid? UserId { get; set; }
    public Guid? RoleId { get; set; }
    public string? UserFullName { get; set; }
    public string? Username { get; set; }
    public string? UserPassword { get; set; }
    public string? UserEmail { get; set; }
    public string? UserPhone { get; set; }
    public DateTime? CreationDate { get; set; }
    public bool? UserStatus { get; set; }
}
