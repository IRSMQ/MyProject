using System;
using System.Collections.Generic;

namespace Test26.Models;

public partial class Log
{
    public Guid LogId { get; set; }

    public string LogType { get; set; } = null!;

    public string RelatedTableName { get; set; } = null!;

    public Guid RelatedId { get; set; }

    public Guid? UserId { get; set; }

    public DateTime Date { get; set; }

    public bool IsDeleted { get; set; }

    public virtual ICollection<ChangeLog> ChangeLogs { get; set; } = new List<ChangeLog>();

    public virtual User? User { get; set; }
}
