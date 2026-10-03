using System;
using System.Collections.Generic;

namespace Test26.Models;

public partial class User
{
    public Guid UserId { get; set; } = Guid.NewGuid();
    public Guid? RoleId { get; set; }
    public string UserFullName { get; set; } = null!;
    public string Username { get; set; } = null!;
    public string UserPassword { get; set; } = null!;
    public string? UserEmail { get; set; }
    public string? UserPhone { get; set; }
    public DateTime CreationDate { get; set; } = DateTime.Now;
    public bool UserStatus { get; set; } = true;
    public virtual Role Role { get; set; } = null!;
    public virtual ICollection<TaskManagement> TaskManagements { get; set; } = new List<TaskManagement>();
}
