using System;
using System.Collections.Generic;

namespace Test26.Models;

public partial class User : ISoftDeletable
{
    public Guid UserId { get; set; }

    public Guid? RoleId { get; set; }

    public string UserFullName { get; set; } = null!;

    public string Username { get; set; } = null!;

    public string UserPassword { get; set; } = null!;

    public string? UserEmail { get; set; }

    public string? UserPhone { get; set; }

    public DateTime CreationDate { get; set; }

    public bool UserStatus { get; set; }

    public bool IsDeleted { get; set; }

    public virtual ICollection<Log> Logs { get; set; } = new List<Log>();

    public virtual ICollection<Project> Projects { get; set; } = new List<Project>();

    public virtual Role? Role { get; set; }
}
