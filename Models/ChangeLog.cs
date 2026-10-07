using System;
using System.Collections.Generic;

namespace Test26.Models;

public partial class ChangeLog : ISoftDeletable
{
    public Guid ChangeLogId { get; set; }
    public Guid LogId { get; set; }

    public string ColumnName { get; set; } = null!;
    public string? OldValue { get; set; }

    public string NewValue { get; set; } = null!;

    public bool IsDeleted { get; set; } = false;

    public virtual Log Log { get; set; } = null!;
}
